using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Camera.Adapters;
using Camera.Core;
using ExiledToolkit;
using HarmonyLib;

namespace Camera;

[BepInPlugin(Guid, "Camera", Version)]
[BepInDependency(ExiledToolkit.Plugin.Guid, ">=0.4.0")]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.camera";
    private const string Version = "0.4.0";

    public override void Load()
    {
        var isOnlyCenterKeyFollowsOn = Config.Bind("Snap", "OnlyCenterKeyFollows", true,
            "Once you have moved the camera away from your character, only the game's center camera key (Space by default, or G with Time Control) and the camera button on the screen bring it back to them. Nothing else does, on any floor: not stairs or ladders, not closing a menu, not leaving build mode after moving or placing something, and not the game's rotate keys. Cutscenes, photo mode, starting a game, the tutorial and travelling to another map still move the camera the game's way. While this is on, StayOnStairs, StayAfterMenuClose and StayAfterBuildMode make no difference. Off: those three settings decide.\n" +
            "你把镜头从角色身上移开后，只有游戏的镜头跟随键（默认 Space，装了 Time Control 时为 G）和屏幕上的镜头按钮能把镜头带回角色身上。在任何楼层，其他操作都不会：走楼梯或爬梯子、关闭菜单、移动或摆放物件后退出规划模式，以及游戏的镜头转向键。过场动画、拍照模式、进入游戏、新手引导和前往其他地图时，镜头仍按游戏原本的方式移动。开启时，StayOnStairs、StayAfterMenuClose 和 StayAfterBuildMode 不起作用。关闭后，由这三项决定。");
        var isStayOnStairsOn = Config.Bind("Snap", "StayOnStairs", true,
            "Keep the camera on the floor you are looking at when your character takes stairs or a ladder, instead of jumping to them. This only matters while you look at another floor (with the floor buttons, or F1 to F3). The game's center camera key (Space by default, or G with Time Control) still brings the camera back to your character. While OnlyCenterKeyFollows is on, this setting makes no difference.\n" +
            "角色走楼梯或爬梯子时，镜头留在你正在查看的楼层，不跳到角色身上。只在你查看其他楼层时起作用（用楼层按钮，或 F1 到 F3）。游戏的镜头跟随键（默认 Space，装了 Time Control 时为 G）仍会把镜头带回角色身上。开启 OnlyCenterKeyFollows 时，此项不起作用。");
        var isStayAfterMenuCloseOn = Config.Bind("Snap", "StayAfterMenuClose", true,
            "Keep the camera on the floor you are looking at when an object's menu closes, instead of jumping back to your character's floor. While OnlyCenterKeyFollows is on, this setting makes no difference.\n" +
            "关闭物件的交互菜单时，镜头留在你正在查看的楼层，不跳回角色所在的楼层。开启 OnlyCenterKeyFollows 时，此项不起作用。");
        var isStayAfterBuildModeOn = Config.Bind("Snap", "StayAfterBuildMode", true,
            "Keep the camera where it is when you leave build mode, instead of jumping back to your character. This only matters while you look at another floor. While OnlyCenterKeyFollows is on, this setting makes no difference.\n" +
            "退出规划模式时，镜头留在原处，不跳回角色身上。只在你查看其他楼层时起作用。开启 OnlyCenterKeyFollows 时，此项不起作用。");

        var isRotateOn = Config.Bind("Rotate", "Enabled", true,
            new ConfigDescription(
                "Turn the camera all the way around your character and tilt it up and down. To turn, hold the middle mouse button and drag, or hold the game's rotate keys, Period (.) and Comma (,). To tilt, drag up and down, or hold Insert or Delete. Off: only the game's own rotate keys turn the camera, the game's way, and there is no tilting. Raising and lowering still work. Takes effect at the next game start.\n" +
                "让镜头绕着角色转一整圈，并可上下倾斜。转动：按住鼠标中键拖动，或按住游戏的镜头转向键 Period（.）和 Comma（,）。倾斜：上下拖动，或按住 Insert 或 Delete。关闭后，只有游戏自带的转向键能按游戏原本的方式转动镜头，也不能倾斜。升高和降低仍然可用。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var mouseButtonText = Config.Bind("Rotate", "MouseButton", "Middle",
            new ConfigDescription(
                "The mouse button you hold and drag to turn and tilt the camera: Middle, Right or None. Drag left and right to turn, up and down to tilt. Dragging with this button no longer moves the camera across the map. Takes effect at the next game start.\n" +
                "按住并拖动来转动和倾斜镜头的鼠标键：Middle（中键）、Right（右键）或 None（不用鼠标）。左右拖动转向，上下拖动倾斜。用这个键拖动时，不再平移镜头。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var mouseSensitivity = Config.Bind("Rotate", "MouseSensitivity", 0.25f,
            "How many degrees the camera turns for each pixel you drag, from 0.01 to 5. Higher turns faster.\n" +
            "每拖动一个像素，镜头转动多少度，范围 0.01 到 5。数值越大，转得越快。");
        var isMouseXInverted = Config.Bind("Rotate", "InvertX", false,
            "Turn the other way when you drag left and right.\n" +
            "左右拖动时，反方向转动。");
        var isMouseYInverted = Config.Bind("Rotate", "InvertY", false,
            "Tilt the other way when you drag up and down.\n" +
            "上下拖动时，反方向倾斜。");
        var yawLimit = Config.Bind("Rotate", "YawLimit", 0f,
            "How far the camera may turn to each side of the game's angle, in degrees. 0 lets it turn all the way round.\n" +
            "镜头从游戏原本的角度向左右各能转多少度。0 表示可以转一整圈。");
        var pitchMin = Config.Bind("Rotate", "PitchMin", 25f,
            "The flattest the camera may look, in degrees below the horizon, from 5 to 89. The game's own angle is always allowed.\n" +
            "镜头最平能到的角度，即低于水平线多少度，范围 5 到 89。游戏原本的角度总是允许的。");
        var pitchMax = Config.Bind("Rotate", "PitchMax", 85f,
            "The steepest the camera may look, in degrees below the horizon, from 5 to 89. 90 would be straight down.\n" +
            "镜头最陡能到的角度，即低于水平线多少度，范围 5 到 89。90 是垂直向下看。");
        var keyRotateSpeed = Config.Bind("Rotate", "KeyRotateSpeed", 90f,
            "How fast the rotate keys (Period and Comma by default) turn the camera, in degrees a second, from 1 to 720.\n" +
            "转向键（默认 Period 和 Comma）转动镜头的速度，单位是度每秒，范围 1 到 720。");
        var keyTiltSpeed = Config.Bind("Rotate", "KeyTiltSpeed", 45f,
            "How fast the tilt keys (Insert and Delete by default) tilt the camera, in degrees a second, from 1 to 720.\n" +
            "倾斜键（默认 Insert 和 Delete）倾斜镜头的速度，单位是度每秒，范围 1 到 720。");
        var isAllowedInBuildMode = Config.Bind("Rotate", "AllowInBuildMode", Suspensions.AllowInBuildModeByDefault,
            "Keep your turned view in build mode, and keep turning and tilting there. Off: while you build, the view eases back to the game's angle, and your view comes back when you leave build mode.\n" +
            "在规划模式中保留你转好的视角，也可以继续转动和倾斜。关闭后，规划时镜头会慢慢回到游戏原本的角度，退出规划模式后再回到你的视角。");

        var heightMin = Config.Bind("Height", "Min", -4f,
            "How far the lower key (PageDown by default) may take the camera below the game's height, in metres, from 0 to -50.\n" +
            "降低键（默认 PageDown）最多能把镜头降到游戏原本高度以下多少米，范围 0 到 -50。");
        var heightMax = Config.Bind("Height", "Max", 20f,
            "How far the raise key (PageUp by default) may lift the camera above the game's height, in metres, from 0 to 100.\n" +
            "升高键（默认 PageUp）最多能把镜头升到游戏原本高度以上多少米，范围 0 到 100。");
        var heightSpeed = Config.Bind("Height", "Speed", 6f,
            "How fast the raise and lower keys move the camera, in metres a second, from 0.1 to 100.\n" +
            "升高键和降低键移动镜头的速度，单位是米每秒，范围 0.1 到 100。");

        var zoomExtraIn = Config.Bind("Zoom", "ExtraIn", 10f,
            new ConfigDescription(
                "How much further the scroll wheel may zoom in than the game allows, in degrees of view angle, from 0 to 60. The view angle never goes below 5 degrees. Takes effect at the next game start.\n" +
                "滚轮能比游戏多放大多少，按视野角度计算（度），范围 0 到 60。视野角度最小 5 度。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var zoomExtraOut = Config.Bind("Zoom", "ExtraOut", 15f,
            new ConfigDescription(
                "How much further the scroll wheel may zoom out than the game allows, in degrees of view angle, from 0 to 60. The view angle never goes above 110 degrees. Takes effect at the next game start.\n" +
                "滚轮能比游戏多缩小多少，按视野角度计算（度），范围 0 到 60。视野角度最大 110 度。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var scrollSpeed = Config.Bind("Zoom", "ScrollSpeed", 1f,
            "How far each scroll wheel step zooms, from 0.1 to 5. 1 is the game's speed and 2 zooms twice as far. The game counts at most five notches per step, so on a fast wheel high values may zoom no further than lower ones.\n" +
            "滚轮每滚一格缩放多少，范围 0.1 到 5。1 是游戏原本的速度，2 是两倍。游戏每次最多只算五格，所以在一次会滚好几格的滚轮上，数值调高也可能不会缩放得更多。");

        var smoothingRate = Config.Bind("Smoothing", "Rate", 12f,
            "How quickly the camera catches up to where you turned it, from 0 to 60. Higher is snappier; 0 moves it at once.\n" +
            "镜头跟上你转到的位置有多快，范围 0 到 60。数值越大越干脆，0 表示立刻到位。");

        var rotateLeftText = Config.Bind("Keys", "RotateLeftKey", "Game",
            new ConfigDescription(
                "Key that turns the camera left. It does nothing while [Rotate] Enabled is off. Game means the game's own rotate-left key, which is Period (.) unless you changed it in the game's key settings. A key the game or another mod already uses is turned off. You can also write a Unity key name such as F, Keypad8 or Mouse3, or None to turn it off. Takes effect at the next game start.\n" +
                "让镜头向左转的键。[Rotate] 中的 Enabled 关闭时不起作用。Game 表示使用游戏自己的镜头左转键，默认是 Period（.），除非你在游戏设置的按键页改过。游戏或其他模组已经在用的键会被关闭。也可以填 Unity 按键名，例如 F、Keypad8 或 Mouse3，填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var rotateRightText = Config.Bind("Keys", "RotateRightKey", "Game",
            new ConfigDescription(
                "Key that turns the camera right. It does nothing while [Rotate] Enabled is off. Game means the game's own rotate-right key, which is Comma (,) unless you changed it in the game's key settings. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "让镜头向右转的键。[Rotate] 中的 Enabled 关闭时不起作用。Game 表示使用游戏自己的镜头右转键，默认是 Comma（,），除非你在游戏设置的按键页改过。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var tiltUpText = Config.Bind("Keys", "TiltUpKey", "Insert",
            new ConfigDescription(
                "Key that tilts the camera steeper, toward looking straight down. It does nothing while [Rotate] Enabled is off. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "让镜头变陡（更接近垂直向下看）的键。[Rotate] 中的 Enabled 关闭时不起作用。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var tiltDownText = Config.Bind("Keys", "TiltDownKey", "Delete",
            new ConfigDescription(
                "Key that tilts the camera flatter, toward the horizon. It does nothing while [Rotate] Enabled is off. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "让镜头变平（更接近水平线）的键。[Rotate] 中的 Enabled 关闭时不起作用。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var raiseText = Config.Bind("Keys", "RaiseKey", "PageUp",
            new ConfigDescription(
                "Key that lifts the camera. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "升高镜头的键。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var lowerText = Config.Bind("Keys", "LowerKey", "PageDown",
            new ConfigDescription(
                "Key that lowers the camera. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "降低镜头的键。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var resetText = Config.Bind("Keys", "ResetKey", "Home",
            new ConfigDescription(
                "Key that puts the camera's angle, height and zoom back to the game's. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "把镜头的角度、高度和缩放恢复成游戏原本状态的键。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart")).Value;
        var doesCenterKeyReset = Config.Bind("Keys", "CenterKeyResetsView", false,
            "Also put the camera's angle, height and zoom back to the game's when you press the game's center camera key (Space by default, or G with Time Control).\n" +
            "按下游戏的镜头跟随键（默认 Space，装了 Time Control 时为 G）时，同时把镜头的角度、高度和缩放恢复成游戏原本的状态。");

        var log = new CameraLog(Log, Path.Combine(Paths.BepInExRootPath, "camera.log"));

        PatchKit.Log = log.Write;

        var mouseButton = DragButtons.Parse(mouseButtonText) ?? Fallback(DragButton.Middle, $"Rotate.MouseButton '{mouseButtonText}' is not Middle, Right or None, using Middle", log.Write);
        CameraSettings Read() => new(
            new SnapToggles(isStayOnStairsOn.Value, isStayAfterMenuCloseOn.Value, isStayAfterBuildModeOn.Value, isOnlyCenterKeyFollowsOn.Value),
            isRotateOn,
            mouseButton,
            Clamp(mouseSensitivity.Value, 0.01f, 5f, 0.25f),
            isMouseXInverted.Value,
            isMouseYInverted.Value,
            isAllowedInBuildMode.Value,
            ViewLimits.From(yawLimit.Value, pitchMin.Value, pitchMax.Value, heightMin.Value, heightMax.Value),
            new KeySpeeds(Clamp(keyRotateSpeed.Value, 1f, 720f, 90f), Clamp(keyTiltSpeed.Value, 1f, 720f, 45f), Clamp(heightSpeed.Value, 0.1f, 100f, 6f)),
            Clamp(smoothingRate.Value, 0f, 60f, 12f),
            zoomExtraIn,
            zoomExtraOut,
            Clamp(scrollSpeed.Value, 0.1f, 5f, 1f),
            KeyChoice.Parse(rotateLeftText, canUseGameKey: true),
            KeyChoice.Parse(rotateRightText, canUseGameKey: true),
            KeyChoice.Parse(tiltUpText, canUseGameKey: false),
            KeyChoice.Parse(tiltDownText, canUseGameKey: false),
            KeyChoice.Parse(raiseText, canUseGameKey: false),
            KeyChoice.Parse(lowerText, canUseGameKey: false),
            KeyChoice.Parse(resetText, canUseGameKey: false),
            doesCenterKeyReset.Value);

        var harmony = new Harmony(Guid);
        var startSettings = Read();

        log.TrimIfLarge();
        log.Write($"===== Camera {Version} started: rotate {OnOff(isRotateOn)} (mouse {mouseButton}), zoom range wider by {zoomExtraIn:0.#} degrees in and {zoomExtraOut:0.#} out");
        CameraKeys.Apply(harmony, startSettings, log.Write);
        ViewPatches.Apply(harmony, startSettings, log.Write);

        LiveSettings.Watch(() =>
        {
            var settings = Read();
            var limits = settings.Limits;

            log.Write($"only the center key follows {OnOff(settings.Snap.OnlyCenterKeyFollows)}, stay on stairs {OnOff(settings.Snap.StayOnStairs)}, after menus {OnOff(settings.Snap.StayAfterMenuClose)}, after build mode {OnOff(settings.Snap.StayAfterBuildMode)}, " +
                $"yaw {(limits.IsYawLimited ? $"within {limits.YawLimit:0.#}" : "all round")}, pitch {limits.PitchMin:0.#} to {limits.PitchMax:0.#}, in build mode {OnOff(settings.IsRotateAllowedInBuildMode)}, " +
                $"height {limits.HeightMin:0.#} to {limits.HeightMax:0.#} m, smoothing {settings.SmoothingRate:0.#}, scroll speed x{settings.ScrollSpeed:0.##}");
            CameraKeys.Use(settings);
            ViewPatches.Use(harmony, settings);
            SnapPatches.Use(harmony, settings.Snap, log.Write, log.WriteFileOnly);
        }, isOnlyCenterKeyFollowsOn, isStayOnStairsOn, isStayAfterMenuCloseOn, isStayAfterBuildModeOn, mouseSensitivity, isMouseXInverted, isMouseYInverted, yawLimit, pitchMin, pitchMax,
            keyRotateSpeed, keyTiltSpeed, isAllowedInBuildMode, heightMin, heightMax, heightSpeed, scrollSpeed, smoothingRate, doesCenterKeyReset);
    }

    private static string OnOff(bool isOn) => isOn ? "on" : "off";

    private static T Fallback<T>(T value, string message, Action<string> log)
    {
        log(message);

        return value;
    }

    /// <summary>Hand-edited config is clamped rather than refused; a value that is not a number at all gets the default.</summary>
    private static float Clamp(float value, float min, float max, float fallback) =>
        float.IsFinite(value) ? Math.Clamp(value, min, max) : fallback;
}
