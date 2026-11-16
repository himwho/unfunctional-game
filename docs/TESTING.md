# Testing and builds

## Automated checks

The lightweight source audit uses Python's standard library:

```sh
python3 tools/audit_project.py
```

It checks campaign/build/default order, scene GUIDs, first-party script metadata, and unresolved
Git LFS pointers. It does not compile C# or resolve references inside Unity prefab instances.

In Unity 6000.2.6f2, use **Unfunctional → Validate Campaign**. This opens each enabled scene,
checks its script references and LevelManager, and checks the bootstrap/player/order contracts.
It restores a previously saved scene setup afterward. Save your work first.
It also checks Polybrush source-model Read/Write settings. If that check fails, use
**Unfunctional → Repair Campaign Polybrush Imports**, inspect the import diffs, and validate again.
The repair preserves scene geometry and painted mesh data.

**Unfunctional → Run Campaign Smoke Tests** opens GLOBAL, enters Play mode, runs integration
checks, and writes `TestResults/CampaignSmoke/results.json` under the Unity project. It leaves
the editor out of Play mode when complete. This is a custom integration runner in the Editor
workflow, not a Unity Test Runner/NUnit suite. The driver lives in Scripts/Tests behind UNITY_EDITOR.

### Batch commands (macOS, from the repository root)

Close the interactive editor for this project before using batch mode; Unity cannot open the
same project twice. Use the pinned editor version, and allow Unity licensing/package access.

```sh
UNITY_EDITOR_BIN='/Applications/Unity/Hub/Editor/6000.2.6f2/Unity.app/Contents/MacOS/Unity'
mkdir -p unfunctional/TestResults

"$UNITY_EDITOR_BIN" -batchmode -nographics -quit \
  -projectPath "$PWD/unfunctional" \
  -executeMethod CampaignValidation.Validate \
  -logFile "$PWD/unfunctional/TestResults/validation.log"

"$UNITY_EDITOR_BIN" -batchmode \
  -projectPath "$PWD/unfunctional" \
  -executeMethod CampaignSmokeTests.Run \
  -campaignResults "$PWD/unfunctional/TestResults/CampaignSmoke" \
  -logFile "$PWD/unfunctional/TestResults/playmode.log"
```

Do **not** add `-quit` to the smoke command: the runner exits after the checks and writes
its own success/failure exit code. Add `-nographics` only when screenshots are not needed.
The runner has a timeout and records runtime errors/exceptions, not just assertion failures.

For a missing achievements scene only, use `CampaignValidation.PrepareAndValidate` once.
Do not use Setup All Scenes as a testing shortcut or to regenerate the authored campaign.

### Smoke coverage and limits

- Boots the confusing menu through GLOBAL, then loads every campaign scene.
- Checks player creation, single EventSystem, scene unload, and duplicate load requests.
- Checks the no-mic fallback without recording or requesting microphone permission.
- Generates, displays, expires, and accepts a local keypad code without sending email.
- Exercises all five workshop task registrations, shovel use and inventory switching,
  repeated tool break/replacement, delayed break callbacks, and door completion gating.
- Checks shooter camera setup, achievements uniqueness, restart reset, pause gating,
  finale completion, ending UI, and return to menu.
- Checks authored QTE timing, stamina settings, 140 workshop uses, RNG probability and hidden restart.
- Forces RNG failure to confirm the whole campaign/calibration reset.
- Checks retained shovel/materials, finale target raycasts, credits focus/navigation and notices.
- Confirms the removed independent settings overlay is absent.
- Uses an isolated checkpoint PlayerPrefs key and restores the previous calibration.
- Checks missing/corrupt/future-version/removed-scene checkpoints; saves after every stage;
  verifies Continue UI, calibration restoration, completed-save suppression, and new-run reset.

The runner jumps between stages and invokes selected methods for deterministic regressions.
It **does not solve every puzzle through player input**, aim every tool, push every trash item,
win shooter waves, verify real email delivery, simulate every random sequence, or profile a release build.
A passing result is a regression check, not proof of a finished or balanced game.

## Manual acceptance checklist

Use GLOBAL and a Development Build as well as Editor Play mode.

- Complete the menu and all ten calibration steps. Deny microphone access and use both fallbacks.
- Verify the intentional wall clip in Level 3 and confirm ordinary doors/walls block bypasses.
- In Level 4, request an F2 reply, submit a wrong code, let a code expire while paused,
  request another, and enter the valid code using the physical keypad. Separately test
  real email only in an authorized test environment with a configured service.
- Complete Gorp's conversation, leave/re-enter the dialogue radius, and test the unique code.
- Finish the QTE/code phase; inspect mislabeled-key feedback, resets, and keyboard layout behavior.
- Traverse the compass route and walk/sprint the entire scenic route. Check stamina recovery.
- Browse distinct store items, earn credits, purchase the fake DLC, and unlock the door.
  Pause/resume and close/reopen the store during each step.
