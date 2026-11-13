# Aero.Movement

My take on the movement for SPT: quicker starts and stops, reliable jumps, climbing that finishes, faster leans and crawls, and completely unbounded kicks.

## Requirements

- SPT 4.1.x (developed and tested on 4.1.6; client only, nothing is installed on the server)
- Optional: Fika (tested with Fika.Core 2.4.4) so other players see your kicks, door breaches and ledge grabs

## Installation

Copy the `BepInEx` folder from the release archive into your SPT folder, so that the mod ends up in:

```
BepInEx/plugins/Aero.Movement/Aero.Movement.dll
```

If you play with Fika, also copy the `BepInEx` folder inside `Fika Compat Patch` into your SPT folder. It adds the
Fika sync next to the mod:

```
BepInEx/plugins/Aero.Movement/Aero.Movement.Fika.dll
```

Without Fika, leave the patch out.

## Multiplayer (Fika)

Kicks, door breaches (including the swing direction) and midair ledge grabs are sent to the other players. They see
them when they also have the mod with `Aero.Movement.Fika.dll`.

## License

MIT, see [LICENSE](LICENSE).
