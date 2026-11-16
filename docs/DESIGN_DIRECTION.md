# Design direction: keep Unfunctional unfunctional

Creator direction, November 15, 2026:

> The point of the game is to show off bad game mechanics. Never assume the mechanics should
> be improved without checking with the creator first. Finish the game while keeping that theme.

This is current user direction, not an inference from the historical design notes.

## Working boundary

- Preserve tedious repetition, fragile tools, tight timings, misleading feedback, inconvenient
  interactions and deliberately severe penalties where they are authored.
- Ask before changing mechanics, difficulty, pacing, task counts, timing windows, stamina,
  rewards, punishment, progression, or adding bypass/assist settings. An optional assist is
  still a design change and requires discussion.
- Art can make the bad idea more expressive: props, materials, wear, signage, animation and
  sound should reinforce the gag. Art proposals do not authorize extra interaction rules.
- A crash, missing asset or broken reference is different from an intentional punishment.
  If a completion fix would change how a puzzle works, explain it and get a decision first.
- Treat design-note TODOs as evidence to discuss, not commands to implement automatically.
  Do not silently turn on unused mechanics or interpret every 'Coming Soon' label as a gap.

## Requested rollback

Removed the added F3 settings menu and preference system, volume/sensitivity overrides,
flash/VHS bypasses, caption scaling, relaxed QTE mode, repetition toggle, stamina improvements,
unlimited stamina and forgiving RNG. Restored original QTE failure progression as well as timing.

Restored workshop counters: shovel 5, hammer 40, saw 40, wrench 55: **140 counted uses**.
Sweeping is physical and excluded from that count. Authored durability remains 3.
Walking source and serialized scene were preserved/restored: 40/s drain, 8/s recovery,
1.5-second recovery delay; the current scene's sprint multiplier is **1.8** (the script default
is 1.3, which is not the serialized scene value). QTE scene: 40 walk events, one-second prompts,
1.5-second code window, reset after three failures. Mislabeling code exists but the scene
currently sets `enableMislabeling: 0`; enabling it requires a design decision.
Level 11 has the original full-campaign reset and no forgiving alternative.

Retained the previously authorized completion work, checkpoint implementation, offline keypad
fallback, workshop supplies, shaped shovel, dressed finale, credits and notices. Those were
outside the two requested rollback areas. No additional gameplay ideas were implemented here.

Old real-option PlayerPrefs data, if present on this machine, is no longer read by the game.
The rollback does not delete unrelated user preferences or campaign saves.

## Verification

Fourteen affected source/shader files were restored byte-for-byte from pre-change backups.
InputManager retains only the unrelated credits Escape handler alongside its original input.
Tests now assert the authored counts/timers/stamina and RNG punishment, plus retained art/credits.
See TESTING.md for the latest runtime result and limits.

## Subsequent creator authorization — November 16, 2026

The creator selected the three environment studies for implementation and explicitly requested
mandatory crafting, custom Levels 1–2 controls, workshop crate/soil/shovel art, product/ad art
and finale awards/audio. Those are now implemented; see ART_IMPLEMENTATION.md. This authorization
adds the crafting recipe and stage to the campaign. It does not reinstate the withdrawn settings,
assists or pacing changes and does not authorize unrelated mechanic changes.
