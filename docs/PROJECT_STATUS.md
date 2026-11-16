# Development status and audit

Audit date: **November 15, 2026 (America/New_York)**.
Source baseline: **`4cf7c1d` on `main`**, latest commit dated **March 17, 2026**.
The checkout already contained uncommitted gameplay additions and documentation when this
verification pass began. Those were preserved. The sections below distinguish the committed
baseline, the existing local implementation, and the changes made during verification.

## Latest authorized implementation — November 16, 2026

The three approved room treatments are implemented for Levels 4, 8 and 11. Levels 1–2 have
custom control art; Level 8 has twenty product illustrations; the workshop has replacement-crate,
soil/debris and snapping-shovel art; the finale has five trophies, certificates, six icons and
five sound variations. Mandatory crafting is a new playable stage between the shooter and finale.
There are fourteen stages. See ART_IMPLEMENTATION.md for the exact recipe and editing workflow.
Original task counts, timings, stamina, store rules and RNG punishment were preserved.

## Where development stopped

The last work was visual and collision polish, not early prototyping:

- `4cf7c1d`: stopped door clipping in Levels 5, 6, 9, and 10.
- `02944c1`: textured Gorp and exit narration in Level 7; fixed door collision.
- `be9d649`: matched Level 8 brightness to Level 4.
- `ba4f32c`: improved Level 4 sticky notes and handwritten text.

There were **12 campaign stages** wired in the serialized GLOBAL scene: Levels 1–11
and the second-person shooter. The achievements script existed, but no achievements
scene, objectives, or real popup UI existed. The last stage's completion only wrote a log line.
The repository README contained two lines, and design notes lagged behind implementation.

## Level-by-level findings

1. **Confusing menu — LEVEL1.** Existing moving/hidden menu puzzle and completion path.
   Needs usability testing so the joke is discoverable, including at different aspect ratios.
2. **Settings puzzle — LEVEL2.** Ten calibration steps implemented. The no-mic escape
   in step 7 was commented out; restored. Language calibration counts sound peaks;
   it is not speech recognition, translation, or TTS despite the old notes.
3. **Wall clipping — LEVEL3.** Authored room, scripted intentional clipping, narration, exit.
   Keep intentional clip surfaces distinct from accidental collision defects.
4. **Email keypad — LEVEL4.** Physical finger/keypad interaction and HTTP validation exist.
   Offline codes were console-only, with no reachable request control in the current
   physical-keypad flow. Added an F2 local reply with visible digits, expiry, and local validation.
   Live email delivery and server availability were not tested or changed.
5. **Gorp dialogue — LEVEL5.** Dialogue, proximity resets, generated keypad code, narration,
   and celebration already implemented. The treadmill idea in the notes is unimplemented;
   it is a proposed design extension, not a missing prerequisite.
6. **Quick-time events — LEVEL6.** Timed prompts, mislabelled keys, failure handling,
   and a code phase exist. The pre-change implementation and strict timers are restored.
   Mislabeling is implemented in code but disabled in the serialized scene; discuss enabling it.
7. **Compass hallways — LEVEL7.** Existing route, compass behavior, Gorp, narration, exit.
   Art/route polish was among the most recent completed work.
8. **DLC store — LEVEL8.** Tablet, fake store, unique-item tracking, credit reward,
   purchase flow, and physical keypad exist. Old “improve store page” notes are partly stale.
9. **Walking simulator — LEVEL9.** Authored scenery, progress, stamina, narration, and exit.
   The huge scene is tracked by Git LFS. Duration and minimum-spec performance need playtests.
10. **Workshop — LEVEL10.** Hammer, saw, wrench, and physics sweeping existed. Only the
    shovel task was disabled; a comment incorrectly referred to “two tasks.” Added digging,
    a held shovel model, inventory switching, and replacement supplies for all five tools.
    Removed the runtime override that ignored the Inspector's durability value of 3.
11. **Bad RNG — LEVEL11.** Both choice buttons and the 75% success path exist. Failure
    intentionally returns to the beginning and disables level restart. Preserved that
    authored behavior; the subsequently requested forgiving mode has now been removed.
12. **Second-person shooter — LEVEL_2PS.** Existing enemy-view camera, waves, guns,
    health, ammo, reload, death/restart, and win handling. It was already reachable from
    GLOBAL, but the scene-registration utility omitted it. Registration now preserves it.
13. **Mandatory crafting — LEVEL_CRAFTING.** Added six workstations and nine ordered transactions,
    a carried intermediate item, restart/pause integration and the display-only handle gag.
14. **Achievement finale — LEVEL12.** Added a self-contained prototype room, five explicit
    objectives (walk, jump, wait, read, press), checklist, bounded notification spam,
    unique objective tracking, and a locked exit. The follow-up replaced placeholder presentation
    with project materials, door art, exhibit dressing, sound, narration, and ending credits.

## Existing local implementation, preserved and tested

- Added a visible campaign ending with menu and quit actions.
- Kept serialized campaign order, default order, and scene-registration utility consistent.
- Added transition re-entry protection and validation before unloading the current scene.
- Corrected transition raycast blocking and retained GLOBAL's EventSystem deterministically.
- Prevented level Update handlers and shooter NPCs from processing gameplay while paused.
- Removed duplicate pause-menu event subscriptions and restored timescale on transitions/teardown.
- Fixed delayed workshop break callbacks so they cannot detach a newly equipped replacement.
- Restored the missing Polybrush 1.2.1 dependency used by Level 9 vegetation meshes.
  Preserved its mesh data and removed 192 empty obsolete scene-only script references.
