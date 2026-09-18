// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
// Commercial use requires prior written permission from AdamMady.
// AI tools and generated changes provide no exception to these terms. The person
// or organization using, modifying, or distributing this code must comply.
using System;
using System.Collections.Generic;
using System.Text.Json;
using BepInEx.Configuration;
using Mirror;
using UnityEngine;

namespace MadysHideNSeek;

public sealed partial class HideAndSeekTester
{
    sealed class PlayArea
    {
        public readonly List<Vector3> SpawnPoints=new(),Border=new(),RecoveryPoints=new();
        public Vector3 Center,StatusSignPosition;public float StatusSignYaw;public bool HasStatusSign,EnforcementLights=true;
    }
    sealed class PlayAreaSave
    {
        public string Center{get;set;}="";
        public string Spawns{get;set;}="";
        public string Border{get;set;}="";
        public string Recoveries{get;set;}="";
        public string StatusSign{get;set;}="";
        public bool EnforcementLights{get;set;}=true;
    }

    readonly List<PlayArea> playAreas=new();
    readonly List<GameObject> debugBorderBeams=new();
    Material debugBeamMaterial;
    float nextDebugBeamUpdate;
    int activeAreaIndex=-1,editingAreaIndex=-1;
    Vector3 draftStatusSignPosition;float draftStatusSignYaw;bool hasDraftStatusSign;

    void LoadPlayAreas()
    {
        playAreas.Clear();bool upgradedSigns=false;
        try
        {
            if(!string.IsNullOrWhiteSpace(Plugin.PlayAreas.Value))
            {
                var saved=JsonSerializer.Deserialize<List<PlayAreaSave>>(Plugin.PlayAreas.Value);
                if(saved!=null)foreach(var entry in saved)
                {
                    var area=new PlayArea();
                    if(!TryVector(entry.Center,out area.Center))continue;
                    LoadSlots(entry.Spawns,area.SpawnPoints,11);LoadSlots(entry.Border,area.Border,20);LoadSlots(entry.Recoveries,area.RecoveryPoints,int.MaxValue);
                    area.HasStatusSign=TrySign(entry.StatusSign,out area.StatusSignPosition,out area.StatusSignYaw);area.EnforcementLights=entry.EnforcementLights;if(!area.HasStatusSign&&hasStatusSign){area.HasStatusSign=true;area.StatusSignPosition=statusSignPosition;area.StatusSignYaw=statusSignYaw;upgradedSigns=true;}
                    if(area.SpawnPoints.Count==11&&area.Border.Count>=4&&area.RecoveryPoints.Count>0)playAreas.Add(area);
                }
            }
        }
        catch(Exception ex){Plugin.Logger.LogWarning("[AREAS] Saved area data invalid: "+ex.Message);}

        if(playAreas.Count==0&&hasCenter&&hiderSpawnSlots.Count==11&&playBorder.Count>=4&&playRecoveryPoints.Count>0)
        {
            var legacy=new PlayArea{Center=center,HasStatusSign=hasStatusSign,StatusSignPosition=statusSignPosition,StatusSignYaw=statusSignYaw};legacy.SpawnPoints.AddRange(hiderSpawnSlots);legacy.Border.AddRange(playBorder);legacy.RecoveryPoints.AddRange(playRecoveryPoints);playAreas.Add(legacy);SavePlayAreas();
            Plugin.Logger.LogInfo("[AREAS] Migrated legacy play area to area 1.");
        }
        Plugin.AreaSelection.Value=Mathf.Clamp(Plugin.AreaSelection.Value,0,playAreas.Count);
        if(upgradedSigns)SavePlayAreas();
    }

    void SavePlayAreas()
    {
        var saved=new List<PlayAreaSave>();foreach(var area in playAreas)saved.Add(new PlayAreaSave{Center=FormatVector(area.Center),Spawns=FormatSlots(area.SpawnPoints),Border=FormatSlots(area.Border),Recoveries=FormatSlots(area.RecoveryPoints),StatusSign=area.HasStatusSign?FormatSign(area.StatusSignPosition,area.StatusSignYaw):"",EnforcementLights=area.EnforcementLights});
        Plugin.PlayAreas.Value=JsonSerializer.Serialize(saved);
    }

