// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
// Commercial use requires prior written permission from AdamMady.
// AI tools and generated changes provide no exception to these terms. The person
// or organization using, modifying, or distributing this code must comply.
extern alias MirrorNative;
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace MadysHideNSeek;

public sealed partial class HideAndSeekTester
{
    static readonly MethodInfo UseHeldUpServer=AccessTools.Method(typeof(PlayerNetworking),"UserCode_CmdUseHeldUp__PeckContext");
    static readonly MethodInfo ReleaseHeldSwitchServer=AccessTools.Method(typeof(PlayerNetworking),"UserCode_CmdReleaseHeldSwitch__ShellReference__PeckContext__Int32");

    static void StopUsingHeldProp(PlayerCharacter player,Prop prop)
    {
        var networking=player?.playerNetworking;
        if(networking==null||prop==null||player.hands?.heldProp!=prop||!NetworkServer.active)return;
        var active=networking.heldSwitch;
        int action=Math.Max(active.actionNumber,networking.playerHeldInformation.actionNumber)+1;
        var context=new PeckContext(player,prop){actionNumber=action};
        try{UseHeldUpServer?.Invoke(networking,new object[]{context});}
        catch(Exception ex){Plugin.Logger.LogWarning("[ITEM RELEASE] held-up failed for "+prop.name+": "+ex.GetBaseException().Message);}
        try
        {
            var reference=active.shellReference;
            if(reference.netId!=0||reference.ticket!=0||reference.index!=0)
                ReleaseHeldSwitchServer?.Invoke(networking,new object[]{reference,context,action});
        }
        catch(Exception ex){Plugin.Logger.LogWarning("[ITEM RELEASE] switch release failed for "+prop.name+": "+ex.GetBaseException().Message);}
        networking.NetworkheldSwitch=new ShellReferenceWithActionNumber(default,action);
        SetState(prop,0);
        BroadcastSnapshot(networking);
        BroadcastSnapshot(prop);
    }

    internal sealed class PickupSnapshot
    {
        public Prop Prop;
        public PropHome Home;
        public PlayerCharacter Owner;
        public bool Blocked=true;
        public string AssignmentOwner;
        public bool RestorePosition;
        public Vector3 Position;
        public Quaternion Rotation;

