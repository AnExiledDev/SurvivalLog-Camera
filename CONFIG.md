# Camera settings

Settings live in `BepInEx\config\com.dev.survivallog.camera.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back. A number outside its range is pulled back into it. Each setting's description in the file is written in English and in Simplified Chinese.

## Controls

| Control | What it does | Setting |
| --- | --- | --- |
| Hold the middle mouse button and drag | Drag left and right to turn the camera around your character, up and down to tilt it | `MouseButton` |
| `Period` (.) / `Comma` (,) | Turn left / right. These are the game's own rotate keys; if you changed them in the game's key settings, the mod uses yours | `RotateLeftKey`, `RotateRightKey` |
| `Insert` / `Delete` | Tilt steeper (toward looking straight down) / flatter | `TiltUpKey`, `TiltDownKey` |
| `PageUp` / `PageDown` | Raise / lower the camera | `RaiseKey`, `LowerKey` |
| `Home` | Put the angle, height and zoom back to the game's | `ResetKey` |
| Scroll wheel | Zoom in and out, further than the game allows | `ExtraIn`, `ExtraOut`, `ScrollSpeed` |
| `Space` | The game's center camera key: brings the camera back to your character | `CenterKeyResetsView` |
| Floor buttons, or `F1` to `F3` | The game's way to look at another floor; the camera then stays there | `StayOnStairs`, `StayAfterMenuClose`, `StayAfterBuildMode` |

## Snap

| Setting | Default | What it does |
| --- | --- | --- |
| `StayOnStairs` | `true` | Keep the camera on the floor you are looking at when your character takes stairs or a ladder, instead of jumping to them as they set off or as they come out on their new floor. |
| `StayAfterMenuClose` | `true` | Keep the camera on the floor you are looking at when an object's menu closes, instead of jumping back to your character's floor. |
| `StayAfterBuildMode` | `true` | Keep the camera where it is when you leave build mode, instead of jumping back to your character. |

These only hold the camera while you look at another floor (with the floor buttons, or `F1` to `F3`), rather than the camera following your character. After a menu or build mode the camera still goes back to your character if you were looking at their own floor. On stairs and ladders it stays put, even when your character comes out on the floor you are looking at. The game's center camera key (`Space` by default) always brings the camera back to your character.

## Rotate

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Turn the camera all the way around your character and tilt it up and down. To turn, hold the middle mouse button and drag, or hold the game's rotate keys, `Period` (.) and `Comma` (,). To tilt, drag up and down, or hold `Insert` or `Delete`. Off: only the game's own rotate keys turn the camera, the game's way, and there is no tilting. Raising and lowering still work. |
| `MouseButton` | `Middle` | The mouse button you hold and drag to turn and tilt the camera: `Middle`, `Right` or `None`. Drag left and right to turn, up and down to tilt. Dragging with this button no longer moves the camera across the map. |
| `MouseSensitivity` | `0.25` | How many degrees the camera turns for each pixel you drag, from 0.01 to 5. Higher turns faster. |
| `InvertX` | `false` | Turn the other way when you drag left and right. |
| `InvertY` | `false` | Tilt the other way when you drag up and down. |
| `YawLimit` | `0` | How far the camera may turn to each side of the game's angle, in degrees. 0 lets it turn all the way round. |
| `PitchMin` | `25` | The flattest the camera may look, in degrees below the horizon, from 5 to 89. The game's own angle is always allowed. |
| `PitchMax` | `85` | The steepest the camera may look, in degrees below the horizon, from 5 to 89. 90 would be straight down. |
| `KeyRotateSpeed` | `90` | How fast the rotate keys (`Period` and `Comma` by default) turn the camera, in degrees a second, from 1 to 720. |
| `KeyTiltSpeed` | `45` | How fast the tilt keys (`Insert` and `Delete` by default) tilt the camera, in degrees a second, from 1 to 720. |
| `AllowInBuildMode` | `true` | Keep your turned view in build mode, and keep turning and tilting there. Off: while you build, the view eases back to the game's angle, and your view comes back when you leave build mode. |

## Height

| Setting | Default | What it does |
| --- | --- | --- |
| `Min` | `-4` | How far the lower key (`PageDown` by default) may take the camera below the game's height, in metres, from 0 to -50. |
| `Max` | `20` | How far the raise key (`PageUp` by default) may lift the camera above the game's height, in metres, from 0 to 100. |
| `Speed` | `6` | How fast the raise and lower keys move the camera, in metres a second, from 0.1 to 100. |

## Zoom

| Setting | Default | What it does |
| --- | --- | --- |
| `ExtraIn` | `10` | How much further the scroll wheel may zoom in than the game allows, in degrees of view angle, from 0 to 60. The view angle never goes below 5 degrees. |
| `ExtraOut` | `15` | How much further the scroll wheel may zoom out than the game allows, in degrees of view angle, from 0 to 60. The view angle never goes above 110 degrees. |
| `ScrollSpeed` | `1` | How far each scroll wheel step zooms, from 0.1 to 5. 1 is the game's speed and 2 zooms twice as far. |

The game counts at most five notches per scroll step, so on a wheel that sends several notches at once, high `ScrollSpeed` values may zoom no further than lower ones. With `ExtraIn` and `ExtraOut` at 0 and `ScrollSpeed` at 1, the mod leaves the game's zoom alone.

## Smoothing

| Setting | Default | What it does |
| --- | --- | --- |
| `Rate` | `12` | How quickly the camera catches up to where you turned it, from 0 to 60. Higher is snappier; 0 moves it at once. |

## Keys

| Setting | Default | What it does |
| --- | --- | --- |
| `RotateLeftKey` | `Game` | Key that turns the camera left. `Game` means the game's own rotate-left key, which is `Period` (.) unless you changed it in the game's key settings. |
| `RotateRightKey` | `Game` | Key that turns the camera right. `Game` means the game's own rotate-right key, which is `Comma` (,) unless you changed it in the game's key settings. |
| `TiltUpKey` | `Insert` | Key that tilts the camera steeper, toward looking straight down. |
| `TiltDownKey` | `Delete` | Key that tilts the camera flatter, toward the horizon. |
| `RaiseKey` | `PageUp` | Key that lifts the camera. |
| `LowerKey` | `PageDown` | Key that lowers the camera. |
| `ResetKey` | `Home` | Key that puts the camera's angle, height and zoom back to the game's. |
| `CenterKeyResetsView` | `false` | Also put the camera's angle, height and zoom back to the game's when you press the game's center camera key (`Space` by default). |

Keys take Unity key names, such as `F`, `Keypad8`, `PageUp` or `Mouse3` (the full list is Unity's [KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) page). `None` turns a key off.

A key the game already uses, or one another Survival Log mod uses, is turned off at startup and the log names the setting and who has the key. Pick another key for it. Time Control's pause and center keys are read from Time Control's own settings file; without that file, `Space` and `G` are kept free for it.

If neither rotate key ends up with a key, the game's own rotate keys keep working the game's way.
