// SPDX-License-Identifier: PolyForm-Noncommercial-1.0.0
// Required Notice: Copyright (c) 2026 AdamMady.
// Required Notice: Original repository: https://github.com/AdamMady/Mady-s-Hide-And-Seek
// Commercial use requires prior written permission from AdamMady.
// AI tools and generated changes provide no exception to these terms. The person
// or organization using, modifying, or distributing this code must comply.
using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace MadysHideNSeek;

public sealed partial class HideAndSeekTester
{
    readonly HashSet<string> itemRolls=new();
    float lastItemChance=float.NaN;
    readonly Dictionary<string,float> nextAuditLog=new();
    void AuditLog(string key,string message)
    {
        if(nextAuditLog.TryGetValue(key,out var next)&&Time.unscaledTime<next)return;
        nextAuditLog[key]=Time.unscaledTime+5f;Plugin.Logger.LogInfo(message);
    }

    Dictionary<string,string> DraftState()=>new Dictionary<string,string>{
        {"draft/sign",hasDraftStatusSign?FormatSign(draftStatusSignPosition,draftStatusSignYaw):""},
        {"draft/enforcementLights",draftEnforcementLights.ToString()},
        {"draft/editingArea",editingAreaIndex.ToString()}
    };
    void RestoreDraftState(Dictionary<string,string> values)
    {
        hasDraftStatusSign=values.TryGetValue("draft/sign",out var sign)&&TrySign(sign,out draftStatusSignPosition,out draftStatusSignYaw);
        draftEnforcementLights=!values.TryGetValue("draft/enforcementLights",out var lights)||!bool.TryParse(lights,out var enabled)||enabled;
        editingAreaIndex=values.TryGetValue("draft/editingArea",out var index)&&int.TryParse(index,out var parsed)?parsed:-1;
        Plugin.Logger.LogInfo($"[DRAFT RESTORE] sign={hasDraftStatusSign} editingArea={editingAreaIndex+1} lights={draftEnforcementLights}");
    }

    void AssignSeekerGear(PlayerCharacter player,string key)
    {
        if(!seekerBeltAssignments.TryGetValue(key,out var belt)||belt==null)
            foreach(var candidate in belts)if(candidate!=null&&!seekerBeltAssignments.Values.ContainsProp(candidate)){seekerBeltAssignments[key]=belt=candidate;break;}
        if(!seekerBellAssignments.TryGetValue(key,out var bell)||bell==null)
            foreach(var candidate in bells)if(candidate!=null&&!seekerBellAssignments.Values.ContainsProp(candidate)){seekerBellAssignments[key]=bell=candidate;break;}
        if(belt!=null)EquipBelt(player,belt);
        if(bell!=null)Stow(player,bell);
        Plugin.Logger.LogInfo($"[GEAR RESERVATION] player={key} belt={belt?.name} bell={bell?.name}");
    }

    // A reserved worker is excluded from transport until its item has been released.
    sealed class ItemStorageJob
    {
        public Prop Prop;
        public PlayerCharacter Worker;
        public PlayerCharacter OriginalHolder;
        public int Attempts;
        public bool NeedsSnatch,Stored;
        public float NextAttempt,NextLog;
    }
    readonly Dictionary<Prop,ItemStorageJob> itemStorageJobs=new(NativeIdentity<Prop>.Instance);
    bool finalThirtyPending;
    float nextStorageCompletion;

    void QueueItemStorage(Prop prop)
    {
        if(prop==null)return;
        if(itemStorageJobs.TryGetValue(prop,out var existing))
        {
            AuditLog("storage-duplicate:"+prop.netId,$"[ITEM STORAGE] existing job reused item={prop.name} netId={prop.netId} managerId={existing.Worker?.playerNetworking?.netId}");
            return;
        }

        var holder=FindHolder(prop);
        itemStorageJobs[prop]=new ItemStorageJob{Prop=prop,OriginalHolder=holder,NeedsSnatch=holder!=null||normalItemAssignments.Values.ContainsProp(prop)||lockedSpeakerAssignments.Values.ContainsProp(prop)};
        Plugin.Logger.LogInfo($"[ITEM STORAGE] queued {prop.name} netId={prop.netId}");
    }