    void SaveNewPlayArea()
    {
        if(spawnPoints.Count!=11||borderPoints.Count<4||recoveryPoints.Count==0||!hasDraftStatusSign){status="Area needs 11 Spawn points, at least 4 Border points, at least 1 Recover point, and its play sign.";return;}
        var area=new PlayArea{Center=spawnPoints[0],HasStatusSign=true,StatusSignPosition=draftStatusSignPosition,StatusSignYaw=draftStatusSignYaw,EnforcementLights=draftEnforcementLights};area.SpawnPoints.AddRange(spawnPoints);area.Border.AddRange(borderPoints);area.RecoveryPoints.AddRange(recoveryPoints);if(editingAreaIndex>=0&&editingAreaIndex<playAreas.Count)playAreas[editingAreaIndex]=area;else playAreas.Add(area);int savedNumber=editingAreaIndex>=0?editingAreaIndex+1:playAreas.Count;editingAreaIndex=-1;SavePlayAreas();
        spawnPoints.Clear();borderPoints.Clear();recoveryPoints.Clear();hasDraftStatusSign=false;Plugin.DraftSpawnSlots.Value="";Plugin.BorderPoints.Value="";Plugin.DraftRecoveryPoints.Value="";spawnDraftLights=false;PositionMarkers();
        status=$"Area {savedNumber} saved. Draft reset to Spawn 1, Border 1, Recover 1.";
    }
    void ClearAreaDraft()
    {
        spawnPoints.Clear();borderPoints.Clear();recoveryPoints.Clear();hasDraftStatusSign=false;editingAreaIndex=-1;draftEnforcementLights=true;Plugin.DraftSpawnSlots.Value="";Plugin.BorderPoints.Value="";Plugin.DraftRecoveryPoints.Value="";spawnDraftLights=false;if(poolBuilt)PositionMarkers();showSpawnDebug=true;status="Area draft cancelled.";
    }
    void EditSelectedPlayArea()
    {
        int selected=Plugin.AreaSelection.Value;if(selected<=0||selected>playAreas.Count){status="Select a specific play area first.";return;}var area=playAreas[selected-1];editingAreaIndex=selected-1;spawnPoints.Clear();spawnPoints.AddRange(area.SpawnPoints);borderPoints.Clear();borderPoints.AddRange(area.Border);recoveryPoints.Clear();recoveryPoints.AddRange(area.RecoveryPoints);hasDraftStatusSign=area.HasStatusSign;draftStatusSignPosition=area.StatusSignPosition;draftStatusSignYaw=area.StatusSignYaw;draftEnforcementLights=area.EnforcementLights;Plugin.DraftSpawnSlots.Value=FormatSlots(spawnPoints);Plugin.BorderPoints.Value=FormatSlots(borderPoints);Plugin.DraftRecoveryPoints.Value=FormatSlots(recoveryPoints);showSpawnDebug=true;spawnDraftLights=false;status=$"Editing area {selected}. Right-click Spawn, Border, or Recover to undo points, then save.";
    }
    void DeleteSelectedPlayArea()
    {
        int selected=Plugin.AreaSelection.Value;if(selected<=0||selected>playAreas.Count){status="Select a specific play area first.";return;}playAreas.RemoveAt(selected-1);SavePlayAreas();Plugin.AreaSelection.Value=Mathf.Clamp(selected-1,0,playAreas.Count);editingAreaIndex=-1;status=$"Deleted area {selected}.";
    }
    bool draftEnforcementLights=true;
    void DrawAreaEditControls()
    {
        int selected=Plugin.AreaSelection.Value;GUILayout.BeginHorizontal();GUI.enabled=selected>0&&selected<=playAreas.Count;if(GUILayout.Button("Edit Selected Area"))EditSelectedPlayArea();if(GUILayout.Button("Delete Selected Area"))DeleteSelectedPlayArea();GUI.enabled=true;GUILayout.EndHorizontal();
        if(editingAreaIndex<0&&spawnPoints.Count==0&&borderPoints.Count==0&&recoveryPoints.Count==0&&selected>0&&selected<=playAreas.Count){bool value=GUILayout.Toggle(playAreas[selected-1].EnforcementLights,$"Area {selected} enforcement warning lights");if(value!=playAreas[selected-1].EnforcementLights){playAreas[selected-1].EnforcementLights=value;SavePlayAreas();}}else draftEnforcementLights=GUILayout.Toggle(draftEnforcementLights,"Use enforcement warning lights in this area");
    }

