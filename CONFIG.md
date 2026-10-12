# Camera settings

Settings live in `BepInEx\config\com.dev.survivallog.camera.cfg`, created the first time the game starts with the mod. Edit the file while the game is closed; changes apply at the next game start. Delete the file to get the defaults back. A number outside its range is pulled back into it.

## Snap

| Setting | Default | What it does |
| --- | --- | --- |
| `StayOnStairs` | `true` | Keep the camera on the floor you are looking at when your character takes stairs or a ladder, instead of snapping to them as they set off or as they come out on their new floor. |
| `StayAfterMenuClose` | `true` | Keep the camera on the floor you are looking at when an object's menu closes, instead of snapping back to your character's floor. |
| `StayAfterBuildMode` | `true` | Keep the camera where it is when build mode ends, instead of snapping back to your character. |

These only hold the camera while you have picked a floor to look at, rather than the camera following your character. After a menu or build mode the camera still goes back to your character if you were looking at their own floor. On stairs and ladders it stays put, even when your character comes out on the floor you are looking at. The game's center camera key always snaps to your character.

## Rotate

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Turn and tilt the camera with the mouse and keys. Off leaves turning to the game's own rotate keys and turns tilting off; raising and lowering still work. |
| `MouseButton` | `Middle` | The mouse button that turns and tilts the camera while held and dragged: `Middle`, `Right` or `None`. A drag with it no longer pans the camera. |
| `MouseSensitivity` | `0.25` | Degrees the camera turns per pixel dragged, from 0.01 to 5. |
| `InvertX` | `false` | Turn the other way when dragging left and right. |
| `InvertY` | `false` | Tilt the other way when dragging up and down. |
| `YawLimit` | `0` | How far the camera may turn either way from the game's angle, in degrees. 0 lets it turn all the way round. |
| `PitchMin` | `25` | The flattest the camera may look, in degrees below the horizon (5 to 89). The game's own angle is always allowed. |
| `PitchMax` | `85` | The steepest the camera may look, in degrees below the horizon (5 to 89; 90 is straight down). |
| `KeyRotateSpeed` | `90` | Degrees a second the rotate keys turn the camera, from 1 to 720. |
| `KeyTiltSpeed` | `45` | Degrees a second the tilt keys tilt the camera, from 1 to 720. |
| `AllowInBuildMode` | `false` | Keep the turned view in build mode. Off eases back to the game's view while you build, and returns to your view afterwards. |

## Height

| Setting | Default | What it does |
| --- | --- | --- |
| `Min` | `-4` | How far the lower key may bring the camera below the game's height, in metres (0 to -50). |
| `Max` | `20` | How far the raise key may lift the camera above the game's height, in metres (0 to 100). |
| `Speed` | `6` | Metres a second the raise and lower keys move the camera, from 0.1 to 100. |

## Zoom

| Setting | Default | What it does |
| --- | --- | --- |
| `ExtraIn` | `10` | Degrees of field of view the scroll wheel may zoom in past the game's limit (0 to 60; never narrower than 5 degrees). |
| `ExtraOut` | `15` | Degrees of field of view the scroll wheel may zoom out past the game's limit (0 to 60; never wider than 110 degrees). |
| `ScrollSpeed` | `1` | Multiplies how far each scroll wheel step zooms, from 0.1 to 5. 1 is the game's speed. |

The game caps one scroll step at five notches' worth, so on a wheel that sends several notches at once, high `ScrollSpeed` values zoom no further than lower ones. With `ExtraIn` and `ExtraOut` at 0 and `ScrollSpeed` at 1, the mod leaves the game's zoom alone.

## Smoothing

| Setting | Default | What it does |
| --- | --- | --- |
| `Rate` | `12` | How quickly the camera eases to where you turned it, from 0 to 60. Higher is snappier; 0 moves it instantly. |

## Keys

| Setting | Default | What it does |
| --- | --- | --- |
| `RotateLeftKey` | `Game` | Key that turns the camera left. `Game` uses the game's own rotate-left key from its key settings. |
| `RotateRightKey` | `Game` | Key that turns the camera right. `Game` uses the game's own rotate-right key from its key settings. |
| `TiltUpKey` | `Insert` | Key that tilts the camera steeper, toward looking straight down. |
| `TiltDownKey` | `Delete` | Key that tilts the camera flatter, toward the horizon. |
| `RaiseKey` | `PageUp` | Key that lifts the camera. |
| `LowerKey` | `PageDown` | Key that lowers the camera. |
| `ResetKey` | `Home` | Key that puts the camera back to the game's angle, height and zoom. |
| `CenterKeyResetsView` | `false` | Also put the camera back to the game's angle and height when the game's center camera key is pressed. |

Keys take Unity key names, such as `F`, `Keypad8`, `PageUp` or `Mouse3` (the full list is Unity's [KeyCode](https://docs.unity3d.com/ScriptReference/KeyCode.html) page). `None` turns a key off.

A key the game already uses, or one another Survival Log mod uses, is turned off at startup and the log names the setting and who has the key. Pick another key for it. Time Control's pause and center keys are read from Time Control's own settings file; without that file, `Space` and `G` are kept free for it.

If neither rotate key ends up with a key, the game's own rotate keys keep working the game's way.
