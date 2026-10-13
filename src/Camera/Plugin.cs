using System;
using System.IO;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Camera.Adapters;
using Camera.Core;
using HarmonyLib;

namespace Camera;

[BepInPlugin(Guid, "Camera", Version)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.camera";
    private const string Version = "0.2.0";

    public override void Load()
    {
        var isStayOnStairsOn = Config.Bind("Snap", "StayOnStairs", true,
            "Keep the camera on the floor you are looking at when your character takes stairs or a ladder, instead of jumping to them. This only matters while you look at another floor (with the floor buttons, or F1 to F3). The game's center camera key (Space by default) still brings the camera back to your character.\n" +
            "角色走楼梯或爬梯子时，镜头留在你正在查看的楼层，不跳到角色身上。只在你查看其他楼层时起作用（用楼层按钮，或 F1 到 F3）。游戏的镜头跟随键（默认 Space）仍会把镜头带回角色身上。").Value;
        var isStayAfterMenuCloseOn = Config.Bind("Snap", "StayAfterMenuClose", true,
            "Keep the camera on the floor you are looking at when an object's menu closes, instead of jumping back to your character's floor.\n" +
            "关闭物件的交互菜单时，镜头留在你正在查看的楼层，不跳回角色所在的楼层。").Value;
        var isStayAfterBuildModeOn = Config.Bind("Snap", "StayAfterBuildMode", true,
            "Keep the camera where it is when you leave build mode, instead of jumping back to your character. This only matters while you look at another floor.\n" +
            "退出规划模式时，镜头留在原处，不跳回角色身上。只在你查看其他楼层时起作用。").Value;

        var isRotateOn = Config.Bind("Rotate", "Enabled", true,
            "Turn the camera all the way around your character and tilt it up and down. To turn, hold the middle mouse button and drag, or hold the game's rotate keys, Period (.) and Comma (,). To tilt, drag up and down, or hold Insert or Delete. Off: only the game's own rotate keys turn the camera, the game's way, and there is no tilting. Raising and lowering still work.\n" +
            "让镜头绕着角色转一整圈，并可上下倾斜。转动：按住鼠标中键拖动，或按住游戏的镜头转向键 Period（.）和 Comma（,）。倾斜：上下拖动，或按住 Insert 或 Delete。关闭后，只有游戏自带的转向键能按游戏原本的方式转动镜头，也不能倾斜。升高和降低仍然可用。").Value;
        var mouseButtonText = Config.Bind("Rotate", "MouseButton", "Middle",
            "The mouse button you hold and drag to turn and tilt the camera: Middle, Right or None. Drag left and right to turn, up and down to tilt. Dragging with this button no longer moves the camera across the map.\n" +
            "按住并拖动来转动和倾斜镜头的鼠标键：Middle（中键）、Right（右键）或 None（不用鼠标）。左右拖动转向，上下拖动倾斜。用这个键拖动时，不再平移镜头。").Value;
        var mouseSensitivity = Config.Bind("Rotate", "MouseSensitivity", 0.25f,
            "How many degrees the camera turns for each pixel you drag, from 0.01 to 5. Higher turns faster.\n" +
            "每拖动一个像素，镜头转动多少度，范围 0.01 到 5。数值越大，转得越快。").Value;
        var isMouseXInverted = Config.Bind("Rotate", "InvertX", false,
            "Turn the other way when you drag left and right.\n" +
            "左右拖动时，反方向转动。").Value;
        var isMouseYInverted = Config.Bind("Rotate", "InvertY", false,
            "Tilt the other way when you drag up and down.\n" +
            "上下拖动时，反方向倾斜。").Value;
        var yawLimit = Config.Bind("Rotate", "YawLimit", 0f,
            "How far the camera may turn to each side of the game's angle, in degrees. 0 lets it turn all the way round.\n" +
            "镜头从游戏原本的角度向左右各能转多少度。0 表示可以转一整圈。").Value;
        var pitchMin = Config.Bind("Rotate", "PitchMin", 25f,
            "The flattest the camera may look, in degrees below the horizon, from 5 to 89. The game's own angle is always allowed.\n" +
            "镜头最平能到的角度，即低于水平线多少度，范围 5 到 89。游戏原本的角度总是允许的。").Value;
        var pitchMax = Config.Bind("Rotate", "PitchMax", 85f,
            "The steepest the camera may look, in degrees below the horizon, from 5 to 89. 90 would be straight down.\n" +
            "镜头最陡能到的角度，即低于水平线多少度，范围 5 到 89。90 是垂直向下看。").Value;
        var keyRotateSpeed = Config.Bind("Rotate", "KeyRotateSpeed", 90f,
            "How fast the rotate keys (Period and Comma by default) turn the camera, in degrees a second, from 1 to 720.\n" +
            "转向键（默认 Period 和 Comma）转动镜头的速度，单位是度每秒，范围 1 到 720。").Value;
        var keyTiltSpeed = Config.Bind("Rotate", "KeyTiltSpeed", 45f,
            "How fast the tilt keys (Insert and Delete by default) tilt the camera, in degrees a second, from 1 to 720.\n" +
            "倾斜键（默认 Insert 和 Delete）倾斜镜头的速度，单位是度每秒，范围 1 到 720。").Value;
        var isAllowedInBuildMode = Config.Bind("Rotate", "AllowInBuildMode", Suspensions.AllowInBuildModeByDefault,
            "Keep your turned view in build mode, and keep turning and tilting there. Off: while you build, the view eases back to the game's angle, and your view comes back when you leave build mode.\n" +
            "在规划模式中保留你转好的视角，也可以继续转动和倾斜。关闭后，规划时镜头会慢慢回到游戏原本的角度，退出规划模式后再回到你的视角。").Value;

        var heightMin = Config.Bind("Height", "Min", -4f,
            "How far the lower key (PageDown by default) may take the camera below the game's height, in metres, from 0 to -50.\n" +
            "降低键（默认 PageDown）最多能把镜头降到游戏原本高度以下多少米，范围 0 到 -50。").Value;
        var heightMax = Config.Bind("Height", "Max", 20f,
            "How far the raise key (PageUp by default) may lift the camera above the game's height, in metres, from 0 to 100.\n" +
            "升高键（默认 PageUp）最多能把镜头升到游戏原本高度以上多少米，范围 0 到 100。").Value;
        var heightSpeed = Config.Bind("Height", "Speed", 6f,
            "How fast the raise and lower keys move the camera, in metres a second, from 0.1 to 100.\n" +
            "升高键和降低键移动镜头的速度，单位是米每秒，范围 0.1 到 100。").Value;

        var zoomExtraIn = Config.Bind("Zoom", "ExtraIn", 10f,
            "How much further the scroll wheel may zoom in than the game allows, in degrees of view angle, from 0 to 60. The view angle never goes below 5 degrees.\n" +
            "滚轮能比游戏多放大多少，按视野角度计算（度），范围 0 到 60。视野角度最小 5 度。").Value;
        var zoomExtraOut = Config.Bind("Zoom", "ExtraOut", 15f,
            "How much further the scroll wheel may zoom out than the game allows, in degrees of view angle, from 0 to 60. The view angle never goes above 110 degrees.\n" +
            "滚轮能比游戏多缩小多少，按视野角度计算（度），范围 0 到 60。视野角度最大 110 度。").Value;
        var scrollSpeed = Config.Bind("Zoom", "ScrollSpeed", 1f,
            "How far each scroll wheel step zooms, from 0.1 to 5. 1 is the game's speed and 2 zooms twice as far. The game counts at most five notches per step, so on a fast wheel high values may zoom no further than lower ones.\n" +
            "滚轮每滚一格缩放多少，范围 0.1 到 5。1 是游戏原本的速度，2 是两倍。游戏每次最多只算五格，所以在一次会滚好几格的滚轮上，数值调高也可能不会缩放得更多。").Value;

        var smoothingRate = Config.Bind("Smoothing", "Rate", 12f,
            "How quickly the camera catches up to where you turned it, from 0 to 60. Higher is snappier; 0 moves it at once.\n" +
            "镜头跟上你转到的位置有多快，范围 0 到 60。数值越大越干脆，0 表示立刻到位。").Value;

        var rotateLeftText = Config.Bind("Keys", "RotateLeftKey", "Game",
            "Key that turns the camera left. Game means the game's own rotate-left key, which is Period (.) unless you changed it in the game's key settings. You can also write a Unity key name such as F, Keypad8 or Mouse3, or None to turn it off. A key the game or another mod already uses is turned off. How to use the camera: hold the middle mouse button and drag to turn and tilt, Period and Comma turn, Insert and Delete tilt, PageUp and PageDown raise and lower, Home resets the view, and the scroll wheel zooms.\n" +
            "让镜头向左转的键。Game 表示使用游戏自己的镜头左转键，默认是 Period（.），除非你在游戏设置的按键页改过。也可以填 Unity 按键名，例如 F、Keypad8 或 Mouse3，填 None 则关闭。游戏或其他模组已经在用的键会被关闭。镜头用法：按住鼠标中键拖动可转动和倾斜，Period 和 Comma 转向，Insert 和 Delete 倾斜，PageUp 和 PageDown 升高和降低，Home 恢复视角，滚轮缩放。").Value;
        var rotateRightText = Config.Bind("Keys", "RotateRightKey", "Game",
            "Key that turns the camera right. Game means the game's own rotate-right key, which is Comma (,) unless you changed it in the game's key settings. None turns it off.\n" +
            "让镜头向右转的键。Game 表示使用游戏自己的镜头右转键，默认是 Comma（,），除非你在游戏设置的按键页改过。填 None 则关闭。").Value;
        var tiltUpText = Config.Bind("Keys", "TiltUpKey", "Insert",
            "Key that tilts the camera steeper, toward looking straight down. None turns it off.\n" +
            "让镜头变陡（更接近垂直向下看）的键。填 None 则关闭。").Value;
        var tiltDownText = Config.Bind("Keys", "TiltDownKey", "Delete",
            "Key that tilts the camera flatter, toward the horizon. None turns it off.\n" +
            "让镜头变平（更接近水平线）的键。填 None 则关闭。").Value;
        var raiseText = Config.Bind("Keys", "RaiseKey", "PageUp",
            "Key that lifts the camera. None turns it off.\n" +
            "升高镜头的键。填 None 则关闭。").Value;
        var lowerText = Config.Bind("Keys", "LowerKey", "PageDown",
            "Key that lowers the camera. None turns it off.\n" +
            "降低镜头的键。填 None 则关闭。").Value;
        var resetText = Config.Bind("Keys", "ResetKey", "Home",
            "Key that puts the camera's angle, height and zoom back to the game's. None turns it off.\n" +
            "把镜头的角度、高度和缩放恢复成游戏原本状态的键。填 None 则关闭。").Value;
        var doesCenterKeyReset = Config.Bind("Keys", "CenterKeyResetsView", false,
            "Also put the camera's angle, height and zoom back to the game's when you press the game's center camera key (Space by default).\n" +
            "按下游戏的镜头跟随键（默认 Space）时，同时把镜头的角度、高度和缩放恢复成游戏原本的状态。").Value;

        var log = new CameraLog(Log, Path.Combine(Paths.BepInExRootPath, "camera.log"));

        PatchKit.Log = log.Write;

        var mouseButton = DragButtons.Parse(mouseButtonText) ?? Fallback(DragButton.Middle, $"Rotate.MouseButton '{mouseButtonText}' is not Middle, Right or None, using Middle", log.Write);
        var settings = new CameraSettings(
            new SnapToggles(isStayOnStairsOn, isStayAfterMenuCloseOn, isStayAfterBuildModeOn),
            isRotateOn,
            mouseButton,
            Clamp(mouseSensitivity, 0.01f, 5f, 0.25f),
            isMouseXInverted,
            isMouseYInverted,
            isAllowedInBuildMode,
            ViewLimits.From(yawLimit, pitchMin, pitchMax, heightMin, heightMax),
            new KeySpeeds(Clamp(keyRotateSpeed, 1f, 720f, 90f), Clamp(keyTiltSpeed, 1f, 720f, 45f), Clamp(heightSpeed, 0.1f, 100f, 6f)),
            Clamp(smoothingRate, 0f, 60f, 12f),
            zoomExtraIn,
            zoomExtraOut,
            Clamp(scrollSpeed, 0.1f, 5f, 1f),
            KeyChoice.Parse(rotateLeftText, canUseGameKey: true),
            KeyChoice.Parse(rotateRightText, canUseGameKey: true),
            KeyChoice.Parse(tiltUpText, canUseGameKey: false),
            KeyChoice.Parse(tiltDownText, canUseGameKey: false),
            KeyChoice.Parse(raiseText, canUseGameKey: false),
            KeyChoice.Parse(lowerText, canUseGameKey: false),
            KeyChoice.Parse(resetText, canUseGameKey: false),
            doesCenterKeyReset);
        var harmony = new Harmony(Guid);
        var limits = settings.Limits;

        log.TrimIfLarge();
        log.Write($"===== Camera {Version} started: stay on stairs {OnOff(isStayOnStairsOn)}, after menus {OnOff(isStayAfterMenuCloseOn)}, after build mode {OnOff(isStayAfterBuildModeOn)}, " +
            $"rotate {OnOff(isRotateOn)} (mouse {mouseButton}, yaw {(limits.IsYawLimited ? $"within {limits.YawLimit:0.#}" : "all round")}, pitch {limits.PitchMin:0.#} to {limits.PitchMax:0.#}, in build mode {OnOff(isAllowedInBuildMode)}), " +
            $"height {limits.HeightMin:0.#} to {limits.HeightMax:0.#} m, smoothing {settings.SmoothingRate:0.#}");
        CameraKeys.Apply(harmony, settings, log.Write);
        ViewPatches.Apply(harmony, settings, log.Write);
        SnapPatches.Apply(harmony, settings.Snap, log.Write, log.WriteFileOnly);
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
