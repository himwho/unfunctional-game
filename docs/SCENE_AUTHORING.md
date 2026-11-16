# Editing department scenes and jumping between stages

## Move Level 4 decorations

1. Stop Play mode and open `unfunctional/Assets/Scenes/LEVEL4.unity`.
2. Expand **Level4_Art** in the Hierarchy.
3. Choose a group: **Furniture**, **Signs**, **Wall Trim**, **Floor Mail**, or **Lighting**.
4. Move, rotate or scale objects using Unity's normal Transform tools. The counter owns its
   telephone and paperwork, so moving the counter moves those details with it.
5. Save the scene with **Cmd+S**. Your transforms persist when you play again.

These are ordinary scene objects. They have no runtime spawner or label-fitting component.
The original door, keypad and clue notes remain separate. Decorations have no active colliders,
so moving them will not silently change the puzzle's interaction targets. Avoid placing visible
geometry over the keypad or clues. TextMesh labels can be edited in the Inspector; their scale
is now authored, so fit longer text manually.

The counter is now along the right/back side, cabinets are beside the front-right wall, and the
header is raised above the door. The new right-wall signs leave the original clue notes clear.
Materials are saved in `Assets/Art/Department/Level4/` and can also be edited normally.

The one-time command **Unfunctional → Scene Art → Install Editable Level 4 and Repair Pause**
has already run. It preserves an existing `Level4_Art` root and its positions. You do not need to
run it when editing. Do not use Setup All Scenes to apply these changes.

## Debug level selector

Open **Assets/Scenes/GLOBAL.unity**, press Play, and focus the Game view. Press **F1** (or **fn+F1**
if macOS reserves the key). You can also choose **LEVEL SELECT (F1)** from the pause menu.

- Click a named stage to load it.
- **Up/Down** selects a row; **Enter** loads it.
- **Page Up/Down** moves five rows; **Home/End** selects the first/last row.
- The mouse wheel scrolls the list. The selected keyboard row scrolls into view automatically.
- **RELOAD CURRENT** restarts the stage; **NEXT LEVEL** loads the following stage.
- **Esc**, **F1**, or **BACK** closes the selector and returns to the previous screen.

Opening the selector pauses gameplay and releases the mouse automatically. Closing it restores
the appropriate cursor mode, including an open shop or the pause screen. All fourteen stages,
including crafting and the finale, are listed. Rows display 1–14; internal scene indices use 0–13.
Debug jumps use normal scene loading and update the normal stage checkpoint.

The selector is available in the Editor and Development Builds. It is disabled in ordinary
release builds by default. This does not change the game's deliberately frustrating mechanics.

## Pause layout

The pause buttons now occupy separate vertical bands in **GLOBAL → PauseCanvas**. Their old
positive/negative vertical offsets caused the overlap and have been removed. You can edit these
RectTransforms in the GLOBAL scene. The debug and ordinary pause panels are mutually exclusive.


## Levels 8, 11 and the finale

These scenes now contain their new objects directly, just like Level 4:

- `Assets/Scenes/LEVEL8.unity` → **Level8_Art**.
- `Assets/Scenes/LEVEL11.unity` → **Level11_Art**.
- `Assets/Scenes/LEVEL12.unity` → **Level12_Art**.

Each root groups **Furniture and Displays**, **Signs and Certificates**, **Wall and Floor
Dressing**, and **Lighting**. The finale also has **Room and Interactions**, containing its
walls, lights, plaque, button, exit and spawn point. The finale manager keeps serialized
references to the interactive objects, so moving or renaming them preserves their wiring.
Move a whole exhibit's parts together when adjusting a plinth, caption or certificate.

Stop Play, adjust the objects, and save the scene. No runtime builder or lettering fitter
replaces these transforms. The art's materials are saved under `Assets/Art/Department/Level8`,
`Level11`, and `Level12`. The finale's exit-indicator material is copied at runtime so an unlock
cannot permanently recolor its saved material.

**Unfunctional → Scene Art → Install Editable Departments** has already run. It preserves
existing art roots. It is for migration/recovery, not routine scene editing. The crafting room
and workshop dressing still use their runtime builders; they were not part of this migration.

## Keypads and doorway repairs

Physical keypads still use the finger and left-click, with the original animation delay.
The aim offset now scales from its authored 1080-pixel reference height. Previously the fixed
320-pixel downward offset could put the ray below a small Game view. A small marker shows the
aim point near the keypad and turns green over a valid key. Level 4's reach is now 1.5 metres;
the previous one-metre camera distance could not reach its lower keys with the scaled player.
Store-overlay clicks no longer also start a physical keypad press in Level 8. The sale sign
and showroom header have been raised clear of the keypad and door.

The player remains **3 metres tall and 1.5 metres wide**. The crafting and finale door visuals
are now **3.8 metres tall and 2.4 metres wide**. These stages retain their existing E-to-complete
exit behavior. The existing mesh wall openings in Levels 4, 8 and 11 are retained. Their
DoorController root collider was an invisible stationary barrier after the leaf fell; success
now releases it. The same repair covers the falling-door sequences in Levels 4–11. Their
imported leaf has a rotated local axis: pushing along that axis drove the door down instead
of tipping it clear. The force now uses the doorway's forward direction and the actual leaf
bounds. The original impulse, mass and opening delays remain intact.

Level 11 still requires walking through its successful exit and still resets the campaign on
a failed roll. Code lengths, expiry, prices and puzzle gates have not been relaxed.
