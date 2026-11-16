# Architecture and maintenance guide

## Bootstrap and scene lifecycle

`Assets/Scenes/GLOBAL.unity` is build index zero. Its GameManager starts at campaign index
zero (`LEVEL1`). `GameManager.levelSceneNames` is serialized in GLOBAL and is the runtime
source of order; the C# initializer only supplies defaults for new components.

A transition fades out, despawns the previous player, unloads the previous level, loads
one new scene additively, makes it active, removes duplicate EventSystems, waits for level
Start methods, and spawns the Player prefab when `LevelManager.needsPlayer` is true.
It restores the game state and persistent calibration overlays, emits `OnLevelLoaded`,
then fades in. `IsLoading` prevents overlapping transitions and gameplay during the fade.
GLOBAL remains loaded; the GameManager also uses DontDestroyOnLoad.

`LevelManager.CompleteLevel()` is idempotent. It advances through GameManager; completing
the final stage enters `Completed`, freezes simulation, unlocks the cursor, and creates
CampaignEnding. Returning to the menu removes that overlay and restores time.

Do not add a second bootstrap GameManager to a level. To test a scene, start GLOBAL and
use F1. Opening a gameplay scene directly does not automatically create shared services or a player.

## Checkpoints

`CampaignProgress` stores one JSON checkpoint in PlayerPrefs using schema version 1. It records
scene name, campaign-complete state, and the three retained calibration values. It rejects
invalid JSON, unsupported versions, menu/unknown scene names, and invalid setting ranges.

`GameManager` saves after the load/fade finishes for gameplay stages. Loading the menu does
not overwrite progress. `ContinueCampaign()` restores calibration and loads the saved scene;
`StartNewCampaign()` clears the checkpoint and calibration before loading the confusing menu.
The original hidden Start button begins a new run at Level 2 and replaces the old checkpoint
once that stage has loaded. Level 11 uses the explicit new-run path for its intentional punishment.
Completing the finale marks the save complete so Continue does not offer a stale stage.

Storage keys are `Unfunctional.Campaign.v1` in players and `Unfunctional.Campaign.Editor.v1`
in the Editor. `CampaignSmokeDriver` temporarily substitutes a random test key and restores
it and the prior calibration on teardown. It does not overwrite a developer's checkpoint.
No mid-level objects, tools, NPCs, or puzzle progress are serialized. Bump/migrate the schema
when changing save semantics; do not repurpose a scene name for a different stage.

## Polybrush source models

The restored Polybrush components initialize mesh data during Play mode. Their source model
imports require Read/Write, which retains CPU-side mesh data. Twenty-six campaign model
imports were repaired. `PolybrushImportRepair` discovers source models through the actual
loaded campaign, changes only ModelImporter.isReadable, and restores the scene setup.
The validator checks this contract without modifying imports. Do not globally enable
Read/Write on every asset or remove painted component data to hide runtime errors.

## Player and input

- `PlayerController`: CharacterController movement, jump/gravity, and mouse look.
- `InputManager`: shared movement values, cursor control, and Esc/pause handling.
- Individual puzzles still use Unity's legacy Input API for many interactions.
- `GamePauseMenu`: subscribes once to state changes and restores the level's cursor preference.
- `LevelManager.CanProcessGameplay`: excludes pause, transitions, completion, and completed levels.
  `Time.timeScale = 0` alone does not prevent Update calls or mouse/key input.

InputManager dispatches Esc through `LevelManager.TryCloseModal()` before toggling pause.
Modal overlays such as the store and keypad own extra cursor/player state. When adding a
modal, test Esc, pause/resume, restart, and scene unload in every order.

## Doors and interaction

`LEVEL_DOOR` contains `DoorController`, visual door panels, and optional keypad/keyhole mounts.
`KeypadController` manages entered digits and raises submission/request events. A level
subscribes to those events and decides whether a code is accepted. Several levels implement
custom falling-door animations rather than calling the generic sliding-door method.

Preserve serialized script GUIDs and `.meta` files when moving assets. Preserve child names
used by runtime lookup, or migrate those lookups and validate the affected scene together.

## Workshop

`Level10_ItemDegradation.cs` contains the authored inventory, tool animations, four original
tasks, physics sweeping, and door interaction. `Level10_WorkshopSupplies.cs` is the same
partial class and adds the shovel station and replacement supply crates.

There are four inventory slots for five tool types. Tools retain remaining durability when
switched. Q discards a generated shovel; it is always replaceable. Authored tools can be dropped
and picked back up. Supply crates clone inactive templates captured before tools are broken.
The shovel must be used at its soil station; wrong tools cannot advance a task.

Sweeping is a physical condition: the trash must be under `Wooden Table`. Its high use target
is a sentinel, not the number of clicks required. Hammer/saw/wrench use counters are currently
40/40/55. Digging takes five uses, for 140 counted successes excluding physical sweeping. Durability comes from the Inspector (3 in the authored
scene). Replacement supplies preserve the degradation joke without a finite-stock lockout.

