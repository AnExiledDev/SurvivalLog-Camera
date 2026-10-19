using System;
using Camera.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Show;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using CameraTouch = GameCore.Scripts.CameraTouch;

namespace Camera.Adapters;

/// <summary>
/// The free camera. The game's follow camera keeps its own place behind the character; this turns that place around
/// the character (yaw), swings it over the top (pitch) and lifts it (height) by rotating the follow offset the game
/// already builds, and turns the follow camera to match. The game's own free mode (looking at another floor) copies
/// the follow camera's angle when it starts, and is orbited the same way while it runs.
/// </summary>
internal static class ViewPatches
{
    private const float MaxStepSeconds = 0.1f;
    private const int RightButton = 1;
    private const int MiddleButton = 2;

    private static CameraSettings? _settings;
    private static ViewDriver _driver = new(ViewLimits.From(0f, 25f, 85f, 0f, 0f), 0f);
    private static Action<string> _log = _ => { };
    private static bool _hasBase;
    private static Quaternion _baseRotation = Quaternion.identity;
    private static float _basePitch = float.NaN;
    private static IntPtr _followCameraPointer;
    private static float _defaultFollowFov = float.NaN;
    private static float _defaultFreeFov = float.NaN;
    private static View _rotationApplied = View.Default;
    private static View? _offsetApplied;
    private static View? _freeApplied;
    private static CameraMode? _lastMode;
    private static Suspension _suspension = Suspension.Hard;
    private static CameraManager? _lastCamera;
    private static bool _isMouseRotating;
    private static float _mouseX;
    private static float _mouseY;
    private static float _scrollNotches;

