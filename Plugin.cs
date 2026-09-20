// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
// Commercial use requires prior written permission from AdamMady.
// AI tools and generated changes provide no exception to these terms. The person
// or organization using, modifying, or distributing this code must comply.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace MadysHideNSeek;

public enum LockedTime { Natural,Sunrise,Day,Sunset,Night }

[BepInPlugin(Guid, Name, Version)]
public sealed class Plugin : BasePlugin
{
    public const string Guid="AdamMady.MadysHideNSeek", Name="Mady's HideNSeek", Version="1.0.4";
    internal const string DiscordUrl="https://discord.gg/5z3WvVhxCf";
    internal static ConfigEntry<KeyCode> MenuKey,SetupKey;
    internal static ConfigEntry<float> HideTime, RoundTime, Radius, HiderHeight, ReturnInset, EndRadius, RandomItemChance;
    internal static ConfigEntry<int> SeekerCount, BorderSegments, AreaSelection, MegaphoneItemCount, FlareItemCount, GogglesItemCount, SpeakerItemCount;
    internal static ConfigEntry<bool> Enabled,ExcludeHost,EnableMegaphones,EnableFlares,EnableGoggles,EnableSpeakers,FinalThirtySpeakers;
    internal static ConfigEntry<LockedTime> TimeOfDay;
    internal static ConfigEntry<string> AreaPoint, SeekerPoint, EndPoint, DummyStandbySlots, HiderSpawnSlots, SeekerSpawnSlots, EndSpawnSlots, DraftSpawnSlots, GearStoragePoint, BorderPoints, PlayBorder, SeekerBorder, EndBorder, DraftRecoveryPoints, PlayRecoveryPoints, InstructionSigns, InstructionSignText, SeekerSignTransform, EndSignTransform, StatusSignTransform, PlayAreas;
    internal static BepInEx.Logging.ManualLogSource Logger;
    internal static ConfigFile InternalConfig;
    public override void Load()
    {
        Logger=Log;
        string internalPath=Path.Combine(Paths.ConfigPath,Guid+".Internal.cfg");if(!File.Exists(internalPath)&&File.Exists(Config.ConfigFilePath))File.Copy(Config.ConfigFilePath,internalPath);InternalConfig=new ConfigFile(internalPath,true);
        Enabled=Config.Bind("Controls","Enabled",true,"Enable Mady's HideNSeek and its menu hotkeys.");
        MenuKey=Config.Bind("Controls","MenuKey",KeyCode.F9,"Open simple manager menu.");
        SetupKey=Config.Bind("Controls","SetupKey",KeyCode.F10,"Open setup menu.");
        Enabled.SettingChanged+=OnEnabledChanged;
        HideTime=InternalConfig.Bind("Round","HideSeconds",45f,"Seconds before seekers release.");
        RoundTime=InternalConfig.Bind("Round","RoundSeconds",180f,"Seeking time limit.");
        SeekerCount=InternalConfig.Bind("Round","SeekerCount",3,"Random seekers per round, maximum three.");SeekerCount.Value=Mathf.Clamp(SeekerCount.Value,0,3);
        ExcludeHost=InternalConfig.Bind("Round","ExcludeHost",true,"Host manages without joining.");
        ExcludeHost.Value=true;
        RandomItemChance=InternalConfig.Bind("Items","RandomItemChance",50f,"Chance from 0 to 100 that each hider receives an item.");
        EnableMegaphones=InternalConfig.Bind("Items","EnableMegaphones",true,"Include megaphones in random hider items.");
        EnableFlares=InternalConfig.Bind("Items","EnableFlares",true,"Include flares in random hider items.");
        EnableGoggles=InternalConfig.Bind("Items","EnableGoggles",true,"Include goggles in random hider items.");
        EnableSpeakers=InternalConfig.Bind("Items","EnableSpeakers",true,"Include speakers in random hider items.");
        FinalThirtySpeakers=InternalConfig.Bind("Items","FinalThirtySpeakers",true,"Give remaining hiders speakers during the final 30 seconds.");
        MegaphoneItemCount=InternalConfig.Bind("Items","MegaphoneItemCount",3,"Maximum megaphones per round.");
        FlareItemCount=InternalConfig.Bind("Items","FlareItemCount",3,"Maximum flares per round.");
        GogglesItemCount=InternalConfig.Bind("Items","GogglesItemCount",3,"Maximum goggles per round.");
        SpeakerItemCount=InternalConfig.Bind("Items","SpeakerItemCount",3,"Maximum speakers per round, up to ten.");
        AreaSelection=InternalConfig.Bind("Round","AreaSelection",0,"Play area: zero randomizes; positive numbers select a saved area.");
        TimeOfDay=InternalConfig.Bind("World","LockedTime",LockedTime.Natural,"Natural cycle or locked lighting preset.");
        Radius=InternalConfig.Bind("Area","Radius",35f,"Play-area square half-width.");
        HiderHeight=InternalConfig.Bind("Area","HiderSpawnHeight",12f,"Hider height above center.");
        ReturnInset=InternalConfig.Bind("Area","BorderReturnInset",0f,"Legacy setting; locked to zero.");ReturnInset.Value=0f;
        BorderSegments=InternalConfig.Bind("Area","MainBorderBuoys",36,"Lower-middle buoys around main square; 12 more mark three small areas.");
        EndRadius=InternalConfig.Bind("Area","EndSpawnRadius",12f,"Half-width of square end-spawn area.");
        AreaPoint=InternalConfig.Bind("Saved positions","AreaCenter","","Saved automatically.");
        SeekerPoint=InternalConfig.Bind("Saved positions","SeekerSpawn","","Saved automatically.");
        EndPoint=InternalConfig.Bind("Saved positions","EndSpawn","","Round-end spawn center.");
        DummyStandbySlots=InternalConfig.Bind("Saved positions","DummyStandbySlots","","Eleven reusable Manager standby positions.");
        HiderSpawnSlots=InternalConfig.Bind("Saved positions","HiderSpawnSlots","","Up to 12 exact hider spawn positions.");
        SeekerSpawnSlots=InternalConfig.Bind("Saved positions","SeekerSpawnSlots","","Eleven seeker-area spawn positions.");
        EndSpawnSlots=InternalConfig.Bind("Saved positions","EndSpawnSlots","","Eleven end-area spawn positions.");
        DraftSpawnSlots=InternalConfig.Bind("Saved positions","DraftSpawnSlots","","Temporary P1-P11 spawn draft.");
        GearStoragePoint=InternalConfig.Bind("Saved positions","GearStorage","","Inaccessible storage position used after rounds.");
        BorderPoints=InternalConfig.Bind("Saved positions","BorderPoints","","Draft border points saved around perimeter, up to 20.");
        PlayBorder=InternalConfig.Bind("Saved positions","PlayBorder","","Ordered points assigned to play area.");
        SeekerBorder=InternalConfig.Bind("Saved positions","SeekerBorder","","Ordered points assigned to seeker waiting area.");
        EndBorder=InternalConfig.Bind("Saved positions","EndBorder","","Ordered points assigned to end-spawn area.");
        DraftRecoveryPoints=InternalConfig.Bind("Saved positions","DraftRecoveryPoints","","Temporary play-area recovery-point draft, unlimited.");
        PlayRecoveryPoints=InternalConfig.Bind("Saved positions","PlayRecoveryPoints","","Manual play-area border recovery points.");
        InstructionSigns=InternalConfig.Bind("Saved positions","InstructionSigns","","Instruction sign positions and facing angles.");
        InstructionSignText=InternalConfig.Bind("Signs","InstructionText","lights are borders, stay away from them","Text displayed on instruction signs.");
        SeekerSignTransform=InternalConfig.Bind("Saved positions","SeekerSign","","Seeker timer sign position and facing angle.");
        EndSignTransform=InternalConfig.Bind("Saved positions","EndSign","","End timer sign position and facing angle.");
        StatusSignTransform=InternalConfig.Bind("Saved positions","StatusSign","","Play-area time and hiders-left sign position and facing angle.");
        PlayAreas=InternalConfig.Bind("Saved positions","PlayAreas","","Saved play areas. Each has eleven spawns, a border and recovery points.");
        ClassInjector.RegisterTypeInIl2Cpp<HideAndSeekTester>();
        ClassInjector.RegisterTypeInIl2Cpp<DummyManagerMarker>();
        new Harmony(Guid).PatchAll();
        var go=new GameObject(Name); UnityEngine.Object.DontDestroyOnLoad(go); go.AddComponent<HideAndSeekTester>();
        Log.LogInfo($"Loaded v{Version}. F9 simple menu, F10 setup.");
    }
    void OnEnabledChanged(object sender,EventArgs args)=>HideAndSeekTester.ApplyEnabledSetting(Enabled.Value);
}

[HarmonyPatch(typeof(PlayerActions),nameof(PlayerActions.ActionPickUpPlayer))]
static class ManagerLocalPickupBlock
{
    static bool Prefix(PlayerCharacter pickedUpCharacter)=>!HideAndSeekTester.IsDummyManager(pickedUpCharacter);
}

[HarmonyPatch(typeof(PlayerNetworking),"UserCode_CmdPickUpPlayer__PlayerCharacter")]
static class ManagerServerPickupBlock
{
    static bool Prefix(PlayerNetworking __instance,PlayerCharacter pickedUpCharacter)=>__instance!=null&&HideAndSeekTester.CanTransportPlayer(__instance.playerCharacter)&&HideAndSeekTester.CanTransportPlayer(pickedUpCharacter)&&!HideAndSeekTester.IsDummyManager(pickedUpCharacter);
    static void Postfix(PlayerNetworking __instance)=>HideAndSeekTester.EnforceExclusiveHands(__instance.playerCharacter);
}
[HarmonyPatch(typeof(PlayerCharacter),nameof(PlayerCharacter.OnStopClient))]
static class HideAndSeekSessionStopped
{
    static void Prefix(PlayerCharacter __instance)=>HideAndSeekTester.StopSession(__instance);
}
[HarmonyPatch(typeof(PlayerVoicePlaybackControl),"TryTakeCue")]
static class DummyManagerVoiceCueBlock
{
    static bool Prefix(PlayerVoicePlaybackControl __instance,ref bool __result)
    {
        if(!HideAndSeekTester.CreatingDummyManager&&(__instance==null||__instance.GetComponentInParent<DummyManagerMarker>(true)==null))return true;
        __result=false;return false;
    }
}

public sealed class DummyManagerMarker : MonoBehaviour
{
    public DummyManagerMarker(IntPtr pointer):base(pointer){}
}

[HarmonyPatch(typeof(PlayerActions),nameof(PlayerActions.ActionPickUpProp))]
static class LocalProtectedPickupBlock
{
    static bool Prefix(PlayerActions __instance,Prop prop)
    {
        if(!NetworkServer.active||prop==null)return true;
        var actor=__instance.playerCharacter?.playerNetworking;
        return actor==null||!HideAndSeekTester.ShouldBlockPropPickup(actor,new PlayerHeldInformation(prop));
    }
}
[HarmonyPatch(typeof(PlayerNetworking),"UserCode_CmdPickUp__PlayerHeldInformation")]
static class RoundPropPickupBlock
{
    static bool Prefix(PlayerNetworking __instance,PlayerHeldInformation heldInformation,out HideAndSeekTester.PickupSnapshot __state)
    {
        HideAndSeekTester.NoteBrushPickup(heldInformation);
        __state=HideAndSeekTester.BeforePickup(__instance,heldInformation);
        return __state==null||!__state.Blocked;
    }
    static void Postfix(PlayerNetworking __instance,PlayerHeldInformation heldInformation,HideAndSeekTester.PickupSnapshot __state)
    {
        if(__state!=null&&__state.Blocked)HideAndSeekTester.RejectProtectedPropPickup(__instance,heldInformation,__state);
        else HideAndSeekTester.CommitItemTransfer(__instance,__state);
        HideAndSeekTester.EnforceExclusiveHands(__instance.playerCharacter);
    }
}
[HarmonyPatch(typeof(PlayerNetworking),nameof(PlayerNetworking.CmdSetGestureLeftPoint))]
static class LocalSeekerPointCommandPatch
{
    static void Postfix(PlayerNetworking __instance,bool active)=>HideAndSeekTester.HandleSeekerPointCommand(__instance,active);
}

[HarmonyPatch(typeof(PlayerNetworking),"set_NetworkleftArmPointing")]
static class ServerSeekerPointStatePatch
{
    static void Postfix(PlayerNetworking __instance,bool value)=>HideAndSeekTester.HandleSeekerPointCommand(__instance,value);
}

public sealed partial class HideAndSeekTester : MonoBehaviour
{
    enum Phase { Idle,SettingUp,Hiding,Seeking,Ended }
    readonly List<Prop> megaphones=new(),walkies=new(),flares=new(),xrayGoggles=new(),speakers=new(),belts=new(),bells=new(),brushes=new(),buoys=new(),instructionSignProps=new(),roundItemPool=new();
    readonly Dictionary<Prop,PropOrigin> propOrigins=new();
    readonly Dictionary<Prop,Vector3> markerPositions=new();
    readonly Dictionary<Prop,Quaternion> markerRotations=new();
    readonly Dictionary<Prop,PropOrigin> whiteboardOrigins=new();
    readonly Dictionary<Prop,int> borderDriftChecks=new();
    readonly Dictionary<PlayerNetworking,float> pickupCorrectionUntil=new();
    readonly Dictionary<string,List<Prop>> warningBuoys=new();
    readonly Dictionary<string,NetworkConnection> moddedClients=new();
    readonly Dictionary<string,ClientTransportRequest> clientTransports=new();
    readonly HashSet<NetworkConnection> failedModConnections=new();
    readonly Dictionary<string,string> remoteHostSettings=new();
    readonly Queue<TransportRequest> priorityTransports=new(),transports=new();
    readonly HashSet<string> seekers=new(), hiders=new(), caught=new(), known=new(), disconnected=new();
    readonly HashSet<string> setupConfirmed=new();
    readonly HashSet<string> deployingSeekers=new();
    bool seekerDeployment;
    readonly HashSet<string> setupItemsGiven=new();

    readonly HashSet<PlayerCharacter> goldApplied=new();
    readonly Dictionary<string,float> borderCooldown=new();
    readonly Dictionary<string,int> recoveryAttempts=new();
    readonly Dictionary<string,float> disconnectDeadlines=new();
    readonly Dictionary<string,Vector3> roundSpawnPositions=new();
    readonly Dictionary<List<Vector3>,Dictionary<string,int>> spawnReservations=new();
    readonly Dictionary<PlayerCharacter,float> fallProtectionUntil=new();
    readonly Dictionary<string,PlayerCharacter> roundPlayers=new();
    readonly Dictionary<string,Corpse> trackedCorpses=new();
    readonly Dictionary<string,Vector3> corpseTargets=new();
    List<Vector3> corpsePlacementCandidates;
    readonly Dictionary<string,Prop> normalItemAssignments=new(),lockedSpeakerAssignments=new(),seekerBeltAssignments=new(),seekerBellAssignments=new();
    readonly List<TransportRequest> activeTransports=new();
    readonly List<GameObject> permanentDummyObjects=new();
    readonly List<PlayerCharacter> dummyManagers=new(),standbyManagers=new();
    readonly List<Vector3> hiderSpawnSlots=new(),seekerSpawnSlots=new(),endSpawnSlots=new(),standbySlots=new(),spawnPoints=new(),borderPoints=new(),playBorder=new(),seekerBorder=new(),endBorder=new(),recoveryPoints=new(),playRecoveryPoints=new(),instructionSignPositions=new();
    readonly List<float> instructionSignYaws=new();
    readonly List<PeckEffectTextInput> instructionSignBoards=new();
    bool simpleVisible,setupVisible,poolBuilt,enforceBorder=true,cleanupPending,showSpawnDebug,spawnDraftLights,finalThirtyTriggered,menuCursorActive,previousCursorVisible;
    float cleanupItemReleaseAt=-1f;
    int cleanupSnatchAttempts;
    readonly Dictionary<PlayerCharacter,Prop> cleanupManagerItems=new();
    readonly Dictionary<Prop,float> brushPickupGraceUntil=new();
    CursorLockMode previousCursorLock;
    Rect simpleWindow=new(25,55,500,560),setupWindow=new(540,55,650,760); Vector2 simpleScroll,setupScroll;
    Vector3 center,seekerSpawn,endSpawn,gearStorage,seekerSignPosition,endSignPosition,statusSignPosition; float seekerSignYaw,endSignYaw,statusSignYaw; bool hasCenter,hasSeeker,hasEndSpawn,hasGearStorage,hasSeekerSign,hasEndSign,hasStatusSign;
    Phase phase; float remaining,nextTick,nextScan,nextCapture,nextTimeApply,nextMarkerPin,nextDummyCorpseCleanup,nextModNetworkCheck,nextSettingsSync;
    int dummySerial,clientTransportSerial;
    bool modHelloSent,serverModHandlerInstalled,clientModHandlerInstalled,modLinkDisabled;
    NetworkMessageDelegate serverModHandler,clientModHandler;
    const ushort ModMessageId=0xE7A1;
    const string ModProtocol="2";
    float nextModHello;
    int completedRemoteToken;
    string syncedHostSettings="",lastSettingsPacket="";
    RemoteClientTransport remoteClientTransport;
    string status="Save positions, build pool once, start round.";
    Prop seekerBoardProp,endBoardProp,statusBoardProp;
    PeckEffectTextInput seekerBoard,endBoard,statusBoard;
    readonly struct PropOrigin{public readonly Vector3 Position;public readonly Quaternion Rotation;public readonly bool Gravity,Kinematic;public PropOrigin(Prop p){Position=p.transform.position;Rotation=p.transform.rotation;Gravity=p.rb!=null&&p.rb.useGravity;Kinematic=p.rb!=null&&p.rb.isKinematic;}}
    sealed class TransportRequest{public PlayerCharacter Player,Worker;public Vector3 Target;public string Reason;public float Deadline,NextStage,StableSince;public int Attempts,Confirmations,Stage;public bool BorderReturn,AirFallbackUsed;}
    sealed class ClientTransportRequest{public PlayerCharacter Player;public Vector3 Target;public string Reason;public float NextSend;public int Token,Attempts;public bool BorderReturn;}
    sealed class RemoteClientTransport{public Vector3 Target;public float Deadline,NextAttempt;public int Token,Confirmations;public bool BorderReturn;}
    public HideAndSeekTester(IntPtr pointer):base(pointer){}
    internal static HideAndSeekTester Instance;
    internal static void ApplyEnabledSetting(bool enabled){if(enabled||Instance==null)return;Instance.simpleVisible=false;Instance.setupVisible=false;Instance.SyncMenuCursor();if(Instance.sessionEnabled||Instance.remoteSessionEnabled)Instance.ResetSession();}
    internal static bool CreatingDummyManager;
    internal static bool IsDummyManager(PlayerCharacter player)=>player!=null&&player.GetComponentInParent<DummyManagerMarker>(true)!=null;
    internal static void HandleSeekerPointCommand(PlayerNetworking networking,bool active)
    {
        var instance=Instance;if(instance==null||!NetworkServer.active||!active||networking?.playerCharacter==null)return;instance.TryLowCeilingCapture(networking.playerCharacter,"grab command");
    }
    internal static bool ShouldBlockPropPickup(PlayerNetworking actor,PlayerHeldInformation held)
    {
        var instance=Instance;if(!ActiveHost||!instance.poolBuilt||actor==null||IsDummyManager(actor.playerCharacter)||!held.hasProp)return false;
        var prop=held.GetProp();if(prop==null)return false;if(instance.propRefreshes.ContainsKey(prop))return true;string key=Key(actor.playerCharacter);
        if(instance.normalItemAssignments.TryGetValue(key,out var assigned)&&assigned!=prop)return true;
        if(instance.lockedSpeakerAssignments.TryGetValue(key,out var locked)&&locked!=prop)return true;
        if(instance.lockedSpeakerAssignments.ContainsValue(prop))return !instance.lockedSpeakerAssignments.TryGetValue(key,out var own)||own!=prop;
        if(instance.normalItemAssignments.ContainsValue(prop)&&actor.playerCharacter.hands?.heldProp!=null&&actor.playerCharacter.hands.heldProp!=prop)return true;
        if(instance.brushes.Contains(prop)||prop.name.StartsWith("SalonBrushProp",StringComparison.Ordinal))return false;
        if(instance.seekers.Contains(key))return true;
        if(instance.normalItemAssignments.ContainsValue(prop))return !instance.hiders.Contains(key)||instance.caught.Contains(key);
        if(instance.belts.Contains(prop)||instance.bells.Contains(prop)||instance.buoys.Contains(prop)||instance.IsSign(prop)||instance.whiteboardOrigins.ContainsKey(prop))return true;
        return prop.name!="MegaphoneProp"||!instance.hiders.Contains(key)||instance.caught.Contains(key);
    }
    internal static void RejectProtectedPropPickup(PlayerNetworking actor,PlayerHeldInformation rejected,PickupSnapshot repair)
    {
        if(actor==null||!NetworkServer.active)return;
        // OnSetHeld ignores corrections older than the owner's predicted action number.
        // Keep the empty state through a network tick before required items are returned.
        var correction=PlayerHeldInformation.DropFromSnatch();
        correction.actionNumber=Mathf.Max(actor.playerHeldInformation.actionNumber,rejected.actionNumber)+1;
        actor.NetworkplayerHeldInformation=correction;
        if(actor.isLocalPlayer&&actor.playerCharacter?.hands?.heldProp!=null)actor.playerCharacter.hands.Drop(correction);
        Instance.pickupCorrectionUntil[actor]=Time.unscaledTime+.3f;
        BroadcastSnapshot(actor);Instance.BeginPropRepair(repair);
    }
    void Awake(){Instance=this;}