## Finale and UI

`Level12_Achievements` builds its own dressed room using `GameArtLibrary`. Real objectives are identified by stable
IDs (`walk`, `jump`, `wait`, `read`, `button`) and recorded in a HashSet. Duplicate and unknown
IDs do not count. Five objectives unlock the exit; the player must still interact with it.
Trivial notification spam is capped at three simultaneous popups and a minimum interval.

`GameScreenUI` builds the new finale/ending canvases using the existing `UIHelper` font and
URP-safe overlay setup. Existing level UIs are retained. New materials are destroyed on unload.

## Presentation assets and credits

`CampaignArtBaker.Prepare` creates `Resources/GameArt/Library.asset`, a shaped shovel mesh
and prefab, and references existing project materials/door/success sound. The assets are
checked in; players do not run the baker. The menu is repeatable and preserves an existing
shovel prefab. Hand edits belong in that prefab; intentional mesh regeneration requires
reviewing the existing asset first. There is no external runtime art dependency.

`CampaignCredits` is opened from the ending. It hides the ending controls while active,
moves keyboard focus, paginates acknowledgments and full bundled font notices, and restores
the ending on close. InputManager routes Escape to the open credits. The withdrawn real-settings system is absent.

## Email puzzle

The live route validates a nine-digit code through HTTPS. The player sends real email outside
the game. F2 explicitly switches the level to local mode, displays a local code, and validates
it locally; it does not contact or send mail. Local codes expire using unscaled time so pausing
does not extend their lifetime. Requests are bounded by a timeout and disposed when complete.
See the backend README before considering live deployment.

## Adding a stage

1. Create a scene with exactly one LevelManager subclass and set player/cursor preferences.
2. Assign a spawn transform or create it before GameManager spawns the player.
3. Use CompleteLevel once the objective and exit conditions are satisfied.
4. Add the scene to GLOBAL's serialized campaign list, the GameManager default list, and
   SceneSetupEditor's registration list. Include its `.meta` file in version control.
5. Run Validate Campaign, smoke tests, and a manual playthrough from the preceding stage.

`CampaignValidation.CreateMissingFinale` is deliberately narrow: it creates only a missing
LEVEL12 scene and never overwrites existing authored geometry.


## Department art and mandatory crafting

The campaign now has fourteen stages. LEVEL_CRAFTING follows LEVEL_2PS and precedes LEVEL12.
LevelCrafting_Mandatory owns a nine-step ordered recipe, station raycasts, one carried item,
and its runtime room. CompleteLevel is called only after the final handle installation.
Stage checkpoints intentionally do not persist individual recipe steps.

GameManager invokes CampaignRoomArt.Dress after each level's Start and before player spawning.
CampaignRoomArt skips Level 4 unconditionally: its decoration is serialized in LEVEL4.unity
under Level4_Art, with persistent materials in Art/Department/Level4. The editor-only
Level4SceneAuthoring migration removes builders and label fitters after saving the geometry;
repeat runs preserve the existing root and positions. DepartmentProps creates temporary
decorative models/materials without active colliders for the other dressed rooms.
Workshop replacement hitboxes stay on their original ToolRack objects; visual crates are separate.
ShopProductArt owns its twenty cropped sprites and destroys them with the level. Widget skins
retain the authored RectTransforms and callbacks. DepartmentArtBaker creates reusable assets.
See ART_IMPLEMENTATION.md for composition, asset paths and how to edit this first art pass.

The smoke runner's menu remains in Editor; its MonoBehaviour and partial test files live in
Scripts/Tests behind UNITY_EDITOR so Unity can instantiate the component while excluding it
from player builds.


## Pause and development level selection

GLOBAL owns the authored pause menu. Buttons use separate normalized vertical bands and zero
anchored offsets. Its canvas order is 800. DebugLevelsButton is wired by GamePauseMenu at
runtime and appears only when DebugPanel is available.

InputManager routes F1 to DebugPanel before gameplay input. The modal selector uses a blocking
UGUI canvas at order 980, above Continue/ending (950) and below transitions (999). Opening it
remembers the current game state, pauses simulation, hides the ordinary pause panel and unlocks
the cursor. Closing restores that state and the current level's cursor preference. Escape is
consumed by the selector, so it cannot also toggle pause. A level jump uses GameManager.LoadLevel,
including transition, player spawning, cursor setup and normal checkpoint behavior.

The selector is enabled in UNITY_EDITOR or DEVELOPMENT_BUILD. Ordinary release builds omit its
entry points unless allowInReleaseBuild is explicitly enabled. This is development tooling;
QTE timing, stamina, workshop repetition/durability, and RNG failure rules are unchanged.
