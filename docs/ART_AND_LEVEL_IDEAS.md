# Art opportunities and new level pitches

**Implementation update — November 16, 2026:** the creator selected the three room studies,
mandatory crafting, custom UI, workshop prop/fracture art, store product images and finale
awards/audio. Those now exist in the project. See ART_IMPLEMENTATION.md for the implemented
scope and editing guide. The pre-implementation audit below is retained as design history;
its selected art gaps are addressed by this first pass. Other level pitches remain proposals.


November 15, 2026. The purpose is to finish **Unfunctional** while celebrating bad mechanics.
Nothing in this document authorizes gameplay changes. The generated concept board is an
exploratory visual proposal, not imported level geometry or final game art.

## What is actually unfinished

All 13 campaign stages are wired, with the new workshop digging task and achievement finale
present. That is not a claim that every puzzle has passed an ordinary-input full playthrough.
The clearest remaining work falls into distinct categories:

- **Levels 1–2, custom UI art:** original TODOs explicitly call for custom buttons/sliders;
  these remain useful art tasks. Keep evasive buttons and obtuse calibration as designed.
- **Level 4, live email route:** local F2 fallback works in tests; real delivery remains unverified.
  The art is established but could tell more of Rodney's story through scenic support clutter.
- **Level 5, treadmill conversation:** this exists as a design-note idea, not implemented content.
  Current Gorp conversation/reset/code mechanics and character art exist. Building a treadmill
  is a new mechanic decision, not a routine completion fix.
- **Level 6, design mismatch:** strict QTE/code flow exists; mislabeling code is available but
  `LEVEL6.unity` sets `enableMislabeling: 0`. Decide whether that disabled joke should be used.
- **Level 8, store presentation:** browse tracking, rewards and fake purchase exist. The interface
  is built in code; bespoke product images, ugly ad layouts and showroom props would add character.
- **Level 10, incomplete presentation:** five tasks now exist, but replacement crates remain
  simple colored boxes and soil is a shrinking/lowering slab. The shaped shovel has a generic
  shake-and-drop break animation; a separate snapping blade/shaft visual is a concrete art gap.
- **Finale, new rather than final:** five objectives, exit and credits are present. Environment
  uses existing materials and a door; custom trophies/certificate props, achievement icon families,
  and sound variation would make the deliberately excessive recognition more distinctive.
- **Supporter-DLC pre-credits:** an unskippable supporter roll is a historical concept, not the
  current skippable credits implementation. It needs a separate scope/design decision.

Levels 3 and 7 have established authored rooms/routes and recent collision/visual work. Level 9
has substantial authored scenery; retain its distance and tedious stamina. The second-person
shooter has its wave/combat loop and an assigned NPC prefab; primitive NPC/arena creation in code
is a fallback, not evidence that its actual scene lacks art. Its full waves/death/end flow still
needs ordinary-input validation. Level 11's unavailable DLC option is intentionally unfinished
inside the fiction; completing that choice would change the gag.

## Rough environment studies

![Three exploratory environment studies](../reference-design/concepts/environment-studies-v1.png)

The shared language comes from existing project art: dirty tile grids, worn painted concrete,
cheap beige doors, strip lights, deadpan labels and inexplicable space windows. The rendering
in this sheet is deliberately a sketch/paint-over look. It proposes models/material treatment;
it does not require making the running game look like an illustration.

### Rodney's customer-support annex — Level 4

An abandoned service counter, tangled beige phone cables, stacked paper and obsolete office
furniture make a single door code feel like an organizational crisis. Reuse the existing door
and keypad; add scenic props outside the player path. Keep the email/code interaction unchanged.
Starter asset list: counter module, telephone, in/out trays, filing cabinet, noticeboard decals.
The board's incidental text is placeholder art copy, not a change to the changing-code rule.

### Premium exit showroom — Level 8

Treat the ordinary beige door like a luxury product inside a visibly cheap discount shop.
Empty gold-painted plinths, sagging red ropes, absurd product cards and laminated upsells sell
the joke. Keep the existing fake store and browsing requirement. No real payment flow.
Starter asset list: plinth, rope post, acrylic sign holder, sale decal sheet, fake product thumbnails.

### Department of probability — Level 11

A government statistics office gives the rigged choice an excessively official presentation.
Use two existing buttons, a 75% placard and a shrouded 'Coming Soon' option. Keep the room sparse
so the missing exit feels blunt. No extra interactable buttons, pity system, or retry option.
Starter asset list: stamped probability placards, button housings, tarpaulin, noticeboard.

### Other art directions worth sketching next

- **Warranty Void workshop:** battered replacement bins, brittle-looking joints, manufacturing
  reject stamps and a display of snapped tools. Give each failure a distinct material/animation.
- **Installer UI archaeology:** clashing generations of widgets for Levels 1–2; rubber buttons,
  bevels, mismatched fonts, cheap slider tracks. Preserve hitboxes/rules unless a change is approved.
- **Achievement civil service:** mass-produced certificates, tiny trophies on oversized plinths,
  crooked employee-recognition displays. Keep the achievement spam and five-objective gate.

## Level ideas — crafting implemented; others remain proposals

### Inventory management for one key

The exit key is an absurdly large inventory shape. The player repeatedly rearranges useless
mandatory objects to fit it; the world contains only one door. Art: luggage-inspection office,
foam trays, stamped inventory manifests. New systems: inventory packing and drag/rotate UI.

### Mandatory crafting department

Opening an ordinary door requires crafting a handle through several pointless intermediates,
while a finished handle sits behind a 'display only' case. Art: flat-pack assembly showroom,
instruction sheets and overpackaged components. New systems: recipe/dependency state and stations.

### The escort with incompatible speeds

An NPC walks faster than the player walks and slower than the player runs, stopping to discuss
unimportant scenery. Art: guided-tour museum full of identical door exhibits. New systems:
escort movement/proximity and dialogue stops. Keep it distinct from Gorp's stationary conversation.

### Loading-screen logistics

A short physical corridor is divided into excessive loading vestibules. Progress estimates
recalculate and congratulate the player prematurely before continuing. Art: freight depot,
loading-bar signs, rotating-hourglass machinery. New systems: a finite scripted loading sequence;
exact wait durations and failure rules require approval before implementation.

### Tutorial about tutorials

Every mundane action triggers another explanation of the action just performed, until the
exit itself gets an overlong lesson on leaving. Art: educational exhibition with enormous
instruction cards and miniature demonstration doors. New systems: a deliberately intrusive,
bounded tutorial state machine. Confirm interruption rules and ending before building it.

These ideas expand the catalogue of bad mechanics rather than making existing mechanics easier.
Choose a concept and approve its rules before adding a new scene or changing campaign order.

## Generation record

The three-panel sheet was generated using the built-in image-generation tool. The exact prompt
is saved beside it as `reference-design/concepts/generation-prompt.txt`. It is not an image of
current gameplay. No generated texture, model, scene replacement or mechanic was imported into Assets.
