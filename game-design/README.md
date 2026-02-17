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
Quicktime event level, forces the user to do timed key-based quicktime events in a long sequence. Using the hallway design from LEVEL4, there should be a quicktime event keyboard key for each step the character needs to take and eventually for each action of opening the door. The displayed key occasionally differs from the actual required key (mislabeling). Failing too many resets the entire sequence.

#### Requirements: 
- QTE prompt UI (key display, timer bar, progress bar, feedback text)
- Mislabeled key system (40% chance of showing wrong key)
- Fail counter with full sequence reset after 3 failures
- Hallway-style 3D level with door at the end

## LEVEL7
Compass following task level. The player spawns in a labyrinth of identical-looking hallways and must follow a compass that always points toward the exit. The compass is redundant because the hallways are essentially one path with no real branches -- the joke is that the compass is "helping" navigate an impossible-to-get-lost-in corridor. The compass needle spins erratically near the end just to cause panic. Every hallway segment looks the same: same textures, same lighting, same dead-end decoys that loop back. A LEVEL_DOOR at the end completes the level.

#### Requirements:
- 3D hallway maze (procedural or prefab segments)
- On-screen compass UI that points toward the exit
- Compass erratic behavior near the end
- Identical-looking hallway segments with fake branches
- LEVEL_DOOR at the exit

## LEVEL8
DLC door level. The player enters a room with a locked door that has a big "BUY DLC TO UNLOCK" sign on it. Interacting with the door opens a full-screen fake in-game shop page with absurd microtransaction items (golden shovel skin, premium air, extra gravity, etc). The player must browse enough items (scroll through them, hover or click on at least 8 different items) before the game "rewards" them with enough fake store credits to "purchase" the door unlock DLC for free. The shop UI is intentionally awful with pop-ups, upsells, and "are you sure you don't want to buy?" confirmations.

#### Requirements:
- 3D room with locked LEVEL_DOOR
- Full-screen fake shop UI overlay (Canvas)
- Scrollable grid of absurd fake items with prices
- Browse/interaction tracking (must view 8+ items)
- Fake currency system and "free credits" reward
- Pop-up confirmations and upsell dialogs
- Door unlocks after "purchasing" the DLC

## LEVEL9
Walking simulator door level. A very long hallway that references every walking simulator ever made. The player must walk/sprint an absurd distance to reach the door. There is a stamina bar for sprinting that depletes quickly and regenerates slowly, making the sprint mechanic feel tedious and repetitive. The hallway occasionally has "scenic" moments (a single chair, a cryptic sign, dramatic lighting) to parody the genre. A progress bar at the top shows how far along the hallway the player is, but it moves deceptively slowly at first and then speeds up near the end.

#### Requirements:
- Very long 3D hallway (200+ units)
- Stamina bar UI for sprinting (fast drain, slow regen)
- Sprint speed boost with CharacterController
- Progress bar that is deceptively slow
- "Scenic" set pieces along the hallway
- LEVEL_DOOR at the far end

## LEVEL10
Item degradation level. The player is in a room with a series of tasks that each require using a tool, but every tool breaks almost immediately. Tasks: dig a hole (shovel breaks after 2 swings), hammer a nail (hammer head flies off), saw a plank (blade snaps), turn a bolt (wrench bends), sweep the floor (broom snaps in half). The player has to keep grabbing replacement tools from a tool rack. After enough perseverance (completing all 5 tasks despite the constant breakage), the exit door unlocks. Each tool break has an over-the-top visual/sound cue.

#### Requirements:
- 3D workshop room
- Tool interaction system (E to pick up, click to use)
- Tool durability system (breaks after 1-3 uses)
- 5 task stations with progress tracking
- Tool rack that respawns replacement tools
- Break animations/effects for each tool
- LEVEL_DOOR unlocks after all tasks complete

## LEVEL11
Bad RNG door level. The player enters a room with two big buttons/levers. A sign explains: "Choose your path." Option A spawns an "Easy Room" that has a 75% chance of containing a door and a 25% chance of being doorless (forcing the player to restart the level). Option B says "Difficult Room" but is labeled "COMING SOON - Requires Premium DLC" and is not actually interactable. So the player is forced to gamble on Option A repeatedly until they get lucky. Each failed attempt shows a taunting message ("No door this time!", "Better luck next time!", "The RNG gods frown upon you."). The room resets each attempt. When a door does spawn, it opens immediately on approach.

#### Requirements:
- 3D room with two choice buttons/levers
- RNG system (75% door spawn chance)
- Room reset on failed attempt
- Taunting failure messages
- Option B permanently disabled ("Coming Soon DLC")
- Auto-opening LEVEL_DOOR when it spawns
- Attempt counter display