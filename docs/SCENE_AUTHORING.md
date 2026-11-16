# Editing Level 4 and jumping between stages

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
