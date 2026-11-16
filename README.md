# Unfunctional

A first-person comedy game about deliberately terrible game mechanics: confusing menus,
pointless calibration, wall clipping, email support, unreliable tools, and achievement spam.
Built with **Unity 6000.2.6f2**, **URP 17.2.0**, and the legacy Input Manager.

## Open and play

1. Install Git LFS before cloning; the repository includes several GB of binary assets.
   ```sh
   git lfs install
   git clone https://github.com/himwho/unfunctional-game.git
   cd unfunctional-game
   git lfs pull
   ```
2. In Unity Hub, add the **`unfunctional/` directory**, not the repository root.
3. Open it with **6000.2.6f2**. Allow the initial asset/package import to finish.
4. Open `Assets/Scenes/GLOBAL.unity` and press Play.
5. In the Editor or a Development Build, press **F1** to open the level selector. It pauses gameplay and
   releases the cursor; click a stage or use **Up/Down + Enter**. **Esc/F1** closes it. The normal
   campaign begins with the intentionally confusing menu.

Do not run **Setup All Scenes** to open an existing checkout. That is a scene-authoring
utility, not a required installation step. The authored scenes and references are already included.

## Controls

- **WASD / mouse:** move and look; **Space:** jump; **Left Shift:** sprint where allowed.
- **E:** interact; **left click:** use a held tool or press physical keypad buttons.
- **Esc:** pause; the pause menu offers resume, restart, and quit (Level 11 intentionally disables restart).
  In the Editor/Development Builds it also offers **LEVEL SELECT (F1)**.
- **Scene editing:** Level 4 decorations are saved under `Level4_Art` in `LEVEL4.unity`.
  See [scene editing and debug controls](docs/SCENE_AUTHORING.md).
- **Workshop:** **1–4 / mouse wheel** select inventory slots; **Q** drops a tool.
  Replacement crates provide unlimited tools. The shovel shares the four-slot inventory.
- **Second-person shooter:** left click fires; **R** reloads or restarts after death.
- **Email puzzle:** email Rodney yourself, or press **F2** for an explicitly local,
  on-screen reply. Local mode sends no email and needs no backend.

The microphone calibration offers a skip when no mic is available, or after ten seconds.
The DLC store is fictional and does not process payments.

## Saving and continuing

The game saves after each gameplay stage finishes loading. On your next launch, choose
**CONTINUE SAVED GAME** on the menu to restart that stage; individual tools, puzzle steps,
and enemy waves reset. Chosen Level 2 calibration values travel with the checkpoint.
Finding the original hidden Start button starts a new run at calibration.

The ending marks the campaign complete and offers a fresh run. Level 11 failure clears the
checkpoint and retained calibration and returns to the menu. There is no forgiving RNG mode.
Editor saves are separate from standalone-build saves; tests use an isolated temporary key.

## Design direction

Deliberately bad mechanics are the point of Unfunctional. Repetition, unreliable tools,
annoying menus, strict timers, misleading presentation and severe penalties are intentional.
**Ask the creator before changing mechanics, difficulty, pacing, penalties, or adding assists.**
Finish missing content and develop the art around the existing rules.

The experimental real F3 menu and pacing changes have been removed at the creator's request.
Workshop counts are restored to 5 shovel / 40 hammer / 40 saw / 55 wrench successes;
sweeping remains physical. QTE behavior, stamina, effects and campaign-reset punishment use
the pre-change implementation. The shaped shovel, dressed finale and credits remain.
See [design direction](docs/DESIGN_DIRECTION.md) and [art opportunities](docs/ART_AND_LEVEL_IDEAS.md).

## Campaign

The build starts in `GLOBAL`, which loads one stage at a time additively:

`LEVEL1 → LEVEL2 → LEVEL3 → LEVEL4 → LEVEL5 → LEVEL6 → LEVEL7 → LEVEL8 → LEVEL9 → LEVEL10 → LEVEL11 → LEVEL_2PS → LEVEL_CRAFTING → LEVEL12 → ending`

The numbered asset names are historical: `LEVEL_2PS` is the second-person shooter,
and `LEVEL12` is the achievements finale. Their names are not their campaign positions.
Changing a C# array initializer does not update an already serialized scene; update
`GLOBAL` and Build Settings together. **Unfunctional → Validate Campaign** checks this.

## Documentation

- [Development status and audit](docs/PROJECT_STATUS.md): where development stopped, implemented changes, known gaps.
- [Implemented art and crafting](docs/ART_IMPLEMENTATION.md): room treatments, recipe, asset locations and editing workflow.
- [Architecture](docs/ARCHITECTURE.md): scenes, lifecycle, systems, and extension points.
- [Testing and builds](docs/TESTING.md): automated checks and manual acceptance checklist.
- [Roadmap](docs/ROADMAP.md): suggested next work, in priority order.
- [Design direction](docs/DESIGN_DIRECTION.md): preserve the intentional bad mechanics; approval boundary.
- [Art and level ideas](docs/ART_AND_LEVEL_IDEAS.md): unfinished work, concept sketches and unimplemented pitches.
- [Credits and attribution](docs/ATTRIBUTION.md): asset sources, notices, and remaining provenance work.
- [Email support server](email-support-server/README.md): optional backend and deployment limitations.
- [Original design notes](game-design/README.md): historical ideas; not an implementation specification.

## Repository layout

- `unfunctional/Assets/Scripts/`: first-party runtime and editor code.
- `unfunctional/Assets/Scenes/`: authored campaign scenes; `GLOBAL` owns shared services.
- `unfunctional/Assets/Prefabs/`: player, doors, and reusable props.
- `unfunctional/Packages/`: pinned Unity package manifest and lock.
- `email-support-server/`: optional Node/SMTP service for Level 4.
- `game-design/`, `reference-design/`: original planning and references.
- `docs/`, `tools/`: maintenance documentation and source validation.

## Validation

```sh
python3 tools/audit_project.py
```

In Unity: **Unfunctional → Validate Campaign**, then **Unfunctional → Run Campaign Smoke Tests**.
The smoke runner opens GLOBAL, enters Play mode, exercises real scene loading and selected
regressions, writes `unfunctional/TestResults/CampaignSmoke/results.json`, then stops Play mode.
See [Testing](docs/TESTING.md) for batch commands and exactly what these checks do and do not cover.

The latest Unity test result and coverage are recorded in [Testing](docs/TESTING.md).
This is a development version. Production email hardening, standalone/platform QA,
and full player-input playthroughs remain on the [roadmap](docs/ROADMAP.md).
