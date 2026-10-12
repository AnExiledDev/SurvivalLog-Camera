# Camera

Turn, tilt and lift the camera, zoom a little further, and stop it snapping back to your character while you look at another floor.

Survival Log's camera looks at your character from one fixed angle, and it snaps back to them whenever they take the stairs, an object menu closes or build mode ends, even while you are looking at another floor. Camera lets you orbit the view all the way round, tilt it steeper or flatter, raise and lower it, and zoom further each way, and it keeps the view on the floor you chose. It only moves the camera. It never changes your character, the world or a save.

## Features

- **Orbit**: drag with the middle mouse button to turn the camera around your character and tilt it. The game's own rotate keys turn it too, smoothly and all the way round.
- **Tilt**: `Insert` tilts the view steeper (toward looking straight down), `Delete` flatter. By default the angle stays between 25 and 85 degrees below the horizon. The game's own angle is always allowed.
- **Height**: `PageUp` lifts the camera, `PageDown` lowers it.
- **Reset**: `Home` puts the angle, height and zoom back to the game's.
- **Wider zoom**: the scroll wheel zooms 10 degrees further in and 15 further out than the game allows, and you can make each scroll step bigger or smaller.
- **Zoom stays put**: the plain game puts your zoom back to an older setting whenever your character takes stairs or a ladder while the camera follows them. Camera keeps the zoom you chose.
- **No more snapping**: while you are looking at another floor, the camera stays there when your character takes stairs or a ladder (also when they come out on their new floor), when an object menu closes, and when build mode ends. Each has its own setting. When your character arrives on the floor you are looking at, the camera follows them again. The game's center camera key still snaps to your character.
- **Steps aside when the game needs the camera**: expeditions, photo mode, cutscenes and the death camera get the game's own view, and your view comes back afterwards. In build mode the view eases back to the game's angle (turn on `AllowInBuildMode` to keep yours).
- **A log file**: what the mod set up, and every snap it held back, is written to `BepInEx\camera.log`.

| Key | What it does | Setting |
| --- | --- | --- |
| Middle mouse drag | Turn and tilt | `MouseButton` |
| The game's rotate keys | Turn left and right | `RotateLeftKey`, `RotateRightKey` |
| `Insert` / `Delete` | Tilt steeper / flatter | `TiltUpKey`, `TiltDownKey` |
| `PageUp` / `PageDown` | Raise / lower | `RaiseKey`, `LowerKey` |
| `Home` | Back to the game's view | `ResetKey` |

The keys do nothing while you type in a text box, while a menu or panel is open, or while you rebind a key. A key the game or another Survival Log mod already uses is turned off and the log says which one; pick another in the settings. Time Control's pause and center keys are read from its own settings and left alone.

Camera adds no data to your save and does not edit game files. You can remove it at any time.

## Known issues and limits

Cameras are fiddly, and this one changes how the game's camera moves in almost every scene. If the view jumps, gets stuck, ends up inside a wall or a key does nothing, please tell me (see [Feedback](#feedback-and-bug-reports)), even if you are not sure the mod caused it. Most pieces have their own setting, so you can turn off the one that misbehaves.

- Leaving build mode only keeps the view while you were looking at another floor. On your character's own floor the game puts the camera back on them, as before.
- While you look at another floor, the game's rotate keys orbit the view there. In the plain game they send the camera back to your character.
- With `MouseButton` on `Middle`, dragging with the middle mouse button turns the camera and no longer pans it. Pan with the keyboard, or set `MouseButton` to `Right` or `None`.
- Build mode, photo mode, expeditions, cutscenes and the death camera use the game's own camera. Your view comes back when they end.
- The death camera may start from your turned angle rather than the game's.
- Zoom changes the field of view, as the game's own zoom does, not the distance to your character. Zoomed far out, the edges of the screen stretch.
- The keys hold back for the whole time a page that takes typing is open (the crafting table and the codex), not only while you type in its search box. The game draws its menus as web pages and only says whether a page takes typing, not whether a text box in it has focus.
- `ScrollSpeed` can only go so far: the game caps one scroll step at five notches' worth, so on a wheel that sends several notches at once, high values zoom no further than lower ones.
- The mod's own text, in the log, is in English.

## Requirements

- Survival Log 1.1.18317. Built for this version; a game update can break it until the mod is updated.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game, version `6.0.0-be.788`.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Download `Camera.dll` from the [latest release](https://github.com/AnExiledDev/SurvivalLog-Camera/releases/latest) and put it in the `BepInEx\plugins` folder inside the game folder (the folder with the game .exe). Full path example: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\Camera.dll`

## Uninstall

Delete `Camera.dll` from the `BepInEx\plugins` folder. For a clean slate, also delete `BepInEx\config\com.dev.survivallog.camera.cfg` and `BepInEx\camera.log`.

## Configuration

Settings live in `BepInEx\config\com.dev.survivallog.camera.cfg`, created the first time the game starts with the mod. Edit it while the game is closed; changes apply at the next game start. See [CONFIG.md](CONFIG.md) for every setting.

## Troubleshooting

Built against BepInEx `6.0.0-be.788`.

Check `BepInEx\LogOutput.log` for a line starting `===== Camera 0.1.1 started`. It lists which pieces are on. The lines after it say whether each hook went in, which keys the camera uses (and which were turned off because something else uses them), and the game's camera angle once a world loads.

The same lines are in `BepInEx\camera.log`, which survives restarts. It also gets a line for every snap the mod held back.

If the camera does not turn at all, look in the log for a line starting `could not`. If a key does nothing, look for its setting name: the log says when a key was turned off and why.

## Feedback and bug reports

Found a bug, have an idea, or something feels off? Come say so on the Core Directive Discord: https://discord.gg/trTsyxUFqg . You can also leave a comment on the Nexus page or [open an issue](https://github.com/AnExiledDev/SurvivalLog-Camera/issues). When reporting a bug, attach `BepInEx\LogOutput.log` and `BepInEx\camera.log`.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against BepInEx and the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies come from the game and are not part of this repo. The .NET 8 SDK is required.

```
dotnet build src/Camera -c Release -p:GameDir="C:\path\to\Survival Log"
```

`GameDir` is the game folder. The default in the project file is a local path you will almost certainly need to override. The output DLL is at `src\Camera\bin\Release\Camera.dll`.

The unit tests cover the pure camera rules (limits, easing, the zoom range, which snaps are held back, key clashes and reading Time Control's keys) and need nothing from the game:

```
dotnet test tests/Camera.Tests
```

## License

MIT. Copyright (c) 2026 AnExiledDev. See [LICENSE](LICENSE).
