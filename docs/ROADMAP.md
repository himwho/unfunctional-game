# Roadmap: finish the satire

Read DESIGN_DIRECTION.md before proposing behavior changes. Deliberate inconvenience is a
core requirement. Discuss mechanic changes with the creator before implementation.

## Implemented first art pass

The requested room treatments for Levels 4, 8 and 11, custom UI for Levels 1–2, workshop
crate/soil/fracture art, twenty store images, finale award art/audio and mandatory crafting are
implemented. See ART_IMPLEMENTATION.md. The crafting stage has its complete recipe and exit.

## Next art and content work

- Review the new rooms in Play mode and at target resolutions. Replace procedural props with
  artist-made meshes and unique wear textures where that adds character; retain interaction volumes.
- Polish the new manufacturing stations' presentation, award sounds and shovel fracture under
  normal tool use. The current pass supplies working models and effects, not a final cinematic pass.
- Playtest the complete route from shooter through crafting to finale, alongside the older rooms.
- Additional level pitches remain in ART_AND_LEVEL_IDEAS.md. Only mandatory crafting was selected
  for implementation; the other mechanics still need a creator decision.

## Decisions to discuss

- Level 5's treadmill conversation is an unimplemented historical pitch, not an agreed task.
- Level 6 mislabeling is implemented but currently off in the scene. Do not enable it silently.
- Language calibration simulates the requirement through sound levels; it is not actual TTS or
  speech recognition. Decide whether the absurd simulation is itself the intended joke.
- The supporter-DLC/unskippable pre-credit idea is not the current skippable credits system.
  Its product scope and timing require a separate decision.
- New levels listed in ART_AND_LEVEL_IDEAS.md need selection and mechanic approval first.

## Completion and release verification

- Complete every existing level with ordinary input in Editor and a standalone build. Record
  accidental blockers separately from intended failure and deliberate punishment.
- Check workshop tool aiming/sweeping, full QTE traversal, shooter waves/death/retry, finale
  interactions and the ending. Tests presently cover selected rules, not every player action.
- Verify quit/relaunch Continue, save corruption handling, microphone denial and focus changes.
- Live email delivery remains unverified; harden and test its backend before relying on it.
- Verify asset provenance in ATTRIBUTION.md; profile the walking scene and shooter on target hardware.
- Add source-audit CI and licensed Unity checks. Measure build dependencies before removing demos.
