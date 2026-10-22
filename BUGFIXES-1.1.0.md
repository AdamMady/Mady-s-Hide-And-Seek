# 1.1.0 diagnostic guide

Build and source review completed. Multiplayer behavior still needs live testing; a successful build does not prove remote prediction or scanner effects are corrected.

## Protected-item rollback revision

Protected-item rejection now builds a fresh empty/snatch state when the server holds no prop. It does not copy old drop coordinates, launch rotation or wind-up from a previous throw. A legitimate held prop is preserved using a fresh held-prop state. The request action number is retained. Native PlayerHands.Drop consumes saved drop coordinates; replaying those against a newly predicted prop was a concrete defect, although the reported bell-to-whiteboard incident was not captured with sufficient data to prove that exact cause.

Removed the unsuccessful observer hide/show refresh. Every protected sign or pooled buoy now shares the same movement-guard classification, including SpareSignProp without a markerPositions entry. After rejecting a grab, loose protected objects receive explicit authoritative placement even if the host sees no drift: saved marker placement when available, otherwise their current host position. Attached gear retains its existing home-reference correction. No repeated repair timer was added.

Logs: `[PICKUP ROLLBACK]` includes prior drop-data flag/coordinates and object netId; `[PROTECTED PROP RETURN]` includes return position and whether it used saved placement; `[PROTECTED MOVE BLOCK]` identifies rejected movement commands. Repeated logs are throttled. Test bell grab -> whiteboard grab, repeated play/end/seeker sign grabs, normal hider-item transfer, and brush pickup. Remote rendering remains unacknowledged; this build is not multiplayer-verified.

Vanilla ActionPickUpPlayer requests server approval without locally picking up; ActionPickUpProp predicts locally before its command. PropHome.blockGrabbing and Prop.blockRemovingFromHomes are not directly replicated. No verified existing networked lock state for these boards has been established, so no speculative lock or host-only flag change was enabled. Bell attachment repair and ten-snatch storage remain intact.

Logs are written through BepInEx to `BepInEx/LogOutput.log`.

| Symptom | Log tags and interpretation |
| --- | --- |
| Final-30 option disabled | `[FINAL THIRTY] speakers disabled` means normal ownership remains active. No final-period storage should be queued. |
| Incorrect item probability | `[ITEM ROLL]` records one roll per player per round and its outcome. A rejoin or completed setup teleport must not produce a second roll. |
| Changing item chance | `[ITEM CHANCE]` records the new chance and the number of existing assignments that remain enforced. |
| Setup item stealing / flicker | `[PICKUP BLOCK]` records actor, item and phase. Setup transfers are blocked. `[ITEM TRANSFER]` and `[ITEM TRANSFER RECONCILE]` record legal ownership changes during play. |
| Item fails to return | `[ITEM ENFORCE]` records required item, observed item, holder and phase, at most once per player every five seconds. |
| Scanner effects / cleanup | `[ITEM STORAGE]` records each queued item, original holder, manager reservation, native snatch attempt, host-observed possession, manager drop and completion. Acquired items stay held for 0.5 seconds; retries only reacquire missing items. The previous holder receives the native snatch state before the manager pickup snapshot. If no manager is free, jobs wait and log periodically. A host-observed drop is not an acknowledgement that a vanilla remote has cleared its visual effect. |
| Cleanup interrupts transport | `[CLEANUP]` records pending jobs and active transport count. Cleanup must not call a global drop-player operation. |
| Rejoin gear conflict | `[GEAR RESERVATION]` records reserved belt/bell. Reservations remain attached to the player key for the round. `[ITEM REJOIN]` records suppression of normal allocations in the final period. |
| Wrong teleport destination | `[TRANSPORT RETARGET]` records old/new targets and old stage. `[TRANSPORT STAGE]` records subsequent stage changes, worker, host-observed position, target and carry state. Stage -1 waits for release before moving the worker. |
| Disable session | `[SESSION RESTORE]` records restored prop count and any individual failures before origin data is cleared. This restores positions/physics flags; it does not recreate an original pinned/carry relationship. |
| Failed preset load | `[PRESET ROLLBACK]` records restoration of settings/drafts. `[DRAFT RESTORE]` records draft sign presence, edit target and warning-light preference. |
| Wrong warning lights / sign | `[AREA DELETE]` records the adjusted active index. `[SIGN CLEAR]` records the selected and active areas. |
| Preview performance | `[PREVIEW]` occurs when allocating another reusable beam, not every preview refresh. |

## Focused live checks

1. With final-30 speakers off, cross 30 seconds with a scanner held; it should remain assigned. With speakers on, verify the remote scanner effect clears before speakers are delivered.
2. During setup, attempt to steal an assigned item. During play, an empty-handed hider should still be able to steal it, while an assigned hider cannot swap.
3. End a round while players are recovering across borders. Item cleanup should advance alongside transport, and managers should release items before returning to standby.
4. Disconnect/rejoin a seeker, then a hider during the final speaker period. Check gear ownership and absence of a normal-item reroll.
5. Redirect an active recovery by catching the player or ending the round. Verify the new target is reached.
6. Save an unfinished draft with a sign, load another preset, then load Autosave. Also try a malformed preset and verify the previous draft remains.

No game launch or multiplayer test was performed while preparing this release. Previous release folders and ZIPs are unchanged.

## Two-client mod link check

Cleanup regression: logs showed attempts 1 through 160 with `managerHolds=True` but no manager drop. `KeepDummyOutOfRoster` removes managers from `allPlayerCharacters`, so `FindHolder` cannot find a manager holding an item. Storage now checks its reserved worker's hands first. Verify a host-held item and a guest-held item each produce `awaiting manager drop`, then `completed`, followed by gear cleanup at round end.

Use two distinct authenticated game accounts, both with the same HideNSeek DLL. Start with the host already inside a lobby; join with the second client, then press Setup and Start Round on the host.

1. Host log: `[MOD LINK] <player> connected with HNS mod ... protocol 2`. Client log: `[MOD LINK] host settings received; automatic client session enabled`. If either is absent, stop and compare both logs before testing teleports.
2. During setup and border recovery, client log: `[MOD TRANSPORT CLIENT] confirmed token=...`; host log: `[MOD TRANSPORT] complete ...`. Check both players see the guest in the intended area. A host `failed` line means the modded path timed out and reverted to manager transport.
3. Load another preset during intermission, then start a round. Check the guest reaches the newly selected area and time of day matches the host.
4. End the round, disconnect the guest, and rejoin. The host must log a new mod link; the guest must receive settings again. Join an ordinary lobby afterward and confirm the guest's HideNSeek session is inactive.

Code inspection confirms packet IDs, field counts, token matching, cancellation, and fallback paths. It cannot prove that the game's EOS connection accepts two clients or that client movement reaches the host; these require the live check above.
