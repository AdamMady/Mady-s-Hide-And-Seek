// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
// Commercial use requires prior written permission from AdamMady.
// AI tools and generated changes provide no exception to these terms. The person
// or organization using, modifying, or distributing this code must comply.
#if false // Self-color disabled until redesigned.
using System;
using System.Collections.Generic;
using HarmonyLib;
using Mirror;

namespace MadysHideNSeek;

[HarmonyPatch(typeof(PeckEffectChangeBrushColor),nameof(PeckEffectChangeBrushColor.Peck))]
static class SelfColorSwatchPatch
{
    static void Postfix(PeckEffectChangeBrushColor __instance,PeckContext context)
        =>HideAndSeekTester.ApplySelfColor(__instance,context);
}

public sealed partial class HideAndSeekTester
{
    readonly Dictionary<string,int> selfColorStage=new();
    readonly Dictionary<string,int[]> selfColors=new();
    readonly Dictionary<string,float> selfColorRestoreUntil=new(),selfColorRestoreNext=new();

    internal static void ApplySelfColor(PeckEffectChangeBrushColor swatch,PeckContext context)
    {
        var instance=Instance;
        if(!ActiveHost||instance==null||!NetworkServer.active||swatch==null||context==null)return;
        var player=context.GetPlayerCharacter();var prop=context.GetProp();
        if(player?.playerNetworking==null||prop==null||!prop.name.StartsWith("SalonBrushProp",StringComparison.Ordinal))return;
        string key=Key(player);instance.selfColorStage.TryGetValue(key,out int stage);int color=swatch.colorID;
        if(!instance.selfColors.TryGetValue(key,out var colors))instance.selfColors[key]=colors=new[]{player.playerNetworking.lookIdHead,player.playerNetworking.lookIdTorso,player.playerNetworking.lookIdLegs};
        if(stage==0)player.playerNetworking.NetworklookIdHead=color;
        else if(stage==1)player.playerNetworking.NetworklookIdTorso=color;
        else player.playerNetworking.NetworklookIdLegs=color;
        colors[stage]=color;
        instance.selfColorStage[key]=(stage+1)%3;
        var brush=prop.GetComponentInChildren<SalonBrush>(true);if(brush!=null)brush.ClearColor();
        BroadcastSnapshot(player.playerNetworking);BroadcastSnapshot(prop);
        Plugin.Logger.LogInfo("[SELF COLOR] "+key+" set "+(stage==0?"head":stage==1?"torso":"legs")+" color="+color);
    }

    void RestoreSelfColors(PlayerCharacter player,string key)
    {
        if(player?.playerNetworking==null||!selfColors.TryGetValue(key,out var colors)||colors.Length<3)return;
        selfColorRestoreUntil[key]=UnityEngine.Time.unscaledTime+5f;selfColorRestoreNext[key]=0f;ApplySavedColors(player,key,colors);
        Plugin.Logger.LogInfo("[SELF COLOR] scheduled restore "+key+" after rejoin");
    }

    void MaintainSelfColorRestores()
    {
        foreach(var pair in new Dictionary<string,float>(selfColorRestoreUntil))
        {
            if(UnityEngine.Time.unscaledTime>=pair.Value){selfColorRestoreUntil.Remove(pair.Key);selfColorRestoreNext.Remove(pair.Key);continue;}
            if(selfColorRestoreNext.TryGetValue(pair.Key,out var next)&&UnityEngine.Time.unscaledTime<next)continue;
            selfColorRestoreNext[pair.Key]=UnityEngine.Time.unscaledTime+.25f;var player=FindPlayer(pair.Key);
            if(player!=null&&selfColors.TryGetValue(pair.Key,out var colors))ApplySavedColors(player,pair.Key,colors);
        }
    }
    static void ApplySavedColors(PlayerCharacter player,string key,int[] colors)
    {
        if(player?.playerNetworking==null||colors==null||colors.Length<3)return;
        player.playerNetworking.NetworklookIdHead=colors[0];player.playerNetworking.NetworklookIdTorso=colors[1];player.playerNetworking.NetworklookIdLegs=colors[2];BroadcastSnapshot(player.playerNetworking);
    }

    static void ClearBrush(Prop prop)
    {
        var brush=prop?.GetComponentInChildren<SalonBrush>(true);if(brush!=null)brush.ClearColor();
    }
}
#endif