    void Start(){LoadDefaultPresetIfUnconfigured();hasCenter=TryVector(Plugin.AreaPoint.Value,out center);hasSeeker=TryVector(Plugin.SeekerPoint.Value,out seekerSpawn);hasEndSpawn=TryVector(Plugin.EndPoint.Value,out endSpawn);hasGearStorage=TryVector(Plugin.GearStoragePoint.Value,out gearStorage);LoadSlots(Plugin.HiderSpawnSlots.Value,hiderSpawnSlots,11);LoadSlots(Plugin.SeekerSpawnSlots.Value,seekerSpawnSlots,11);LoadSlots(Plugin.EndSpawnSlots.Value,endSpawnSlots,11);LoadSlots(Plugin.DummyStandbySlots.Value,standbySlots,11);LoadSlots(Plugin.DraftSpawnSlots.Value,spawnPoints,11);LoadSlots(Plugin.BorderPoints.Value,borderPoints,20);LoadSlots(Plugin.PlayBorder.Value,playBorder,20);LoadSlots(Plugin.SeekerBorder.Value,seekerBorder,20);LoadSlots(Plugin.EndBorder.Value,endBorder,20);LoadSlots(Plugin.DraftRecoveryPoints.Value,recoveryPoints,int.MaxValue);LoadSlots(Plugin.PlayRecoveryPoints.Value,playRecoveryPoints,int.MaxValue);LoadSigns(Plugin.InstructionSigns.Value);hasSeekerSign=TrySign(Plugin.SeekerSignTransform.Value,out seekerSignPosition,out seekerSignYaw);hasEndSign=TrySign(Plugin.EndSignTransform.Value,out endSignPosition,out endSignYaw);hasStatusSign=TrySign(Plugin.StatusSignTransform.Value,out statusSignPosition,out statusSignYaw);LoadPlayAreas();MarkSetupBaseline();}
    static bool GameTextInputActive()
    {
        if(ControlsManager.textInputModeActive)return true;
        try{if(SignTextInput.IsSignInputActive())return true;}catch{}
        try{return TextChatInput.instance?.inputIsOpen==true;}catch{return false;}
    }
    void Update()
    {
        ObserveSession();
        if(!Plugin.Enabled.Value){simpleVisible=false;setupVisible=false;SyncMenuCursor();if(sessionEnabled||remoteSessionEnabled)ResetSession();return;}
        if(!NetworkClient.active||!NetworkClient.isConnected){simpleVisible=false;setupVisible=false;SyncMenuCursor();return;}
        if(!GameTextInputActive()&&Input.GetKeyDown(Plugin.MenuKey.Value)) simpleVisible=!simpleVisible;
        if(!GameTextInputActive()&&Input.GetKeyDown(Plugin.SetupKey.Value)) setupVisible=!setupVisible;
        SyncMenuCursor();

        MaintainModNetworking();

        if(!NetworkServer.active){if(remoteSessionEnabled){ProcessRemoteClientTransport();UpdateFallProtection();if(Time.unscaledTime>=nextTimeApply){nextTimeApply=Time.unscaledTime+1f;ApplyRemoteTime();}}return;}
        if(!sessionEnabled)return;
        if(Time.unscaledTime>=nextTimeApply){nextTimeApply=Time.unscaledTime+1f;ApplyTime();}
        if(Time.unscaledTime>=nextDummyCorpseCleanup){nextDummyCorpseCleanup=Time.unscaledTime+.25f;CleanupDummyCorpses();}
        KeepDummyOutOfRoster();
        UpdateFallProtection();
        ProtectManager();
        ProcessPickupCorrections();ProcessPropRepairs();
        ProcessClientTransports();
        ProcessTransport();
        if(poolBuilt&&Time.unscaledTime>=nextMarkerPin){nextMarkerPin=Time.unscaledTime+.05f;MaintainMarkers();}
        if(cleanupPending&&(cleanupItemReleaseAt>=0f||(activeTransports.Count==0&&clientTransports.Count==0&&priorityTransports.Count==0&&transports.Count==0)))
        {
            if(cleanupItemReleaseAt<0f){PrepareBorrowedPropRelease();cleanupSnatchAttempts=0;RepeatCleanupSnatch();cleanupItemReleaseAt=Time.unscaledTime+.05f;}
            else if(Time.unscaledTime>=cleanupItemReleaseAt){if(cleanupSnatchAttempts<10){RepeatCleanupSnatch();cleanupItemReleaseAt=Time.unscaledTime+.05f;}else{cleanupPending=false;cleanupItemReleaseAt=-1f;RestoreBorrowedProps();}}
        }
        if(phase==Phase.Ended){if(hasEndSpawn&&Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.05f;HandleConnections();MaintainCorpses();EnforceEndArea();}return;}
        if(phase==Phase.SettingUp)
        {
            if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.05f;HandleConnections();MaintainCorpses();if(CheckNoHidersRemain())return;}
            if(EnsureSetupPositions())BeginHiding();return;
        }
        if(phase==Phase.Idle)return;
        if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+.05f;HandleConnections();MaintainCorpses();if(CheckNoHidersRemain())return;EnforceBorders();MaintainRequiredItems();}
        if(phase==Phase.Seeking&&seekerDeployment){MaintainSeekerDeployment();return;}
        if(phase==Phase.Seeking&&Time.unscaledTime>=nextCapture){nextCapture=Time.unscaledTime+.05f;DetectCaptures();}
        if(phase==Phase.Seeking&&!finalThirtyTriggered&&remaining<=30f)BeginFinalThirty();
        if(phase==Phase.Ended)return;
        remaining-=Time.unscaledDeltaTime;
        if(Time.unscaledTime>=nextTick){nextTick=Time.unscaledTime+1f;UpdateBoards();}
        if(remaining<=0f){if(phase==Phase.Hiding)BeginSeeking();else EndRound("TIME UP - HIDERS WIN");}
    }
    void SyncMenuCursor()
    {
        bool open=simpleVisible||setupVisible;
        if(open)
        {
            if(!menuCursorActive){previousCursorLock=Cursor.lockState;previousCursorVisible=Cursor.visible;menuCursorActive=true;}
            Cursor.lockState=CursorLockMode.None;Cursor.visible=true;return;
        }
        if(!menuCursorActive)return;Cursor.lockState=previousCursorLock;Cursor.visible=previousCursorVisible;menuCursorActive=false;
    }
    void OnGUI()
    {
        if(simpleVisible)simpleWindow=GUI.Window(730921,simpleWindow,(GUI.WindowFunction)DrawSimple,"Mady's HideNSeek");
        if(setupVisible)setupWindow=GUI.Window(730922,setupWindow,(GUI.WindowFunction)DrawSetup,"Mady's HideNSeek Setup");
        if(showSpawnDebug)DrawSpawnDebug();
    }
    void DrawSimple(int id)
    {
        simpleScroll=GUILayout.BeginScrollView(simpleScroll);var local=LocalPlayer();
        if(local==null||!NetworkServer.active)GUILayout.Label(remoteSessionEnabled?"Connected to host hide and seek. Setup is automatic.":"Host a lobby to configure hide and seek.");else{
            GUILayout.Label(sessionEnabled?$"Phase: {phase} | players: {Participants().Count}/11":"Normal lobby - hide and seek is inactive.");
            if(GUILayout.Button(sessionEnabled&&poolBuilt&&standbyManagers.Count==11?"Setup complete":"Setup"))SetupSession();
            LiveFloat("Hide seconds",Plugin.HideTime,5f,300f);LiveFloat("Round seconds",Plugin.RoundTime,15f,1800f);LiveInt("Seeker count",Plugin.SeekerCount,0,3);
            DrawAreaSelector();
            Plugin.RandomItemChance.Value=GUILayout.HorizontalSlider(Mathf.Clamp(Plugin.RandomItemChance.Value,0f,100f),0f,100f);GUILayout.Label($"Hider item chance: {Plugin.RandomItemChance.Value:0}%");
            Plugin.FinalThirtySpeakers.Value=GUILayout.Toggle(Plugin.FinalThirtySpeakers.Value,"Speakers during final 30 seconds");
            DrawItemCategoryControls();
            GUILayout.Label("Locked time: "+Plugin.TimeOfDay.Value);GUILayout.BeginHorizontal();TimeButton("Natural",LockedTime.Natural);TimeButton("Sunrise",LockedTime.Sunrise);TimeButton("Day",LockedTime.Day);TimeButton("Sunset",LockedTime.Sunset);TimeButton("Night",LockedTime.Night);GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();GUI.enabled=sessionEnabled&&poolBuilt&&standbyManagers.Count==11&&(phase==Phase.Idle||phase==Phase.Ended)&&!cleanupPending;if(GUILayout.Button("Start round"))StartRound();GUI.enabled=sessionEnabled&&(phase==Phase.Hiding||phase==Phase.Seeking||phase==Phase.SettingUp);if(GUILayout.Button("End round"))EndRound("ROUND STOPPED");GUI.enabled=true;GUILayout.EndHorizontal();
            if(GUILayout.Button("Join the Discord!"))Application.OpenURL(Plugin.DiscordUrl);
        }
        GUILayout.TextArea(status,GUILayout.Height(65));GUILayout.EndScrollView();GUI.DragWindow(new Rect(0,0,10000,24));
    }
    void DrawSetup(int id)
    {
        setupScroll=GUILayout.BeginScrollView(setupScroll);var local=LocalPlayer();
        if(local==null||!NetworkServer.active)GUILayout.Label(remoteSessionEnabled?"Connected to host hide and seek. Setup is automatic.":"Host a lobby to configure hide and seek.");else{
            DrawPresets();
            GUILayout.Label($"Play areas: {playAreas.Count} | seeker: {(seekerBorder.Count>=4&&seekerSpawnSlots.Count==11?"Fully Set":"Missing")} | end: {(endBorder.Count>=4&&endSpawnSlots.Count==11?"Fully Set":"Missing")}");
            DrawAreaSelector();DrawAreaEditControls();
            GUILayout.Label("RIGHT-CLICK placement buttons to undo latest point.");
            if(GUILayout.Button("Set gear storage"))Save(local.transform.position,Plugin.GearStoragePoint,ref gearStorage,ref hasGearStorage,"gear storage");
            if(RightClickedLastControl()){hasGearStorage=false;Plugin.GearStoragePoint.Value="";status="Gear storage cleared.";}
            if(GUILayout.Button(standbySlots.Count<11?$"Set Manager standby {standbySlots.Count+1}":"Manager standbys: Fully Set (click to replace)"))AddStandbyPoint(local.transform.position);
            if(RightClickedLastControl())UndoPoint(standbySlots,Plugin.DummyStandbySlots,"Manager standby");
            GUILayout.Label($"Manager standby positions: {standbySlots.Count}/11");
            if(GUILayout.Button("Set Instruction Sign"))AddInstructionSign(local);
            if(RightClickedLastControl()){instructionSignPositions.Clear();instructionSignYaws.Clear();SaveSigns();status="Instruction sign cleared.";}
            GUILayout.Label("Instruction sign: "+(instructionSignPositions.Count==1?"Set":"Missing")+" | facing = manager facing");
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(hasSeekerSign?"Seeker sign: Set":"Set seeker sign"))SaveTimerSign(local,true);
            if(RightClickedLastControl()){hasSeekerSign=false;Plugin.SeekerSignTransform.Value="";status="Seeker sign cleared.";}
            if(GUILayout.Button(hasEndSign?"End sign: Set":"Set end sign"))SaveTimerSign(local,false);
            if(RightClickedLastControl()){hasEndSign=false;Plugin.EndSignTransform.Value="";status="End sign cleared.";}
            GUILayout.EndHorizontal();
            if(GUILayout.Button(AreaSignButtonLabel()))SaveDraftStatusSign(local);
            if(RightClickedLastControl())ClearAreaStatusSign();
            GUILayout.Label("Instruction sign text");
            string signText=GUILayout.TextField(Plugin.InstructionSignText.Value);
            if(signText!=Plugin.InstructionSignText.Value){Plugin.InstructionSignText.Value=signText;ApplyInstructionText();}
            GUILayout.Label($"Area draft: Spawn={spawnPoints.Count}/11 Border={borderPoints.Count}/20 Recover={recoveryPoints.Count}");
            DrawSpawnSlots(local);
            GUI.enabled=spawnPoints.Count==11&&borderPoints.Count>=4&&recoveryPoints.Count>0&&hasDraftStatusSign;
            if(GUILayout.Button(editingAreaIndex>=0?"Save Edited Play Area":"Save New Play Area"))SaveNewPlayArea();
            if(RightClickedLastControl())ClearAreaDraft();
            GUI.enabled=true;
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Spawn -> Seeker"))SaveSpawnDraft(Plugin.SeekerSpawnSlots,seekerSpawnSlots,Plugin.SeekerPoint,ref seekerSpawn,ref hasSeeker,"seeker");
            if(GUILayout.Button("Spawn -> End"))SaveSpawnDraft(Plugin.EndSpawnSlots,endSpawnSlots,Plugin.EndPoint,ref endSpawn,ref hasEndSpawn,"end");
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("Border -> Seeker"))SaveBorderDraft(Plugin.SeekerBorder,seekerBorder,"seeker");
            if(GUILayout.Button("Border -> End"))SaveBorderDraft(Plugin.EndBorder,endBorder,"end");
            GUILayout.EndHorizontal();

            GUILayout.Label($"Buoys: {buoys.Count} found | {Mathf.Max(0,buoys.Count-StaticBorderCount()-WarningBuoyCount())} unused");
            bool showObjects=GUILayout.Toggle(showSpawnDebug,"Show Objects");if(showObjects!=showSpawnDebug)SetShowObjects(showObjects);
            bool draftLights=GUILayout.Toggle(spawnDraftLights,"Spawn Draft Lights");if(draftLights!=spawnDraftLights){if(!draftLights)ReturnBoundaryBuoys(phase==Phase.Idle||phase==Phase.Ended?0:playBorder.Count+seekerBorder.Count+endBorder.Count,borderPoints.Count);spawnDraftLights=draftLights&&poolBuilt;if(poolBuilt)PositionMarkers();status=spawnDraftLights?"Draft lights spawned.":"Draft lights returned.";}
        }
        GUILayout.TextArea(status,GUILayout.Height(65));GUILayout.EndScrollView();GUI.DragWindow(new Rect(0,0,10000,24));
    }
    void DrawSpawnDebug()
    {
        var camera=Camera.main;if(camera==null)return;
        for(int i=0;i<playAreas.Count;i++){var color=AreaColor(i);DrawDebugList(camera,playAreas[i].SpawnPoints,$"AREA {i+1} SPAWN ",color);DrawDebugList(camera,playAreas[i].RecoveryPoints,$"AREA {i+1} RECOVER ",color);DrawDebugList(camera,playAreas[i].Border,$"AREA {i+1} BORDER ",color);if(playAreas[i].HasStatusSign)DrawDebugPoint(camera,playAreas[i].StatusSignPosition,$"AREA {i+1} SIGN",color);}
        DrawDebugList(camera,seekerSpawnSlots,"SEEKER SPAWN ",Color.yellow);DrawDebugList(camera,endSpawnSlots,"END SPAWN ",Color.cyan);DrawDebugList(camera,standbySlots,"MANAGER ",new Color(1f,.65f,0f));DrawDebugList(camera,spawnPoints,"DRAFT SPAWN ",Color.white);
        DrawDebugList(camera,recoveryPoints,"DRAFT RECOVER ",Color.white);
        if(hasDraftStatusSign)DrawDebugPoint(camera,draftStatusSignPosition,"DRAFT PLAY SIGN",Color.magenta);
        var lightColor=new Color(1f,.35f,.1f);DrawDebugList(camera,seekerBorder,"SEEKER BORDER ",lightColor);DrawDebugList(camera,endBorder,"END BORDER ",Color.cyan);DrawDebugList(camera,borderPoints,"DRAFT BORDER ",Color.magenta);
        DrawDebugList(camera,instructionSignPositions,"SIGN",Color.white);if(hasSeekerSign)DrawDebugPoint(camera,seekerSignPosition,"SEEKER SIGN",Color.yellow);if(hasEndSign)DrawDebugPoint(camera,endSignPosition,"END SIGN",Color.cyan);if(hasStatusSign)DrawDebugPoint(camera,statusSignPosition,"PLAY STATUS",Color.green);
    }
    static void DrawDebugList(Camera camera,List<Vector3> points,string prefix,Color color)
    {
        var old=GUI.color;GUI.color=color;for(int i=0;i<points.Count;i++){var screen=camera.WorldToScreenPoint(points[i]);if(screen.z<=0f)continue;GUI.Label(new Rect(screen.x-55f,Screen.height-screen.y-10f,110f,20f),$"{prefix}{i+1}");}GUI.color=old;
    }
    static void DrawDebugPoint(Camera camera,Vector3 point,string label,Color color)
    {
        var screen=camera.WorldToScreenPoint(point);if(screen.z<=0f)return;var old=GUI.color;GUI.color=color;GUI.Label(new Rect(screen.x-65f,Screen.height-screen.y-10f,130f,20f),label);GUI.color=old;
    }
    void CreateDummyManager(PlayerCharacter local)
    {
        if(!ActiveHost||local==null){status="Press Setup in F9 first.";return;}if(standbySlots.Count!=11){status="Set all 11 Manager standby positions first.";return;}ClearDummyManager();CleanupDummyCorpses();
        try
        {
            for(int i=0;i<standbySlots.Count;i++)standbyManagers.Add(SpawnDummy(standbySlots[i],$"Shared {i+1}",permanentDummyObjects));
            KeepDummyOutOfRoster();status="11 shared gold transport dummies ready.";
        }
        catch(Exception ex){status="Manager creation failed: "+ex.Message;Plugin.Logger.LogError(ex);ClearDummyManager();}
    }
    PlayerCharacter SpawnDummy(Vector3 position,string label,List<GameObject> objectList)
    {
        var prefab=NetworkManager.singleton?.playerPrefab;if(prefab==null)throw new Exception("Player prefab unavailable.");CreatingDummyManager=true;
        try
        {
        var clone=UnityEngine.Object.Instantiate(prefab);clone.SetActive(false);clone.name="HNS Manager "+label;clone.AddComponent<DummyManagerMarker>();foreach(var voice in clone.GetComponentsInChildren<PlayerVoicePlaybackControl>(true))if(voice!=null)voice.enabled=false;
            var player=clone.GetComponentInChildren<PlayerCharacter>(true);if(player==null){UnityEngine.Object.Destroy(clone);throw new Exception("PlayerCharacter missing from prefab.");}var networking=player.playerNetworking;string dummyId="HNS_DUMMY_"+(++dummySerial).ToString(CultureInfo.InvariantCulture);if(networking!=null)networking.identifier=dummyId;clone.transform.position=position;clone.SetActive(true);NetworkServer.Spawn(clone);if(networking!=null)networking.Networkidentifier=dummyId;objectList.Add(clone);dummyManagers.Add(player);SetManagerGold(player);DirectLocalTeleport(player,position);
            if(player.rb!=null){player.rb.isKinematic=false;player.rb.useGravity=false;player.rb.constraints=RigidbodyConstraints.FreezeAll;player.rb.linearVelocity=Vector3.zero;player.rb.angularVelocity=Vector3.zero;}
            var identity=clone.GetComponent<NetworkIdentity>();bool authority=identity!=null&&NetworkServer.localConnection!=null&&identity.AssignClientAuthority(NetworkServer.localConnection);Plugin.Logger.LogInfo($"[DUMMY MANAGER] {label} netId={(identity==null?0:identity.netId)} authority={authority} position={position}");return player;
        }
        finally{CreatingDummyManager=false;}
    }
    void ClearDummyManager()
    {
        ClearWorkers();foreach(var item in permanentDummyObjects)DestroyDummy(item);permanentDummyObjects.Clear();standbyManagers.Clear();dummyManagers.Clear();goldApplied.Clear();activeTransports.Clear();
    }
    void ClearWorkers(){priorityTransports.Clear();transports.Clear();foreach(var request in new List<TransportRequest>(activeTransports))if(request?.Worker?.hands?.heldCharacter!=null)ManagerDrop(request.Worker);activeTransports.Clear();for(int i=0;i<standbyManagers.Count&&i<standbySlots.Count;i++)if(standbyManagers[i]!=null)DirectLocalTeleport(standbyManagers[i],standbySlots[i]);}
    static void DestroyDummy(GameObject item){if(item==null)return;if(NetworkServer.active)NetworkServer.Destroy(item);else UnityEngine.Object.Destroy(item);}
    static void ClearExistingCorpses()
    {
        if(Corpse.allCorpses==null)return;var remove=new List<GameObject>();foreach(var corpse in Corpse.allCorpses)if(corpse!=null)remove.Add(corpse.gameObject);foreach(var item in remove)if(NetworkServer.active)NetworkServer.Destroy(item);else UnityEngine.Object.Destroy(item);
    }
    static void CleanupDummyCorpses()
    {
        if(Corpse.allCorpses==null)return;var remove=new List<GameObject>();foreach(var corpse in Corpse.allCorpses)if(corpse!=null&&corpse.identifier!=null&&corpse.identifier.StartsWith("HNS_DUMMY_",StringComparison.Ordinal))remove.Add(corpse.gameObject);foreach(var item in remove)if(NetworkServer.active)NetworkServer.Destroy(item);else UnityEngine.Object.Destroy(item);
    }
    void KeepDummyOutOfRoster()
    {
        if(PlayerCharacter.allPlayerCharacters==null)return;foreach(var manager in dummyManagers)if(manager!=null){PlayerCharacter.allPlayerCharacters.Remove(manager);SetManagerGold(manager);}
    }
    void SetManagerGold(PlayerCharacter player)
    {
        if(player==null)return;
        var networking=player.playerNetworking;var lookSet=player.looks?.lookSet;var colors=lookSet?.colors;if(networking==null||colors==null||colors.Length==0)return;
        int gold=0;float best=float.MaxValue;for(int i=0;i<colors.Length;i++){var color=lookSet.GetColor(i);float max=Mathf.Max(color.r,Mathf.Max(color.g,color.b));if(max>1f){color.r/=max;color.g/=max;color.b/=max;}Color.RGBToHSV(color,out var hue,out var saturation,out var value);float hueDistance=Mathf.Abs(hue-.13f);hueDistance=Mathf.Min(hueDistance,1f-hueDistance);float score=hueDistance*8f+Mathf.Abs(saturation-.9f)+Mathf.Abs(value-1f)*.5f;if(score<best){best=score;gold=i;}}
        if(goldApplied.Contains(player)&&networking.lookIdHead==gold&&networking.lookIdTorso==gold&&networking.lookIdLegs==gold)return;
        networking.ServerSetLook(gold,PlayerLooks.LookPart.Head,false);networking.ServerSetLook(gold,PlayerLooks.LookPart.Torso,false);networking.ServerSetLook(gold,PlayerLooks.LookPart.Legs,false);
        networking.NetworklookIdHead=gold;networking.NetworklookIdTorso=gold;networking.NetworklookIdLegs=gold;goldApplied.Add(player);
    }
    void AddInstructionSign(PlayerCharacter local)
    {
        instructionSignPositions.Clear();instructionSignYaws.Clear();instructionSignPositions.Add(local.transform.position);instructionSignYaws.Add(local.transform.eulerAngles.y);SaveSigns();
        if(poolBuilt){ClaimInstructionSigns();PositionInstructionSigns();}
        status="Instruction sign set at manager position and facing.";
    }
    void SaveTimerSign(PlayerCharacter local,bool seeker)
    {
        if(local==null)return;var position=local.transform.position;float yaw=local.transform.eulerAngles.y;
        if(seeker){seekerSignPosition=position;seekerSignYaw=yaw;hasSeekerSign=true;Plugin.SeekerSignTransform.Value=FormatSign(position,yaw);}
        else{endSignPosition=position;endSignYaw=yaw;hasEndSign=true;Plugin.EndSignTransform.Value=FormatSign(position,yaw);}
        if(poolBuilt)PositionMarkers();status=(seeker?"Seeker":"End")+" sign position and facing saved.";
    }
    void SaveStatusSign(PlayerCharacter local)
    {
        if(local==null)return;statusSignPosition=local.transform.position;statusSignYaw=local.transform.eulerAngles.y;hasStatusSign=true;Plugin.StatusSignTransform.Value=FormatSign(statusSignPosition,statusSignYaw);
        if(poolBuilt)PositionMarkers();status="Play status sign position and facing saved.";
    }
    void ApplyInstructionText(){for(int i=0;i<instructionSignBoards.Count;i++)SetBoard(instructionSignBoards[i],Plugin.InstructionSignText.Value);}
    void TimeButton(string label,LockedTime value){if(GUILayout.Button(label)){Plugin.TimeOfDay.Value=value;ApplyTime();status="Time set: "+label;}}
    void ApplyTime()
    {
        if(!sessionEnabled||!NetworkServer.active||!SkyManager.initalized)return;
        switch(Plugin.TimeOfDay.Value){
            case LockedTime.Natural:SkyManager.ClearFixedTime();break;
            case LockedTime.Sunrise:SkyManager.SetFixedTime(6f);break;
            case LockedTime.Day:SkyManager.SetFixedTime(12f);break;
            case LockedTime.Sunset:SkyManager.SetFixedTime(18f);break;
            case LockedTime.Night:SkyManager.SetFixedTime(0f);break;
        }
    }
    void LiveFloat(string label,ConfigEntry<float> entry,float min,float max)
    {
        float previous=entry.Value;
        GUILayout.BeginHorizontal();GUILayout.Label(label+": "+entry.Value.ToString("0"),GUILayout.Width(190));
        float value=GUILayout.HorizontalSlider(entry.Value,min,max);if(Math.Abs(value-entry.Value)>.01f)entry.Value=Mathf.Round(value);
        if(GUILayout.Button("-",GUILayout.Width(28)))entry.Value=Mathf.Max(min,entry.Value-1f);
        if(GUILayout.Button("+",GUILayout.Width(28)))entry.Value=Mathf.Min(max,entry.Value+1f);GUILayout.EndHorizontal();
        if(entry.Value!=previous)
        {
            if((entry==Plugin.HideTime&&phase==Phase.Hiding)||(entry==Plugin.RoundTime&&phase==Phase.Seeking))remaining=Mathf.Max(0f,remaining+entry.Value-previous);
        }
    }
    void LiveInt(string label,ConfigEntry<int> entry,int min,int max)
    {
        GUILayout.BeginHorizontal();GUILayout.Label(label+": "+entry.Value,GUILayout.Width(190));
        int value=Mathf.RoundToInt(GUILayout.HorizontalSlider(entry.Value,min,max));if(value!=entry.Value)entry.Value=value;
        if(GUILayout.Button("-",GUILayout.Width(28)))entry.Value=Mathf.Max(min,entry.Value-1);
        if(GUILayout.Button("+",GUILayout.Width(28)))entry.Value=Mathf.Min(max,entry.Value+1);GUILayout.EndHorizontal();
    }
    void Save(Vector3 value,ConfigEntry<string> cfg,ref Vector3 target,ref bool flag,string label){target=value;flag=true;cfg.Value=FormatVector(value);status="Saved "+label+" until overwritten.";}
    void DrawSpawnSlots(PlayerCharacter local)
    {
        GUILayout.Label("Stand at each point, click once, then save area.");
        GUI.enabled=spawnPoints.Count<11;
        if(DraftButton(spawnPoints.Count==0?"Set Spawn 1":spawnPoints.Count==11?"Spawn 11 Fully Set":$"Spawn {spawnPoints.Count} Set - next Spawn {spawnPoints.Count+1}",out var undoP))AddDraftPoint(local.transform.position,spawnPoints,11,Plugin.DraftSpawnSlots,"Spawn ");
        if(undoP)UndoPoint(spawnPoints,Plugin.DraftSpawnSlots,"spawn point");
        GUI.enabled=true;
        GUI.enabled=borderPoints.Count<20;
        if(DraftButton(borderPoints.Count==0?"Set Border 1":$"Border {borderPoints.Count} Set - next Border {borderPoints.Count+1}",out var undoB))AddDraftPoint(local.transform.position,borderPoints,20,Plugin.BorderPoints,"Border ");
        if(undoB)UndoPoint(borderPoints,Plugin.BorderPoints,"border point");
        GUI.enabled=true;
        if(DraftButton(recoveryPoints.Count==0?"Set Recover 1":$"Recover {recoveryPoints.Count} Set - next Recover {recoveryPoints.Count+1}",out var undoR))AddDraftPoint(local.transform.position,recoveryPoints,int.MaxValue,Plugin.DraftRecoveryPoints,"Recover ");
        if(undoR)UndoPoint(recoveryPoints,Plugin.DraftRecoveryPoints,"recovery point");
    }
    void AddDraftPoint(Vector3 value,List<Vector3> points,int maximum,ConfigEntry<string> config,string prefix)
    {
        if(points.Count>=maximum)return;
        points.Add(value);config.Value=FormatSlots(points);showSpawnDebug=true;status=$"{prefix}{points.Count} set.";
    }
    void AddStandbyPoint(Vector3 value)
    {
        if(standbySlots.Count>=11)standbySlots.Clear();standbySlots.Add(value);Plugin.DummyStandbySlots.Value=FormatSlots(standbySlots);status=$"Manager standby {standbySlots.Count} set.";
    }
    void SaveRecoveryPoints(ConfigEntry<string> config,List<Vector3> target,string label)
    {
        if(recoveryPoints.Count==0){status="Set at least 1 Recover point before saving "+label+" recoveries.";return;}
        target.Clear();target.AddRange(recoveryPoints);config.Value=FormatSlots(target);recoveryPoints.Clear();Plugin.DraftRecoveryPoints.Value="";status=$"Saved {target.Count} {label} recovery point(s). Recover draft reset.";
    }
    void SaveSpawnDraft(ConfigEntry<string> slotsConfig,List<Vector3> zoneSpawns,ConfigEntry<string> centerConfig,ref Vector3 centerTarget,ref bool hasCenterTarget,string label)
    {
        if(spawnPoints.Count!=11){status="Set all 11 Spawn points before saving "+label+" positions.";return;}
        zoneSpawns.Clear();zoneSpawns.AddRange(spawnPoints);slotsConfig.Value=FormatSlots(zoneSpawns);if(!hasCenterTarget){centerTarget=spawnPoints[0];hasCenterTarget=true;centerConfig.Value=FormatVector(centerTarget);}spawnPoints.Clear();Plugin.DraftSpawnSlots.Value="";status="Saved 11 "+label+" positions. Spawn draft reset.";
    }
    void SaveBorderDraft(ConfigEntry<string> borderConfig,List<Vector3> zoneBorder,string label)
    {
        if(borderPoints.Count<4){status="Set at least 4 Border points before saving "+label+" border.";return;}
        zoneBorder.Clear();zoneBorder.AddRange(borderPoints);borderConfig.Value=FormatSlots(zoneBorder);borderPoints.Clear();Plugin.BorderPoints.Value="";if(poolBuilt)PositionMarkers();status="Saved "+label+" border. Border draft reset.";
    }

    void BuildPool()
    {
        if(!ActiveHost){status="Press Setup in F9 first.";return;}if(!hasEndSpawn){status="Save end area first.";return;}if(poolBuilt){status="Pool already ready.";return;}
        try{
            buoys.Clear();instructionSignProps.Clear();instructionSignBoards.Clear();whiteboardOrigins.Clear();seekerBoardProp=null;endBoardProp=null;statusBoardProp=null;seekerBoard=null;endBoard=null;statusBoard=null;
            if(Prop.allProps!=null)foreach(var prop in Prop.allProps)if(prop!=null&&prop.GetComponentInChildren<PeckEffectTextInput>(true)!=null)whiteboardOrigins[prop]=new PropOrigin(prop);
            GrowPool();
            var boards=new List<Prop>();ClaimProps("SignProp center",2+Mathf.Min(1,instructionSignPositions.Count),boards);if(boards.Count>0)seekerBoardProp=boards[0];if(boards.Count>1)endBoardProp=boards[1];if(boards.Count>2){instructionSignProps.Add(boards[2]);instructionSignBoards.Add(boards[2].GetComponentInChildren<PeckEffectTextInput>(true));}
            ClaimStatusSign();
            ClaimBuoys(RequiredBuoyCount());
            PositionBrushes();
            if(seekerBoardProp==null)throw new Exception("map lacks required whiteboard");
            seekerBoard=seekerBoardProp.GetComponentInChildren<PeckEffectTextInput>(true);
            if(endBoardProp!=null)endBoard=endBoardProp.GetComponentInChildren<PeckEffectTextInput>(true);
            PositionMarkers();poolBuilt=true;status=$"Available: {megaphones.Count} megaphones, {walkies.Count} walkies, {flares.Count} flares, {xrayGoggles.Count} goggles, {speakers.Count} speakers, {belts.Count} belts, {bells.Count} bells, {(endBoard!=null?2:1)+instructionSignProps.Count+(statusBoard!=null?1:0)} boards, {buoys.Count} buoys.";Plugin.Logger.LogInfo("[POOL] "+status);
        }catch(Exception ex){status="Pool failed: "+ex.Message;Plugin.Logger.LogError(ex);}
    }
    void GrowPool()
    {
        ClaimProps("MegaphoneProp",Mathf.Clamp(Plugin.MegaphoneItemCount.Value,0,12),megaphones);
        ClaimAllProps("WalkieTalkieProp",walkies);
        ClaimAllProps("FlareGunProp",flares);
        ClaimAllProps("XrayGogglesProp",xrayGoggles);
        ClaimAllProps("PegTileProp Speaker",speakers);
        ClaimAllProps("HolsterProp",belts);
        ClaimAllProps("CowBell",bells);
        ClaimAllProps("SalonBrushProp",brushes);
        if(poolBuilt)ClaimBuoys(RequiredBuoyCount());
    }
    void ClaimProps(string name,int wanted,List<Prop> target)
    {
        target.RemoveAll(p=>p==null);
        if(Prop.allProps!=null)foreach(var prop in Prop.allProps)
        {
            if(target.Count>=wanted)break;
            if(prop==null||target.Contains(prop)||prop.exclusiveHolder!=null)continue;
            bool matches=name=="SignProp center"?prop.name.StartsWith("SignProp",StringComparison.Ordinal)&&prop.GetComponentInChildren<PeckEffectTextInput>(true)!=null:prop.name==name;
            if(matches){target.Add(prop);RememberOrigin(prop);}
        }
        if(target.Count<wanted)Plugin.Logger.LogWarning($"[POOL] Map has {target.Count}/{wanted} real networked {name} objects. Using available objects.");
    }
    void ClaimAllProps(string prefix,List<Prop> target)
    {
        target.RemoveAll(p=>p==null);if(Prop.allProps==null)return;
        foreach(var prop in Prop.allProps)if(prop!=null&&!target.Contains(prop)&&prop.exclusiveHolder==null&&prop.name.StartsWith(prefix,StringComparison.Ordinal)){target.Add(prop);RememberOrigin(prop);}
    }
    void ClaimInstructionSigns()
    {
        if(Prop.allProps==null||instructionSignPositions.Count==0||instructionSignProps.Count>=1)return;
        while(instructionSignProps.Count<1)
        {
            Prop found=null;
            foreach(var prop in Prop.allProps)
            {
                if(prop==null||prop==seekerBoardProp||prop==endBoardProp||instructionSignProps.Contains(prop)||prop.exclusiveHolder!=null)continue;
                if(prop.name.StartsWith("SignProp",StringComparison.Ordinal)&&prop.GetComponentInChildren<PeckEffectTextInput>(true)!=null){found=prop;break;}
            }
            if(found==null)break;
            instructionSignProps.Add(found);
            instructionSignBoards.Add(found.GetComponentInChildren<PeckEffectTextInput>(true));
            RememberOrigin(found);
        }
    }
    void ClaimStatusSign()
    {
        if(Prop.allProps==null)return;var wanted=new Vector3(496.65f,92.21f,-454.54f);float best=float.MaxValue;
        foreach(var prop in Prop.allProps)
        {
            if(prop==null||prop.exclusiveHolder!=null||prop.name!="SpareSignProp"||prop.GetComponentInChildren<PeckEffectTextInput>(true)==null)continue;
            float distance=(prop.transform.position-wanted).sqrMagnitude;if(distance<best){best=distance;statusBoardProp=prop;}
        }
        if(statusBoardProp==null){Plugin.Logger.LogWarning("[POOL] Selected SpareSignProp unavailable.");return;}RememberOrigin(statusBoardProp);statusBoard=statusBoardProp.GetComponentInChildren<PeckEffectTextInput>(true);
    }
    void ClaimBuoys(int wanted)
    {
        buoys.RemoveAll(prop=>prop==null);if(Prop.allProps==null||buoys.Count>=wanted)return;int before=buoys.Count;
        foreach(var prop in Prop.allProps)if(prop!=null&&!buoys.Contains(prop)&&IsBuoyMarker(prop)){buoys.Add(prop);RememberOrigin(prop);}
        if(!poolBuilt){Vector3 storage=MarkerStorage();buoys.Sort((a,b)=>(a.transform.position-storage).sqrMagnitude.CompareTo((b.transform.position-storage).sqrMagnitude));}
        if(buoys.Count!=before)Plugin.Logger.LogInfo($"[POOL] Indexed {buoys.Count} buoy markers; stored markers prioritized.");
    }
    static bool IsBuoyMarker(Prop prop)=>prop!=null&&(prop.name.StartsWith("BuoyProp",StringComparison.Ordinal)||prop.name.StartsWith("BuoyLight",StringComparison.Ordinal)||prop.name.StartsWith("BuoyRedProp",StringComparison.Ordinal));
    int StaticBorderCount()=>playBorder.Count+seekerBorder.Count+endBorder.Count+(spawnDraftLights?borderPoints.Count:0);
    int RequiredBuoyCount()=>Mathf.Clamp(StaticBorderCount()+Mathf.Max(33,Plugin.BorderSegments.Value),24,128);
    int WarningBuoyCount(){int count=0;foreach(var group in warningBuoys.Values)count+=group?.Count??0;return count;}
    void RememberOrigin(Prop prop){if(prop!=null&&!propOrigins.ContainsKey(prop))propOrigins[prop]=new PropOrigin(prop);}
    void StartRound()
    {
        recoveryAttempts.Clear();
        simpleVisible=false;setupVisible=false;SyncMenuCursor();
        if(standbyManagers.Count!=11){status="Press Setup in F9 first.";return;}
        if(!ActivateSelectedPlayArea())return;
        if(!ActiveHost||!poolBuilt||!hasCenter||!hasSeeker||!hasEndSpawn||playBorder.Count<4||seekerBorder.Count<4||endBorder.Count<4||playRecoveryPoints.Count==0||hiderSpawnSlots.Count!=11||seekerSpawnSlots.Count!=11||endSpawnSlots.Count!=11){status="Save play, seeker, and end areas, then press Setup in F9.";return;}
        if(!ValidateSpawnSlots(hiderSpawnSlots,"Play")||!ValidateSpawnSlots(seekerSpawnSlots,"Seeker")||!ValidateSpawnSlots(endSpawnSlots,"End"))return;
        spawnReservations.Clear();
        corpsePlacementCandidates=null;
        ClearWorkers();deployingSeekers.Clear();seekerDeployment=false;propRefreshes.Clear();pickupCorrectionUntil.Clear();seekers.Clear();hiders.Clear();caught.Clear();known.Clear();disconnected.Clear();disconnectDeadlines.Clear();trackedCorpses.Clear();corpseTargets.Clear();roundPlayers.Clear();setupConfirmed.Clear();setupItemsGiven.Clear();normalItemAssignments.Clear();lockedSpeakerAssignments.Clear();seekerBeltAssignments.Clear();seekerBellAssignments.Clear();finalThirtyTriggered=false;borderCooldown.Clear();roundSpawnPositions.Clear();priorityTransports.Clear();transports.Clear();activeTransports.Clear();cleanupPending=false;cleanupItemReleaseAt=-1f;var people=Participants();
        if(people.Count==0){status="No remote players. Host is manager only.";return;}Shuffle(people);
        int count=people.Count==1?0:Mathf.Clamp(Plugin.SeekerCount.Value,1,Mathf.Min(3,people.Count-1));

        for(int i=0;i<people.Count;i++){string key=Key(people[i]);known.Add(key);roundPlayers[key]=people[i];if(i<count){seekers.Add(key);roundSpawnPositions[key]=ReserveSpawn(key,seekerSpawnSlots);}else{hiders.Add(key);roundSpawnPositions[key]=ReserveSpawn(key,hiderSpawnSlots);}}
        phase=Phase.SettingUp;remaining=0f;nextTick=0;GrowPool();PrepareRoundItems(people);PositionBrushes();
        foreach(var p in people)Teleport(p,roundSpawnPositions[Key(p)]);PositionMarkers();SetBoard(seekerBoard,"SETTING UP");SetBoard(endBoard,"SETTING UP");SetBoard(statusBoard,"SETTING UP");
        status=$"Positioning {seekers.Count} seeker(s), {hiders.Count} hider(s). Timer paused.";Plugin.Logger.LogInfo("[ROUND] "+status);
    }
    void BeginHiding(){phase=Phase.Hiding;remaining=Mathf.Max(1f,Plugin.HideTime.Value);nextTick=0;ParkIdleWorkers();UpdateBoards();status="Everyone positioned. Hiding timer started.";Plugin.Logger.LogInfo("[ROUND] "+status);}
    bool EnsureSetupPositions()
    {
        if(activeTransports.Count>0||priorityTransports.Count>0||transports.Count>0)return false;bool ready=true;
        foreach(var player in Participants())
        {
            string key=Key(player);if(!known.Contains(key)){ready=false;continue;}if(setupConfirmed.Contains(key))continue;if(clientTransports.ContainsKey(key)){ready=false;continue;}
            ready=false;var target=roundSpawnPositions.TryGetValue(key,out var assigned)?assigned:(seekers.Contains(key)?seekerSpawn:SlotOrFallback(hiderSpawnSlots,0,center));QueueTransport(player,target,"setup retry");
        }
        return ready;
    }
    void BeginSeeking()
    {
        phase=Phase.Seeking;remaining=Mathf.Max(1f,Plugin.RoundTime.Value);nextTick=0;seekerDeployment=true;deployingSeekers.Clear();

        foreach(var p in Players())if(seekers.Contains(Key(p)))
        {
            string key=Key(p);var target=ReserveSpawn(key,hiderSpawnSlots);roundSpawnPositions[key]=target;deployingSeekers.Add(key);Teleport(p,target);
        }
        ParkIdleWorkers();SetBoard(seekerBoard,"POSITIONING SEEKERS");SetBoard(endBoard,"POSITIONING SEEKERS");SetBoard(statusBoard,"POSITIONING SEEKERS");status="Waiting for seekers to arrive.";
    }
    void MaintainSeekerDeployment()
    {
        foreach(var key in new List<string>(deployingSeekers))
        {
            var player=FindPlayer(key);
            if(player==null){if(!disconnectDeadlines.TryGetValue(key,out var deadline)||Time.unscaledTime>=deadline)deployingSeekers.Remove(key);continue;}
            if(!IsTransporting(player)&&roundSpawnPositions.TryGetValue(key,out var target))QueueTransport(player,target,"seeker deployment retry");
        }
        if(deployingSeekers.Count>0)return;
        seekerDeployment=false;remaining=Mathf.Max(1f,Plugin.RoundTime.Value);nextTick=0;UpdateBoards();status="Seekers positioned. Seeking timer started.";
    }
    void ConfirmSeekerDeployment(PlayerCharacter player,Vector3 target)
    {
        if(seekerDeployment&&player!=null&&roundSpawnPositions.TryGetValue(Key(player),out var assigned)&&Vector3.Distance(assigned,target)<.1f)deployingSeekers.Remove(Key(player));
    }
    void BeginFinalThirty()
    {
        finalThirtyTriggered=true;StoreNormalItems();if(!Plugin.FinalThirtySpeakers.Value){status="Final 30 seconds: normal items stored; speakers disabled.";Plugin.Logger.LogInfo("[ROUND] "+status);return;}var remainingHiders=new List<PlayerCharacter>();foreach(var player in Participants())if(hiders.Contains(Key(player))&&!caught.Contains(Key(player)))remainingHiders.Add(player);Shuffle(remainingHiders);
        var available=new List<Prop>();foreach(var speaker in speakers)if(speaker!=null)available.Add(speaker);ShuffleProps(available);
        for(int i=0;i<remainingHiders.Count&&i<available.Count;i++){lockedSpeakerAssignments[Key(remainingHiders[i])]=available[i];GiveHeldItem(remainingHiders[i],available[i]);}
        status=$"Final 30 seconds: normal items stored; {lockedSpeakerAssignments.Count} remaining hider(s) locked to speakers.";Plugin.Logger.LogInfo("[ROUND] "+status);
    }
    void MaintainLockedSpeakers()
    {
        if(!finalThirtyTriggered||lockedSpeakerAssignments.Count==0)return;var remove=new List<string>();
        foreach(var pair in lockedSpeakerAssignments){PlayerCharacter player=null;foreach(var candidate in Players())if(Key(candidate)==pair.Key){player=candidate;break;}if(caught.Contains(pair.Key)){remove.Add(pair.Key);continue;}if(player==null)continue;if(player.hands?.heldProp!=pair.Value)GiveHeldItem(player,pair.Value);}
        foreach(var key in remove)ReleaseLockedSpeaker(key);
    }
    void ReleaseLockedSpeaker(string key)
    {
        if(!lockedSpeakerAssignments.TryGetValue(key,out var speaker))return;lockedSpeakerAssignments.Remove(key);if(speaker!=null)PlaceLoose(speaker,MarkerStorage()+new Vector3(lockedSpeakerAssignments.Count*.5f,2f,0f),Quaternion.identity);
    }
    void StoreNormalItems(){int index=0;foreach(var item in NormalItems())if(item!=null)PlaceLoose(item,MarkerStorage()+new Vector3(index++*.5f,0f,1f),Quaternion.identity);normalItemAssignments.Clear();}
    void MaintainRequiredItems()
    {
        foreach(var player in Players())EnforceExclusiveHands(player);MaintainSeekerGear();MaintainHiderItems();MaintainLockedSpeakers();
    }
    void MaintainSeekerGear()
    {
        foreach(var pair in seekerBeltAssignments){var player=FindPlayer(pair.Key);if(player==null||pair.Value==null||propRefreshes.ContainsKey(pair.Value))continue;if(player.registry?.holsterPocket?.pinnedProp!=pair.Value)EquipBelt(player,pair.Value);}
        foreach(var pair in seekerBellAssignments){var player=FindPlayer(pair.Key);if(player==null||pair.Value==null||propRefreshes.ContainsKey(pair.Value))continue;var belt=player.registry?.holsterPocket?.pinnedProp;if(belt==null||belt.childPropHomes==null||belt.childPropHomes.Count==0||belt.childPropHomes[0].pinnedProp!=pair.Value)Stow(player,pair.Value);}
    }
    void ProcessPickupCorrections()
    {
        if(pickupCorrectionUntil.Count==0)return;
        foreach(var pair in new Dictionary<PlayerNetworking,float>(pickupCorrectionUntil))
            if(pair.Key==null||Time.unscaledTime>=pair.Value)pickupCorrectionUntil.Remove(pair.Key);
    }
    void MaintainHiderItems()
    {
        if(Plugin.RandomItemChance.Value<=0f||finalThirtyTriggered)return;
        foreach(var pair in new List<KeyValuePair<string,Prop>>(normalItemAssignments))
        {
            if(!normalItemAssignments.TryGetValue(pair.Key,out var current)||current!=pair.Value)continue;
            var owner=FindPlayer(pair.Key);if(owner==null||pickupCorrectionUntil.ContainsKey(owner.playerNetworking)||propRefreshes.ContainsKey(pair.Value))continue;if(caught.Contains(pair.Key)){normalItemAssignments.Remove(pair.Key);PlaceLoose(pair.Value,MarkerStorage(),Quaternion.identity);continue;}
            if(owner.hands?.heldProp==pair.Value)continue;
            var holder=FindHolder(pair.Value);
            if(holder!=null&&hiders.Contains(Key(holder))&&!caught.Contains(Key(holder)))
            {
                string holderKey=Key(holder);if(normalItemAssignments.ContainsKey(holderKey)||lockedSpeakerAssignments.ContainsKey(holderKey)){GiveHeldItem(owner,pair.Value);continue;}
                normalItemAssignments.Remove(pair.Key);normalItemAssignments[holderKey]=pair.Value;continue;
            }
            GiveHeldItem(owner,pair.Value);
        }
    }
    static PlayerCharacter FindPlayer(string key){foreach(var player in Players())if(Key(player)==key)return player;return null;}
    static PlayerCharacter FindHolder(Prop prop){foreach(var player in Players())if(player.hands?.heldProp==prop)return player;return null;}
    bool CheckNoHidersRemain()
    {
        if((phase!=Phase.SettingUp&&phase!=Phase.Hiding&&phase!=Phase.Seeking)||hiders.Count==0)return false;
        foreach(var key in hiders)if(!caught.Contains(key)&&(FindPlayer(key)!=null||(disconnectDeadlines.TryGetValue(key,out var deadline)&&Time.unscaledTime<deadline)))return false;
        EndRound("NO HIDERS LEFT - SEEKERS WIN");return true;
    }
    void EndRound(string message)
    {
        if(phase==Phase.Ended)return;
        string boardMessage=message;
        if(message.IndexOf("SEEKERS WIN",StringComparison.Ordinal)>=0)
        {
            int secondsLeft=phase==Phase.Seeking?Mathf.Max(0,Mathf.CeilToInt(remaining)):Mathf.Max(0,Mathf.CeilToInt(Plugin.RoundTime.Value));
            boardMessage=$"SEEKERS WIN\nWITH ONLY {secondsLeft} SECONDS LEFT";
        }
        else if(message.IndexOf("HIDERS WIN",StringComparison.Ordinal)>=0)boardMessage="HIDERS WIN";
        recoveryAttempts.Clear();
        ReturnBoundaryBuoys(0,playBorder.Count+seekerBorder.Count+endBorder.Count);
        phase=Phase.Ended;seekerDeployment=false;deployingSeekers.Clear();remaining=0;SetBoard(seekerBoard,boardMessage);SetBoard(endBoard,boardMessage);SetBoard(statusBoard,boardMessage);
        foreach(var player in Participants()){var key=Key(player);if(caught.Contains(key))continue;var target=ReserveSpawn(key,endSpawnSlots);roundSpawnPositions[key]=target;QueueTransport(player,target,"round ended");}
        ReturnAllWarningBuoys();ParkIdleWorkers();cleanupItemReleaseAt=-1f;cleanupPending=true;status=message;
    }
    void HandleConnections()
    {
        var live=new HashSet<string>();foreach(var p in Participants())live.Add(Key(p));
        foreach(var key in known)if(!live.Contains(key)&&disconnected.Add(key)){disconnectDeadlines[key]=Time.unscaledTime+5f;ReleaseHeldAssignment(key);moddedClients.Remove(key);clientTransports.Remove(key);Plugin.Logger.LogInfo("[LEAVE] preserving round state for "+key);}
        foreach(var p in Participants())
        {
            string key=Key(p);
            if(known.Contains(key))
            {
                bool replaced=roundPlayers.TryGetValue(key,out var previous)&&previous!=p;roundPlayers[key]=p;
                if(!disconnected.Remove(key)&&!replaced)continue;
                disconnectDeadlines.Remove(key);setupConfirmed.Remove(key);setupItemsGiven.Remove(key);TeleportReturningPlayer(p,key);Plugin.Logger.LogInfo($"[REJOIN] {key} restored as {(seekers.Contains(key)?"seeker":caught.Contains(key)?"caught":"hider")} during {phase}");continue;
            }
            known.Add(key);roundPlayers[key]=p;hiders.Add(key);var spawn=(phase==Phase.SettingUp||phase==Phase.Hiding)?ReserveSpawn(key,hiderSpawnSlots):Vector3.zero;
            if(phase==Phase.SettingUp||phase==Phase.Hiding){roundSpawnPositions[key]=spawn;Teleport(p,spawn);Plugin.Logger.LogInfo("[JOIN] new hider "+key);}
            else{caught.Add(key);var target=ReserveSpawn(key,endSpawnSlots);roundSpawnPositions[key]=target;Teleport(p,target);Plugin.Logger.LogInfo("[JOIN] new player sent to end area "+key);}
        }
    }
    void TeleportReturningPlayer(PlayerCharacter player,string key)
    {
        Vector3 target;
        if(phase==Phase.Ended||caught.Contains(key))target=ReserveSpawn(key,endSpawnSlots);
        else if(seekers.Contains(key)&&(phase==Phase.SettingUp||phase==Phase.Hiding))target=ReserveSpawn(key,seekerSpawnSlots);
        else target=ReserveSpawn(key,hiderSpawnSlots);
        roundSpawnPositions[key]=target;Teleport(player,target);
    }
    void ReleaseHeldAssignment(string key)
    {
        if(normalItemAssignments.TryGetValue(key,out var normal)&&normal!=null&&FindHolder(normal)!=null)PlaceLoose(normal,MarkerStorage(),Quaternion.identity);
        if(lockedSpeakerAssignments.TryGetValue(key,out var speaker)&&speaker!=null&&FindHolder(speaker)!=null)PlaceLoose(speaker,MarkerStorage(),Quaternion.identity);
    }
    void MaintainCorpses()
    {
        if(Corpse.allCorpses==null||!hasEndSpawn||endBorder.Count<4)return;
        foreach(var corpse in Corpse.allCorpses)
        {
            if(corpse==null||string.IsNullOrWhiteSpace(corpse.identifier)||corpse.prop==null)continue;if(corpse.identifier.StartsWith("HNS_DUMMY_",StringComparison.Ordinal))continue;string key=NormalizeKey(corpse.identifier);if(!known.Contains(key))continue;
            if(!trackedCorpses.TryGetValue(key,out var tracked)||tracked!=corpse)
            {
                var target=ChooseCorpseTarget();if(float.IsNaN(target.x))continue;trackedCorpses[key]=corpse;corpseTargets[key]=target;PlaceLoose(corpse.prop,target,Quaternion.identity);Plugin.Logger.LogInfo($"[CORPSE] moved inside end area {target}: {key}");
            }
            else if(corpseTargets.TryGetValue(key,out var home)&&((endBorder.Count>=4&&!InsideBorder(corpse.prop.transform.position,endBorder))||corpse.prop.transform.position.y<home.y-5f))PlaceLoose(corpse.prop,home,Quaternion.identity);
        }
    }
    Vector3 ChooseCorpseTarget()
    {
        if(corpsePlacementCandidates==null)
        {
        corpsePlacementCandidates=new List<Vector3>();
        Vector3 middle=Vector3.zero;foreach(var point in endBorder)middle+=point;middle/=endBorder.Count;
        foreach(var edge in endBorder)for(int step=1;step<=4;step++)
        {
            var candidate=Vector3.Lerp(middle,edge,step*.18f);
            if(!InsideBorder(candidate,endBorder))continue;
            var grounded=SafeGround(candidate,.6f);
            if(float.IsNaN(grounded.x)||!InsideBorder(grounded,endBorder)||DistanceToBorder(grounded,endBorder)<1f)continue;
            float clearance=float.PositiveInfinity;
            foreach(var spawn in endSpawnSlots)clearance=Mathf.Min(clearance,HorizontalDistanceSquared(grounded,spawn));
            if(clearance>=4f)corpsePlacementCandidates.Add(grounded);
        }
        }
        Vector3 best=new Vector3(float.NaN,float.NaN,float.NaN);float bestClearance=4f;
        foreach(var candidate in corpsePlacementCandidates)
        {
            float clearance=float.PositiveInfinity;
            foreach(var occupied in corpseTargets.Values)clearance=Mathf.Min(clearance,HorizontalDistanceSquared(candidate,occupied));
            if(clearance>=bestClearance){bestClearance=clearance;best=candidate;}
        }
        return best;
    }
    static float HorizontalDistanceSquared(Vector3 a,Vector3 b){float x=a.x-b.x,z=a.z-b.z;return x*x+z*z;}
    bool HasLiveCorpse(string key)=>trackedCorpses.TryGetValue(key,out var corpse)&&corpse!=null&&corpse.prop!=null;
    void PrepareRoundItems(List<PlayerCharacter> people)
    {
        BuildRoundItemPool();var items=NormalItems();
        foreach(var player in people){string key=Key(player);if(!seekers.Contains(key))TryAssignRandomHiderItem(key);}
        int storedItems=0;foreach(var item in items)PlaceLoose(item,MarkerStorage()+new Vector3(storedItems++*.5f,0f,0f),Quaternion.identity);
        for(int i=0;i<walkies.Count;i++)PlaceLoose(walkies[i],MarkerStorage()+new Vector3(i*.5f,1.5f,0f),Quaternion.identity);
        var selectedSpeakers=new HashSet<Prop>();foreach(var item in roundItemPool)if(speakers.Contains(item))selectedSpeakers.Add(item);int stored=0;foreach(var speaker in speakers)if(!selectedSpeakers.Contains(speaker))PlaceLoose(speaker,MarkerStorage()+new Vector3(stored++*.5f,1f,0f),Quaternion.identity);
    }
    void BuildRoundItemPool()
    {
        roundItemPool.Clear();AddRandomCategoryItems(megaphones,Plugin.EnableMegaphones.Value?Plugin.MegaphoneItemCount.Value:0);AddRandomCategoryItems(flares,Plugin.EnableFlares.Value?Plugin.FlareItemCount.Value:0);AddRandomCategoryItems(xrayGoggles,Plugin.EnableGoggles.Value?Plugin.GogglesItemCount.Value:0);AddRandomCategoryItems(speakers,Plugin.EnableSpeakers.Value?Mathf.Min(10,Plugin.SpeakerItemCount.Value):0);
    }
    void AddRandomCategoryItems(List<Prop> source,int wanted){var available=new List<Prop>();foreach(var prop in source)if(prop!=null)available.Add(prop);ShuffleProps(available);for(int i=0;i<available.Count&&i<Mathf.Max(0,wanted);i++)roundItemPool.Add(available[i]);}
    List<Prop> NormalItems()
    {
        return new List<Prop>(roundItemPool);
    }
    void TryAssignRandomHiderItem(string key)
    {
        if(UnityEngine.Random.value>=Mathf.Clamp01(Plugin.RandomItemChance.Value/100f))return;var categories=AvailableItemCategories();if(categories.Count==0)return;
        string category=categories[UnityEngine.Random.Range(0,categories.Count)];var choices=CategoryItems(category);if(choices.Count==0)return;
        normalItemAssignments[key]=choices[UnityEngine.Random.Range(0,choices.Count)];
    }
    List<string> AvailableItemCategories()
    {
        var result=new List<string>();if(CategoryItems("megaphone").Count>0)result.Add("megaphone");if(CategoryItems("flare").Count>0)result.Add("flare");if(CategoryItems("goggles").Count>0)result.Add("goggles");if(CategoryItems("speaker").Count>0)result.Add("speaker");return result;
    }
    List<Prop> CategoryItems(string category)
    {
        var result=new List<Prop>();foreach(var item in roundItemPool)if(!normalItemAssignments.ContainsValue(item)&&((category=="megaphone"&&megaphones.Contains(item))||(category=="flare"&&flares.Contains(item))||(category=="goggles"&&xrayGoggles.Contains(item))||(category=="speaker"&&speakers.Contains(item))))result.Add(item);return result;
    }
    static void AddUnique(List<Prop> result,HashSet<Prop> seen,List<Prop> source){foreach(var prop in source)if(prop!=null&&seen.Add(prop))result.Add(prop);}
    void EquipAfterConfirmedTeleport(PlayerCharacter player)
    {
        if(player==null||!setupItemsGiven.Add(Key(player)))return;
        string key=Key(player);
        if(seekers.Contains(key)){int index=0;foreach(var candidate in Participants()){if(!seekers.Contains(Key(candidate)))continue;if(candidate==player)break;index++;}if(index<belts.Count){seekerBeltAssignments[key]=belts[index];EquipBelt(player,belts[index]);}if(index<bells.Count){seekerBellAssignments[key]=bells[index];Stow(player,bells[index]);}return;}
        if(Plugin.RandomItemChance.Value>0f&&hiders.Contains(key)&&!caught.Contains(key))
        {
            if(!normalItemAssignments.TryGetValue(key,out var item)){TryAssignRandomHiderItem(key);normalItemAssignments.TryGetValue(key,out item);}
            if(item!=null)GiveHeldItem(player,item);
        }
    }
    static void GiveHeldItem(PlayerCharacter player,Prop item)
    {
        if(player?.playerNetworking==null||item==null||Instance.propRefreshes.ContainsKey(item))return;
        var networking=player.playerNetworking;
        if(player.hands?.heldCharacter!=null)networking.UserCode_CmdDropHeldPlayer();
        if(player.hands?.heldProp!=null)StopUsingHeldProp(player,player.hands.heldProp);
        networking.ServerDropPropAutomatic(true);BroadcastSnapshot(networking);
        if(ReleaseProp(item)){networking.ServerPickUpPropAutomatic(item);BroadcastSnapshot(item);BroadcastSnapshot(networking);}
    }
    static void ShuffleProps(List<Prop> list){for(int i=list.Count-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);var value=list[i];list[i]=list[j];list[j]=value;}}
    void EquipBelt(PlayerCharacter p,Prop belt){if(p==null||belt==null||p.registry?.holsterPocket==null)return;var home=p.registry.holsterPocket;if(home.pinnedProp==belt)return;if(home.pinnedProp!=null||!home.IsSafeToPlace(belt)||!ReleaseProp(belt))return;belt.ServerSetPinned(home);}
    void Stow(PlayerCharacter p,Prop item){var belt=p?.registry?.holsterPocket?.pinnedProp;if(belt==null||item==null||belt.childPropHomes==null||belt.childPropHomes.Count==0)return;var home=belt.childPropHomes[0];if(home!=null&&home.pinnedProp==null&&home.IsSafeToPlace(item)&&ReleaseProp(item))item.ServerSetPinned(home);}
    static bool ReleaseProp(Prop prop)
    {
        if(prop==null||!NetworkServer.active)return false;
        foreach(var player in Players())if(player.hands!=null&&player.hands.heldProp==prop){StopUsingHeldProp(player,prop);player.playerNetworking.ServerDropPropAutomatic();}
        if(prop.exclusiveHolder!=null)return false;
        if(prop.currentHome!=null)prop.ServerSetUnpinned();
        return prop.currentHome==null;
    }
    void DetectCaptures()
    {
        foreach(var seeker in Players())
        {
            string seekerKey=Key(seeker);if(!seekers.Contains(seekerKey))continue;
            if(seeker.hands?.heldCharacter!=null)
            {
                var victim=seeker.hands.heldCharacter;string victimKey=Key(victim);if(hiders.Contains(victimKey)&&!caught.Contains(victimKey)){seeker.hands.ProcessDrop();CaptureHider(seeker,victim,"normal pickup");}
            }
            bool pointing=seeker.playerNetworking!=null&&seeker.playerNetworking.leftArmPointing;
            if(seeker.playerNetworking!=null&&seeker.playerNetworking.isLocalPlayer&&seeker.decisions!=null)pointing|=seeker.decisions.leftHandIsPointing;
            if(!pointing)continue;
            TryLowCeilingCapture(seeker,"synchronized point state");
        }
        CheckAllCaught();
    }
    void TryLowCeilingCapture(PlayerCharacter seeker,string signal)
    {
        if(seeker==null||phase!=Phase.Seeking||seekerDeployment||!seekers.Contains(Key(seeker))||seeker.gestures==null)return;
        bool overhead=HasOverheadObstruction(seeker);if(!overhead)return;
        var target=PointedNearbyHider(seeker);if(target!=null){CaptureHider(seeker,target,"low-ceiling left-arm point");CheckAllCaught();}
        
    }
    static bool HasOverheadObstruction(PlayerCharacter seeker)
    {
        var gestures=seeker?.gestures;
        if(gestures==null||seeker.kernal==null)return false;
        // Root-relative origin stays fixed while crouching; use vanilla ceiling mask and character-up direction.
        Vector3 direction=seeker.kernal.up;
        Vector3 origin=seeker.transform.position+direction*.2f;
        const float distance=1.95f; // 2.15m root height minus the fixed 0.2m ray origin.
        if(!Physics.Raycast(origin,direction,out var hit,distance,gestures.raiseMask.value,QueryTriggerInteraction.Ignore))return false;
        var collider=hit.collider;
        return collider!=null&&!collider.transform.IsChildOf(seeker.transform)&&collider.GetComponentInParent<PlayerCharacter>()==null
            &&Vector3.Dot(hit.point-origin,direction)>0f&&Vector3.Dot(hit.normal,direction)<-.2f;
    }
    PlayerCharacter PointedNearbyHider(PlayerCharacter seeker)
    {
        var caster=seeker?.caster;var camera=seeker?.cameraTransform;var gestures=seeker?.gestures;if(caster==null||camera==null||gestures==null)return null;
        bool local=seeker.playerNetworking!=null&&seeker.playerNetworking.isLocalPlayer;Transform aim=camera;
        Vector3 origin=CaptureAimOrigin(aim,local);Vector3 direction=aim.forward.normalized;const float reach=1f;
        PlayerCharacter target=null;
        var hitObject=caster.CastThroughHands(new Ray(origin,direction),reach);target=hitObject?.GetComponentInParent<PlayerCharacter>();
        if(target==null)target=NearbyHiderOnPointRay(seeker,origin,direction,reach);
        if(target==null||target==seeker)return null;Vector3 targetPoint=target.cameraTransform!=null?target.cameraTransform.position:target.transform.position+Vector3.up;float targetAlong=Vector3.Dot(targetPoint-origin,direction);if(targetAlong<=0f||targetAlong>reach)return null;
        string key=Key(target);return hiders.Contains(key)&&!caught.Contains(key)?target:null;
    }
    static Vector3 CaptureAimOrigin(Transform aim,bool local){return aim.position+(local?Vector3.zero:aim.right*-.2f);}
    PlayerCharacter NearbyHiderOnPointRay(PlayerCharacter seeker,Vector3 origin,Vector3 direction,float reach)
    {
        PlayerCharacter best=null;float bestAlong=float.MaxValue;
        foreach(var candidate in Players())
        {
            string key=Key(candidate);if(candidate==null||candidate==seeker||!hiders.Contains(key)||caught.Contains(key))continue;
            Vector3 point=candidate.cameraTransform!=null?candidate.cameraTransform.position:candidate.transform.position+Vector3.up;Vector3 delta=point-origin;float along=Vector3.Dot(delta,direction);
            if(along<=0f||along>reach)continue;float miss=(delta-direction*along).magnitude;if(miss>.05f||along>=bestAlong)continue;
            Vector3 rayDirection=delta.normalized;if(Physics.Raycast(origin,rayDirection,out var hit,delta.magnitude+.25f,casterMask(seeker)))
            {
                var firstPlayer=hit.collider?.GetComponentInParent<PlayerCharacter>();if(firstPlayer!=candidate)continue;
            }
            best=candidate;bestAlong=along;
        }
        return best;
    }
    static int casterMask(PlayerCharacter player){return player?.caster!=null?player.caster.layerMask.value:Physics.DefaultRaycastLayers;}
    void CaptureHider(PlayerCharacter seeker,PlayerCharacter victim,string method)
    {
        if(victim==null)return;string key=Key(victim);if(!hiders.Contains(key)||caught.Contains(key))return;caught.Add(key);ReturnWarningBuoy(key);ReleaseLockedSpeaker(key);var target=ReserveSpawn(key,endSpawnSlots);roundSpawnPositions[key]=target;QueueTransport(victim,target,"caught");Plugin.Logger.LogInfo($"[CAPTURE] {Key(seeker)} -> {key} via {method}; transport queued");
    }
    void CheckAllCaught(){if(hiders.Count>0&&caught.Count>=hiders.Count)EndRound("ALL CAUGHT - SEEKERS WIN");}
    void EnforceBorders()
    {
        if(!enforceBorder)return;foreach(var p in Players()){var key=Key(p);if(IsTransporting(p)||(!hiders.Contains(key)&&!seekers.Contains(key)))continue;
            List<Vector3> zone,recoveries;
            if(caught.Contains(key)){zone=endBorder;recoveries=endSpawnSlots;}
            else if(phase==Phase.Hiding&&seekers.Contains(key)){zone=seekerBorder;recoveries=seekerSpawnSlots;}
            else{zone=playBorder;recoveries=playRecoveryPoints;}
            if(zone.Count<4)continue;var position=p.transform.position;bool inside=InsideBorder(position,zone);float edgeDistance=DistanceToBorder(position,zone);
            bool allowWarning=zone!=playBorder||ActiveAreaUsesEnforcementLights();if(allowWarning)UpdateWarningBuoy(p,edgeDistance<=3f,zone);else ReturnWarningBuoy(key);
            // Border is horizontal only. Falling or moving between map elevations is allowed.
            if(inside){recoveryAttempts.Remove(key);continue;}
            if(borderCooldown.TryGetValue(key,out var until)&&Time.unscaledTime<until)continue;
            if(!TryNearestRecovery(position,recoveries,zone,out var returnTarget)){borderCooldown[key]=Time.unscaledTime+1f;continue;}
            if(!AllowRecovery(key))continue;
            borderCooldown[key]=Time.unscaledTime+.1f;ReturnWarningBuoy(key);Teleport(p,returnTarget,false,true);Plugin.Logger.LogInfo("[BORDER] returning "+key+" to safe point "+returnTarget);}
    }
    internal static void NoteBrushPickup(PlayerHeldInformation held)
    {
        if(!ActiveHost||Instance==null||!held.hasProp)return;var prop=held.GetProp();
        if(prop!=null&&prop.name.StartsWith("SalonBrushProp",StringComparison.Ordinal))Instance.brushPickupGraceUntil[prop]=Time.unscaledTime+2f;
    }
    bool AllowRecovery(string key)
    {
        recoveryAttempts.TryGetValue(key,out var count);
        if(count>=3)return false;
        recoveryAttempts[key]=count+1;
        if(count==2)Plugin.Logger.LogWarning("[BORDER] Third consecutive recovery for "+key+"; further automatic attempts paused until observed inside. Check saved recovery points.");
        return true;
    }
    static bool TryNearestRecovery(Vector3 position,List<Vector3> recoveries,List<Vector3> zone,out Vector3 target)
    {
        target=default;float best=float.PositiveInfinity;bool found=false;bool interior=false;
        foreach(var recovery in recoveries){if(zone.Count>=4&&!InsideBorder(recovery,zone))continue;bool safe=zone.Count<4||DistanceToBorder(recovery,zone)>=1.5f;if(interior&&!safe)continue;float distance=(recovery-position).sqrMagnitude;if(safe&&!interior){best=float.PositiveInfinity;interior=true;}if(distance>=best)continue;best=distance;target=recovery;found=true;}
        return found;
    }
    void UpdateWarningBuoy(PlayerCharacter player,bool show,List<Vector3> zone)
    {
        string key=Key(player);
        if(!show){ReturnWarningBuoy(key);return;}
        if(!warningBuoys.TryGetValue(key,out var group))
        {
            group=new List<Prop>();warningBuoys[key]=group;
        }
        group.RemoveAll(p=>p==null);
        ClaimBuoys(StaticBorderCount()+Mathf.Max(1,Participants().Count));
        var used=new HashSet<Prop>();foreach(var existing in warningBuoys.Values)foreach(var prop in existing)if(prop!=null)used.Add(prop);
        const int desired=1;
        while(group.Count>desired){var extra=group[group.Count-1];group.RemoveAt(group.Count-1);markerPositions.Remove(extra);markerRotations.Remove(extra);PlaceLoose(extra,MarkerStorage(),Quaternion.identity);}
        for(int i=StaticBorderCount();i<buoys.Count&&group.Count<desired;i++)if(buoys[i]!=null&&!used.Contains(buoys[i])){group.Add(buoys[i]);used.Add(buoys[i]);SetState(buoys[i],1);}
        if(group.Count==0)return;
        var wanted=ClosestBorderPoint(player.transform.position,zone);wanted.y=player.transform.position.y+.7f;var tangent=ClosestBorderDirection(player.transform.position,zone);
        for(int i=0;i<group.Count;i++){var position=wanted+tangent*((i-(group.Count-1)*.5f)*.6f);markerPositions[group[i]]=position;markerRotations[group[i]]=Quaternion.identity;PlaceStatic(group[i],position,Quaternion.identity);}
    }
    static bool InsideBorder(Vector3 point,List<Vector3> zone)
    {
        bool inside=false;for(int i=0,j=zone.Count-1;i<zone.Count;j=i++)
        {
            var a=zone[i];var b=zone[j];bool crosses=(a.z>point.z)!=(b.z>point.z)&&point.x<(b.x-a.x)*(point.z-a.z)/(b.z-a.z)+a.x;if(crosses)inside=!inside;
        }
        return inside;
    }
    static float DistanceToBorder(Vector3 point,List<Vector3> zone){float best=float.MaxValue;for(int i=0;i<zone.Count;i++){var closest=ClosestPointXZ(point,zone[i],zone[(i+1)%zone.Count]);best=Mathf.Min(best,Vector2.Distance(new Vector2(point.x,point.z),new Vector2(closest.x,closest.z)));}return best;}
    static Vector3 ClosestBorderPoint(Vector3 point,List<Vector3> zone){float best=float.MaxValue;Vector3 result=point;for(int i=0;i<zone.Count;i++){var candidate=ClosestPointXZ(point,zone[i],zone[(i+1)%zone.Count]);float distance=(new Vector2(point.x-candidate.x,point.z-candidate.z)).sqrMagnitude;if(distance<best){best=distance;result=candidate;}}return result;}
    static Vector3 ClosestBorderDirection(Vector3 point,List<Vector3> zone)
    {
        float best=float.MaxValue;Vector3 direction=Vector3.right;
        for(int i=0;i<zone.Count;i++){var a=zone[i];var b=zone[(i+1)%zone.Count];var candidate=ClosestPointXZ(point,a,b);float distance=new Vector2(point.x-candidate.x,point.z-candidate.z).sqrMagnitude;if(distance<best){best=distance;direction=new Vector3(b.x-a.x,0f,b.z-a.z).normalized;}}
        return direction.sqrMagnitude>.01f?direction:Vector3.right;
    }
    static Vector3 ClosestPointXZ(Vector3 point,Vector3 a,Vector3 b){var ab=new Vector2(b.x-a.x,b.z-a.z);float denominator=ab.sqrMagnitude;if(denominator<.001f)return a;float t=Mathf.Clamp01(Vector2.Dot(new Vector2(point.x-a.x,point.z-a.z),ab)/denominator);return new Vector3(Mathf.Lerp(a.x,b.x,t),Mathf.Lerp(a.y,b.y,t),Mathf.Lerp(a.z,b.z,t));}
    void ReturnWarningBuoy(string key)
    {
        if(!warningBuoys.TryGetValue(key,out var group))return;warningBuoys.Remove(key);for(int i=0;i<group.Count;i++){var buoy=group[i];if(buoy==null)continue;markerPositions.Remove(buoy);markerRotations.Remove(buoy);PlaceLoose(buoy,MarkerStorage()+new Vector3(WarningBuoyCount()+i,0f,0f),Quaternion.identity);}
    }
    void ReturnAllWarningBuoys(){var keys=new List<string>(warningBuoys.Keys);foreach(var key in keys)ReturnWarningBuoy(key);}
    Vector3 RandomHiderSpawn()
    {
        float inside=Mathf.Max(2f,Plugin.Radius.Value-Plugin.ReturnInset.Value);
        var flat=center+new Vector3(UnityEngine.Random.Range(-inside,inside),0f,UnityEngine.Random.Range(-inside,inside));
        float highest=float.NegativeInfinity;
        foreach(var hit in Physics.RaycastAll(new Vector3(flat.x,center.y+500f,flat.z),Vector3.down,1000f,-1,QueryTriggerInteraction.Ignore))
        {
            if(hit.collider==null||hit.normal.y<.6f||hit.collider.GetComponentInParent<Prop>()!=null||hit.collider.GetComponentInParent<PlayerCharacter>()!=null)continue;
            if(hit.collider.name.IndexOf("water",StringComparison.OrdinalIgnoreCase)>=0)continue;
            if(hit.point.y>highest)highest=hit.point.y;
        }
        flat.y=(float.IsNegativeInfinity(highest)?center.y:highest)+Plugin.HiderHeight.Value;
        return flat;
    }
    bool ValidateSpawnSlots(List<Vector3> slots,string area)=>true;
    Vector3 ReserveSpawn(string key,List<Vector3> slots)
    {
        if(!spawnReservations.TryGetValue(slots,out var reserved)){reserved=new Dictionary<string,int>();spawnReservations[slots]=reserved;}
        var live=new HashSet<string>();foreach(var player in Participants())live.Add(Key(player));
        if(reserved.TryGetValue(key,out var index)&&index<slots.Count)return slots[index];
        for(int i=0;i<slots.Count;i++)if(!reserved.ContainsValue(i)){reserved[key]=i;return slots[i];}
        foreach(var owner in new List<string>(reserved.Keys))if(!live.Contains(owner)){int available=reserved[owner];reserved.Remove(owner);reserved[key]=available;return slots[available];}
        status="No free spawn available. Waiting for a slot.";
        return new Vector3(float.NaN,float.NaN,float.NaN);
    }
    static Vector3 SlotOrFallback(List<Vector3> slots,int index,Vector3 fallback)=>slots.Count==0?fallback:slots[Mathf.Abs(index)%slots.Count];
    void PositionMarkers()
    {
        if(!hasSeeker||!hasEndSpawn)return;
        ReturnAllWarningBuoys();
        markerPositions.Clear();markerRotations.Clear();borderDriftChecks.Clear();
        if(hasSeekerSign)PinMarker(seekerBoardProp,seekerSignPosition,Quaternion.Euler(0f,seekerSignYaw,0f));else PinGroundMarker(seekerBoardProp,seekerSpawn+Vector3.forward*2f,Quaternion.LookRotation(Vector3.back),1.7f);
        if(hasEndSign)PinMarker(endBoardProp,endSignPosition,Quaternion.Euler(0f,endSignYaw,0f));else PinGroundMarker(endBoardProp,endSpawn+Vector3.forward*2f,Quaternion.LookRotation(Vector3.back),1.7f);
        if(showSpawnDebug&&hasDraftStatusSign)PinMarker(statusBoardProp,draftStatusSignPosition,Quaternion.Euler(0f,draftStatusSignYaw,0f));else if(hasStatusSign)PinMarker(statusBoardProp,statusSignPosition,Quaternion.Euler(0f,statusSignYaw,0f));
        PositionInstructionSigns();
        if(!hasCenter)return;
        PositionBuoyBoundaries();
    }
    void PositionInstructionSigns()
    {
        ClaimInstructionSigns();
        int count=Mathf.Min(instructionSignPositions.Count,instructionSignProps.Count);
        for(int i=0;i<count;i++)
        {
            var rotation=Quaternion.Euler(0f,instructionSignYaws[i],0f);
            PinMarker(instructionSignProps[i],instructionSignPositions[i],rotation);
            if(i<instructionSignBoards.Count)SetBoard(instructionSignBoards[i],Plugin.InstructionSignText.Value);
        }
    }
    void PositionBuoyBoundaries()
    {
        int index=0;if(phase!=Phase.Idle&&phase!=Phase.Ended){index=PositionBorder(playBorder,index);index=PositionBorder(seekerBorder,index);index=PositionBorder(endBorder,index);}if(spawnDraftLights)PositionBorder(borderPoints,index);
    }
    int PositionBorder(List<Vector3> zone,int start)
    {
        int count=Mathf.Min(zone.Count,buoys.Count-start);
        for(int i=0;i<count;i++){PinMarker(buoys[start+i],zone[i],Quaternion.identity);SetState(buoys[start+i],1);}
        return start+count;
    }
    void ReturnBoundaryBuoys(int start,int count)
    {
        int end=Mathf.Min(buoys.Count,start+count);for(int i=Mathf.Max(0,start);i<end;i++){var prop=buoys[i];if(prop==null)continue;markerPositions.Remove(prop);markerRotations.Remove(prop);PlaceLoose(prop,MarkerStorage()+new Vector3((i-start)*.35f,0f,0f),Quaternion.identity);}
    }
    void PinMarker(Prop prop,Vector3 position,Quaternion rotation){if(prop==null)return;markerPositions[prop]=position;markerRotations[prop]=rotation;PlaceStatic(prop,position,rotation);}
    void PinGroundMarker(Prop prop,Vector3 wanted,Quaternion rotation,float lift=.15f){var grounded=SafeGround(wanted,lift);if(!float.IsNaN(grounded.x))PinMarker(prop,grounded,rotation);else Plugin.Logger.LogWarning("[MARKER] no stable ground for "+(prop==null?"null":prop.name)+" near "+wanted);}
    void MaintainMarkers()
    {
        var liveKeys=new HashSet<string>();foreach(var player in Players())liveKeys.Add(Key(player));var stale=new List<string>();foreach(var key in warningBuoys.Keys)if(!liveKeys.Contains(key))stale.Add(key);foreach(var key in stale)ReturnWarningBuoy(key);
        foreach(var pair in markerPositions)
        {
            var prop=pair.Key;if(prop==null)continue;var rotation=markerRotations[prop];
            if(prop==seekerBoardProp){if(hasSeekerSign)MaintainFloatingMarker(prop,pair.Value,rotation);else if(BoardOutsideArea(prop,seekerBorder,pair.Value))PlaceStatic(prop,pair.Value,rotation);continue;}
            if(prop==endBoardProp){if(hasEndSign)MaintainFloatingMarker(prop,pair.Value,rotation);else if(BoardOutsideArea(prop,endBorder,pair.Value))PlaceStatic(prop,pair.Value,rotation);continue;}
            if(prop==statusBoardProp){MaintainFloatingMarker(prop,pair.Value,rotation);continue;}
            if(instructionSignProps.Contains(prop)){MaintainFloatingMarker(prop,pair.Value,rotation);continue;}
            if(IsWarningBuoy(prop)){PlaceStatic(prop,pair.Value,rotation);continue;}
            if(buoys.Contains(prop)){MaintainBorderBuoy(prop,pair.Value,rotation);continue;}
            if(prop.exclusiveHolder!=null||prop.currentHome!=null||Vector3.Distance(prop.transform.position,pair.Value)>.75f||Quaternion.Angle(prop.transform.rotation,rotation)>20f)PlaceStatic(prop,pair.Value,rotation);
        }
        MaintainBrushes();
    }
    void PositionBrushes(){for(int i=0;i<brushes.Count;i++)PlaceLoose(brushes[i],SlotOrFallback(endSpawnSlots,i,endSpawn)+Vector3.up*.5f,Quaternion.identity);}
    void MaintainBrushes()
    {
        for(int i=0;i<brushes.Count;i++){var brush=brushes[i];if(brush==null||brush.exclusiveHolder!=null||FindHolder(brush)!=null||(brushPickupGraceUntil.TryGetValue(brush,out var grace)&&Time.unscaledTime<grace))continue;brushPickupGraceUntil.Remove(brush);bool inside=endBorder.Count>=4?InsideBorder(brush.transform.position,endBorder):HorizontalDistanceSquared(brush.transform.position,endSpawn)<=Plugin.EndRadius.Value*Plugin.EndRadius.Value;if(!inside)PlaceLoose(brush,SlotOrFallback(endSpawnSlots,i,endSpawn)+Vector3.up*.5f,Quaternion.identity);}
    }
    void MaintainFloatingMarker(Prop prop,Vector3 home,Quaternion rotation)
    {
        var current=prop.transform.position;
        if(new Vector2(current.x-home.x,current.z-home.z).magnitude>.25f)PlaceStatic(prop,home,rotation);
    }
    void MaintainBorderBuoy(Prop prop,Vector3 home,Quaternion rotation)
    {
        var current=prop.transform.position;
        float drift=new Vector2(current.x-home.x,current.z-home.z).magnitude;
        if(drift<=.3f){borderDriftChecks[prop]=0;return;}
        int checks=borderDriftChecks.TryGetValue(prop,out var count)?count+1:1;
        borderDriftChecks[prop]=checks;
        if(checks<5)return;
        borderDriftChecks[prop]=0;PlaceStatic(prop,home,rotation);
    }
    bool IsWarningBuoy(Prop prop){foreach(var group in warningBuoys.Values)if(group.Contains(prop))return true;return false;}
    static bool BoardOutsideArea(Prop board,List<Vector3> zone,Vector3 home)=>zone.Count>=4&&(!InsideBorder(board.transform.position,zone)||board.transform.position.y<home.y-5f);
    Vector3 MarkerStorage()=>hasGearStorage?gearStorage:(hasCenter?center+Vector3.down*500f:new Vector3(0f,-500f,0f));
    void EnforceEndArea()
    {
        foreach(var player in Participants())
        {
            var key=Key(player);if(IsTransporting(player)||(borderCooldown.TryGetValue(key,out var until)&&Time.unscaledTime<until))continue;
            bool inside=endBorder.Count>=4?InsideBorder(player.transform.position,endBorder):Mathf.Abs(player.transform.position.x-endSpawn.x)<=Plugin.EndRadius.Value&&Mathf.Abs(player.transform.position.z-endSpawn.z)<=Plugin.EndRadius.Value;
            if(endBorder.Count>=4)UpdateWarningBuoy(player,DistanceToBorder(player.transform.position,endBorder)<=3f,endBorder);
            // End-area border is horizontal only; normal drops inside it are valid movement.
            if(inside){recoveryAttempts.Remove(key);continue;}
            ReturnWarningBuoy(key);
            if(TryNearestRecovery(player.transform.position,endSpawnSlots,endBorder,out var target)&&AllowRecovery(key)){borderCooldown[key]=Time.unscaledTime+.1f;Teleport(player,target,false,true);}
        }
    }
    void UpdateBoards(){int sec=Mathf.Max(0,Mathf.CeilToInt(remaining)),left=ActiveHiderCount();if(phase==Phase.Hiding){SetBoard(seekerBoard,"SEEK IN "+sec);SetBoard(endBoard,"ROUND STARTING");SetBoard(statusBoard,$"HIDE: {sec}\nHIDERS: {left}");}else if(phase==Phase.Seeking){SetBoard(seekerBoard,"SEEK NOW");SetBoard(endBoard,"ROUND ENDS IN "+sec);SetBoard(statusBoard,$"TIME LEFT: {sec}\nHIDERS LEFT: {left}");}}
    int ActiveHiderCount(){int count=0;foreach(var key in hiders)if(!caught.Contains(key)&&FindPlayer(key)!=null)count++;return count;}
    static void SetBoard(PeckEffectTextInput b,string text){if(b!=null&&NetworkServer.active)b.NetworknetworkedText=text;}
    static void SetState(Prop p,int state){if(p==null)return;foreach(var t in p.GetComponentsInChildren<TrackedPeckState>(true))if(t!=null)t.SetState(state);}
    static void PlaceStatic(Prop p,Vector3 pos,Quaternion rot)
    {
        if(!ReleaseProp(p))return;
        p.transform.SetPositionAndRotation(pos,rot);
        if(p.rb!=null){p.rb.isKinematic=false;p.rb.position=pos;p.rb.rotation=rot;p.rb.linearVelocity=Vector3.zero;p.rb.angularVelocity=Vector3.zero;p.rb.useGravity=true;p.rb.WakeUp();}
        if(p.houseNetworkTransform!=null)
        {
            var networkTransform=p.houseNetworkTransform;uint packed=Compression.CompressQuaternion(rot);
            networkTransform.targetPosition=pos;networkTransform.targetRotation=rot;
            networkTransform.ProcessMove(pos,packed,0);networkTransform.RpcMove(pos,packed,0);networkTransform.RpcVelocity(Vector3.zero,Vector3.zero);networkTransform.OwnerUpdate(true);
        }
    }
    void RestoreBorrowedProps()
    {
        foreach(var pair in new Dictionary<PlayerCharacter,Prop>(cleanupManagerItems))
        {
            var manager=pair.Key;var prop=pair.Value;
            if(manager?.playerNetworking!=null){DirectLocalTeleport(manager,MarkerStorage());manager.playerNetworking.ServerDropPropAutomatic(true);BroadcastSnapshot(manager.playerNetworking);}
            if(prop!=null)BroadcastSnapshot(prop);
        }
        cleanupManagerItems.Clear();
        normalItemAssignments.Clear();lockedSpeakerAssignments.Clear();
        foreach(var player in Players())
        {
            if(player.hands?.heldCharacter!=null)player.actions.ActionDropPlayer();
            if(player.hands?.heldProp!=null&&propOrigins.ContainsKey(player.hands.heldProp)){StopUsingHeldProp(player,player.hands.heldProp);player.playerNetworking.ServerDropPropAutomatic();}
        }
        foreach(var pair in propOrigins)
        {
            var prop=pair.Key;if(prop==null)continue;
            if(buoys.Contains(prop)||IsGear(prop)||IsSign(prop))continue;
            if(prop.currentHome!=null)prop.ServerSetUnpinned();
            prop.transform.SetPositionAndRotation(pair.Value.Position,pair.Value.Rotation);
            if(prop.rb!=null){prop.rb.position=pair.Value.Position;prop.rb.rotation=pair.Value.Rotation;if(!pair.Value.Kinematic){prop.rb.isKinematic=false;prop.rb.linearVelocity=Vector3.zero;prop.rb.angularVelocity=Vector3.zero;}prop.rb.useGravity=pair.Value.Gravity;prop.rb.isKinematic=pair.Value.Kinematic;}
            SetState(prop,0);
            if(prop.houseNetworkTransform!=null){prop.houseNetworkTransform.targetPosition=pair.Value.Position;prop.houseNetworkTransform.targetRotation=pair.Value.Rotation;prop.houseNetworkTransform.OwnerUpdate(true);}
        }
        StoreGear();
        Plugin.Logger.LogInfo("[CLEANUP] Gear moved to storage. Signs and border markers retained.");
    }
    void RepeatCleanupSnatch()
    {
        cleanupSnatchAttempts++;
        foreach(var pair in cleanupManagerItems)
        {
            var manager=pair.Key;var prop=pair.Value;
            if(manager?.playerNetworking==null||prop==null)continue;
            try{manager.playerNetworking.ServerPickUpPropAutomatic(prop);}
            catch(Exception ex){Plugin.Logger.LogWarning("[ITEM RELEASE] snatch "+cleanupSnatchAttempts+"/10 failed: "+ex.GetBaseException().Message);}
        }
        if(cleanupSnatchAttempts==10)Plugin.Logger.LogInfo("[ITEM RELEASE] completed 10 snatch attempts; releasing to storage next tick.");
    }
    void PrepareBorrowedPropRelease()
    {
        cleanupManagerItems.Clear();int snatched=0,fallback=0;
        var usedManagers=new HashSet<PlayerCharacter>();
        var usedProps=new HashSet<Prop>();
        var targets=new Dictionary<PlayerCharacter,Prop>();
        foreach(var pair in normalItemAssignments){var player=FindPlayer(pair.Key);if(player!=null&&pair.Value!=null)targets[player]=pair.Value;}
        foreach(var pair in lockedSpeakerAssignments){var player=FindPlayer(pair.Key);if(player!=null&&pair.Value!=null)targets[player]=pair.Value;}
        foreach(var player in Players())if(!IsDummyManager(player)&&player.hands?.heldProp!=null&&propOrigins.ContainsKey(player.hands.heldProp))targets[player]=player.hands.heldProp;
        foreach(var target in targets)
        {
            var player=target.Key;var prop=target.Value;
            if(!usedProps.Add(prop))continue;
            PlayerCharacter manager=null;
            foreach(var candidate in standbyManagers)
                if(candidate!=null&&!usedManagers.Contains(candidate)&&candidate.hands?.heldCharacter==null&&candidate.hands?.heldProp==null){manager=candidate;break;}
            if(manager==null){StopUsingHeldProp(player,prop);fallback++;continue;}
            usedManagers.Add(manager);
            cleanupManagerItems[manager]=prop;
            snatched++;
        }
        Plugin.Logger.LogInfo("[ITEM RELEASE] managers reserved for "+snatched+" held item(s); direct fallback="+fallback+". Performing 10 snatches at 0.05s intervals before storage.");
    }
    bool IsGear(Prop prop)=>megaphones.Contains(prop)||walkies.Contains(prop)||flares.Contains(prop)||xrayGoggles.Contains(prop)||speakers.Contains(prop)||belts.Contains(prop)||bells.Contains(prop)||brushes.Contains(prop);
    bool IsSign(Prop prop)=>prop==seekerBoardProp||prop==endBoardProp||prop==statusBoardProp||instructionSignProps.Contains(prop);
    void StoreGear()
    {
        var storage=hasGearStorage?gearStorage:(hasCenter?center+Vector3.down*500f:new Vector3(0f,-500f,0f));
        var gear=new List<Prop>();gear.AddRange(megaphones);gear.AddRange(walkies);gear.AddRange(flares);gear.AddRange(xrayGoggles);gear.AddRange(speakers);gear.AddRange(belts);gear.AddRange(bells);
        int index=0;foreach(var prop in gear){if(prop==null)continue;PlaceLoose(prop,storage+new Vector3(index%6,(index/6)*.5f,(index%2)*.5f),Quaternion.identity);SetState(prop,0);index++;}
    }
    static void PlaceLoose(Prop prop,Vector3 position,Quaternion rotation)
    {
        if(!ReleaseProp(prop))return;prop.transform.SetPositionAndRotation(position,rotation);
        if(prop.rb!=null){prop.rb.isKinematic=false;prop.rb.position=position;prop.rb.rotation=rotation;prop.rb.linearVelocity=Vector3.zero;prop.rb.angularVelocity=Vector3.zero;prop.rb.useGravity=true;}
        if(prop.houseNetworkTransform!=null){uint packed=Compression.CompressQuaternion(rotation);prop.houseNetworkTransform.targetPosition=position;prop.houseNetworkTransform.targetRotation=rotation;prop.houseNetworkTransform.ProcessMove(position,packed,0);prop.houseNetworkTransform.RpcMove(position,packed,0);prop.houseNetworkTransform.RpcVelocity(Vector3.zero,Vector3.zero);prop.houseNetworkTransform.OwnerUpdate(true);}
    }
    static List<PlayerCharacter> Players()
    {
        var selected=new Dictionary<string,PlayerCharacter>();
        if(PlayerCharacter.allPlayerCharacters!=null)foreach(var p in PlayerCharacter.allPlayerCharacters)
        {
            if(p==null||p.playerNetworking==null||p.playerNetworking.netIdentity==null)continue;
            var identity=p.playerNetworking.netIdentity;
            if(NetworkServer.active)
            {
                if(!NetworkServer.spawned.TryGetValue(identity.netId,out var spawned)||spawned!=identity)continue;
                var connection=identity.connectionToClient;
                if(!IsDummyManager(p)&&(connection==null||connection.identity!=identity))continue;
            }
            string key=Key(p);
            if(!selected.TryGetValue(key,out var previous)||identity.netId>previous.playerNetworking.netIdentity.netId)selected[key]=p;
        }
        return new List<PlayerCharacter>(selected.Values);
    }
    static List<PlayerCharacter> Participants(){var r=Players();r.RemoveAll(IsDummyManager);if(r.Count>11)r.RemoveRange(11,r.Count-11);return r;}
    static PlayerCharacter LocalPlayer(){foreach(var p in Players())if(p.playerNetworking.isLocalPlayer&&!IsDummyManager(p))return p;return null;}
    static string Key(PlayerCharacter p)=>p==null?"null":!string.IsNullOrWhiteSpace(p.playerNetworking?.identifier)?NormalizeKey(p.playerNetworking.identifier):"net:"+p.playerNetworking.netIdentity.netId;
    static string NormalizeKey(string value)=>string.IsNullOrWhiteSpace(value)?"null":value.Trim();
    static Vector3 SafeGround(Vector3 wanted,float lift=1.5f)
    {
        // Search outward from the requested point; reject props, players, water and steep slopes.
        const int mask=-1;
        for(int ring=0;ring<=6;ring++)
        {
            int samples=ring==0?1:16;
            for(int i=0;i<samples;i++)
            {
                float angle=i*Mathf.PI*2f/samples;
                var point=wanted+new Vector3(Mathf.Cos(angle)*ring*2f,0f,Mathf.Sin(angle)*ring*2f);
                var hits=Physics.RaycastAll(point+Vector3.up*250f,Vector3.down,600f,mask,QueryTriggerInteraction.Ignore);
                float best=float.PositiveInfinity;Vector3 candidate=default;bool found=false;
                foreach(var hit in hits)
                {
                    var collider=hit.collider;
                    if(collider==null||hit.normal.y<.75f||collider.GetComponentInParent<Prop>()!=null||collider.GetComponentInParent<PlayerCharacter>()!=null)continue;
                    if(collider.attachedRigidbody!=null&&!collider.attachedRigidbody.isKinematic)continue;
                    if(collider.name.IndexOf("water",StringComparison.OrdinalIgnoreCase)>=0)continue;
                    var feet=hit.point+Vector3.up*.65f;
                    if(Physics.CheckCapsule(feet,hit.point+Vector3.up*2f,.45f,mask,QueryTriggerInteraction.Ignore))continue;
                    float score=Mathf.Abs(hit.point.y-wanted.y);
                    if(score<best){best=score;candidate=hit.point+Vector3.up*Mathf.Max(1.5f,lift);found=true;}
                }
                if(found)return candidate;
            }
        }
        Plugin.Logger.LogWarning("[TELEPORT] No safe ground near "+wanted+"; teleport cancelled.");return new Vector3(float.NaN,float.NaN,float.NaN);
    }
    void Teleport(PlayerCharacter p,Vector3 pos,bool preserveHeight=false,bool borderReturn=false)
    {
        if(p==null||!NetworkServer.active||float.IsNaN(pos.x))return;
        if(p.playerNetworking!=null&&p.playerNetworking.isLocalPlayer)
        {
            DirectLocalTeleport(p,pos);if(Vector3.Distance(p.transform.position,pos)<=1f)ConfirmSeekerDeployment(p,pos);if(borderReturn){borderCooldown[Key(p)]=Time.unscaledTime+.1f;if(TransportDestinationContains(new TransportRequest{Player=p,Target=pos}))recoveryAttempts.Remove(Key(p));}StabilizePlayer(p,borderReturn?.2f:.5f);if(phase==Phase.SettingUp&&roundSpawnPositions.TryGetValue(Key(p),out var setupTarget)&&Vector3.Distance(setupTarget,pos)<.1f){setupConfirmed.Add(Key(p));EquipAfterConfirmedTeleport(p);Plugin.Logger.LogInfo("[TRANSPORT] local setup confirmed "+Key(p));}
        }
        else QueueTransport(p,pos,"round teleport",borderReturn);
    }
    void QueueTransport(PlayerCharacter player,Vector3 target,string reason,bool borderReturn=false)
    {
        if(player==null||float.IsNaN(target.x))return;
        if(player.playerNetworking!=null&&player.playerNetworking.isLocalPlayer){DirectLocalTeleport(player,target);if(Vector3.Distance(player.transform.position,target)<=1f)ConfirmSeekerDeployment(player,target);StabilizePlayer(player,borderReturn?.35f:.5f);if(phase==Phase.SettingUp&&roundSpawnPositions.TryGetValue(Key(player),out var localTarget)&&HorizontalDistanceSquared(localTarget,target)<.01f){setupConfirmed.Add(Key(player));EquipAfterConfirmedTeleport(player);}return;}
        if(moddedClients.ContainsKey(Key(player))){QueueClientTransport(player,target,reason,borderReturn);return;}
        foreach(var queued in priorityTransports)if(queued.Player==player){queued.Target=target;queued.Reason=reason;queued.BorderReturn=borderReturn;return;}
        TransportRequest existing=null;foreach(var queued in transports)if(queued.Player==player){existing=queued;break;}
        if(existing!=null)
        {
            existing.Target=target;existing.Reason=reason;existing.BorderReturn=borderReturn;
            if(!borderReturn){int count=transports.Count;for(int i=0;i<count;i++){var queued=transports.Dequeue();if(queued!=existing)transports.Enqueue(queued);}priorityTransports.Enqueue(existing);}return;
        }
        foreach(var active in activeTransports)if(active.Player==player){active.Target=target;active.Reason=reason;active.BorderReturn=borderReturn;active.Confirmations=0;active.StableSince=0f;return;}
        var request=new TransportRequest{Player=player,Target=target,Reason=reason,BorderReturn=borderReturn};if(borderReturn)transports.Enqueue(request);else priorityTransports.Enqueue(request);
        Plugin.Logger.LogInfo($"[TRANSPORT] queued {Key(player)} -> {target} ({reason})");
    }
    void MaintainModNetworking()
    {
        if(Time.unscaledTime<nextModNetworkCheck)return;nextModNetworkCheck=Time.unscaledTime+.5f;
        try
        {
            if(!NetworkServer.active)serverModHandlerInstalled=false;
            if(!NetworkClient.active){clientModHandlerInstalled=false;modHelloSent=false;modLinkDisabled=false;ClearRemoteModSession();}else if(!NetworkClient.isConnected){modHelloSent=false;ClearRemoteModSession();}
            if(modLinkDisabled)return;
            if(NetworkServer.active&&NetworkServer.handlers!=null)
            {
                serverModHandler??=(NetworkMessageDelegate)OnServerModMessage;
                if(!serverModHandlerInstalled){if(NetworkServer.handlers.ContainsKey(ModMessageId)&&NetworkServer.handlers[ModMessageId].Pointer!=serverModHandler.Pointer){modLinkDisabled=true;Plugin.Logger.LogWarning("[MOD LINK] message ID collision on server; mod link disabled safely");return;}NetworkServer.handlers[ModMessageId]=serverModHandler;serverModHandlerInstalled=true;}
                else if(!NetworkServer.handlers.ContainsKey(ModMessageId)){NetworkServer.handlers[ModMessageId]=serverModHandler;}
                if(sessionEnabled&&Time.unscaledTime>=nextSettingsSync){nextSettingsSync=Time.unscaledTime+1f;string packet=BuildHostSettingsPacket();if(packet!=lastSettingsPacket){lastSettingsPacket=packet;foreach(var connection in new List<NetworkConnection>(moddedClients.Values))SendRaw(connection,packet);}}
            }
            if(NetworkClient.active&&NetworkClient.handlers!=null)
            {
                clientModHandler??=(NetworkMessageDelegate)OnClientModMessage;
                if(!clientModHandlerInstalled){if(NetworkClient.handlers.ContainsKey(ModMessageId)&&NetworkClient.handlers[ModMessageId].Pointer!=clientModHandler.Pointer){modLinkDisabled=true;Plugin.Logger.LogWarning("[MOD LINK] message ID collision on client; mod link disabled safely");return;}NetworkClient.handlers[ModMessageId]=clientModHandler;clientModHandlerInstalled=true;}
                else if(!NetworkClient.handlers.ContainsKey(ModMessageId)){NetworkClient.handlers[ModMessageId]=clientModHandler;}
                if(!NetworkServer.active&&NetworkClient.isConnected&&NetworkClient.connection!=null&&Time.unscaledTime>=nextModHello&&RemoteHostAdvertisesMod()){SendRaw(NetworkClient.connection,"H|"+ModProtocol+"|"+Plugin.Version);modHelloSent=true;nextModHello=Time.unscaledTime+2f;}
            }
        }
        catch(Exception ex){Plugin.Logger.LogWarning("[MOD LINK] setup failed: "+ex.Message);}
    }
    static bool RemoteHostAdvertisesMod()
    {
        if(PlayerCharacter.allPlayerCharacters==null)return false;foreach(var player in PlayerCharacter.allPlayerCharacters){var id=player?.playerNetworking?.identifier;if(!string.IsNullOrEmpty(id)&&id.StartsWith("HNS_DUMMY_",StringComparison.Ordinal))return true;}return false;
    }
    void OnServerModMessage(NetworkConnection connection,NetworkReader reader,int channel)
    {
        try
        {
            if(!Plugin.Enabled.Value||!sessionEnabled)return;string payload=reader.ReadString();if(string.IsNullOrEmpty(payload))return;var fields=payload.Split('|');if(fields.Length==0)return;
            var player=connection?.identity?.GetComponentInChildren<PlayerCharacter>(true);if(player==null||IsDummyManager(player))return;string key=Key(player);
            if(fields[0]=="H")
            {
                if(failedModConnections.Contains(connection))return;
                if(fields.Length<3||fields[1]!=ModProtocol){Plugin.Logger.LogWarning($"[MOD LINK] {key} uses incompatible protocol; dummy transport retained");return;}
                bool firstHello=!moddedClients.TryGetValue(key,out var previousConnection)||previousConnection!=connection;moddedClients[key]=connection;SendHostSettings(connection);if(firstHello){CancelDummyTransport(player);Plugin.Logger.LogInfo($"[MOD LINK] {key} connected with HNS mod {fields[2]} protocol {fields[1]}");}return;
            }
            if(fields[0]=="A"&&fields.Length>=3&&fields[1]==ModProtocol&&int.TryParse(fields[2],NumberStyles.Integer,CultureInfo.InvariantCulture,out var token)&&clientTransports.TryGetValue(key,out var request)&&request.Token==token&&Vector3.Distance(player.transform.position,request.Target)<=1f)FinishClientTransport(key,request,true);
        }
        catch(Exception ex){Plugin.Logger.LogWarning("[MOD LINK] server packet failed: "+ex.Message);}
    }
    void OnClientModMessage(NetworkConnection connection,NetworkReader reader,int channel)
    {
        try
        {
            if(!Plugin.Enabled.Value||NetworkServer.active)return;
            string payload=reader.ReadString();if(string.IsNullOrEmpty(payload))return;var fields=payload.Split('|');
            if(fields[0]=="D"&&fields.Length>=2&&fields[1]==ModProtocol){ClearRemoteModSession();return;}
            if(fields[0]=="S"&&fields.Length>=3&&fields[1]==ModProtocol&&modHelloSent){ApplyRemoteHostSettings(Encoding.UTF8.GetString(Convert.FromBase64String(fields[2])));remoteSessionEnabled=true;return;}
            if(fields[0]=="C"&&fields.Length>=3&&fields[1]==ModProtocol&&int.TryParse(fields[2],out var cancelledToken))
            {
                completedRemoteToken=Math.Max(completedRemoteToken,cancelledToken);
                if(remoteClientTransport!=null&&remoteClientTransport.Token<=cancelledToken)remoteClientTransport=null;
                return;
            }
            if(!remoteSessionEnabled||fields[0]!="T"||fields.Length<7||fields[1]!=ModProtocol)return;var local=LocalPlayer();if(local==null)return;
            if(!int.TryParse(fields[2],NumberStyles.Integer,CultureInfo.InvariantCulture,out var token)||!float.TryParse(fields[3],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)||!float.TryParse(fields[4],NumberStyles.Float,CultureInfo.InvariantCulture,out var y)||!float.TryParse(fields[5],NumberStyles.Float,CultureInfo.InvariantCulture,out var z))return;
            if(!float.IsFinite(x)||!float.IsFinite(y)||!float.IsFinite(z)||token<=0)return;
            if(token<=completedRemoteToken){if(token==completedRemoteToken)SendRaw(NetworkClient.connection,"A|"+ModProtocol+"|"+token.ToString(CultureInfo.InvariantCulture));return;}
            if(remoteClientTransport!=null&&token<=remoteClientTransport.Token)return;
            bool border=fields[6]=="1";var target=new Vector3(x,y,z);remoteClientTransport=new RemoteClientTransport{Target=target,Token=token,BorderReturn=border,Deadline=Time.unscaledTime+5f};DirectLocalTeleport(local,target);StabilizePlayer(local,border?.35f:.5f);
        }
        catch(Exception ex){Plugin.Logger.LogWarning("[MOD LINK] client packet failed: "+ex.Message);}
    }
    void SendHostSettings(NetworkConnection connection)
    {
        if(sessionEnabled&&connection!=null)SendRaw(connection,BuildHostSettingsPacket());
    }
    string BuildHostSettingsPacket()
    {
        string settings=string.Join("\n",new[]{
            "version="+Plugin.Version,"phase="+phase,"hide="+Plugin.HideTime.Value.ToString("R",CultureInfo.InvariantCulture),"round="+Plugin.RoundTime.Value.ToString("R",CultureInfo.InvariantCulture),"seekers="+Plugin.SeekerCount.Value,"excludeHost="+Plugin.ExcludeHost.Value,"itemChance="+Plugin.RandomItemChance.Value.ToString("R",CultureInfo.InvariantCulture),"time="+Plugin.TimeOfDay.Value,"areaSelection="+Plugin.AreaSelection.Value,"activeArea="+(activeAreaIndex+1),
            "radius="+Plugin.Radius.Value.ToString("R",CultureInfo.InvariantCulture),"hiderHeight="+Plugin.HiderHeight.Value.ToString("R",CultureInfo.InvariantCulture),"returnInset="+Plugin.ReturnInset.Value.ToString("R",CultureInfo.InvariantCulture),"endRadius="+Plugin.EndRadius.Value.ToString("R",CultureInfo.InvariantCulture),"borderSegments="+Plugin.BorderSegments.Value,
            "area="+Plugin.AreaPoint.Value,"seeker="+Plugin.SeekerPoint.Value,"end="+Plugin.EndPoint.Value,"dummyStandbys="+Plugin.DummyStandbySlots.Value,"playSpawns="+Plugin.HiderSpawnSlots.Value,"seekerSpawns="+Plugin.SeekerSpawnSlots.Value,"endSpawns="+Plugin.EndSpawnSlots.Value,"draftSpawns="+Plugin.DraftSpawnSlots.Value,
            "gear="+Plugin.GearStoragePoint.Value,"draftBorder="+Plugin.BorderPoints.Value,"playBorder="+Plugin.PlayBorder.Value,"seekerBorder="+Plugin.SeekerBorder.Value,"endBorder="+Plugin.EndBorder.Value,"draftRecoveries="+Plugin.DraftRecoveryPoints.Value,"playRecoveries="+Plugin.PlayRecoveryPoints.Value,
            "instructionSigns="+Plugin.InstructionSigns.Value,"instructionText="+Convert.ToBase64String(Encoding.UTF8.GetBytes(Plugin.InstructionSignText.Value??"")),"seekerSign="+Plugin.SeekerSignTransform.Value,"endSign="+Plugin.EndSignTransform.Value,"statusSign="+Plugin.StatusSignTransform.Value,"playAreas="+Convert.ToBase64String(Encoding.UTF8.GetBytes(Plugin.PlayAreas.Value??"")),
            "itemCategories="+$"{Plugin.EnableMegaphones.Value}:{Plugin.MegaphoneItemCount.Value},{Plugin.EnableFlares.Value}:{Plugin.FlareItemCount.Value},{Plugin.EnableGoggles.Value}:{Plugin.GogglesItemCount.Value},{Plugin.EnableSpeakers.Value}:{Plugin.SpeakerItemCount.Value}"
        });return "S|"+ModProtocol+"|"+Convert.ToBase64String(Encoding.UTF8.GetBytes(settings));
    }
    static void SendRaw(NetworkConnection connection,string payload)
    {
        if(connection==null)return;var writer=NetworkWriterPool.Get();try{writer.WriteUShort(ModMessageId);writer.WriteString(payload);connection.Send(writer.ToArraySegment());}finally{NetworkWriterPool.Return(writer);}
    }
    void QueueClientTransport(PlayerCharacter player,Vector3 target,string reason,bool borderReturn)
    {
        string key=Key(player);if(clientTransports.TryGetValue(key,out var existing)){if(existing.Player==player&&existing.Target==target&&existing.BorderReturn==borderReturn)return;existing.Target=target;existing.Reason=reason;existing.BorderReturn=borderReturn;existing.Token=++clientTransportSerial;existing.Attempts=0;existing.NextSend=0f;return;}
        clientTransports[key]=new ClientTransportRequest{Player=player,Target=target,Reason=reason,BorderReturn=borderReturn,Token=++clientTransportSerial};
    }
    void ProcessClientTransports()
    {
        foreach(var pair in new Dictionary<string,ClientTransportRequest>(clientTransports))
        {
            var request=pair.Value;if(request?.Player==null||!moddedClients.TryGetValue(pair.Key,out var connection)){FinishClientTransport(pair.Key,request,false);continue;}
            if(Time.unscaledTime<request.NextSend)continue;if(request.Attempts++>=10){SendRaw(connection,"C|"+ModProtocol+"|"+request.Token.ToString(CultureInfo.InvariantCulture));failedModConnections.Add(connection);FinishClientTransport(pair.Key,request,false);moddedClients.Remove(pair.Key);QueueTransport(request.Player,request.Target,request.Reason,request.BorderReturn);continue;}
            request.NextSend=Time.unscaledTime+.5f;SendRaw(connection,string.Format(CultureInfo.InvariantCulture,"T|{0}|{1}|{2:R}|{3:R}|{4:R}|{5}",ModProtocol,request.Token,request.Target.x,request.Target.y,request.Target.z,request.BorderReturn?1:0));
        }
    }
    void ProcessRemoteClientTransport()
    {
        var request=remoteClientTransport;if(request==null)return;var local=LocalPlayer();if(local==null||NetworkClient.connection==null||Time.unscaledTime>=request.Deadline){remoteClientTransport=null;return;}
        if(Vector3.Distance(local.transform.position,request.Target)<=1f)
        {
            if(++request.Confirmations>=2){completedRemoteToken=request.Token;SendRaw(NetworkClient.connection,"A|"+ModProtocol+"|"+request.Token.ToString(CultureInfo.InvariantCulture));remoteClientTransport=null;}return;
        }
        request.Confirmations=0;if(Time.unscaledTime<request.NextAttempt)return;request.NextAttempt=Time.unscaledTime+.1f;DirectLocalTeleport(local,request.Target);StabilizePlayer(local,request.BorderReturn?.35f:.5f);
    }
    void ApplyRemoteHostSettings(string settings)
    {
        syncedHostSettings=settings??"";remoteHostSettings.Clear();foreach(var line in syncedHostSettings.Split('\n')){int split=line.IndexOf('=');if(split>0)remoteHostSettings[line.Substring(0,split)]=line.Substring(split+1);}
        ApplyFairPlayMods();ApplyRemoteTime();
    }
    void ApplyRemoteTime()
    {
        if(!SkyManager.initalized||!remoteHostSettings.TryGetValue("time",out var value)||!Enum.TryParse<LockedTime>(value,out var time))return;
        switch(time){case LockedTime.Natural:SkyManager.ClearFixedTime();break;case LockedTime.Sunrise:SkyManager.SetFixedTime(6f);break;case LockedTime.Day:SkyManager.SetFixedTime(12f);break;case LockedTime.Sunset:SkyManager.SetFixedTime(18f);break;case LockedTime.Night:SkyManager.SetFixedTime(0f);break;}
    }
    void ClearRemoteModSession()
    {
        modHelloSent=false;nextModHello=0f;if(!remoteSessionEnabled&&remoteClientTransport==null&&remoteHostSettings.Count==0)return;remoteSessionEnabled=false;remoteClientTransport=null;syncedHostSettings="";remoteHostSettings.Clear();RestoreFairPlayMods();if(SkyManager.initalized)SkyManager.ClearFixedTime();foreach(var pair in fallProtectionUntil)if(pair.Key?.faller!=null)pair.Key.faller.ignoreFalling=false;fallProtectionUntil.Clear();
    }
    void FinishClientTransport(string key,ClientTransportRequest request,bool success)
    {
        clientTransports.Remove(key);if(request?.Player!=null&&request.BorderReturn)borderCooldown[key]=Time.unscaledTime+(success?.1f:.25f);
        if(success&&request?.Player!=null){ConfirmSeekerDeployment(request.Player,request.Target);if(roundSpawnPositions.TryGetValue(key,out var setupTarget)&&HorizontalDistanceSquared(setupTarget,request.Target)<.01f){if(phase==Phase.SettingUp)setupConfirmed.Add(key);if(phase==Phase.SettingUp||phase==Phase.Hiding||phase==Phase.Seeking)EquipAfterConfirmedTeleport(request.Player);}}
        if(request!=null)Plugin.Logger.LogInfo($"[MOD TRANSPORT] {(success?"complete":"failed")} {key} ({request.Reason}) attempts={request.Attempts}");
    }
    void CancelDummyTransport(PlayerCharacter player)
    {
        foreach(var request in new List<TransportRequest>(activeTransports))if(request.Player==player){var target=request.Target;var reason=request.Reason;var border=request.BorderReturn;request.AirFallbackUsed=true;FinishTransport(request,false);QueueClientTransport(player,target,reason,border);}
        PromoteQueuedTransports(priorityTransports,player);PromoteQueuedTransports(transports,player);
    }
    void PromoteQueuedTransports(Queue<TransportRequest> queue,PlayerCharacter player){int count=queue.Count;for(int i=0;i<count;i++){var request=queue.Dequeue();if(request.Player==player)QueueClientTransport(player,request.Target,request.Reason,request.BorderReturn);else queue.Enqueue(request);}}
    bool ProtectManager()
    {
        bool held=false;foreach(var player in Players())foreach(var manager in dummyManagers)if(manager!=null&&player!=manager&&player.hands?.heldCharacter==manager&&player.playerNetworking!=null){held=true;player.playerNetworking.UserCode_CmdDropHeldPlayer();Plugin.Logger.LogInfo("[MANAGER] forced drop by "+Key(player));}return held;
    }
    bool IsTransporting(PlayerCharacter player)
    {
        if(player==null)return false;
        if(clientTransports.ContainsKey(Key(player)))return true;
        foreach(var active in activeTransports)if(active.Player==player)return true;
        foreach(var queued in priorityTransports)if(queued.Player==player)return true;
        foreach(var queued in transports)if(queued.Player==player)return true;
        return false;
    }
    static void ManagerPickUp(PlayerCharacter manager,PlayerCharacter target)
    {
        if(CanTransportPlayer(manager)&&CanTransportPlayer(target))manager.playerNetworking.UserCode_CmdPickUpPlayer__PlayerCharacter(target);
    }
    internal static bool CanTransportPlayer(PlayerCharacter player)
    {
        if(player==null||player.hands==null||player.playerNetworking==null||player.playerNetworking.netIdentity==null)return false;
        if(!NetworkServer.active)return true;
        var identity=player.playerNetworking.netIdentity;
        if(!NetworkServer.spawned.TryGetValue(identity.netId,out var live)||live!=identity)return false;
        return IsDummyManager(player)||(identity.connectionToClient!=null&&identity.connectionToClient.identity==identity);
    }
    static void ManagerDrop(PlayerCharacter manager)
    {
        if(manager?.playerNetworking!=null)manager.playerNetworking.UserCode_CmdDropHeldPlayer();
    }
    void ProcessTransport()
    {
        while(priorityTransports.Count>0){if(!StartTransport(priorityTransports.Peek()))break;priorityTransports.Dequeue();}
        while(priorityTransports.Count==0&&transports.Count>0){if(!StartTransport(transports.Peek()))break;transports.Dequeue();}
        foreach(var request in new List<TransportRequest>(activeTransports))ProcessTransport(request);
    }
    bool StartTransport(TransportRequest request)
    {
        if(request==null||!CanTransportPlayer(request.Player))return true;request.Worker=WorkerFor(request.Player,request.Target);if(request.Worker==null)return false;
        DirectLocalTeleport(request.Worker,request.Target);
        request.Stage=0;request.Attempts=0;request.Confirmations=0;request.StableSince=0f;request.NextStage=Time.unscaledTime;request.Deadline=Time.unscaledTime+.5f;activeTransports.Add(request);
        Plugin.Logger.LogInfo($"[TRANSPORT] leased shared worker {standbyManagers.IndexOf(request.Worker)+1} to {Key(request.Player)}");return true;
    }
    PlayerCharacter WorkerFor(PlayerCharacter player,Vector3 target)
    {
        var busy=new HashSet<PlayerCharacter>();foreach(var active in activeTransports)if(active?.Worker!=null)busy.Add(active.Worker);
        PlayerCharacter best=null;float bestDistance=float.MaxValue;foreach(var worker in standbyManagers){if(worker==null||busy.Contains(worker)||cleanupManagerItems.ContainsKey(worker)||worker.hands?.heldCharacter!=null)continue;float distance=HorizontalDistanceSquared(worker.transform.position,player.transform.position);if(distance<bestDistance){bestDistance=distance;best=worker;}}return best;
    }
    void ProcessTransport(TransportRequest request)
    {
        if(request==null||Time.unscaledTime<request.NextStage)return;var worker=request.Worker;if(!CanTransportPlayer(request.Player)||!CanTransportPlayer(worker)){FinishTransport(request,false);return;}
        switch(request.Stage)
        {
            case 0:
                if(worker.hands?.heldCharacter!=null)ManagerDrop(worker);ManagerPickUp(worker,request.Player);request.Stage=1;request.NextStage=Time.unscaledTime+.05f;request.Deadline=Time.unscaledTime+.5f;break;
            case 1:
                if(worker.hands?.heldCharacter==request.Player){request.Stage=2;request.Confirmations=0;request.NextStage=Time.unscaledTime+.1f;break;}
                if(++request.Attempts<10){ManagerPickUp(worker,request.Player);request.NextStage=Time.unscaledTime+.05f;break;}FinishTransport(request,false);break;
            case 2:
                if(worker.hands?.heldCharacter!=request.Player){if(++request.Attempts<10){request.Stage=0;request.NextStage=Time.unscaledTime+.05f;break;}FinishTransport(request,false);break;}
                if(request.Confirmations==0){Vector3 carryOffset=request.Player.transform.position-worker.transform.position;if(carryOffset.sqrMagnitude<16f){DirectLocalTeleport(worker,request.Target-carryOffset);request.Confirmations=1;request.NextStage=Time.unscaledTime+.1f;break;}if(++request.Attempts<10){request.NextStage=Time.unscaledTime+.05f;break;}FinishTransport(request,false);break;}
                if(HorizontalDistanceSquared(request.Player.transform.position,request.Target)>9f){DirectLocalTeleport(worker,request.Target);if(++request.Attempts<10){request.NextStage=Time.unscaledTime+.05f;break;}FinishTransport(request,false);break;}
                if(request.BorderReturn&&Vector3.Distance(request.Player.transform.position,request.Target)>.35f){request.StableSince=0f;var correction=request.Target-request.Player.transform.position;DirectLocalTeleport(worker,worker.transform.position+correction);if(++request.Attempts<10){request.NextStage=Time.unscaledTime+.05f;break;}FinishTransport(request,false);break;}
                if(request.BorderReturn){if(request.StableSince==0f)request.StableSince=Time.unscaledTime;if(Time.unscaledTime-request.StableSince<.2f){request.NextStage=Time.unscaledTime+.05f;break;}}
                ManagerDrop(worker);request.Stage=3;request.Deadline=Time.unscaledTime+2f;request.NextStage=Time.unscaledTime+.2f;break;
            case 3:
                if(worker.hands?.heldCharacter!=null){ManagerDrop(worker);if(Time.unscaledTime>=request.Deadline){Plugin.Logger.LogWarning("[TRANSPORT] release timed out; worker left in place until released");FinishTransport(request,false);break;}request.NextStage=Time.unscaledTime+.05f;break;}
                if(HorizontalDistanceSquared(request.Player.transform.position,request.Target)>9f){if(++request.Attempts<10){request.Stage=0;request.NextStage=Time.unscaledTime+.05f;break;}FinishTransport(request,false);break;}
                request.Stage=4;request.StableSince=Time.unscaledTime;request.NextStage=Time.unscaledTime+.1f;break;
            default:
                bool arrived=TransportDestinationContains(request);float releasedFor=Time.unscaledTime-request.StableSince;
                if(!arrived)
                {
                    if(releasedFor<.25f){request.NextStage=Time.unscaledTime+.1f;break;}
                    Plugin.Logger.LogWarning("[TRANSPORT] client correction left destination after release; player="+request.Player.transform.position+" target="+request.Target);FinishTransport(request,false);break;
                }
                float confirmationWindow=request.BorderReturn?.6f:2f;
                if(releasedFor<confirmationWindow){request.NextStage=Time.unscaledTime+.1f;break;}
                ReturnWorkerToStandby(worker);FinishTransport(request,true);break;
        }
    }
    void ReturnWorkerToStandby(PlayerCharacter worker){if(worker==null||worker.hands?.heldCharacter!=null)return;int index=standbyManagers.IndexOf(worker);if(index>=0)DirectLocalTeleport(worker,WorkerParkingPosition(index));}
    bool TransportDestinationContains(TransportRequest request)
    {
        var player=request.Player;
        if(player==null)return false;
        var key=Key(player);var zone=phase==Phase.Ended||caught.Contains(key)?endBorder:(phase==Phase.SettingUp||phase==Phase.Hiding)&&seekers.Contains(key)?seekerBorder:playBorder;
        if(zone.Count>=4&&InsideBorder(request.Target,zone))return InsideBorder(player.transform.position,zone);
        // Explicit destinations outside the current role's area still use their own border.
        foreach(var candidate in new[]{endBorder,seekerBorder,playBorder})if(candidate.Count>=4&&InsideBorder(request.Target,candidate))return InsideBorder(player.transform.position,candidate);
        return HorizontalDistanceSquared(player.transform.position,request.Target)<=1f;
    }
    void ParkIdleWorkers()
    {
        var busy=new HashSet<PlayerCharacter>();foreach(var request in activeTransports)if(request?.Worker!=null)busy.Add(request.Worker);
        for(int i=0;i<standbyManagers.Count;i++)if(standbyManagers[i]!=null&&standbyManagers[i].hands?.heldCharacter==null&&!busy.Contains(standbyManagers[i])&&!cleanupManagerItems.ContainsKey(standbyManagers[i]))DirectLocalTeleport(standbyManagers[i],WorkerParkingPosition(i));
    }
    Vector3 WorkerParkingPosition(int index)
    {
        if(phase==Phase.Idle||phase==Phase.SettingUp)return SlotOrFallback(standbySlots,index,center);
        if(phase==Phase.Hiding&&index<seekers.Count)return OutsideBorderPosition(seekerBorder,index,Mathf.Max(1,seekers.Count),SlotOrFallback(seekerSpawnSlots,index,seekerSpawn));
        if(phase==Phase.Ended)return OutsideBorderPosition(endBorder,index,standbyManagers.Count,SlotOrFallback(endSpawnSlots,index,endSpawn));
        int offset=phase==Phase.Hiding?seekers.Count:0;return OutsideBorderPosition(playBorder,index-offset,Mathf.Max(1,standbyManagers.Count-offset),SlotOrFallback(hiderSpawnSlots,index-offset,center));
    }
    static Vector3 OutsideBorderPosition(List<Vector3> border,int index,int count,Vector3 fallback)
    {
        if(border==null||border.Count<4)return fallback;Vector3 center=Vector3.zero;foreach(var point in border)center+=point;center/=border.Count;
        int slot=Mathf.FloorToInt((index+.5f)*border.Count/Mathf.Max(1,count))%border.Count;var edge=border[slot];var outward=new Vector3(edge.x-center.x,0f,edge.z-center.z);if(outward.sqrMagnitude<.01f)return edge;return edge+outward.normalized*1.5f;
    }
    void FinishTransport(TransportRequest request,bool success)
    {
        if(!success&&request!=null&&!request.AirFallbackUsed&&CanTransportPlayer(request.Player)&&CanTransportPlayer(request.Worker)&&!TransportDestinationContains(request)&&request.Worker.hands.heldCharacter==null)
        {
            request.AirFallbackUsed=true;request.Stage=0;request.Attempts=0;request.Confirmations=0;request.StableSince=0f;
            DirectLocalTeleport(request.Worker,request.Player.transform.position+Vector3.up*12f);
            request.NextStage=Time.unscaledTime+.5f;
            Plugin.Logger.LogWarning("[TRANSPORT] Air pickup fallback for "+Key(request.Player)+" -> "+request.Target);
            return;
        }
        if(request?.Worker?.hands?.heldCharacter!=null)ManagerDrop(request.Worker);if(request?.Worker!=null&&request.Stage<4)ReturnWorkerToStandby(request.Worker);activeTransports.Remove(request);if(request==null)return;
        if(request.Player!=null&&request.BorderReturn)borderCooldown[Key(request.Player)]=Time.unscaledTime+(success?.1f:.25f);if(success){ConfirmSeekerDeployment(request.Player,request.Target);StabilizePlayer(request.Player,request.BorderReturn?.2f:.5f);if(roundSpawnPositions.TryGetValue(Key(request.Player),out var setupTarget)&&Vector3.Distance(setupTarget,request.Target)<.1f){if(phase==Phase.SettingUp)setupConfirmed.Add(Key(request.Player));if(phase==Phase.SettingUp||phase==Phase.Hiding||phase==Phase.Seeking)EquipAfterConfirmedTeleport(request.Player);}}
        Plugin.Logger.LogInfo($"[TRANSPORT] {(success?"complete":"failed")} {Key(request.Player)} ({request.Reason}) attempts={request.Attempts}");
    }
    void StabilizePlayer(PlayerCharacter player,float duration)
    {
        if(player==null||IsDummyManager(player))return;fallProtectionUntil[player]=Time.unscaledTime+duration;
        if(player.faller!=null){player.faller.ignoreFalling=true;player.faller.ClearNextFall();}
        if(player.playerNetworking!=null){player.playerNetworking.NetworkcontrolsVelocity=Vector3.zero;player.playerNetworking.NetworkisSitting=false;}
        ZeroPlayerVelocity(player);
    }
    void UpdateFallProtection()
    {
        foreach(var pair in new Dictionary<PlayerCharacter,float>(fallProtectionUntil))
        {
            var player=pair.Key;if(player==null){fallProtectionUntil.Remove(player);continue;}
            if(Time.unscaledTime>=pair.Value){if(player.faller!=null)player.faller.ignoreFalling=false;fallProtectionUntil.Remove(player);continue;}
            if(player.faller!=null)player.faller.ClearNextFall();ZeroPlayerVelocity(player);
        }
    }
    static void ZeroPlayerVelocity(PlayerCharacter player)
    {
        if(player?.rb!=null&&!player.rb.isKinematic){player.rb.linearVelocity=Vector3.zero;player.rb.angularVelocity=Vector3.zero;}
        if(NetworkServer.active&&player?.houseNetworkTransform!=null)player.houseNetworkTransform.RpcVelocity(Vector3.zero,Vector3.zero);
    }
    static void DirectLocalTeleport(PlayerCharacter player,Vector3 target)
    {
        if(player==null)return;
        if(IsDummyManager(player))
        {
            var networkTransform=player.houseNetworkTransform;var rotation=player.transform.rotation;player.transform.position=target;
            if(networkTransform!=null){uint packed=Compression.CompressQuaternion(rotation);networkTransform.targetPosition=target;networkTransform.targetRotation=rotation;networkTransform.ProcessMove(target,packed,0);networkTransform.RpcMove(target,packed,0);networkTransform.OwnerUpdate(true);}
            return;
        }
        if(player.grease!=null)player.grease.Teleport(target,player.transform.rotation,true);
    }
    static void Shuffle(List<PlayerCharacter> list){for(int i=list.Count-1;i>0;i--){int j=UnityEngine.Random.Range(0,i+1);var t=list[i];list[i]=list[j];list[j]=t;}}
    void SaveSigns()
    {
        var values=new string[instructionSignPositions.Count];
        for(int i=0;i<values.Length;i++)values[i]=FormatVector(instructionSignPositions[i])+","+instructionSignYaws[i].ToString("R",CultureInfo.InvariantCulture);
        Plugin.InstructionSigns.Value=string.Join(";",values);
    }
    void LoadSigns(string text)
    {
        instructionSignPositions.Clear();instructionSignYaws.Clear();if(string.IsNullOrWhiteSpace(text))return;
        foreach(var value in text.Split(';'))
        {
            var fields=value.Split(',');if(fields.Length!=4)continue;
            if(!float.TryParse(fields[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)||!float.TryParse(fields[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var y)||!float.TryParse(fields[2],NumberStyles.Float,CultureInfo.InvariantCulture,out var z)||!float.TryParse(fields[3],NumberStyles.Float,CultureInfo.InvariantCulture,out var yaw))continue;
            instructionSignPositions.Add(new Vector3(x,y,z));instructionSignYaws.Add(yaw);break;
        }
        SaveSigns();
    }
    static string FormatVector(Vector3 v)=>string.Format(CultureInfo.InvariantCulture,"{0:R},{1:R},{2:R}",v.x,v.y,v.z);
    static string FormatSign(Vector3 position,float yaw)=>FormatVector(position)+","+yaw.ToString("R",CultureInfo.InvariantCulture);
    static bool TrySign(string text,out Vector3 position,out float yaw)
    {
        position=default;yaw=0f;if(string.IsNullOrWhiteSpace(text))return false;var fields=text.Split(',');
        return fields.Length==4&&float.TryParse(fields[0],NumberStyles.Float,CultureInfo.InvariantCulture,out position.x)&&float.TryParse(fields[1],NumberStyles.Float,CultureInfo.InvariantCulture,out position.y)&&float.TryParse(fields[2],NumberStyles.Float,CultureInfo.InvariantCulture,out position.z)&&float.TryParse(fields[3],NumberStyles.Float,CultureInfo.InvariantCulture,out yaw);
    }
    static string FormatSlots(List<Vector3> slots){var values=new string[slots.Count];for(int i=0;i<slots.Count;i++)values[i]=FormatVector(slots[i]);return string.Join(";",values);}
    static void LoadSlots(string text,List<Vector3> slots,int maximum){slots.Clear();if(string.IsNullOrWhiteSpace(text))return;foreach(var value in text.Split(';')){if(slots.Count>=maximum)break;if(TryVector(value,out var slot))slots.Add(slot);}}
    static bool TryVector(string text,out Vector3 v){v=default;if(string.IsNullOrWhiteSpace(text))return false;var p=text.Split(',');if(p.Length!=3||!float.TryParse(p[0],NumberStyles.Float,CultureInfo.InvariantCulture,out var x)||!float.TryParse(p[1],NumberStyles.Float,CultureInfo.InvariantCulture,out var y)||!float.TryParse(p[2],NumberStyles.Float,CultureInfo.InvariantCulture,out var z))return false;v=new Vector3(x,y,z);return true;}
    void OnDestroy(){simpleVisible=false;setupVisible=false;SyncMenuCursor();ClearDummyManager();}
}

