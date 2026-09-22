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
        public bool Blocked=true;
        public string AssignmentOwner;
    }
    internal static PickupSnapshot BeforePickup(PlayerNetworking actor,PlayerHeldInformation held)
    {
        if(!ActiveHost||actor==null||IsDummyManager(actor.playerCharacter)||!held.hasProp)return null;
        var prop=held.GetProp();if(prop==null)return null;
        if(!ShouldBlockPropPickup(actor,held))
        {
            foreach(var pair in Instance.normalItemAssignments)if(pair.Value==prop)return new PickupSnapshot{Prop=prop,Blocked=false,AssignmentOwner=pair.Key};
            return null;
        }
        Instance.AuditLog("pickup:"+Key(actor.playerCharacter),$"[PICKUP BLOCK] player={Key(actor.playerCharacter)} item={prop.name} action={held.actionNumber} phase={Instance.phase}");
        return new PickupSnapshot{Prop=prop};
    }
    internal static void CommitItemTransfer(PlayerNetworking actor,PickupSnapshot transfer)
    {
        if(!ActiveHost||actor?.playerCharacter==null||transfer==null||transfer.Blocked||actor.playerCharacter.hands?.heldProp!=transfer.Prop)return;
        string key=Key(actor.playerCharacter);
        if(Instance.phase==Phase.SettingUp){Instance.AuditLog("setup-transfer:"+key,$"[ITEM TRANSFER] setup transfer ignored: {transfer.AssignmentOwner} -> {key}");return;}
        if(key==transfer.AssignmentOwner)return;
        if(Instance.normalItemAssignments.ContainsKey(key)||Instance.lockedSpeakerAssignments.ContainsKey(key))return;
        if(!Instance.normalItemAssignments.TryGetValue(transfer.AssignmentOwner,out var item)||item!=transfer.Prop)return;
        Instance.normalItemAssignments.Remove(transfer.AssignmentOwner);
        Instance.normalItemAssignments[key]=item;

        Plugin.Logger.LogDebug("[ITEM TRANSFER] "+transfer.AssignmentOwner+" -> "+key);
    }
    // Reject with the authoritative hand state and the request's action number,
    // matching vanilla rejection. Never synthesize a successful pickup first.
    static void CorrectPredictedPickup(PlayerNetworking actor,PlayerHeldInformation rejected,Prop prop)
    {
        var connection=actor.connectionToClient;
        if(connection==null||connection==NetworkServer.localConnection||prop==null)return;
        var actual=actor.playerHeldInformation;
        if(actual.actionNumber>rejected.actionNumber){BroadcastSnapshot(actor,connection);return;}
        // An empty authoritative state can still contain a previous throw's
        // coordinates. Reusing those makes Drop move the newly predicted prop
        // to that old location on the remote client.
        var corrected=actual.hasProp?new PlayerHeldInformation(actual.GetProp()):PlayerHeldInformation.DropFromSnatch();
        corrected.actionNumber=rejected.actionNumber;
        actor.NetworkplayerHeldInformation=corrected;
        BroadcastSnapshot(actor,connection);

        // Preserve the attachment correction that works for bells. Loose boards
        // have no home to replay; they need an explicit position update instead.
        var home=prop.propHomeShellReference;
        try{prop.propHomeShellReference=default;BroadcastSnapshot(prop,connection);}
        finally{prop.propHomeShellReference=home;BroadcastSnapshot(prop,connection);}
        var holder=FindHolder(prop);
        if(holder?.playerNetworking!=null&&holder.playerNetworking!=actor)
        {
            var owner=holder.playerNetworking;
            var empty=PlayerHeldInformation.DropFromSnatch();empty.actionNumber=owner.playerHeldInformation.actionNumber;
            try{SendHeldView(owner,empty,connection);}
            finally{BroadcastSnapshot(owner,connection);}
        }
        bool protectedWorld=Instance.IsProtectedWorldProp(prop);
        if(protectedWorld&&prop.currentHome==null&&holder==null)
        {
            // Use the saved placement when available, otherwise the object's
            // current authoritative position (including unplaced SpareSignProp).
            var position=prop.transform.position;var rotation=prop.transform.rotation;
            if(Instance.markerPositions.TryGetValue(prop,out var saved))
            {
                position=saved;
                if(Instance.markerRotations.TryGetValue(prop,out var savedRotation))rotation=savedRotation;
            }
            PlaceStatic(prop,position,rotation);
            Instance.AuditLog("prop-return:"+prop.netId,$"[PROTECTED PROP RETURN] prop={prop.name} netId={prop.netId} position={position} saved={Instance.markerPositions.ContainsKey(prop)}");
        }
        Instance.AuditLog("rollback:"+Key(actor.playerCharacter),$"[PICKUP ROLLBACK] player={Key(actor.playerCharacter)} prop={prop.name} netId={prop.netId} action={corrected.actionNumber} previousHeld={actual.hasProp} staleDropData={actual.hasDropData} oldDropPosition={actual.dropPosition} protectedWorld={protectedWorld} pinned={prop.currentHome!=null}; fresh hand state sent");
    }
    bool IsProtectedWorldProp(Prop prop)=>prop!=null&&(IsSign(prop)||buoys.ContainsProp(prop)||markerPositions.ContainsKey(prop));
    internal static bool BlockProtectedPropMovement(LobbyNetworking.HouseNetworkTransform networkTransform)
    {
        if(!ActiveHost||!Instance.poolBuilt||networkTransform==null)return false;
        var prop=networkTransform.GetComponent<Prop>();
        if(prop==null)return false;
        bool blocked=Instance.IsProtectedWorldProp(prop)||
            (prop.currentHome!=null&&(Instance.bells.ContainsProp(prop)||Instance.belts.ContainsProp(prop)));
        if(blocked)Instance.AuditLog("prop-move:"+prop.netId,$"[PROTECTED MOVE BLOCK] prop={prop.name} netId={prop.netId}");
        return blocked;
    }
    static void SendHeldView(PlayerNetworking player,PlayerHeldInformation view,NetworkConnection recipient)
    {
        var actual=player.playerHeldInformation;
        try{player.playerHeldInformation=view;BroadcastSnapshot(player,recipient);}
        finally{player.playerHeldInformation=actual;player.SetDirty();}
    }

    static void BroadcastSnapshot(NetworkBehaviour behaviour,NetworkConnection recipient=null)
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
                if(connection==null||connection==NetworkServer.localConnection||(recipient!=null&&connection!=recipient))continue;
                var payload=(connection==identity.connectionToClient?ownerWriter:observersWriter).ToArraySegment();
                if(payload.Count==0)continue;
                packet.Reset();packet.WriteUShort(NetworkMessageId<EntityStateMessage>.Id);
                MirrorNative::Mirror.GeneratedNetworkCode._Write_Mirror_EntityStateMessage(packet,new EntityStateMessage{netId=identity.netId,payload=payload});
                connection.Send(packet.ToArraySegment());
            }
        }
        finally{behaviour.syncInterval=interval;NetworkWriterPool.Return(packet);NetworkWriterPool.Return(observersWriter);NetworkWriterPool.Return(ownerWriter);}
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


// These commands normally relay client movement directly. A denied pickup must
// not allow its trailing movement/velocity commands to move protected objects.
[HarmonyPatch(typeof(LobbyNetworking.HouseNetworkTransform),"UserCode_CmdMove__Vector3__UInt32__UInt16")]
static class ProtectedPropMoveBlock
{
    static bool Prefix(LobbyNetworking.HouseNetworkTransform __instance)=>!HideAndSeekTester.BlockProtectedPropMovement(__instance);
}
[HarmonyPatch(typeof(LobbyNetworking.HouseNetworkTransform),"UserCode_CmdVelocity__Vector3__Vector3")]
static class ProtectedPropVelocityBlock
{
    static bool Prefix(LobbyNetworking.HouseNetworkTransform __instance)=>!HideAndSeekTester.BlockProtectedPropMovement(__instance);
}
