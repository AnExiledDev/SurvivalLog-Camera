# Camera

A camera you can turn, tilt and lift, that stays where you put it.

Survival Log's camera looks at your character from one fixed angle, and it snaps back to them whenever they take the stairs, an object menu closes or build mode ends, even while you are looking at another floor. Camera lets you orbit the view all the way round, tilt it steeper or flatter, raise and lower it, zoom a little further each way, and keeps the view on the floor you chose.

## Features

- **Orbit**: drag with the middle mouse button to turn the camera around your character and tilt it. The game's own rotate keys turn it too, smoothly and all the way round.
- **Tilt**: `Insert` tilts the view steeper (toward top-down), `Delete` flatter. The angle stays between 25 and 85 degrees below the horizon by default; the game's own angle is always allowed.
- **Height**: `PageUp` lifts the camera, `PageDown` lowers it.
- **Reset**: `Home` puts the angle, height and zoom back to the game's.
- **Wider zoom**: the scroll wheel zooms 10 degrees further in and 15 further out than the game allows.
- **No more snapping**: while you are looking at another floor, the camera stays there when your character takes stairs or a ladder, when an object menu closes, and when build mode ends. Each has its own setting. The game's center camera key still snaps to your character.
- **Stands back when the game needs the camera**: expeditions, photo mode, cutscenes and the death camera get the game's own view, and your view comes back afterwards. In build mode the view eases back to the game's angle (set `AllowInBuildMode` to keep it).
- **A log**: what the mod set up, and every snap it held back, is written to `BepInEx\camera.log`.

| Key | What it does | Setting |
| --- | --- | --- |
| Middle mouse drag | Turn and tilt | `Rotate.MouseButton` |
| The game's rotate keys | Turn left and right | `Keys.RotateLeftKey`, `Keys.RotateRightKey` |
| `Insert` / `Delete` | Tilt steeper / flatter | `Keys.TiltUpKey`, `Keys.TiltDownKey` |
| `PageUp` / `PageDown` | Raise / lower | `Keys.RaiseKey`, `Keys.LowerKey` |
| `Home` | Back to the game's view | `Keys.ResetKey` |

Keys do nothing while you type in a text box, while a menu or panel is open, or while you rebind a key. A key the game or another Survival Log mod already uses is turned off and the log says which one; pick another in the config. The config file is `BepInEx\config\com.dev.survivallog.camera.cfg`.

Camera adds nothing to your save and edits no game files. Remove it at any time.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. The .NET 8 SDK is required.

```
dotnet build src/Camera -c Release -p:GameDir="C:\path\to\Survival Log"
```

The output DLL is at `src\Camera\bin\Release\Camera.dll`.

The unit tests cover the pure camera rules (limits, easing, zoom range, which snaps are held back, key clashes) and need nothing from the game:

```
dotnet test tests/Camera.Tests
```

Layout: `src/Camera/Core` is plain C# with no game or Unity types; `src/Camera/Adapters` holds the Harmony hooks, which only read the game and hand plain values to the core.

## License

MIT. See [LICENSE](LICENSE).
