using System;
using System.IO;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Camera.Adapters;
using Camera.Core;
using HarmonyLib;

namespace Camera;

[BepInPlugin(Guid, "Camera", Version)]
[BepInDependency(OptionalLive.ToolkitGuid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BasePlugin
{
    private const string Guid = "com.dev.survivallog.camera";
    private const string Version = "0.4.2";

    public override void Load()
    {
        if (!OptionalLive.IsToolkitInstalled) Log.LogInfo(OptionalLive.MissingNote);

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
                null, "ModManager.Restart", "ModManager.ShowWhen=Enabled")).Value;
        var mouseSensitivity = Config.Bind("Rotate", "MouseSensitivity", 0.25f,
            new ConfigDescription("How many degrees the camera turns for each pixel you drag, from 0.01 to 5. Higher turns faster.\n" +
            "每拖动一个像素，镜头转动多少度，范围 0.01 到 5。数值越大，转得越快。", null, "ModManager.ShowWhen=Enabled"));
        var isMouseXInverted = Config.Bind("Rotate", "InvertX", false,
            new ConfigDescription("Turn the other way when you drag left and right.\n" +
            "左右拖动时，反方向转动。", null, "ModManager.ShowWhen=Enabled"));
        var isMouseYInverted = Config.Bind("Rotate", "InvertY", false,
            new ConfigDescription("Tilt the other way when you drag up and down.\n" +
            "上下拖动时，反方向倾斜。", null, "ModManager.ShowWhen=Enabled"));
        var yawLimit = Config.Bind("Rotate", "YawLimit", 0f,
            new ConfigDescription("How far the camera may turn to each side of the game's angle, in degrees. 0 lets it turn all the way round.\n" +
            "镜头从游戏原本的角度向左右各能转多少度。0 表示可以转一整圈。", null, "ModManager.ShowWhen=Enabled"));
        var pitchMin = Config.Bind("Rotate", "PitchMin", 25f,
            new ConfigDescription("The flattest the camera may look, in degrees below the horizon, from 5 to 89. The game's own angle is always allowed.\n" +
            "镜头最平能到的角度，即低于水平线多少度，范围 5 到 89。游戏原本的角度总是允许的。", null, "ModManager.ShowWhen=Enabled"));
        var pitchMax = Config.Bind("Rotate", "PitchMax", 85f,
            new ConfigDescription("The steepest the camera may look, in degrees below the horizon, from 5 to 89. 90 would be straight down.\n" +
            "镜头最陡能到的角度，即低于水平线多少度，范围 5 到 89。90 是垂直向下看。", null, "ModManager.ShowWhen=Enabled"));
        var keyRotateSpeed = Config.Bind("Rotate", "KeyRotateSpeed", 90f,
            new ConfigDescription("How fast the rotate keys (Period and Comma by default) turn the camera, in degrees a second, from 1 to 720.\n" +
            "转向键（默认 Period 和 Comma）转动镜头的速度，单位是度每秒，范围 1 到 720。", null, "ModManager.ShowWhen=Enabled"));
        var keyTiltSpeed = Config.Bind("Rotate", "KeyTiltSpeed", 45f,
            new ConfigDescription("How fast the tilt keys (Insert and Delete by default) tilt the camera, in degrees a second, from 1 to 720.\n" +
            "倾斜键（默认 Insert 和 Delete）倾斜镜头的速度，单位是度每秒，范围 1 到 720。", null, "ModManager.ShowWhen=Enabled"));
        var isAllowedInBuildMode = Config.Bind("Rotate", "AllowInBuildMode", Suspensions.AllowInBuildModeByDefault,
            new ConfigDescription("Keep your turned view in build mode, and keep turning and tilting there. Off: while you build, the view eases back to the game's angle, and your view comes back when you leave build mode.\n" +
            "在规划模式中保留你转好的视角，也可以继续转动和倾斜。关闭后，规划时镜头会慢慢回到游戏原本的角度，退出规划模式后再回到你的视角。", null, "ModManager.ShowWhen=Enabled"));

        var heightMin = Config.Bind("Height", "Min", -4f,
            "How far the lower key (PageDown by default) may take the camera below the game's height, in metres, from 0 to -50.\n" +
            "降低键（默认 PageDown）最多能把镜头降到游戏原本高度以下多少米，范围 0 到 -50。");
        var heightMax = Config.Bind("Height", "Max", 20f,
            "How far the raise key (PageUp by default) may lift the camera above the game's height, in metres, from 0 to 100.\n" +
            "升高键（默认 PageUp）最多能把镜头升到游戏原本高度以上多少米，范围 0 到 100。");
        var heightSpeed = Config.Bind("Height", "Speed", 6f,
            "How fast the raise and lower keys move the camera, in metres a second, from 0.1 to 100.\n" +
            "升高键和降低键移动镜头的速度，单位是米每秒，范围 0.1 到 100。");

        var zoomClosest = Config.Bind("Zoom", "Closest", 0.5f,
            "How close the scroll wheel may bring the camera, as a share of the game's distance from your character, from 0.2 to 1. 0.5 is half the game's distance. The scroll wheel moves the camera itself in and out; the lens stays as it is. With Closest and Farthest both at 1, the scroll wheel zooms the game's way.\n" +
            "滚轮最多能把镜头拉到多近，按游戏原本到角色距离的比例计算，范围 0.2 到 1。0.5 是游戏距离的一半。滚轮会让镜头本身前后移动，视野角度保持不变。Closest 和 Farthest 都为 1 时，滚轮按游戏原本的方式缩放。");
        var zoomFarthest = Config.Bind("Zoom", "Farthest", 3f,
            "How far the scroll wheel may move the camera away, as a multiple of the game's distance from your character, from 1 to 10. 3 is three times the game's distance.\n" +
            "滚轮最多能把镜头拉到多远，按游戏原本到角色距离的倍数计算，范围 1 到 10。3 是游戏距离的三倍。");
        var scrollSpeed = Config.Bind("Zoom", "ScrollSpeed", 1f,
            "How far each scroll wheel notch moves the camera, from 0.1 to 5. At 1 each notch moves it a tenth of its distance; 2 moves it twice as far. The game counts at most five notches at a time, so on a fast wheel high values may zoom no further than lower ones.\n" +
            "滚轮每滚一格镜头移动多少，范围 0.1 到 5。为 1 时每格移动当前距离的十分之一，2 是两倍。游戏每次最多只算五格，所以在一次会滚好几格的滚轮上，数值调高也可能不会缩放得更多。");

        var smoothingRate = Config.Bind("Smoothing", "Rate", 12f,
            "How quickly the camera catches up to where you turned it, from 0 to 60. Higher is snappier; 0 moves it at once.\n" +
            "镜头跟上你转到的位置有多快，范围 0 到 60。数值越大越干脆，0 表示立刻到位。");

        var rotateLeftText = Config.Bind("Keys", "RotateLeftKey", "Game",
            new ConfigDescription(
                "Key that turns the camera left. It does nothing while [Rotate] Enabled is off. Game means the game's own rotate-left key, which is Period (.) unless you changed it in the game's key settings. A key the game or another mod already uses is turned off. You can also write a Unity key name such as F, Keypad8 or Mouse3, or None to turn it off. Takes effect at the next game start.\n" +
                "让镜头向左转的键。[Rotate] 中的 Enabled 关闭时不起作用。Game 表示使用游戏自己的镜头左转键，默认是 Period（.），除非你在游戏设置的按键页改过。游戏或其他模组已经在用的键会被关闭。也可以填 Unity 按键名，例如 F、Keypad8 或 Mouse3，填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart", "ModManager.ShowWhen=Rotate.Enabled")).Value;
        var rotateRightText = Config.Bind("Keys", "RotateRightKey", "Game",
            new ConfigDescription(
                "Key that turns the camera right. It does nothing while [Rotate] Enabled is off. Game means the game's own rotate-right key, which is Comma (,) unless you changed it in the game's key settings. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "让镜头向右转的键。[Rotate] 中的 Enabled 关闭时不起作用。Game 表示使用游戏自己的镜头右转键，默认是 Comma（,），除非你在游戏设置的按键页改过。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart", "ModManager.ShowWhen=Rotate.Enabled")).Value;
        var tiltUpText = Config.Bind("Keys", "TiltUpKey", "Insert",
            new ConfigDescription(
                "Key that tilts the camera steeper, toward looking straight down. It does nothing while [Rotate] Enabled is off. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "让镜头变陡（更接近垂直向下看）的键。[Rotate] 中的 Enabled 关闭时不起作用。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart", "ModManager.ShowWhen=Rotate.Enabled")).Value;
        var tiltDownText = Config.Bind("Keys", "TiltDownKey", "Delete",
            new ConfigDescription(
                "Key that tilts the camera flatter, toward the horizon. It does nothing while [Rotate] Enabled is off. A key the game or another mod already uses is turned off. None turns it off. Takes effect at the next game start.\n" +
                "让镜头变平（更接近水平线）的键。[Rotate] 中的 Enabled 关闭时不起作用。游戏或其他模组已经在用的键会被关闭。填 None 则关闭。下次启动游戏时生效。",
                null, "ModManager.Restart", "ModManager.ShowWhen=Rotate.Enabled")).Value;
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
            ViewLimits.From(yawLimit.Value, pitchMin.Value, pitchMax.Value, heightMin.Value, heightMax.Value, zoomClosest.Value, zoomFarthest.Value),
            new KeySpeeds(Clamp(keyRotateSpeed.Value, 1f, 720f, 90f), Clamp(keyTiltSpeed.Value, 1f, 720f, 45f), Clamp(heightSpeed.Value, 0.1f, 100f, 6f)),
            Clamp(smoothingRate.Value, 0f, 60f, 12f),
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

        log.Write($"===== Camera {Version} started: rotate {OnOff(isRotateOn)} (mouse {mouseButton})");
        CameraKeys.Apply(harmony, startSettings, log.Write);
        ViewPatches.Apply(harmony, startSettings, log.Write);

        OptionalLive.Watch(() =>
        {
            var settings = Read();
            var limits = settings.Limits;

            log.Write($"only the center key follows {OnOff(settings.Snap.OnlyCenterKeyFollows)}, stay on stairs {OnOff(settings.Snap.StayOnStairs)}, after menus {OnOff(settings.Snap.StayAfterMenuClose)}, after build mode {OnOff(settings.Snap.StayAfterBuildMode)}, " +
                $"yaw {(limits.IsYawLimited ? $"within {limits.YawLimit:0.#}" : "all round")}, pitch {limits.PitchMin:0.#} to {limits.PitchMax:0.#}, in build mode {OnOff(settings.IsRotateAllowedInBuildMode)}, " +
                $"height {limits.HeightMin:0.#} to {limits.HeightMax:0.#} m, zoom {MathF.Exp(limits.ZoomOutMin):0.##} to {MathF.Exp(limits.ZoomOutMax):0.##} times the game's distance, " +
                $"smoothing {settings.SmoothingRate:0.#}, scroll speed x{settings.ScrollSpeed:0.##}");
            CameraKeys.Use(settings);
            ViewPatches.Use(settings);
            SnapPatches.Use(harmony, settings.Snap, log.Write, log.WriteFileOnly);
        }, isOnlyCenterKeyFollowsOn, isStayOnStairsOn, isStayAfterMenuCloseOn, isStayAfterBuildModeOn, mouseSensitivity, isMouseXInverted, isMouseYInverted, yawLimit, pitchMin, pitchMax,
            keyRotateSpeed, keyTiltSpeed, isAllowedInBuildMode, heightMin, heightMax, heightSpeed, zoomClosest, zoomFarthest, scrollSpeed, smoothingRate, doesCenterKeyReset);

        WarmUpInBackground(log);
    }

    /// <summary>
    /// The first frame the camera turns, a key is pressed or a snap is judged would otherwise compile that code on the
    /// game's frame. WarmUp is pure .NET on objects of its own, so it runs on a worker; a failure only costs that
    /// first frame its head start, and is noted in the mod's file only (the BepInEx console is left to the game's thread).
    /// </summary>
    private static void WarmUpInBackground(CameraLog log) =>
        Task.Run(() =>
        {
            try
            {
                WarmUp.Run();
            }
            catch (Exception ex)
            {
                log.WriteFileOnly("warm-up failed, the first camera turn may take a moment longer: " + ex.Message);
            }
        });

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
