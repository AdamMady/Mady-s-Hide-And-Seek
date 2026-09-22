# Changelog

## 1.1.0

- Compare native game-object identity in item tracking, ownership checks, and protected-board lookups. Multiple managed references to one item now share a cleanup job, and instruction signs use their registered protection and saved position.
- Include item and manager network IDs in cleanup diagnostics, including duplicate job requests.
- Fixed endless item cleanup snatches by checking the reserved manager's hands directly; managers are absent from the normal player roster. This lets cleanup advance to dropping/storing the item and removing seeker gear.
- Close and lock the Setup menu during round setup, hiding, and seeking; reopen it during intermission.
- Retain modded-client teleport acknowledgements while awaiting movement updates; retry the destination if a completed client teleport drifted away. Cancelled requests remain cancelled.
- Reset pending teleport acknowledgement when retargeting or replacing a player, and include the final-30 speaker option in host settings sync.
- Clear stale throw coordinates when rejecting protected-item grabs; preserve legitimate held items and existing bell attachment correction.
- Replace unsuccessful hide/show refresh with explicit protected-sign/light placement, including boards without saved marker entries.
- Block client movement/velocity commands for placed signs/lights and attached seeker gear so trailing commands from rejected pickups cannot move them.
- Reject edits to HideNSeek's claimed signs and resend the host's current text.
- Changed the seeker-win sign to one continuous line.
- Isolated item-storage errors with logged retries so transport processing can continue; cleanup retries instead of silently abandoning failed storage.
- Excluded signs/lights from gear cleanup and prevented delayed pickup repairs from competing with item-storage jobs.
- Preserved normal hider items and their ownership rules when final-30 speakers are disabled.
- Removed the second item-chance roll after setup teleports. Changing item chance no longer disables maintenance of existing assignments.
- Blocked item transfers during setup; transfers between eligible hiders remain available during play.
- Used native snatch notifications for final-30 transitions, caught-player item storage, and round-end cleanup. Managers hold acquired items for 0.5 seconds, then explicitly drop before storage; retries no longer repeatedly drop and re-pick an already held item.
- Reserved cleanup managers without waiting for every player teleport to finish. Busy managers no longer trigger the direct-release fallback; at least one manager remains available for player transport.
- Prevented cleanup from dropping players carried by transport managers.
- Preserved seeker belt/bell reservations across rejoins and prevented normal-item allocation during the final speaker period.
- Restarted transport stages when an active destination changes, waiting for release before repositioning the manager.
- Restored borrowed prop positions before clearing an active host session.
- Restored draft settings on failed preset loads and included draft sign placement, light preference, and edit target in preset saves/autosaves.
- Prevented area deletion during rounds and corrected active-area indices after deletion.
- Cleared live play-sign placement when its saved position is cleared.
- Reused object-preview border beams instead of repeatedly destroying and recreating them.
- Moved routine pickup, item, transport, and setup traces to Debug logging; round events and actionable warnings remain visible.

## 1.0.3

- Fixed an issue where the HideNSeek menus would open while typing in text chat or on a whiteboard.
- Made it impossible to open the menus while outside a lobby.

## 1.0.2

- Fixed a bug where the round would not start and a map was unplayable.
- Fixed desynchronized whiteboard positions.
- Fixed scanner effects remaining after a round ended when the scanner was in use.
- Changed the description to "Hide And Seek in Big Walk that only the host needs!"

## 1.0.1

- Fixed a minor bug where a hider could quickly drop their item, grab another hider's item, and keep it.
- Changed the description to "Hide And Seek in Big Walk that only YOU need!"

## 1.0.0

- Initial release.
