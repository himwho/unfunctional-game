# Game Design Document

### LEVELS
- A bad puzzle for just starting the game using shitty menus?
- Audio/Video Settings Menu Puzzle
- clipping through first door
- email support door (setup and send an email)
- dumb NPC conversation
- Quicktime events to door
- compass following task level (constantly collowing compass and paths) all hallways to make the compass redundant mostly
- dlc door, open a fake shop page, reward the user with store game credits after enough browsing of fake items has occurred
- long run distance door (reference all walking simulators), add stamina bar mechanics for sprinting that gets repetitive
- item degradation level (you use items but they immediately break before you finish each task), shovel that breaks while digging
- bad RNG door (give the player a choice and inform them of the % chance of failure), two options are that the user can either spawn an easy room which has a 25% chance of not including a door which would force the user to quit and restart, or spawn a difficult room but we make this chance a "coming soon with DLC" and not available yet
- 2nd Person shooter door, fight dumb AI but camera view is from their perspective of you
- Supporters Steam DLC that adds that user's name to a pre-credit roll that is unskippable
x over achiever, constant achievement abuse level


#### TODO
- game-design: finalize level descriptions and order of levels
- design: moodboard and color pallette
- design: add references and pulls to `reference-design` dir
- Level4: Email support for temp code to unlock door


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
An obscene amount of audio/video adjustment steps, we will view these settings as different screens we have to progress through: 
1. Brightness screen adjustment, the NEXT button needs to be a certain brightness to work
2. Contrast screen adjustment, but if you go too far you have to redo the brightness
3. Left, Top and bottom edge/tear adjustment (next button hidden as overflow)
4. Right edge/tear adjustment (next button hidden as overflow)
5. Left audio channel gain adjustment (max volume)
6. Right audio channel gain adjustment (max volume)
7. Input gain adjustment, which requires using the microphone input, we require the user to yell loudly because 10% of the loudest 3second average gain of the input mic will be used to set the output volume
8. Set the language by using TTS, require the user to read two words of each of these languages arabic, english, spanish, french, german, chinese (mandarin), chinese (fuzhou)
9. Setting for visual compression, start with nothing readable because of bad visual degradation and have the slider "raise resolution"
10. Show a screen with an obscene amount of particles that are obscuring the text instructions saying "set at 22% of the slider to continue"

#### Requirements: 
- UI Menus
- Input sliders alter visuals Scripting
- Input sliders alter audio Scripting

## LEVEL3
A simple room as per our design aesthetic, door doesn't directly open but instead requires a wall clip glitch

#### Requirements: 
- Mesh level
- Setup clipping rules for Main Camera/Pawn
- Narration trying to excuse the mistake

## LEVEL4
Level is a room with a door and a keypad next to the door, above the keypad is a sticky note that has rodney@please.nyc (and another sticky note saying "this guy always changes the code"), the player is expected to tab out of the game and email rodney@please.nyc, and get a response of just a temp code (which only works for 15 seconds), the code response should be 9 digits and the user has to type that in to open the door and complete the level.

#### Requirements:
- EC2 server that has the mail server and a simple nodejs app for generating a 9 digit numerical code that only lasts 15 seconds and emails that response code to every user who emails the "rodney@please.nyc"

## LEVEL5
Dumb NPC, an unnecessary and long NPC conversation

#### Requirements:
- NPC Pawn
- Dialogue script and UI
- Dialogue Options and scripting for decision tree
- Restart dialogue when exit nearby distance check
- Dialogue at the end needs to generate a unique code each level load

## LEVEL6
Quitime event level, forces the user to do timed key based quicktime events in a long sequence. Using the hallway design from LEVEL4, there should be a quicktime event keyboard key for each step the character needs to take and eventually for each action of opening the door.

#### Requirements: 
- Quicktime events are displayed 

## LEVEL7

#### Requirements:
- 