# Camera settings

Settings live in `BepInEx\config\com.dev.survivallog.camera.cfg`, created the first time the game starts with the mod. Change them in game on the Mod Manager's Mods screen, or edit the file while the game is closed. Settings you change on the Mods screen take effect at once. The exceptions are `Rotate` `Enabled`, `MouseButton` and every `Keys` setting except `CenterKeyResetsView`, which take effect the next time you start the game. Changing a setting doesn't move the camera from where you turned it. Delete the file to get the defaults back. A number outside its range is pulled back into it. Each setting's description in the file is written in English and in Simplified Chinese.

## Controls

| Control | What it does | Setting |
| --- | --- | --- |
| Hold the middle mouse button and drag | Drag left and right to turn the camera around your character, up and down to tilt it | `MouseButton` |
| `Period` (.) / `Comma` (,) | Turn left / right. These are the game's rotate keys, so if you changed them in the game's key settings, the mod uses yours | `RotateLeftKey`, `RotateRightKey` |
| `Insert` / `Delete` | Tilt steeper (toward looking straight down) / flatter | `TiltUpKey`, `TiltDownKey` |
| `PageUp` / `PageDown` | Raise / lower the camera | `RaiseKey`, `LowerKey` |
| `Home` | Reset the angle, height and zoom to default | `ResetKey` |
| Scroll wheel | Move the camera closer to or further from your character | `Closest`, `Farthest`, `ScrollSpeed` |
| `Space` (`G` with Time Control) | The game's center camera key: brings the camera back to your character | `CenterKeyResetsView` |
| Floor buttons, or `F1` to `F3` | Look at another floor, the same as without the mod. The camera then stays there | `OnlyCenterKeyFollows`, `StayOnStairs`, `StayAfterMenuClose`, `StayAfterBuildMode` |

## Snap

| Setting | Default | What it does |
| --- | --- | --- |
| `OnlyCenterKeyFollows` | `true` | Once you have moved the camera away from your character, only the game's center camera key (`Space` by default, or `G` with Time Control) and the camera button on the screen bring it back to them. Stairs and ladders, closing a menu, leaving build mode after moving or placing something and the game's rotate keys all leave the camera where it is, on any floor. Cutscenes, photo mode, starting a game, the tutorial and travelling to another map still move the camera like they normally do. When this is off, `StayOnStairs`, `StayAfterMenuClose` and `StayAfterBuildMode` decide. |
| `StayOnStairs` | `true` | Keep the camera on the floor you are looking at when your character takes stairs or a ladder, instead of jumping to them as they set off or as they come out on their new floor. |
| `StayAfterMenuClose` | `true` | Keep the camera on the floor you are looking at when an object's menu closes, instead of jumping back to your character's floor. |
| `StayAfterBuildMode` | `true` | Keep the camera where it is when you leave build mode, instead of jumping back to your character. |

`OnlyCenterKeyFollows` works for any view you moved off your character, on their floor too, and it already covers everything the other three settings do, so they make no difference while it is on. A game event the mod doesn't know about can still move the camera to another floor or map.

With `OnlyCenterKeyFollows` off, the other three only keep the camera in place while you look at another floor (with the floor buttons, or `F1` to `F3`) instead of following your character. After a menu or build mode the camera still goes back to your character if you were looking at their floor. On stairs and ladders it stays put, even when your character comes out on the floor you are looking at. The game's center camera key (`Space` by default, or `G` with Time Control) always brings the camera back to your character.