    public static void Apply(Harmony harmony, CameraSettings settings, Action<string> log)
    {
        _settings = settings;
        _log = log;
        _driver = new ViewDriver(settings.Limits, settings.SmoothingRate);

        var isViewHooked = PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraManager), nameof(CameraManager.Update), postfix: nameof(AfterCameraUpdate),
            done: "free camera hooked", consequence: "the camera cannot be turned, tilted or raised");

        if (isViewHooked)
        {
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraManager), nameof(CameraManager.RebuildFollowOffset), postfix: nameof(AfterRebuildFollowOffset),
                done: "follow offset hooked", consequence: "the camera turns in place instead of around your character");
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraManager), nameof(CameraManager.Initialize), prefix: nameof(BeforeInitialize),
                done: "a new world starts from the game's view", consequence: "a turned view may carry into the next world");
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraManager), nameof(CameraManager.SetCameraMode), postfix: nameof(AfterSetCameraMode),
                done: "free mode follows the turned view", consequence: "looking at another floor may jump when the view is turned");
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraManager), nameof(CameraManager.OnScrollWheel), prefix: nameof(ScrollZoom),
                done: "the scroll wheel moves the camera in and out", consequence: "the scroll wheel keeps the game's lens zoom");
        }

        if (isViewHooked && settings.IsRotateOn)
        {
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraManager), nameof(CameraManager.ReadCameraYawInput), prefix: nameof(SkipGameYawInput),
                done: "the game's own rotate keys drive this camera instead", consequence: "the game's rotate keys turn the camera twice");
        }

        if (isViewHooked && settings.IsRotateOn && settings.MouseButton != DragButton.None)
        {
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraTouch), nameof(CameraTouch.OnBeginDrag), prefix: nameof(BeginMouseRotate),
                done: $"{settings.MouseButton} mouse drag turns the camera", consequence: "the mouse cannot turn the camera");
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraTouch), nameof(CameraTouch.OnDrag), prefix: nameof(MouseRotate),
                done: "mouse drag hooked", consequence: "the mouse cannot turn the camera");
            PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraTouch), nameof(CameraTouch.OnEndDrag), prefix: nameof(EndMouseRotate),
                done: "mouse drag end hooked", consequence: "releasing the mouse after turning may select what is under it");
        }

        PatchKit.Patch(harmony, typeof(ViewPatches), typeof(CameraManager), nameof(CameraManager.SetCameraMode), prefix: nameof(CarryFollowZoom),
            done: "the zoom stays put when the game tells the camera to keep following", consequence: "the zoom may jump back when your character takes stairs or a ladder");
    }

    /// <summary>
    /// Called on the main thread after each settings change. The view the player turned to stays; new limits and
    /// smoothing apply from the next frame.
    /// </summary>
    public static void Use(CameraSettings settings)
    {
        _settings = settings;
        _driver.Use(settings.Limits, settings.SmoothingRate);
    }

    /// <summary>Puts the follow camera back the way the game left it, so the next world captures the game's angle and not a turned one.</summary>
    private static void BeforeInitialize(CameraManager __instance)
    {
        try
        {
            RestoreGameAngle(__instance);
        }
        catch (Exception ex)
        {
            PatchKit.Fail("restoring the game's camera angle", ex);
        }

        ForgetWorld();
    }

    private static void RestoreGameAngle(CameraManager camera)
    {
        var follow = camera.FollowCamera;

        if (_hasBase && follow != null && follow.Pointer == _followCameraPointer && !_rotationApplied.IsDefault) follow.transform.rotation = _baseRotation;
    }

    private static void ForgetWorld()
    {
        _hasBase = false;
        _followCameraPointer = IntPtr.Zero;
        _basePitch = float.NaN;
        _rotationApplied = View.Default;
        _offsetApplied = null;
        _freeApplied = null;
        _lastMode = null;
        _suspension = Suspension.Hard;
        _isMouseRotating = false;
        _mouseX = 0f;
        _mouseY = 0f;
        _scrollNotches = 0f;
        _driver.Forget();
    }

    private static void AfterCameraUpdate(CameraManager __instance, float __1)
    {
        if (_settings is not { } settings) return;

        try
        {
            CameraKeys.SetUpOnce();
            _lastCamera = __instance;

            var follow = __instance.FollowCamera;

            if (follow == null) return;

            if (!_hasBase || follow.Pointer != _followCameraPointer) CaptureBase(__instance, follow);

            _suspension = Suspensions.Of(Flags(__instance), settings.IsRotateAllowedInBuildMode);
            NoteModeChange(__instance.cameraMode);

            var deltaTime = float.IsFinite(__1) ? Math.Clamp(__1, 0f, MaxStepSeconds) : 0f;
            var (push, isResetPressed) = _suspension == Suspension.None ? ReadInput(__instance, settings, deltaTime) : (ViewPush.None, false);

            ClearMouse();

            if (isResetPressed) RestoreZoom(__instance);

            var view = _driver.Step(push, isResetPressed, _suspension, _basePitch, deltaTime);

            ApplyToFollowCamera(__instance, follow, view);

            if (_suspension != Suspension.Hard && __instance.cameraMode == CameraMode.Free) OrbitFreeCamera(__instance, view);
        }
        catch (Exception ex)
        {
            PatchKit.Fail("moving the camera", ex);
        }
    }

    /// <summary>The game's angle, read once per follow camera before this mod has turned it.</summary>
    private static void CaptureBase(CameraManager camera, Component follow)
    {
        var rotation = follow.transform.rotation;
        var pitch = rotation.eulerAngles.x;

        ForgetWorld();
        _baseRotation = rotation;
        _basePitch = pitch > 180f ? pitch - 360f : pitch;
        _followCameraPointer = follow.Pointer;
        _defaultFollowFov = camera._targetFollowFOV_k__BackingField;
        _defaultFreeFov = camera._targetFreeFOV_k__BackingField;
        _hasBase = true;
        _log($"camera angle read: {_basePitch:0.#} degrees down, zoom {_defaultFollowFov:0.#}");
    }

    private static CameraFlags Flags(CameraManager camera) =>
        new(
            camera._isPhotoMode,
            camera.isSubCameraActive,
            camera._isDeadZooming,
            camera.IsInstallPlanMode || camera._isPlanModeActive);

    private static (ViewPush Push, bool IsResetPressed) ReadInput(CameraManager camera, CameraSettings settings, float deltaTime)
    {
        var push = ViewPush.None;
        var isResetPressed = false;

        if (Input.anyKey && CameraKeys.IsKeyboardFree(camera))
        {
            push = ViewInput.FromKeys(CameraKeys.Held(), settings.Speeds, deltaTime);
            isResetPressed = CameraKeys.IsResetDown() || (settings.DoesCenterKeyResetView && CameraKeys.IsCenterDown());
        }

        if ((_mouseX != 0f || _mouseY != 0f) && CameraKeys.IsMouseFree(camera))
        {
            push += ViewInput.FromMouse(_mouseX, _mouseY, settings.MouseSensitivity, settings.IsMouseXInverted, settings.IsMouseYInverted);
        }

        if (_scrollNotches != 0f) push += ViewInput.FromScroll(_scrollNotches, settings.ScrollSpeed);

        return (push, isResetPressed);
    }

    private static void ClearMouse()
    {
        _mouseX = 0f;
        _mouseY = 0f;
        _scrollNotches = 0f;
    }

    /// <summary>The reset key also puts the game's lens zoom back where the world started.</summary>
    private static void RestoreZoom(CameraManager camera)
    {
        if (float.IsFinite(_defaultFollowFov) && _defaultFollowFov > 0f) camera.targetFollowFOV = _defaultFollowFov;

        if (float.IsFinite(_defaultFreeFov) && _defaultFreeFov > 0f) camera.targetFreeFOV = _defaultFreeFov;
    }

    /// <summary>
    /// The rotation is written every frame while the view is turned, and once more on the way back to the game's
    /// angle; while untouched the game's camera is left entirely alone. The offset is rebuilt through the game's own
    /// method so the peek and raise it adds stay in.
    /// </summary>
    private static void ApplyToFollowCamera(CameraManager camera, Component follow, View view)
    {
        if (!view.IsDefault || !_rotationApplied.IsDefault)
        {
            follow.transform.rotation = Turn(view) * _baseRotation;
            _rotationApplied = view;
        }

        if (_offsetApplied != view) camera.RebuildFollowOffset();
    }

    private static void AfterRebuildFollowOffset(CameraManager __instance)
    {
        try
        {
            var view = _driver.Current;

            _offsetApplied = view;

            if (!_hasBase || view.IsDefault) return;

            __instance._followTransposer.m_FollowOffset = FollowOffset(__instance, view) + __instance._peekOffset;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("turning the follow offset", ex);
        }
    }

    /// <summary>Entering free mode copies the follow camera's angle, so the free camera starts at whatever view was on screen.</summary>
    private static void AfterSetCameraMode(CameraManager __instance)
    {
        try
        {
            NoteModeChange(__instance.cameraMode);
        }
        catch (Exception ex)
        {
            PatchKit.Fail("noting a camera mode change", ex);
        }
    }

    /// <summary>
    /// Gives the free camera the follow camera's zoom just before the game hands the free zoom to the follow camera,
    /// so a camera that was already following keeps the zoom on screen. The free zoom is never seen while following:
    /// entering free mode overwrites it with the follow zoom.
    /// </summary>
    private static void CarryFollowZoom(CameraManager __instance, CameraMode __0)
    {
        try
        {
            if (!FollowZoom.ShouldCarry(__0 == CameraMode.Follow, __instance.cameraMode == CameraMode.Follow)) return;

            __instance._currentFreeFOV_k__BackingField = __instance._currentFollowFOV_k__BackingField;
            __instance._targetFreeFOV_k__BackingField = __instance._targetFollowFOV_k__BackingField;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("keeping the zoom", ex);
        }
    }

    /// <summary>Watched from the per-frame update too, for any path into free mode that does not go through SetCameraMode.</summary>
    private static void NoteModeChange(CameraMode mode)
    {
        if (_lastMode == mode) return;

        _lastMode = mode;
        _freeApplied = mode == CameraMode.Free ? _rotationApplied : null;
    }

    /// <summary>
    /// Moves the free camera around the same point the follow camera turns around, by the change since last frame,
    /// moving the game's own target for its easing with it so the two never pull apart. The point is kept inside the
    /// game's world bounds.
    /// </summary>
    private static void OrbitFreeCamera(CameraManager camera, View view)
    {
        var free = camera.FreeCamera;

        if (free == null) return;

        if (_freeApplied is not { } applied)
        {
            _freeApplied = view;

            return;
        }

        if (applied == view) return;

        var target = camera._targetCameraPosition_k__BackingField;
        var moved = InsideWorld(camera, target + FollowOffset(camera, view) - FollowOffset(camera, applied));
        var transform = free.transform;

        camera._targetCameraPosition_k__BackingField = moved;
        transform.position += moved - target;
        transform.rotation = Turn(view) * _baseRotation;
        _freeApplied = view;
    }

    private static Vector3 InsideWorld(CameraManager camera, Vector3 position)
    {
        var min = camera._minWorldPosition_k__BackingField;
        var max = camera._maxWorldPosition_k__BackingField;

        if (min.x <= max.x) position.x = Mathf.Clamp(position.x, min.x, max.x);

        if (min.z <= max.z) position.z = Mathf.Clamp(position.z, min.z, max.z);

        return position;
    }

    /// <summary>The turn a view makes: pitch about the game camera's own right-hand side, then yaw about straight up.</summary>
    private static Quaternion Turn(View view) =>
        Quaternion.AngleAxis(view.Yaw, Vector3.up) * Quaternion.AngleAxis(view.Pitch, _baseRotation * Vector3.right);

    /// <summary>
    /// The game's offset (RebuildFollowOffset: its yaw times base plus raise) stretched by the view's zoom, with the
    /// view's turn inside the yaw and the view's height on top. The offset runs along the camera's line of sight, so
    /// stretching it moves the camera straight back from what it looks at.
    /// </summary>
    private static Vector3 FollowOffset(CameraManager camera, View view) =>
        camera._yawRotation * (Turn(view) * ((camera._baseFollowOffset + camera._gmRaiseOffset) * MathF.Exp(view.ZoomOut))) + Vector3.up * view.Height;

    /// <summary>
    /// With rotation on, the game's rotate keys are read by this mod instead and the game's own turn is held still.
    /// When the mod ended up with no rotate key at all (both off, both taken, or the keys could not be set up), the
    /// game keeps its own turn so the keys still do something.
    /// </summary>
    private static bool SkipGameYawInput(CameraManager __instance)
    {
        try
        {
            CameraKeys.SetUpOnce();

            if (!CameraKeys.HasRotateKey) return true;

            __instance._yawTargetDir = 0f;

            return false;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("holding the game's own camera turn", ex);

            return true;
        }
    }

    /// <summary>
    /// The game's drag handler pans the camera with any button and selects on release, so a turning drag is taken
    /// over whole. isOnDrag is set so the click that ends it selects nothing: Unity's input module sends the click
    /// before the end of the drag, and the game's click handler reads and clears the flag (CameraTouch.c:325-327).
    /// </summary>
    private static bool BeginMouseRotate(CameraTouch __instance, PointerEventData __0)
    {
        try
        {
            if (!IsRotateButton(__0)) return true;

            _isMouseRotating = _hasBase && _suspension == Suspension.None && _lastCamera != null && CameraKeys.IsMouseFree(_lastCamera);

            if (!_isMouseRotating) return true;

            __instance.isOnDrag = true;

            return false;
        }
        catch (Exception ex)
        {
            _isMouseRotating = false;
            PatchKit.Fail("starting a mouse turn", ex);

            return true;
        }
    }

    private static bool MouseRotate(CameraTouch __instance, PointerEventData __0)
    {
        try
        {
            if (!_isMouseRotating || !IsRotateButton(__0)) return true;

            var delta = __0.delta;

            _mouseX += delta.x;
            _mouseY += delta.y;
            __instance.isOnDrag = true;

            return false;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("turning with the mouse", ex);

            return true;
        }
    }

    /// <summary>
    /// When the button comes up off the game world no click is sent, so the flag set for the drag would swallow the
    /// player's next click. It is cleared here, after the click (if any) has already been ignored.
    /// </summary>
    private static bool EndMouseRotate(CameraTouch __instance, PointerEventData __0)
    {
        try
        {
            if (!_isMouseRotating || !IsRotateButton(__0)) return true;

            _isMouseRotating = false;
            __instance.isOnDrag = false;

            return false;
        }
        catch (Exception ex)
        {
            _isMouseRotating = false;
            PatchKit.Fail("ending a mouse turn", ex);

            return true;
        }
    }

    private static bool IsRotateButton(PointerEventData eventData)
    {
        var button = (int)eventData.button;

        return _settings?.MouseButton switch
        {
            DragButton.Middle => button == MiddleButton,
            DragButton.Right => button == RightButton,
            _ => false,
        };
    }

    /// <summary>
    /// The game zooms by narrowing and widening the lens (OnScrollWheel), so the camera never moves. While the
    /// player's view applies, the wheel moves the camera in and out instead and the lens is left alone; the notches
    /// are picked up by the next camera update. Photo mode, cutscenes, the death camera and build mode with
    /// AllowInBuildMode off keep the game's own zoom.
    /// </summary>
    private static bool ScrollZoom(float __0)
    {
        try
        {
            if (_settings is not { } settings || !settings.Limits.CanZoom || !_hasBase || _suspension != Suspension.None) return true;

            if (float.IsFinite(__0)) _scrollNotches += __0;

            return false;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("zooming the camera", ex);

            return true;
        }
    }
}