    void ProcessItemStorage()
    {
        if(itemStorageJobs.Count==0&&!cleanupPending&&!finalThirtyPending)return;
        foreach(var pair in new Dictionary<Prop,ItemStorageJob>(itemStorageJobs,NativeIdentity<Prop>.Instance))
        {
            var job=pair.Value;var prop=job.Prop;
            try
            {
            if(prop==null){ReleaseStorageWorker(job);itemStorageJobs.Remove(pair.Key);continue;}
            if(Time.unscaledTime<job.NextAttempt)continue;
            if(!ReferenceEquals(job.Worker,null)&&!CanTransportPlayer(job.Worker)){ReleaseStorageWorker(job);job.Attempts=0;job.Stored=false;}
            if(job.Worker==null)
            {
                // Loose items need no release ceremony. Held items must be snatched.
                if(!job.NeedsSnatch&&FindHolder(prop)==null){PlaceLoose(prop,MarkerStorage(),Quaternion.identity);itemStorageJobs.Remove(prop);continue;}
                foreach(var candidate in standbyManagers)
                {
                    if(cleanupManagerItems.Count>=Mathf.Max(1,standbyManagers.Count-1))break;
                    if(!CanTransportPlayer(candidate)||cleanupManagerItems.ContainsKey(candidate)||candidate.hands.heldCharacter!=null||candidate.hands.heldProp!=null)continue;
                    bool busy=false;foreach(var transport in activeTransports)if(transport.Worker==candidate){busy=true;break;}
                    if(busy)continue;
                    job.Worker=candidate;job.Attempts=0;job.Stored=false;cleanupManagerItems[candidate]=prop;break;
                }
                if(job.Worker==null)
                {
                    if(Time.unscaledTime>=job.NextLog){Plugin.Logger.LogWarning($"[ITEM STORAGE] waiting for free manager: {prop.name}; transports={activeTransports.Count}");job.NextLog=Time.unscaledTime+5f;}
                    job.NextAttempt=Time.unscaledTime+.05f;continue;
                }
                Plugin.Logger.LogInfo($"[ITEM STORAGE] managerId={job.Worker.playerNetworking.netId} reserved for {prop.name} netId={prop.netId}");
            }
            if(!CanTransportPlayer(job.Worker)){ReleaseStorageWorker(job);job.Attempts=0;continue;}
            // Managers are removed from allPlayerCharacters, so FindHolder
            // cannot observe the worker even after a successful pickup.
            var holder=job.Worker.hands?.heldProp==prop?job.Worker:FindHolder(prop);
            if(!job.Stored&&holder!=job.Worker)
            {
                job.Attempts++;job.NextAttempt=Time.unscaledTime+.05f;
                if(holder!=null)
                {
                    if(job.OriginalHolder==null)job.OriginalHolder=holder;
                    // Match native snatching: automatic pickup alone uses an
                    // ordinary drop, without the previous owner's snatch flag.
                    holder.playerNetworking.ServerDropPropFromSnatch();
                    BroadcastSnapshot(holder.playerNetworking);
                }
                job.Worker.playerNetworking.ServerPickUpPropAutomatic(prop);
                BroadcastSnapshot(job.Worker.playerNetworking);
                // Keep the transfer stable across network ticks. Re-picking an
                // already held item would drop it again on every attempt.
                if(job.Worker.hands.heldProp==prop)job.NextAttempt=Time.unscaledTime+.5f;
                AuditLog("storage-snatch:"+prop.GetInstanceID(),$"[ITEM STORAGE] snatch attempt={job.Attempts} item={prop.name} netId={prop.netId} managerId={job.Worker.playerNetworking.netId} original={job.OriginalHolder?.name} managerHolds={job.Worker.hands.heldProp==prop}; host observation, not remote ACK");
                continue;
            }
            if(holder!=null&&holder!=job.Worker)
            {
                Plugin.Logger.LogWarning($"[ITEM STORAGE] holder changed for {prop.name}; retrying native snatch");
                job.Attempts=0;job.Stored=false;job.NextAttempt=Time.unscaledTime+.25f;continue;
            }
            if(!job.Stored||holder==job.Worker)
            {
                if(!job.Stored)DirectLocalTeleport(job.Worker,MarkerStorage());
                job.Worker.playerNetworking.ServerDropPropAutomatic(true);BroadcastSnapshot(job.Worker.playerNetworking);
                job.Stored=true;job.NextAttempt=Time.unscaledTime+.1f;
                AuditLog("storage-drop:"+prop.GetInstanceID(),$"[ITEM STORAGE] awaiting manager drop: {prop.name} netId={prop.netId} managerId={job.Worker.playerNetworking.netId}");continue;
            }
            PlaceLoose(prop,MarkerStorage(),Quaternion.identity);
            Plugin.Logger.LogInfo($"[ITEM STORAGE] completed {prop.name} netId={prop.netId} managerId={job.Worker.playerNetworking.netId}; attempts={job.Attempts}");
            ReleaseStorageWorker(job);itemStorageJobs.Remove(prop);
            }
            catch(Exception ex)
            {
                job.NextAttempt=Time.unscaledTime+.5f;
                AuditLog("storage-error:"+pair.Key.GetHashCode(),$"[ITEM STORAGE ERROR] item={prop?.name} attempts={job.Attempts} stored={job.Stored}; retry in 0.5s: {ex.GetBaseException().Message}");
            }
        }
        if(itemStorageJobs.Count!=0||Time.unscaledTime<nextStorageCompletion)return;
        if(cleanupPending)
        {
            try{RestoreBorrowedProps();cleanupPending=false;}
            catch(Exception ex){nextStorageCompletion=Time.unscaledTime+.5f;AuditLog("cleanup-error","[CLEANUP ERROR] retry in 0.5s: "+ex.GetBaseException().Message);}
        }
        else if(finalThirtyPending){finalThirtyPending=false;if(phase==Phase.Seeking)GiveFinalSpeakers();}
    }