## Rotate

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Turn the camera all the way around your character and tilt it up and down. To turn, hold the middle mouse button and drag, or hold the game's rotate keys, `Period` (.) and `Comma` (,). To tilt, drag up and down, or hold `Insert` or `Delete`. When this is off, only the game's rotate keys turn the camera, the same as without the mod, and you can't tilt it. Raising and lowering still work. |
| `MouseButton` | `Middle` | The mouse button you hold and drag to turn and tilt the camera: `Middle`, `Right` or `None`. Drag left and right to turn, up and down to tilt. Dragging with this button no longer moves the camera across the map. |
| `MouseSensitivity` | `0.25` | How many degrees the camera turns for each pixel you drag, from 0.01 to 5. Higher turns faster. |
| `InvertX` | `false` | Turn the other way when you drag left and right. |
| `InvertY` | `false` | Tilt the other way when you drag up and down. |
| `YawLimit` | `0` | How far the camera may turn to each side of the game's angle, in degrees. 0 lets it turn all the way round. |
| `PitchMin` | `25` | The flattest the camera may look, in degrees below the horizon, from 5 to 89. The game's angle is always allowed. |
| `PitchMax` | `85` | The steepest the camera may look, in degrees below the horizon, from 5 to 89. 90 would be straight down. |
| `KeyRotateSpeed` | `90` | How fast the rotate keys (`Period` and `Comma` by default) turn the camera, in degrees a second, from 1 to 720. |
| `KeyTiltSpeed` | `45` | How fast the tilt keys (`Insert` and `Delete` by default) tilt the camera, in degrees a second, from 1 to 720. |
| `AllowInBuildMode` | `true` | Keep your turned view in build mode, and keep turning and tilting there. When this is off, the view eases back to the game's angle while you build, and your view comes back when you leave build mode. |

## Height

| Setting | Default | What it does |
| --- | --- | --- |
| `Min` | `-4` | How far the lower key (`PageDown` by default) may take the camera below the game's height, in metres, from 0 to -50. |
| `Max` | `20` | How far the raise key (`PageUp` by default) may lift the camera above the game's height, in metres, from 0 to 100. |
| `Speed` | `6` | How fast the raise and lower keys move the camera, in metres a second, from 0.1 to 100. |

## Zoom

| Setting | Default | What it does |
| --- | --- | --- |
| `Closest` | `0.5` | How close the scroll wheel may bring the camera, as a share of the game's distance from your character, from 0.2 to 1. 0.5 is half the game's distance. |
| `Farthest` | `3` | How far the scroll wheel may move the camera away, as a multiple of the game's distance from your character, from 1 to 10. 3 is three times the game's distance. |
| `ScrollSpeed` | `1` | How far each scroll wheel notch moves the camera, from 0.1 to 5. At 1, each notch moves it a tenth of its distance from your character, and 2 moves it twice as far. |

The scroll wheel moves the camera itself toward or away from your character and leaves the lens alone, so the edges of the screen never stretch. The game counts at most five notches per scroll step, so on a wheel that sends several notches at once, high `ScrollSpeed` values may zoom no further than lower ones. With `Closest` and `Farthest` both at 1, the scroll wheel zooms the same as without the mod.

## Smoothing

| Setting | Default | What it does |
| --- | --- | --- |
| `Rate` | `12` | How quickly the camera catches up to where you turned it, from 0 to 60. Higher is snappier, and 0 moves it at once. |

## Keys

| Setting | Default | What it does |
| --- | --- | --- |
| `RotateLeftKey` | `Game` | The key that turns the camera left. It does nothing while `[Rotate] Enabled` is off. `Game` means the game's rotate-left key, which is `Period` (.) unless you changed it in the game's key settings. |
| `RotateRightKey` | `Game` | The key that turns the camera right. It does nothing while `[Rotate] Enabled` is off. `Game` means the game's rotate-right key, which is `Comma` (,) unless you changed it in the game's key settings. |
| `TiltUpKey` | `Insert` | The key that tilts the camera steeper, toward looking straight down. It does nothing while `[Rotate] Enabled` is off. |
| `TiltDownKey` | `Delete` | The key that tilts the camera flatter, toward the horizon. It does nothing while `[Rotate] Enabled` is off. |
| `RaiseKey` | `PageUp` | The key that lifts the camera. |
| `LowerKey` | `PageDown` | The key that lowers the camera. |
| `ResetKey` | `Home` | The key that resets the camera's angle, height and zoom to default. |
| `CenterKeyResetsView` | `false` | When you press the game's center camera key (`Space` by default, or `G` with Time Control), also reset the camera's angle, height and zoom to default. |

Keys take Unity key names, such as `F`, `Keypad8`, `PageUp` or `Mouse3` (the full list is Unity's [KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) page). `None` turns a key off.

A key the game already uses, or one another Survival Log mod uses, is turned off at startup and the log names the setting and who has the key. Pick another key for it. The mod reads Time Control's pause and center keys from Time Control's settings file. Without that file, it keeps `Space` and `G` free for Time Control.

If neither rotate setting ends up with a key, the game's rotate keys work the same as without the mod.
