// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
// Commercial use requires prior written permission from AdamMady.
// AI tools and generated changes provide no exception to these terms. The person
// or organization using, modifying, or distributing this code must comply.
using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace MadysHideNSeek;

public sealed partial class HideAndSeekTester
{
    readonly List<Behaviour> fairPlayBehaviours=new();
    FieldInfo nameTagsVisibleField;
    bool nameTagsWereVisible;
    FieldInfo moderationNameTagsField;
    bool moderationNameTagsWereVisible;
    bool fairPlayApplied;

    void ApplyFairPlayMods()
    {
        if(fairPlayApplied)return;fairPlayApplied=true;
        var nameTagsType=FindLoadedType("BigWalkNametags.Plugin");
        nameTagsVisibleField=nameTagsType?.GetField("TagsVisible",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        if(nameTagsVisibleField?.GetValue(null) is bool visible){nameTagsWereVisible=visible;nameTagsVisibleField.SetValue(null,false);}
        var moderationType=FindLoadedType("BigOrb.OrbBehaviour");
        moderationNameTagsField=moderationType?.GetField("NametagsOn",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        if(moderationNameTagsField?.GetValue(null) is bool moderationVisible){moderationNameTagsWereVisible=moderationVisible;moderationNameTagsField.SetValue(null,false);}
        DisableBehaviour("modtolocatefriends.overlaything");
        Plugin.Logger.LogInfo($"[FAIR PLAY] BigNameTags and Moderation Improvements nametags hidden; {fairPlayBehaviours.Count} Player Locator overlay(s) disabled.");
    }

    void RestoreFairPlayMods()
    {
        if(!fairPlayApplied)return;
        try{if(nameTagsVisibleField!=null)nameTagsVisibleField.SetValue(null,nameTagsWereVisible);}catch(Exception ex){Plugin.Logger.LogWarning("[FAIR PLAY] BigNameTags restore failed: "+ex.Message);}
        try{if(moderationNameTagsField!=null)moderationNameTagsField.SetValue(null,moderationNameTagsWereVisible);}catch(Exception ex){Plugin.Logger.LogWarning("[FAIR PLAY] Moderation Improvements nametags restore failed: "+ex.Message);}
        foreach(var behaviour in fairPlayBehaviours)if(behaviour!=null)behaviour.enabled=true;
        fairPlayBehaviours.Clear();nameTagsVisibleField=null;moderationNameTagsField=null;fairPlayApplied=false;
    }

    void DisableBehaviour(string typeName)
    {
        var type=FindLoadedType(typeName);if(type==null)return;
        foreach(var item in Resources.FindObjectsOfTypeAll(Il2CppType.From(type)))if(item is Behaviour behaviour&&behaviour.enabled){behaviour.enabled=false;fairPlayBehaviours.Add(behaviour);}
    }

    static Type FindLoadedType(string name){foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()){var type=assembly.GetType(name,false);if(type!=null)return type;}return null;}
}

