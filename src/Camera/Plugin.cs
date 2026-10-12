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
    private const string Version = "0.1.0";

    public override void Load()
    {
        var isStayOnStairsOn = Config.Bind("Snap", "StayOnStairs", true,
            "Keep the camera on the floor you are looking at when your character takes stairs or a ladder, instead of snapping to them. Only while you are looking at another floor; the center key still snaps.").Value;
        var isStayAfterMenuCloseOn = Config.Bind("Snap", "StayAfterMenuClose", true,
            "Keep the camera on the floor you are looking at when an object's menu closes, instead of snapping back to your character's floor.").Value;
        var isStayAfterBuildModeOn = Config.Bind("Snap", "StayAfterBuildMode", true,
            "Keep the camera where it is when build mode ends, instead of snapping back to your character.").Value;

        var isRotateOn = Config.Bind("Rotate", "Enabled", true,
            "Turn and tilt the camera with the mouse and keys. Off leaves turning to the game's own rotate keys and turns tilting off; raising and lowering still work.").Value;
        var mouseButtonText = Config.Bind("Rotate", "MouseButton", "Middle",
            "The mouse button that turns and tilts the camera while held and dragged: Middle, Right or None. A drag with it no longer pans the camera.").Value;
        var mouseSensitivity = Config.Bind("Rotate", "MouseSensitivity", 0.25f,
            "Degrees the camera turns per pixel dragged, from 0.01 to 5.").Value;
        var isMouseXInverted = Config.Bind("Rotate", "InvertX", false,
            "Turn the other way when dragging left and right.").Value;
        var isMouseYInverted = Config.Bind("Rotate", "InvertY", false,
            "Tilt the other way when dragging up and down.").Value;
        var yawLimit = Config.Bind("Rotate", "YawLimit", 0f,
            "How far the camera may turn either way from the game's angle, in degrees. 0 lets it turn all the way round.").Value;
        var pitchMin = Config.Bind("Rotate", "PitchMin", 25f,
            "The flattest the camera may look, in degrees below the horizon (5 to 89). The game's own angle is always allowed.").Value;
        var pitchMax = Config.Bind("Rotate", "PitchMax", 85f,
            "The steepest the camera may look, in degrees below the horizon (5 to 89; 90 is straight down).").Value;
        var keyRotateSpeed = Config.Bind("Rotate", "KeyRotateSpeed", 90f,
            "Degrees a second the rotate keys turn the camera, from 1 to 720.").Value;
        var keyTiltSpeed = Config.Bind("Rotate", "KeyTiltSpeed", 45f,
            "Degrees a second the tilt keys tilt the camera, from 1 to 720.").Value;
        var isAllowedInBuildMode = Config.Bind("Rotate", "AllowInBuildMode", false,
            "Keep the turned view in build mode. Off eases back to the game's view while building, as the game itself does not turn the camera there, and returns to your view afterwards.").Value;

        var heightMin = Config.Bind("Height", "Min", -4f,
            "How far the lower key may bring the camera below the game's height, in metres (0 to -50).").Value;
        var heightMax = Config.Bind("Height", "Max", 20f,
            "How far the raise key may lift the camera above the game's height, in metres (0 to 100).").Value;
        var heightSpeed = Config.Bind("Height", "Speed", 6f,
            "Metres a second the raise and lower keys move the camera, from 0.1 to 100.").Value;

        var zoomExtraIn = Config.Bind("Zoom", "ExtraIn", 10f,
            "Degrees of field of view the scroll wheel may zoom in past the game's limit (0 to 60; never narrower than 5 degrees).").Value;
        var zoomExtraOut = Config.Bind("Zoom", "ExtraOut", 15f,
            "Degrees of field of view the scroll wheel may zoom out past the game's limit (0 to 60; never wider than 110 degrees).").Value;
        var scrollSpeed = Config.Bind("Zoom", "ScrollSpeed", 1f,
            "Multiplies how far each scroll wheel step zooms, from 0.1 to 5. 1 is the game's speed. The game caps one step at five notches' worth, so on a wheel that sends several notches at once, high values zoom no further than lower ones.").Value;

        var smoothingRate = Config.Bind("Smoothing", "Rate", 12f,
            "How quickly the camera eases to where you turned it, from 0 to 60. Higher is snappier; 0 moves it instantly.").Value;

        var rotateLeftText = Config.Bind("Keys", "RotateLeftKey", "Game",
            "Key that turns the camera left. Game uses the game's own rotate-left key from its key settings. None turns it off.").Value;
        var rotateRightText = Config.Bind("Keys", "RotateRightKey", "Game",
            "Key that turns the camera right. Game uses the game's own rotate-right key from its key settings. None turns it off.").Value;
        var tiltUpText = Config.Bind("Keys", "TiltUpKey", "Insert",
            "Key that tilts the camera steeper, toward looking straight down. None turns it off.").Value;
        var tiltDownText = Config.Bind("Keys", "TiltDownKey", "Delete",
            "Key that tilts the camera flatter, toward the horizon. None turns it off.").Value;
        var raiseText = Config.Bind("Keys", "RaiseKey", "PageUp",
            "Key that lifts the camera. None turns it off.").Value;
        var lowerText = Config.Bind("Keys", "LowerKey", "PageDown",
            "Key that lowers the camera. None turns it off.").Value;
        var resetText = Config.Bind("Keys", "ResetKey", "Home",
            "Key that puts the camera back to the game's angle, height and zoom. None turns it off.").Value;
        var doesCenterKeyReset = Config.Bind("Keys", "CenterKeyResetsView", false,
            "Also put the camera back to the game's angle and height when the game's center camera key is pressed.").Value;

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