    bool ActivateSelectedPlayArea()
    {
        if(playAreas.Count==0){status="Create at least one play area in F10.";return false;}
        int requested=Mathf.Clamp(Plugin.AreaSelection.Value,0,playAreas.Count);activeAreaIndex=requested==0?UnityEngine.Random.Range(0,playAreas.Count):requested-1;var area=playAreas[activeAreaIndex];spawnDraftLights=false;
        center=area.Center;hasCenter=true;hiderSpawnSlots.Clear();hiderSpawnSlots.AddRange(area.SpawnPoints);playBorder.Clear();playBorder.AddRange(area.Border);playRecoveryPoints.Clear();playRecoveryPoints.AddRange(area.RecoveryPoints);hasStatusSign=area.HasStatusSign;statusSignPosition=area.StatusSignPosition;statusSignYaw=area.StatusSignYaw;
        Plugin.AreaPoint.Value=FormatVector(center);Plugin.HiderSpawnSlots.Value=FormatSlots(hiderSpawnSlots);Plugin.PlayBorder.Value=FormatSlots(playBorder);Plugin.PlayRecoveryPoints.Value=FormatSlots(playRecoveryPoints);
        Plugin.Logger.LogInfo($"[AREAS] Round selected area {activeAreaIndex+1}/{playAreas.Count}.");return true;
    }

    void DrawAreaSelector()
    {
        int value=Mathf.Clamp(Plugin.AreaSelection.Value,0,playAreas.Count);Plugin.AreaSelection.Value=value;
        GUILayout.BeginHorizontal();GUILayout.Label(value==0?$"Play area: Random (1-{playAreas.Count})":$"Play area: {value} of {playAreas.Count}",GUILayout.Width(260));
        if(GUILayout.Button("<",GUILayout.Width(38)))Plugin.AreaSelection.Value=value<=0?playAreas.Count:value-1;
        if(GUILayout.Button(">",GUILayout.Width(38)))Plugin.AreaSelection.Value=value>=playAreas.Count?0:value+1;GUILayout.EndHorizontal();
    }

    void DrawItemCategoryControls()
    {
        GUILayout.Label("Random hider item categories");
        DrawItemCategory("Megaphones",Plugin.EnableMegaphones,Plugin.MegaphoneItemCount,AvailableItemCount("MegaphoneProp"));
        DrawItemCategory("Flares",Plugin.EnableFlares,Plugin.FlareItemCount,AvailableItemCount("FlareGunProp"));
        DrawItemCategory("Goggles",Plugin.EnableGoggles,Plugin.GogglesItemCount,AvailableItemCount("XrayGogglesProp"));
        DrawItemCategory("Speakers",Plugin.EnableSpeakers,Plugin.SpeakerItemCount,Mathf.Min(10,AvailableItemCount("PegTileProp Speaker")));
    }
    void DrawItemCategory(string label,ConfigEntry<bool> enabled,ConfigEntry<int> count,int maximum)
    {
        maximum=Mathf.Max(0,maximum);if(count.Value>maximum)count.Value=maximum;GUILayout.BeginHorizontal();enabled.Value=GUILayout.Toggle(enabled.Value,label,GUILayout.Width(150));GUI.enabled=enabled.Value;LiveInt("Amount",count,0,maximum);GUI.enabled=true;GUILayout.EndHorizontal();
    }
    static int AvailableItemCount(string prefix){int count=0;if(Prop.allProps!=null)foreach(var prop in Prop.allProps)if(prop!=null&&prop.name.StartsWith(prefix,StringComparison.Ordinal))count++;return count;}