- Centralized Escape handling so closing the store/keypad does not also pause the game.
- Added scene validation and play-mode integration checks; see TESTING.md for limitations.

## Changes made during verification

- **Checkpoint and Continue:** added versioned stage-boundary saves using stable scene names,
  a visible Continue button, saved calibration, invalid-save rejection, completion tracking,
  and explicit fresh-run reset. Opening the menu preserves the checkpoint. Level 11's
  intentional whole-campaign punishment explicitly clears it. Editor and player storage differ.
- **Level 9 runtime repair:** the initial live run completed 105 assertions but recorded
  5,920 Polybrush mesh-access errors plus four native temporary-allocator messages. The
  affected model imports discarded CPU mesh data required by the restored Polybrush components.
  Enabled Read/Write on **26 source models** found through the actual campaign scenes, leaving
  authored geometry, prefab links, and painted mesh data intact. The subsequent full run
  recorded **zero errors**, including no repeat of the allocator messages.
- Added **Repair Campaign Polybrush Imports**, a targeted Editor utility, and added a
  source-mesh readability check to **Validate Campaign** to prevent recurrence.
- Extended the smoke runner with checkpoint tests and temporary isolated storage. Tests
  cover malformed/unsupported/missing-scene saves, every stage checkpoint, Continue UI,
  calibration restoration, completed campaigns, and new-run cleanup.
- Updated setup, architecture, status, test evidence, and the roadmap to describe the
  verified checkout. No commit, push, deployment, or live email was performed.

Read/Write keeps a CPU copy of each affected model's mesh data, so memory profiling of the
walking scene remains important. Unity documents this behavior in
[ModelImporter.isReadable](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/ModelImporter-isReadable.html).
A future optimization can bake the required Polybrush result and remove runtime dependencies,
but must preserve authored vertex data and be visually compared before replacement.

## Current direction and rollback

The creator clarified that bad mechanics are the purpose of the game. The real F3 settings,
assist modes and pacing changes were withdrawn. Fourteen affected source/shader files were
restored exactly from pre-change backups; input retains the separate credits Escape handler.
Workshop counts are back to 140, QTE and stamina behavior is restored, and RNG failure resets
the whole campaign without a forgiving alternative. No authored scenes were regenerated.

The shaped shovel, existing-material finale, narration/sound, skippable credits and font notices
remain. See DESIGN_DIRECTION.md for the current approval boundary and ART_AND_LEVEL_IDEAS.md
for unfinished art, implementation gaps and clearly separated new pitches.

## Remaining findings

### Release blockers

- **Optional email backend needs production hardening.** `/api/request-code` can send
  to a supplied address without rate limiting or input validation. SMTP buffers the full
  message in memory and trusts the message From header for replies. Automated messages
  can cause reply loops. Debug mode exposes active codes and email addresses. No lockfile
  or automated server tests existed at audit time. These are source findings, not claims
  that the deployed service is currently exploitable or even running.
- **Save release QA.** Stage-boundary Continue now works in Editor integration tests.
  Verify quitting/relaunching a standalone build, upgrades between save-schema versions,
  and platform-specific persistence before release. Mid-stage state is deliberately not saved.
- **Visual and device QA remains necessary.** Scene-load smoke tests are not full manual
  playthroughs. Physical keypad aiming, tool alignment, shader output, collision exploits,
  controller devices, and final pacing need acceptance testing on target hardware.

### Quality and maintenance

- Most controls use hard-coded `Input.GetKeyDown`; no central rebinding UI or controller-first flow.
- Changing controls, assists, timers or pacing requires creator approval. Do not treat deliberate
  inconvenience as a defect to optimize away.
- The finale and shovel have received an art pass and credits are implemented. Asset ownership
  and redistribution records still need verification; see ATTRIBUTION.md.
- Large level scripts mix UI construction, scene lookup, rules, physics, and presentation.
  `Level10_ItemDegradation` alone was over 3,100 lines. The new workshop extension is separated
  to avoid rewriting the authored animations during this pass.
- Scene coupling relies on object names such as `Metal Nail`, `Wooden Table`, `hammerhead`,
  and `ButtonTop`. Renaming these silently breaks behavior; prefer serialized references over time.
- Large imported demo packs, high-resolution textures, and baked lightmaps increase import
  time and repository size. Measure build dependencies before deleting anything.
- No CI workflow was present. Add automated source validation first, then licensed Unity checks.

## Validation evidence

The initial source audit, scene validation and checkpoint pass completed 127 checks with zero
recorded errors after fixing 26 Polybrush model imports. Later counts associated with the now
withdrawn settings pass are historical and no longer describe this implementation.

Current coverage and the rollback result are recorded in TESTING.md and
`unfunctional/TestResults/CampaignSmoke/results.json`. The runner traverses actual scenes and
uses direct method calls for selected regressions. It does not prove a full ordinary-input
playthrough, live SMTP delivery, release performance, or standalone save persistence.
