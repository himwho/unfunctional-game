# Game Design Document

### LEVELS
- A bad puzzle for just starting the game using shitty menus?
- Audio/Video Settings Menu Puzzle
- clipping through first door
- dumb NPC conversation
- Quicktime events to door
- over achiever, constant achievement abuse level
- email support door (setup and send an email)
- compass following task level (constantly collowing compass and paths)
- dlc door, open a fake shop page
- long run distance door (reference all walking simulators), add stamina bar mechanics
- bad RNG door
- item degradation level (you use items but they immediately break before you finish each task)
- 2nd Person shooter door, fight dumb AI but camera view is from their perspective of you
- FPS but the every aim gun throws your aim somewhere random


#### TODO
- game-design: finalize level descriptions and order of levels
- planning: breakdown development components for first 3 levels
- design: moodboard and color pallette
- design: add references and pulls to `reference-design` dir
- Asset: Create main character (temp stick figure design?)
- Asset: Create Input Management system


#### DONE


# LEVEL DESCRIPTIONS

## GLOBAL
- Main Character Asset
- Pause Menu Asset

## LEVEL1
A stupidly confusing MainMenu UI system, difficult to find the `Start Game` button

#### Requirements:
- UI Menus
- Clicking InputManager

## LEVEL2
An obscene amount of audio/video adjustment steps:
- brightness
- contrast
- left screen tear
- right screen tear
- top screen tear
- bottom screan tear
- left channel audio
- right channel audio
- poor threshold compression parameter
- visual effect adjustment for particles/fog (that barely exist)

#### Requirements: 
- UI Menus
- Input sliders alter visuals Scripting
- Input sliders alter audio Scripting

## LEVEL3
A simple room as per our design aesthetic, door doesn't directly open but instead requires a wall clip glitch

#### Requirements: 
- Mesh level
- Setup clipping rules for Main Camera/Pawn

## LEVEL4
Dumb NPC, an unnecessary and long NPC conversation

#### Requirements:
- NPC Pawn
- Dialogue script and UI
- Dialogue Options and scripting for decision tree

## LEVEL5
Quitime event level, forces the user to do timed key based quicktime events in a long sequence

#### Requirements: 
- Quicktime events script (script attaches to each object to be a quicktime event with public var being correct key and display key (additional puzzle of display key changing or being incorrect?)
- Animations for responses?

## LEVEL6