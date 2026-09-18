// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
// Commercial use requires prior written permission from AdamMady.
// AI tools and generated changes provide no exception to these terms. The person
// or organization using, modifying, or distributing this code must comply.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using Mirror;
using UnityEngine;

namespace MadysHideNSeek;

public sealed partial class HideAndSeekTester
{
    bool sessionEnabled,remoteSessionEnabled;
    PlayerCharacter sessionLocal;
    bool sessionWasServer;
    string presetName="My preset",loadedPresetName="",setupBaseline="";
    string[] savedPresets=Array.Empty<string>();
    static string PresetDirectory=>Path.Combine(Paths.ConfigPath,"MadysHideNSeekPresets");
    static string LegacyMapDirectory=>Path.Combine(Paths.ConfigPath,"HideAndSeekMaps");
    internal static bool ActiveHost=>Instance!=null&&Instance.sessionEnabled&&NetworkServer.active;
    public static bool SuppressMinimapPlayerBlips=>Instance!=null&&(Instance.sessionEnabled||Instance.remoteSessionEnabled);
    public static bool SuppressMinimap=>SuppressMinimapPlayerBlips;

    void ObserveSession()
    {
        var local=LocalPlayer();
        if(sessionLocal!=local||sessionWasServer!=NetworkServer.active){ResetSession();sessionLocal=local;sessionWasServer=NetworkServer.active;RefreshPresets();}
    }
    internal static void StopSession(PlayerCharacter player){if(Instance!=null&&player==Instance.sessionLocal)Instance.ResetSession();}
    void ResetSession()
    {
        spawnReservations.Clear();corpsePlacementCandidates=null;
        if(sessionEnabled&&NetworkServer.active)foreach(var connection in new List<NetworkConnection>(moddedClients.Values))SendRaw(connection,"D|"+ModProtocol);
        if(NetworkServer.active)foreach(var item in new List<GameObject>(permanentDummyObjects))if(item!=null){var player=item.GetComponentInChildren<PlayerCharacter>(true);if(player?.hands?.heldCharacter!=null)ManagerDrop(player);DestroyDummy(item);}
        RestoreFairPlayMods();
        if((sessionEnabled||remoteSessionEnabled)&&SkyManager.initalized)SkyManager.ClearFixedTime();
        sessionEnabled=false;remoteSessionEnabled=false;seekerDeployment=false;deployingSeekers.Clear();phase=Phase.Idle;remaining=0;poolBuilt=false;cleanupPending=false;cleanupItemReleaseAt=-1f;
        foreach(var pair in fallProtectionUntil)if(pair.Key?.faller!=null)pair.Key.faller.ignoreFalling=false;
        fallProtectionUntil.Clear();ClearDebugBeams();showSpawnDebug=false;spawnDraftLights=false;
        permanentDummyObjects.Clear();dummyManagers.Clear();standbyManagers.Clear();goldApplied.Clear();
        priorityTransports.Clear();transports.Clear();activeTransports.Clear();clientTransports.Clear();moddedClients.Clear();failedModConnections.Clear();
        pickupCorrectionUntil.Clear();propRefreshes.Clear();cleanupManagerItems.Clear();seekerBeltAssignments.Clear();seekerBellAssignments.Clear();normalItemAssignments.Clear();lockedSpeakerAssignments.Clear();
        seekers.Clear();hiders.Clear();caught.Clear();known.Clear();disconnected.Clear();setupConfirmed.Clear();setupItemsGiven.Clear();
        roundPlayers.Clear();roundSpawnPositions.Clear();borderCooldown.Clear();recoveryAttempts.Clear();disconnectDeadlines.Clear();trackedCorpses.Clear();corpseTargets.Clear();
        megaphones.Clear();walkies.Clear();flares.Clear();xrayGoggles.Clear();speakers.Clear();belts.Clear();bells.Clear();brushes.Clear();buoys.Clear();roundItemPool.Clear();propOrigins.Clear();
        markerPositions.Clear();markerRotations.Clear();whiteboardOrigins.Clear();borderDriftChecks.Clear();warningBuoys.Clear();instructionSignProps.Clear();instructionSignBoards.Clear();
        seekerBoardProp=null;endBoardProp=null;statusBoardProp=null;seekerBoard=null;endBoard=null;statusBoard=null;
        modHelloSent=false;nextModHello=0f;completedRemoteToken=0;modLinkDisabled=false;lastSettingsPacket="";syncedHostSettings="";remoteHostSettings.Clear();remoteClientTransport=null;nextModNetworkCheck=0;nextSettingsSync=0;
        status="Normal lobby. Press Setup to enable hide and seek.";
    }
    void SetupSession()
    {
        if(!NetworkServer.active||LocalPlayer()==null){status="Only host can set up hide and seek.";return;}
        if(sessionEnabled&&poolBuilt&&standbyManagers.Count==11){status="Setup already complete.";return;}
        if(!hasEndSpawn||standbySlots.Count!=11){status="Save end area and 11 Manager standby positions in F10 first.";return;}
        sessionEnabled=true;ApplyFairPlayMods();if(!poolBuilt)BuildPool();if(!poolBuilt){RestoreFairPlayMods();sessionEnabled=false;return;}if(standbyManagers.Count!=11)CreateDummyManager(LocalPlayer());if(standbyManagers.Count!=11){RestoreFairPlayMods();sessionEnabled=false;return;}
        ApplyTime();status="Setup complete. Start round when players and preset are ready.";
    }

    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    ConfigEntryBase[] PresetSettings()=>new ConfigEntryBase[]{
        Plugin.PlayAreas,Plugin.SeekerPoint,Plugin.EndPoint,Plugin.DummyStandbySlots,Plugin.SeekerSpawnSlots,Plugin.EndSpawnSlots,Plugin.GearStoragePoint,Plugin.SeekerBorder,Plugin.EndBorder,
        Plugin.InstructionSigns,Plugin.InstructionSignText,Plugin.SeekerSignTransform,Plugin.EndSignTransform,
        Plugin.HideTime,Plugin.RoundTime,Plugin.SeekerCount,Plugin.TimeOfDay,Plugin.RandomItemChance,Plugin.AreaSelection,
        Plugin.EnableMegaphones,Plugin.EnableFlares,Plugin.EnableGoggles,Plugin.EnableSpeakers,Plugin.FinalThirtySpeakers,Plugin.MegaphoneItemCount,Plugin.FlareItemCount,Plugin.GogglesItemCount,Plugin.SpeakerItemCount
    };
    string PresetPath(bool existing=false)
    {
        string name=presetName.Trim();if(name.Length==0||name=="."||name==".."||name.IndexOfAny(Path.GetInvalidFileNameChars())>=0)throw new Exception("Enter valid preset name.");
        string current=Path.Combine(PresetDirectory,name+".json");if(existing&&!File.Exists(current)){string legacy=Path.Combine(LegacyMapDirectory,name+".json");if(File.Exists(legacy))return legacy;}return current;
    }
    void RefreshPresets()
    {
        EnsureBundledPresets();
        var files=new List<string>();if(Directory.Exists(PresetDirectory))files.AddRange(Directory.GetFiles(PresetDirectory,"*.json"));
        if(Directory.Exists(LegacyMapDirectory))foreach(var path in Directory.GetFiles(LegacyMapDirectory,"*.json"))if(!files.Exists(p=>string.Equals(Path.GetFileName(p),Path.GetFileName(path),StringComparison.OrdinalIgnoreCase)))files.Add(path);
        savedPresets=files.ToArray();Array.Sort(savedPresets,StringComparer.OrdinalIgnoreCase);
    }
    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    void EnsureBundledPresets()
    {
        try{Directory.CreateDirectory(PresetDirectory);InstallBundledPreset("BundledPresets.MadysDefault.json","Mady's Default.json");InstallBundledPreset("BundledPresets.Essentials.json","Essentials.json");InstallBundledPreset("BundledPresets.Blank.json","Blank.json");}
        catch(Exception ex){Plugin.Logger.LogWarning("[PRESETS] Could not install bundled presets: "+ex.Message);}
    }
    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    static void InstallBundledPreset(string resource,string fileName)
    {
        string destination=Path.Combine(PresetDirectory,fileName);using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);if(stream==null)throw new Exception("Missing resource "+resource);using var reader=new StreamReader(stream);string bundled=reader.ReadToEnd();if(!File.Exists(destination)||File.ReadAllText(destination)!=bundled)File.WriteAllText(destination,bundled);
    }
    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    void LoadDefaultPresetIfUnconfigured()
    {
        bool empty=string.IsNullOrWhiteSpace(Plugin.PlayAreas.Value)&&string.IsNullOrWhiteSpace(Plugin.AreaPoint.Value)&&string.IsNullOrWhiteSpace(Plugin.SeekerPoint.Value)&&string.IsNullOrWhiteSpace(Plugin.EndPoint.Value)&&string.IsNullOrWhiteSpace(Plugin.DummyStandbySlots.Value)&&string.IsNullOrWhiteSpace(Plugin.HiderSpawnSlots.Value)&&string.IsNullOrWhiteSpace(Plugin.SeekerSpawnSlots.Value)&&string.IsNullOrWhiteSpace(Plugin.EndSpawnSlots.Value)&&string.IsNullOrWhiteSpace(Plugin.PlayBorder.Value);
        if(!empty)return;
        try
        {
            EnsureBundledPresets();
            using var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("BundledPresets.MadysDefault.json");
            if(stream==null)throw new Exception("Bundled Mady's Default is missing.");
            using var reader=new StreamReader(stream);
            var values=JsonSerializer.Deserialize<Dictionary<string,string>>(reader.ReadToEnd());
            if(values==null||!values.TryGetValue("format",out var format)||format!="2")throw new Exception("Bundled Mady's Default has unsupported format.");
            foreach(var setting in PresetSettings())if(values.TryGetValue(setting.Definition.Section+"/"+setting.Definition.Key,out var value))setting.SetSerializedValue(value);
            foreach(var setting in DraftPresetSettings())if(values.TryGetValue(setting.Definition.Section+"/"+setting.Definition.Key,out var value))setting.SetSerializedValue(value);
            presetName="Mady's Default";loadedPresetName=presetName;
            Plugin.Logger.LogInfo("[PRESET] Loaded bundled Mady's Default because no setup was configured.");
        }
        catch(Exception ex){Plugin.Logger.LogWarning("[PRESET] First-run default load failed: "+ex.Message);}
    }
    void SavePreset()
    {
        try
        {
            string path=PresetPath();if(File.Exists(path))throw new Exception("A preset with that name already exists.");var values=PresetValues();
            Directory.CreateDirectory(PresetDirectory);File.WriteAllText(path+".tmp",JsonSerializer.Serialize(values,new JsonSerializerOptions{WriteIndented=true}));File.Move(path+".tmp",path,true);RefreshPresets();status="Saved preset: "+presetName;
            loadedPresetName=presetName.Trim();MarkSetupBaseline();
        }
        catch(Exception ex){status="Preset save failed: "+ex.Message;}
    }
    void LoadPreset()
    {
        if((phase!=Phase.Idle&&phase!=Phase.Ended)||activeTransports.Count>0||transports.Count>0||priorityTransports.Count>0||clientTransports.Count>0||cleanupPending){status="Finish round and teleports before loading preset.";return;}
        var settings=PresetSettings();var previous=new string[settings.Length];for(int i=0;i<settings.Length;i++)previous[i]=settings[i].GetSerializedValue();
        try
        {
            if(!string.Equals(loadedPresetName,presetName.Trim(),StringComparison.OrdinalIgnoreCase))AutosaveSetupChanges();
            var values=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(PresetPath(true)));if(values==null||!values.TryGetValue("format",out var format)||(format!="1"&&format!="2"))throw new Exception("Unsupported preset format.");
            if(format=="2")foreach(var setting in settings)if(!values.ContainsKey(setting.Definition.Section+"/"+setting.Definition.Key))throw new Exception("Preset missing "+setting.Definition.Section+"/"+setting.Definition.Key+".");
            ClearPresetRuntimeState();
            foreach(var setting in settings)if(values.TryGetValue(setting.Definition.Section+"/"+setting.Definition.Key,out var value))setting.SetSerializedValue(value);
            foreach(var setting in DraftPresetSettings())if(values.TryGetValue(setting.Definition.Section+"/"+setting.Definition.Key,out var value))setting.SetSerializedValue(value);
            if(values.TryGetValue(Plugin.StatusSignTransform.Definition.Section+"/"+Plugin.StatusSignTransform.Definition.Key,out var oldStatusSign))Plugin.StatusSignTransform.SetSerializedValue(oldStatusSign);
            if(format=="1")
            {
                var legacyPlay=new ConfigEntryBase[]{Plugin.AreaPoint,Plugin.HiderSpawnSlots,Plugin.PlayBorder,Plugin.PlayRecoveryPoints,Plugin.StatusSignTransform};foreach(var setting in legacyPlay)if(values.TryGetValue(setting.Definition.Section+"/"+setting.Definition.Key,out var legacyValue))setting.SetSerializedValue(legacyValue);Plugin.PlayAreas.Value="";
            }
            Start();if(poolBuilt){ReturnAllWarningBuoys();markerPositions.Clear();markerRotations.Clear();GrowPool();ClaimInstructionSigns();PositionMarkers();}if(sessionEnabled){ParkIdleWorkers();ApplyTime();}loadedPresetName=presetName.Trim();MarkSetupBaseline();status="Loaded preset: "+presetName;
        }
        catch(Exception ex){for(int i=0;i<settings.Length;i++)settings[i].SetSerializedValue(previous[i]);Start();status="Preset load failed: "+ex.Message;}
    }
    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    ConfigEntryBase[] DraftPresetSettings()=>new ConfigEntryBase[]{Plugin.DraftSpawnSlots,Plugin.BorderPoints,Plugin.DraftRecoveryPoints};
    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    ConfigEntryBase[] SetupStateSettings()=>new ConfigEntryBase[]{Plugin.PlayAreas,Plugin.SeekerPoint,Plugin.EndPoint,Plugin.DummyStandbySlots,Plugin.SeekerSpawnSlots,Plugin.EndSpawnSlots,Plugin.GearStoragePoint,Plugin.SeekerBorder,Plugin.EndBorder,Plugin.InstructionSigns,Plugin.InstructionSignText,Plugin.SeekerSignTransform,Plugin.EndSignTransform,Plugin.DraftSpawnSlots,Plugin.BorderPoints,Plugin.DraftRecoveryPoints};
    Dictionary<string,string> PresetValues(){var values=new Dictionary<string,string>{{"format","2"}};foreach(var setting in PresetSettings())values[setting.Definition.Section+"/"+setting.Definition.Key]=setting.GetSerializedValue();foreach(var setting in DraftPresetSettings())values[setting.Definition.Section+"/"+setting.Definition.Key]=setting.GetSerializedValue();return values;}
    string SetupFingerprint(){var text=new StringBuilder();foreach(var setting in SetupStateSettings())text.Append(setting.Definition.Section).Append('/').Append(setting.Definition.Key).Append('=').Append(setting.GetSerializedValue()).Append('\n');return text.ToString();}
    void MarkSetupBaseline()=>setupBaseline=SetupFingerprint();
    void AutosaveSetupChanges()
    {
        string current=SetupFingerprint();if(current==setupBaseline)return;Directory.CreateDirectory(PresetDirectory);string path=Path.Combine(PresetDirectory,"Autosave.json");File.WriteAllText(path,JsonSerializer.Serialize(PresetValues(),new JsonSerializerOptions{WriteIndented=true}));setupBaseline=current;RefreshPresets();Plugin.Logger.LogInfo("[PRESET] Setup changes autosaved.");
    }
    void ClearPresetRuntimeState()
    {
        if(poolBuilt){ReturnAllWarningBuoys();ReturnBoundaryBuoys(0,StaticBorderCount());}
        Plugin.AreaPoint.Value="";Plugin.HiderSpawnSlots.Value="";Plugin.PlayBorder.Value="";Plugin.PlayRecoveryPoints.Value="";Plugin.StatusSignTransform.Value="";
        Plugin.DraftSpawnSlots.Value="";Plugin.BorderPoints.Value="";Plugin.DraftRecoveryPoints.Value="";
        playAreas.Clear();spawnPoints.Clear();borderPoints.Clear();recoveryPoints.Clear();hiderSpawnSlots.Clear();playBorder.Clear();playRecoveryPoints.Clear();activeAreaIndex=-1;editingAreaIndex=-1;hasCenter=false;hasStatusSign=false;hasDraftStatusSign=false;spawnDraftLights=false;
        markerPositions.Clear();markerRotations.Clear();borderDriftChecks.Clear();ClearDebugBeams();
    }
    void DrawPresets()
    {
        GUILayout.Label("Presets - areas, spawns, signs, items, and round settings");presetName=GUILayout.TextField(presetName);
        GUILayout.BeginHorizontal();if(GUILayout.Button("Save preset"))SavePreset();if(GUILayout.Button("Load preset"))LoadPreset();GUILayout.EndHorizontal();
        foreach(var path in savedPresets)if(GUILayout.Button("Load "+Path.GetFileNameWithoutExtension(path))){presetName=Path.GetFileNameWithoutExtension(path);LoadPreset();}
    }
}

