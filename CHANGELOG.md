# Changelog

## 0.2.0

### New
- Each major feature has its own on/off switch, named "Enabled" inside its own section: 1. Ground Movement, 5. Ledge Grab and 9. Door Kick.
- Compatible with Air Strafing (com.dark.airstrafing). While its air control is on, Air Strafing moves you in the air and the alternative jump arc stands aside; switching it off in-game hands the air movement back. Everything else in Aero.Movement (jump fixes, ledge grab, door kicks, ground movement) keeps working alongside it.
- The Acceleration and Stopping friction descriptions give their Source engine meaning (sv_accelerate, sv_friction) and Half-Life 2's values (10 and 4).

### Fixed
- The slowest walking speeds (the last three or four steps of the scroll wheel) moved at a crawl, down to about a quarter of the game's speed at the slowest step. Every step now walks at the game's own speed, standing and crouched, whatever the Acceleration and Stopping friction settings.
- The kick key is no longer V by default, because V is the game's default for mounting a weapon. It is unbound; jumping at a door you can kick still kicks it.
- Lean speed and the climbing changes (thin ledges and railings, climb reach, midair ledge grab) apply only to your own character. They used to change shared game settings, which also affected bots and other players.

### Settings changes
- "Snappier jump arc" is now "Alternative jump arc".
- New section 5. Ledge Grab (its switch and the ledge grab key); Lean, Prone, Sprint and Door Kick are now sections 6 to 9.
- Settings are not carried over from 0.1.0 for the renamed and moved entries: the ground movement, ledge grab and door kick switches, the ledge grab key, the alternative jump arc, and everything in Lean, Prone, Sprint and Door Kick start at their defaults once. Set them again if you had changed them.

## 0.1.0
- First public release.