        public float ReadyAt;
    }
    readonly Dictionary<Prop,PickupSnapshot> propRefreshes=new();
    internal static PickupSnapshot BeforePickup(PlayerNetworking actor,PlayerHeldInformation held)
    {
        if(!ActiveHost||actor==null||!held.hasProp)return null;
        var prop=held.GetProp();
        if(prop==null)return null;
        if(!ShouldBlockPropPickup(actor,held))
        {
            foreach(var pair in Instance.normalItemAssignments)if(pair.Value==prop)return new PickupSnapshot{Prop=prop,Blocked=false,AssignmentOwner=pair.Key};
            return null;
        }
        if(Instance.propRefreshes.TryGetValue(prop,out var pending))return pending;
        var repair=new PickupSnapshot{Prop=prop,Home=prop.currentHome,Owner=FindHolder(prop)};
        bool assigned=Instance.normalItemAssignments.ContainsValue(prop)||Instance.lockedSpeakerAssignments.ContainsValue(prop);
        if(!assigned)
        {
            if(Instance.markerPositions.TryGetValue(prop,out var position)){repair.RestorePosition=true;repair.Position=position;repair.Rotation=Instance.markerRotations.TryGetValue(prop,out var rotation)?rotation:prop.transform.rotation;}
            else if(Instance.whiteboardOrigins.TryGetValue(prop,out var boardOrigin)){repair.RestorePosition=true;repair.Position=boardOrigin.Position;repair.Rotation=boardOrigin.Rotation;}
            else if((Instance.IsSign(prop)||Instance.buoys.Contains(prop))&&Instance.propOrigins.TryGetValue(prop,out var origin)){repair.RestorePosition=true;repair.Position=origin.Position;repair.Rotation=origin.Rotation;}
        }
        return repair;
    }
    internal static void CommitItemTransfer(PlayerNetworking actor,PickupSnapshot transfer)
    {
        if(!ActiveHost||actor?.playerCharacter==null||transfer==null||transfer.Blocked||actor.playerCharacter.hands?.heldProp!=transfer.Prop)return;
        string key=Key(actor.playerCharacter);
        if(key==transfer.AssignmentOwner)return;
        if(Instance.normalItemAssignments.ContainsKey(key)||Instance.lockedSpeakerAssignments.ContainsKey(key))return;
        if(!Instance.normalItemAssignments.TryGetValue(transfer.AssignmentOwner,out var item)||item!=transfer.Prop)return;
        Instance.normalItemAssignments.Remove(transfer.AssignmentOwner);
        Instance.normalItemAssignments[key]=item;
        Plugin.Logger.LogInfo("[ITEM TRANSFER] "+transfer.AssignmentOwner+" -> "+key);
    }
    [Il2CppInterop.Runtime.Attributes.HideFromIl2Cpp]
    void BeginPropRepair(PickupSnapshot repair)
    {
        if(repair?.Prop==null)return;
        if(propRefreshes.ContainsKey(repair.Prop))return;
        propRefreshes[repair.Prop]=repair;repair.ReadyAt=Time.unscaledTime+.35f;
        if(repair.Prop.currentHome!=null)repair.Prop.ServerSetUnpinned();
        if(repair.Owner?.playerNetworking!=null)
        {
            var owner=repair.Owner.playerNetworking;
            StopUsingHeldProp(repair.Owner,repair.Prop);
            var dropped=PlayerHeldInformation.DropFromSnatch();dropped.actionNumber=owner.playerHeldInformation.actionNumber+1;
            owner.NetworkplayerHeldInformation=dropped;
            BroadcastSnapshot(owner);
        }
        // Explicit full messages keep the two states separate even within one server tick.
        BroadcastSnapshot(repair.Prop);
    }
    static void BroadcastSnapshot(NetworkBehaviour behaviour)
    {
        var identity=behaviour?.netIdentity;
        if(identity?.observers==null)return;
        var ownerWriter=NetworkWriterPool.Get();var observersWriter=NetworkWriterPool.Get();var packet=NetworkWriterPool.Get();
        float interval=behaviour.syncInterval;
        try
        {
            // Use normal entity updates, not spawn messages: never reset player transforms or voice lifecycle.
            behaviour.syncInterval=0f;behaviour.SetDirty();identity.SerializeServer(false,ownerWriter,observersWriter);
            foreach(var connection in identity.observers.Values)
            {
                if(connection==null||connection==NetworkServer.localConnection)continue;
                var payload=(connection==identity.connectionToClient?ownerWriter:observersWriter).ToArraySegment();
                if(payload.Count==0)continue;
                packet.Reset();packet.WriteUShort(NetworkMessageId<EntityStateMessage>.Id);
                MirrorNative::Mirror.GeneratedNetworkCode._Write_Mirror_EntityStateMessage(packet,new EntityStateMessage{netId=identity.netId,payload=payload});
                connection.Send(packet.ToArraySegment());
            }
        }
        finally{behaviour.syncInterval=interval;NetworkWriterPool.Return(packet);NetworkWriterPool.Return(observersWriter);NetworkWriterPool.Return(ownerWriter);}
    }
    void ProcessPropRepairs()
    {
        if(propRefreshes.Count==0)return;
        foreach(var pair in new Dictionary<Prop,PickupSnapshot>(propRefreshes))
        {
            var repair=pair.Value;if(Time.unscaledTime<repair.ReadyAt)continue;propRefreshes.Remove(pair.Key);
            var prop=repair.Prop;if(prop==null)continue;
            if(phase!=Phase.Ended&&repair.Home!=null&&(repair.Home.pinnedProp==null||repair.Home.pinnedProp==prop))
            {
                if(ReleaseProp(prop))prop.ServerSetPinned(repair.Home);
            }
            else if(repair.RestorePosition)PlaceStatic(prop,repair.Position,repair.Rotation);
            else if(phase!=Phase.Ended&&repair.Owner!=null&&!caught.Contains(Key(repair.Owner)))
            {
                string key=Key(repair.Owner);
                bool stillAssigned=normalItemAssignments.TryGetValue(key,out var item)&&item==prop||lockedSpeakerAssignments.TryGetValue(key,out item)&&item==prop;
                if(stillAssigned)GiveHeldItem(repair.Owner,prop);
            }
            BroadcastSnapshot(prop);
            if(repair.Owner?.playerNetworking!=null)BroadcastSnapshot(repair.Owner.playerNetworking);
        }
    }
    internal static bool MustDropCarriedPlayer(PlayerCharacter player)
    {
        if(!ActiveHost||player==null||IsDummyManager(player))return false;
        string key=Key(player);
        return player.hands?.heldProp!=null||Instance.normalItemAssignments.ContainsKey(key)||Instance.lockedSpeakerAssignments.ContainsKey(key);
    }
    internal static void EnforceExclusiveHands(PlayerCharacter player)
    {
        if(MustDropCarriedPlayer(player)&&player.hands?.heldCharacter!=null)player.playerNetworking.UserCode_CmdDropHeldPlayer();
    }
}