- Workshop: dig five times, replace broken tools, hammer/saw/wrench at their actual targets,
  and physically sweep all trash under the table. Test all slots, repeated same-tool pickups,
  switching during cooldown, dropping/retrieving tools, and exhausted original tool stock.
- Force both RNG outcomes using the Inspector in a test session. Verify the intentional
  restart punishment, the unavailable DLC option, and restoration of the pause restart button.
- Shooter: fire, reload, switch viewpoint, take damage, die/retry, clear every wave, and enter mandatory crafting.
- Crafting: walk the nine-step recipe, inspect the display-only handle, use the wrong station,
  pause/restart, then install the handle to enter the finale.
- Finale: earn each objective via normal movement/input; verify repeated button/plaque use
  does not count twice. Walk to the unlocked exit, finish, return to the menu, and quit.
- Test pause, restart, focus loss and 16:9/ultrawide displays. Identify accidental failures without
  changing authored inconvenience, effects or timing to satisfy a test.
- Quit/relaunch at several stages and choose Continue. Confirm the stage restarts, calibration
  is restored, solving the hidden menu starts fresh, and Level 11 failure clears the checkpoint.
  Repeat in a standalone build: Editor and player saves are intentionally separate.

## Build

Open Build Profiles, select the intended desktop platform, and verify GLOBAL is first followed
by all 14 campaign scenes. Produce a Development Build for QA, then an ordinary build for final
acceptance. F1 is intended for the Editor/Development Builds; inspect `allowInReleaseBuild` before release.

No installable release build or cross-platform certification is implied by the automation above.
Record OS/GPU, resolution, build settings, commit, logs, and remaining failures with every playtest.

## Historical rollback verification

The prior 189-check report described features since withdrawn and is superseded.
The current runner tests the original rules plus retained completion/art work. The rollback run completed **144 checks with zero recorded errors** in Unity
6000.2.6f2 with Metal on November 15, 2026 (America/New_York). Campaign validation runs before
the smoke suite. Source audit and `git diff --check` also passed.

The source restoration compares 14 source/shader files byte-for-byte with pre-change backups.
InputManager retains only the unrelated credits Escape handler. Static searches require no
remaining PlayerOptions, AccessibilityMenu, relaxed-QTE, forgiving-RNG or VHS-bypass code.

The initial 127-check run and repair of 26 readable Polybrush model imports remain part of
project history. Tests use selected direct calls and do not establish ordinary-input completion.
The earlier computer-control mouse experiment delivered clicks with an off-view cursor position;
physical aiming, mouse UI interaction, complete shooter/QTE playthroughs, standalone builds,
live SMTP, platform profiling and the full license-viewer page sequence still need testing.

## Current art and crafting verification

The November 16, 2026 run covers all fourteen campaign stages in Unity 6000.2.6f2 / Metal.
**223 checks passed with zero recorded errors in the latest scene-authoring/debug run.** Campaign validation and the source audit also passed.
The report is `TestResults/CampaignSmoke/results.json`; captures are in the same directory.

New checks cover custom widget sprites/ranges, twenty product images and original store rules,
no active decoration colliders in the dressed rooms/workshop, independently simulated shovel
halves and replacement cleanup, crafting inventory/order/pause/restart/station rays, final
installation and transition to the finale, award icons and sound assets. Rendering captures use
end-of-frame reads; room study cameras hide the player body only for those captures.

The historical comfort-feature report is superseded. The current suite still asserts that F3
options are absent, workshop counts remain 140, strict QTE/stamina values are preserved and
RNG failure clears the campaign. Check names, not just a count, distinguish those runs.
No standalone build or full ordinary-input playthrough was performed in this art pass.


## Scene authoring and development UI verification

The latest November 16 run includes saved Level 4 geometry/materials, absence of runtime
builders/label fitters, repeated Dress calls preserving transforms, no duplicate art roots,
no new art colliders, and signage bounds clear of the original clue notes.

Development-menu checks cover main-menu/pause return states, simulation/cursor restoration,
all fourteen unique stage buttons, jumps to Level 4/crafting/finale/menu, pointer hit ordering,
Continue-overlay layering, open-shop cursor restoration, and non-overlapping pause button rects.
`debug-level-selector.png`, `pause-menu-fixed.png` and `level4-editable-art.png` record the result.
The screenshot capture waits for the newly enabled debug canvas to reach the renderer.

The smoke check count can vary slightly because workshop break/replacement checks run per
replacement. Read the recorded check names and zero-error result, not just the count.
Native F1 opening was also checked in the Unity Game view. Keyboard scrolling and closing are
covered by an additional interactive check. No standalone player build was made for this change.