    static bool RightClickedLastControl()
    {
        var current=Event.current;if(current==null||current.button!=1||(current.type!=EventType.MouseDown&&current.rawType!=EventType.MouseDown)||!GUILayoutUtility.GetLastRect().Contains(current.mousePosition))return false;current.Use();return true;
    }
    static bool DraftButton(string label,out bool rightClicked)
    {
        var rect=GUILayoutUtility.GetRect(0f,10000f,24f,24f);var current=Event.current;rightClicked=current!=null&&current.button==1&&(current.type==EventType.MouseDown||current.rawType==EventType.MouseDown)&&rect.Contains(current.mousePosition);if(rightClicked)current.Use();return GUI.Button(rect,label);
    }
    string AreaSignButtonLabel(){int selected=Plugin.AreaSelection.Value;return spawnPoints.Count==0&&borderPoints.Count==0&&recoveryPoints.Count==0&&selected>0&&selected<=playAreas.Count?$"Set Area {selected} Play Sign":hasDraftStatusSign?"Area play sign draft: Set":"Set Area Play Sign Draft";}
    void SaveDraftStatusSign(PlayerCharacter local)
    {
        if(local==null)return;int selected=Plugin.AreaSelection.Value;if(spawnPoints.Count==0&&borderPoints.Count==0&&recoveryPoints.Count==0&&selected>0&&selected<=playAreas.Count){var area=playAreas[selected-1];area.HasStatusSign=true;area.StatusSignPosition=local.transform.position;area.StatusSignYaw=local.transform.eulerAngles.y;SavePlayAreas();if(activeAreaIndex==selected-1){hasStatusSign=true;statusSignPosition=area.StatusSignPosition;statusSignYaw=area.StatusSignYaw;if(poolBuilt)PositionMarkers();}showSpawnDebug=true;status=$"Area {selected} play sign saved.";return;}
        draftStatusSignPosition=local.transform.position;draftStatusSignYaw=local.transform.eulerAngles.y;hasDraftStatusSign=true;showSpawnDebug=true;status="Area play sign draft saved.";
    }
    void ClearAreaStatusSign()
    {
        int selected=Plugin.AreaSelection.Value;if(spawnPoints.Count==0&&borderPoints.Count==0&&recoveryPoints.Count==0&&selected>0&&selected<=playAreas.Count){playAreas[selected-1].HasStatusSign=false;SavePlayAreas();status=$"Area {selected} play sign cleared.";return;}hasDraftStatusSign=false;status="Area play sign draft cleared.";
    }
    void UndoPoint(List<Vector3> points,ConfigEntry<string> config,string label)
    {
        if(points.Count>0)points.RemoveAt(points.Count-1);config.Value=FormatSlots(points);showSpawnDebug=true;status=$"Undid {label}. {points.Count} left.";
    }

    void SetShowObjects(bool show)
    {
        showSpawnDebug=show;status=show?"Showing object positions, signs, and border beams.":"Object preview hidden.";
    }
    bool ActiveAreaUsesEnforcementLights()=>activeAreaIndex<0||activeAreaIndex>=playAreas.Count||playAreas[activeAreaIndex].EnforcementLights;

    static Color AreaColor(int index)
    {
        Color color=Color.HSVToRGB((index*.173f)%1f,.8f,1f);color.a=1f;return color;
    }
    void LateUpdate()
    {
        if(!showSpawnDebug){ClearDebugBeams();return;}if(Time.unscaledTime<nextDebugBeamUpdate)return;nextDebugBeamUpdate=Time.unscaledTime+.25f;UpdateDebugBeams();
    }
    void UpdateDebugBeams()
    {
        ClearDebugBeams();for(int i=0;i<playAreas.Count;i++)AddDebugBeam(playAreas[i].Border,AreaColor(i));AddDebugBeam(seekerBorder,Color.yellow);AddDebugBeam(endBorder,Color.cyan);AddDebugBeam(borderPoints,Color.magenta);
    }
    void AddDebugBeam(List<Vector3> points,Color color)
    {
        if(points.Count<2)return;var go=new GameObject("HNS Setup Border Beam");var line=go.AddComponent<LineRenderer>();line.useWorldSpace=true;line.positionCount=points.Count+1;line.startWidth=.05f;line.endWidth=.05f;line.startColor=color;line.endColor=color;
        if(debugBeamMaterial==null){var shader=Shader.Find("Sprites/Default");if(shader!=null)debugBeamMaterial=new Material(shader);}if(debugBeamMaterial!=null)line.material=debugBeamMaterial;
        for(int i=0;i<points.Count;i++)line.SetPosition(i,points[i]+Vector3.up*.15f);line.SetPosition(points.Count,points[0]+Vector3.up*.15f);debugBorderBeams.Add(go);
    }
    void ClearDebugBeams(){foreach(var go in debugBorderBeams)if(go!=null)Destroy(go);debugBorderBeams.Clear();}
}