    void ReleaseStorageWorker(ItemStorageJob job)
    {
        if(!ReferenceEquals(job.Worker,null)){if(CanTransportPlayer(job.Worker))ReturnWorkerToStandby(job.Worker);cleanupManagerItems.Remove(job.Worker);}
        job.Worker=null;
    }

    void BeginRoundItemCleanup()
    {
        finalThirtyPending=false;
        foreach(var item in normalItemAssignments.Values)QueueItemStorage(item);
        foreach(var item in lockedSpeakerAssignments.Values)QueueItemStorage(item);
        foreach(var player in Players())if(!IsDummyManager(player)&&player.hands?.heldProp!=null&&IsGear(player.hands.heldProp)&&!brushes.ContainsProp(player.hands.heldProp))QueueItemStorage(player.hands.heldProp);
        normalItemAssignments.Clear();lockedSpeakerAssignments.Clear();
        Plugin.Logger.LogInfo($"[CLEANUP] item jobs={itemStorageJobs.Count}; transports continue independently");
    }

    void RestoreSessionProps()
    {
        if(!NetworkServer.active)return;
        int restored=0;
        foreach(var pair in propOrigins)
        {
            var prop=pair.Key;if(prop==null)continue;
            try
            {
                PlaceLoose(prop,pair.Value.Position,pair.Value.Rotation);
                if(prop.rb!=null){prop.rb.useGravity=pair.Value.Gravity;prop.rb.isKinematic=pair.Value.Kinematic;}
                restored++;
            }
            catch(Exception ex){Plugin.Logger.LogWarning($"[SESSION RESTORE] {prop.name}: {ex.GetBaseException().Message}");}
        }
        Plugin.Logger.LogInfo($"[SESSION RESTORE] restored={restored}/{propOrigins.Count}");
    }
}
