# Department art and mandatory crafting

Implemented locally November 15–16, 2026, in Unity **6000.2.6f2**. This is a playable first art pass,
with deliberately cheap institutional furniture, gold-painted prestige props, worn teal concrete,
paperwork and mail-order product collages. Existing gameplay remains intentionally inconvenient.

## What is in the game

- **Level 4 / Rodney's support annex:** abandoned service counter, modeled telephone and coiled
  cord, paper stacks, filing cabinets, unattended ticket display, noticeboard and correspondence.
  The existing door, clue notes, finger and keypad retain their interactions. Decorations are
  now saved directly in `LEVEL4.unity` under `Level4_Art`; edit their transforms outside Play mode.
- **Level 8 / premium outlet:** four empty gold-painted plinths, velvet ropes, sale signs,
  a red runner, product posters, twenty bespoke catalogue illustrations and selected-product art.
  The twenty illustrations match the original catalogue in order. Browsing eight distinct items,
  the 99,999-credit reward, 49,999-credit door purchase and code flow are unchanged.
- **Level 11 / probability department:** filing cabinets, claims desk, rejection crate,
  probability notices and a folded shroud over a display cabinet beside the unavailable option.
  The original two buttons and both authored outcomes remain; the shroud is scenery.
- **Levels 1–2:** riveted, beveled button skins; ribbed slider handles and etched track ticks.
  Existing hitboxes, slider ranges, callback wiring, evasive motion and calibration rules remain.
- **Level 10:** slatted replacement crates with corner straps, nails and stencils; irregular soil
  clods and surrounding debris; a shovel that flexes and snaps into separately simulated blade
  and grip pieces with jagged wood ends and a generated cracking sound. Replacement hitboxes,
  five digging uses, all 140 counted uses, durability 3 and other tool animations remain.
- **Finale:** five modeled awards (shoe, jump arrow, pause bars, book and button), framed
  certificates with seals/ribbons, six matching UI icons and five synthesized award sounds.
  The five required objectives, notification escalation and exit gate are unchanged.
- **New stage / LEVEL_CRAFTING:** a complete room with six workstations, a recipe ledger,
  carried intermediate items, a finished display-only handle and an installable exit handle.

## Crafting sequence

The stage sits **after LEVEL_2PS and before LEVEL12**. There are now fourteen campaign stages,
plus the GLOBAL bootstrap. Use **E** while looking at a station within 3.2 metres.

1. MATERIALS: collect an alloy billet.
2. GRINDER: turn the billet into metal filings.
3. GRINDER: turn the filings into artisanal metal powder.
4. COMPACTOR: press the powder back into an alloy billet.
5. FOUNDRY: cast an unapproved handle.
6. NOTARY: notarize the handle.
7. DISPATCH: package the notarized handle.
8. DISPATCH: unpackage it.
9. EXIT: install the handle and proceed to the achievement finale.

The display handle cannot be collected. Wrong stations and repeated resource pickups cannot
consume, duplicate or erase the carried item. There is one ordered inventory item and no
exhaustible supply that can make the recipe impossible. Pause blocks transactions. Restart
and Continue restart this stage from step one, consistent with the campaign's stage checkpoints.

## Playing and editing

Open **Assets/Scenes/GLOBAL.unity** and press Play. In the Editor, use **F1** and scroll to
**13 Mandatory crafting (LEVEL_CRAFTING)** to inspect the new room directly. F1 pauses
gameplay and unlocks the cursor automatically. Click a row or use Up/Down + Enter. Esc/F1
returns to the previous screen, including pause if opened there. The finale is displayed as
**14 Achievement finale**. Internal scene indices remain zero-based. The development selector
scrolls in small Game views and remains unavailable in ordinary release builds by default.

All required scenes and baked assets are present. **Do not run Setup All Scenes.** If restoring
missing generated assets, use **Unfunctional → Prepare Department Art and Crafting**, followed
by **File → Save Project**. Preparation preserves existing assets, creates the crafting scene
only if absent, and registers its place in the campaign. It is not an authored-scene rebuild.

- `Core/CampaignRoomArt.cs`: compatibility hook; it never spawns or moves geometry.
- `Editor/DepartmentSceneComposition.cs`: editor-only recipes for Levels 8, 11 and the finale.
- `Editor/DepartmentSceneAuthoring.cs`: saves their art and materials without rebuilding existing roots.
- `Editor/Level4SceneAuthoring.cs`: one-time scene migration and pause layout repair. Existing
  `Level4_Art` is preserved on repeat runs. No authoring component is attached to the saved props.
- `Core/DepartmentProps.cs`: reusable model pieces and material ownership. Decorative props
  have no active colliders; original interaction targets remain exposed.
- `Core/DepartmentLabelFit.cs`: fits decorative lettering inside physical signs.
- `Levels/LevelCrafting_Mandatory.cs` and `CraftingStation.cs`: room, recipe and interaction targets.
- `Levels/Level10_WorkshopArt.cs`: crate/soil dressing and shovel fracture presentation.
- `UI/ShopProductArt.cs`: scene-owned sprites cut from the atlas and selected-product image.
- `UI/UnfunctionalUISkin.cs`: changes graphics without changing input or control geometry.
- `Editor/DepartmentArtBaker.cs`: creates reusable meshes, UI sprites and audio only when absent.
- `Resources/GameArt/`: atlas, widget sprites, award icons, fracture meshes and WAV files.

**Level 4:** open `Assets/Scenes/LEVEL4.unity` outside Play mode and expand `Level4_Art`.
Furniture, Signs, Wall Trim, Floor Mail and Lighting contain editable objects. Move/rotate/scale
the groups or individual objects and save the scene. Nothing respawns or refits these props
at runtime. New material assets are in `Assets/Art/Department/Level4/`.

**Levels 8, 11 and the finale:** edit `Level8_Art`, `Level11_Art` and `Level12_Art` in their
respective scenes. Their named groups are editable outside Play mode. The finale's room and
interactive exhibits are also saved in the scene; its manager uses serialized references.
Materials live in `Assets/Art/Department/Level8`, `Level11`, and `Level12`.

Crafting and workshop dressing still assemble at runtime. Changes made only to those runtime
objects disappear when Play ends. See [SCENE_AUTHORING.md](SCENE_AUTHORING.md) for the workflow.
Decorative variation uses deterministic geometry, without consuming Unity's gameplay RNG.

## Art provenance

`ShopProducts.png` is generated with the built-in image generation tool. The exact prompt is
stored in `reference-design/concepts/product-art-prompt.txt`. Its 4×5 layout is cropped by Unity
without altering the original bitmap. It depicts the game's fictional products and fictional prices.
The approved environment board remains in `reference-design/concepts/environment-studies-v1.png`.

Widget/icon graphics, soil/fracture meshes and sounds are generated by project code. Props use
the project's existing wood, metal, tile and concrete materials. No new asset-store package,
external music, real payment integration or network service was added.

## Verification and remaining work

See TESTING.md and the latest `TestResults/CampaignSmoke/results.json` for the recorded run.
The suite checks all fourteen scene transitions, asset availability, decorative collider isolation,
the original hostile rule values, physical interaction rays, shovel fracture/replacement cleanup,
crafting order/pause/restart/completion, and crafting-to-finale progression. Captures are taken
from the Unity renderer; some use a staged camera with the player model hidden for inspection.

This is not a full ordinary-input playthrough or a standalone release build. Physical aiming,
tool-use animation under all camera angles, every shooter wave, microphone/email integration,
resolution coverage and release performance still need acceptance playtests. The procedural props
are intentionally simple; unique wear textures, authored animation polish and artist-made meshes
can build on this pass without changing the game's bad-mechanic premise.
