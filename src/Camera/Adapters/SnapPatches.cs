using System;
using Camera.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Logic;
using GameCore.HotUpdate.Battle.Show;
using GameCore.HotUpdate.ReduxUI;
using HarmonyLib;
using UnityEngine;

namespace Camera.Adapters;

/// <summary>
/// Holds back the game's automatic snaps to the character while the player is looking around.
/// Each snap is the game asking SetCameraMode for Follow from inside one known method, on the same call stack, so
/// that method is marked while it runs and the request made inside it can be traced to it. Stairs and ladders make
/// two: one as the character sets off (AE_FloorMove) and a forced one as they come out on the new floor
/// (AE_FloorTeleport, through the FloorChanged event to CameraManager.JumpMap).
/// With OnlyCenterKeyFollows on, the game's own camera moves (cutscenes, photo mode, the world starting, the tutorial
/// door, map travel) and its camera mode toggle are marked too, and the center key and the camera button are timed,
/// so <see cref="SnapRules.Judge"/> can let only those through.
/// </summary>
internal static class SnapPatches
{
    private static readonly SnapScope Scope = new();

    private static SnapToggles _toggles;
    private static Action<string> _logFileOnly = _ => { };
    private static float _buttonPressedAt = float.NegativeInfinity;

    private static bool _isGuardTried;
    private static bool _isGuardHooked;
    private static bool _areStairsMarked;
    private static bool _isMenuCloseMarked;
    private static bool _isBuildExitMarked;
    private static bool _areCenterKeyMovesMarked;

    /// <summary>
    /// Called on the main thread at start and after each change. Each hook goes in the first time a setting needs it and stays in;
    /// with its setting off, a marked snap is let through the game's way.
    /// </summary>
    public static void Use(Harmony harmony, SnapToggles toggles, Action<string> log, Action<string> logFileOnly)
    {
        _toggles = toggles;
        _logFileOnly = logFileOnly;

        if (!toggles.IsAnyOn)
        {
            log("all automatic camera snaps left as the game has them");

            return;
        }

        if (!_isGuardTried)
        {
            _isGuardTried = true;
            _isGuardHooked = PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.SetCameraMode), prefix: nameof(KeepViewIfSnap),
                done: "camera snap guard hooked", consequence: "the camera still snaps to your character on stairs, after menus and after build mode");
        }

        if (!_isGuardHooked) return;

        var isOnlyCenterKey = toggles.OnlyCenterKeyFollows;

        if ((isOnlyCenterKey || toggles.StayOnStairs) && !_areStairsMarked)
        {
            _areStairsMarked = true;
            MarkStairs(harmony);
        }

        if ((isOnlyCenterKey || toggles.StayAfterMenuClose) && !_isMenuCloseMarked)
        {
            _isMenuCloseMarked = true;
            MarkMenuClose(harmony);
        }

        if ((isOnlyCenterKey || toggles.StayAfterBuildMode) && !_isBuildExitMarked)
        {
            _isBuildExitMarked = true;
            MarkBuildExit(harmony);
        }

        if (!isOnlyCenterKey || _areCenterKeyMovesMarked) return;

        _areCenterKeyMovesMarked = true;
        MarkCameraToggle(harmony);
        MarkGameCameraMoves(harmony);
    }

    private static void MarkStairs(Harmony harmony)
    {
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(AE_FloorMove), nameof(AE_FloorMove.Run), prefix: nameof(EnterStairs), finalizer: nameof(LeaveStairs),
            done: "the camera stays put when your character takes stairs or a ladder", consequence: "the camera still snaps to your character on stairs and ladders");
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(AE_FloorTeleport), nameof(AE_FloorTeleport.Run), prefix: nameof(EnterArrival), finalizer: nameof(LeaveArrival),
            done: "the camera stays put when your character comes out on another floor", consequence: "the camera still jumps to your character's new floor after stairs and ladders");
    }

    private static void MarkMenuClose(Harmony harmony)
    {
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(Reducer_Web_SelectMenu), nameof(Reducer_Web_SelectMenu.TryReturnToPlayerFloor), prefix: nameof(EnterMenuClose), finalizer: nameof(LeaveMenuClose),
            done: "the camera stays on the floor you were viewing when an object menu closes", consequence: "the camera still snaps to your character's floor when an object menu closes");
    }

    private static void MarkBuildExit(Harmony harmony)
    {
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.OnExitInstallPlanMode), prefix: nameof(EnterBuildExit), finalizer: nameof(LeaveBuildExit),
            done: "the camera stays where it was when build mode ends", consequence: "the camera still snaps to your character when build mode ends");
    }

    /// <summary>
    /// The center key, the rotate keys and the camera button all toggle the camera mode through the same reducer. The
    /// keys reach it through LogicAdapter.OnCameraModeChanged; the button calls the toggle action directly, so an
    /// action sent from outside OnCameraModeChanged is the button. Without the key mark every key would count as the
    /// button, so the button is only timed when that mark is in place.
    /// </summary>
    private static void MarkCameraToggle(Harmony harmony)
    {
        const string toggleLost = "the game's rotate keys may still send the camera back to your character from another floor";
        const string buttonLost = "the camera button on the screen does not bring the camera back to your character while you look around (the center key still does)";

        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(Reducer_Web_CoreUI0), nameof(Reducer_Web_CoreUI0.Trigger_RA_CameraMode), prefix: nameof(EnterCameraToggle), finalizer: nameof(LeaveCameraToggle),
            done: "the game's camera mode toggle is watched", consequence: toggleLost);
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(Reducer_Web_CoreUI1), nameof(Reducer_Web_CoreUI1.Trigger_RA_CameraMode), prefix: nameof(EnterCameraToggle), finalizer: nameof(LeaveCameraToggle),
            done: "the game's camera mode toggle is watched on the second screen layout", consequence: toggleLost);

        var isKeyMarked = PatchKit.Patch(harmony, typeof(SnapPatches), typeof(LogicAdapter), nameof(LogicAdapter.OnCameraModeChanged), prefix: nameof(EnterGameCameraKey), finalizer: nameof(LeaveGameCameraKey),
            done: "the game's camera keys are told apart from the camera button", consequence: buttonLost);

        if (!isKeyMarked) return;

        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(Ac_CoreUI0_CameraMode), nameof(Ac_CoreUI0_CameraMode.SendAction), prefix: nameof(NoteCameraButton),
            done: "the camera button on the screen brings the camera back to your character", consequence: buttonLost);
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(Ac_CoreUI1_CameraMode), nameof(Ac_CoreUI1_CameraMode.SendAction), prefix: nameof(NoteCameraButton),
            done: "the camera button on the second screen layout brings the camera back to your character", consequence: buttonLost);
    }

    /// <summary>The game's own camera moves, which keep going to the character whatever the player was looking at.</summary>
    private static void MarkGameCameraMoves(Harmony harmony)
    {
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.SetFollowTargetForCutscene), prefix: nameof(EnterCutscene), finalizer: nameof(LeaveCutscene),
            done: "cutscenes still move the camera", consequence: "a cutscene may not show its scene while you look around on your character's floor");
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.EnterPhotoMode), prefix: nameof(EnterPhotoMode), finalizer: nameof(LeavePhotoMode),
            done: "photo mode still starts on your character", consequence: "photo mode may not start on your character while you look around on their floor");
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.ExitPhotoMode), prefix: nameof(EnterPhotoMode), finalizer: nameof(LeavePhotoMode),
            done: "leaving photo mode still returns to your character", consequence: "leaving photo mode may not return to your character while you look around on their floor");
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.StartBattle), prefix: nameof(EnterWorldStart), finalizer: nameof(LeaveWorldStart),
            done: "a world still starts with the camera on your character", consequence: "a world may not start with the camera on your character");
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(Guide_PlayDoorAnimation), nameof(Guide_PlayDoorAnimation.Update), prefix: nameof(EnterTutorialDoor), finalizer: nameof(LeaveTutorialDoor),
            done: "the tutorial's door scene still moves the camera", consequence: "the tutorial's door scene may not move the camera while you look around");
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(AE_MoveMap), nameof(AE_MoveMap.Run), prefix: nameof(EnterMapTravel), finalizer: nameof(LeaveMapTravel),
            done: "travelling to another map still moves the camera", consequence: "travelling to another map may not move the camera there");
        PatchKit.Patch(harmony, typeof(SnapPatches), typeof(AE_Exit2Start), nameof(AE_Exit2Start.Run), prefix: nameof(EnterMapTravel), finalizer: nameof(LeaveMapTravel),
            done: "returning from a map still moves the camera", consequence: "returning from a map may not move the camera back");
    }

    private static void EnterStairs() => Enter(SnapSource.Stairs);

    private static void LeaveStairs() => Leave(SnapSource.Stairs);

    private static void EnterArrival() => Enter(SnapSource.StairsArrival);

    private static void LeaveArrival() => Leave(SnapSource.StairsArrival);

    private static void EnterMenuClose() => Enter(SnapSource.MenuClose);

    private static void LeaveMenuClose() => Leave(SnapSource.MenuClose);

    private static void EnterBuildExit() => Enter(SnapSource.BuildExit);

    private static void LeaveBuildExit() => Leave(SnapSource.BuildExit);

    private static void EnterCameraToggle() => Enter(SnapSource.CameraToggle);

    private static void LeaveCameraToggle() => Leave(SnapSource.CameraToggle);

    private static void EnterGameCameraKey() => Enter(SnapSource.GameCameraKey);

    private static void LeaveGameCameraKey() => Leave(SnapSource.GameCameraKey);

    private static void EnterCutscene() => Enter(SnapSource.Cutscene);

    private static void LeaveCutscene() => Leave(SnapSource.Cutscene);

    private static void EnterPhotoMode() => Enter(SnapSource.PhotoMode);

    private static void LeavePhotoMode() => Leave(SnapSource.PhotoMode);

    private static void EnterWorldStart() => Enter(SnapSource.WorldStart);

    private static void LeaveWorldStart() => Leave(SnapSource.WorldStart);

    private static void EnterTutorialDoor() => Enter(SnapSource.TutorialDoor);

    private static void LeaveTutorialDoor() => Leave(SnapSource.TutorialDoor);

    private static void EnterMapTravel() => Enter(SnapSource.MapTravel);

    private static void LeaveMapTravel() => Leave(SnapSource.MapTravel);

    /// <summary>
    /// These run inside the game's own stairs, menu, build and camera code, so a failure here must never reach it.
    /// Leave runs as a finalizer, so the mark is cleared even when the game's method throws.
    /// </summary>
    private static void Enter(SnapSource source)
    {
        try
        {
            Scope.Enter(source, Time.frameCount);
        }
        catch (Exception ex)
        {
            PatchKit.Fail($"marking a camera snap ({source})", ex);
        }
    }

    private static void Leave(SnapSource source)
    {
        try
        {
            Scope.Leave(source);
        }
        catch (Exception ex)
        {
            PatchKit.Fail($"ending a camera snap ({source})", ex);
        }
    }

    /// <summary>Notes the camera button only when it asks for Follow (<see cref="SnapRules.IsCameraButtonFollowAsk"/>).</summary>
    private static void NoteCameraButton()
    {
        try
        {
            var isCameraFree = WorldCamera.Existing()?._cameraMode_k__BackingField == CameraMode.Free;

            if (SnapRules.IsCameraButtonFollowAsk(Scope.Active(Time.frameCount), isCameraFree)) _buttonPressedAt = Time.unscaledTime;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("noting the camera button", ex);
        }
    }

    /// <returns>False drops the game's request, so the camera stays where the player put it.</returns>
    private static bool KeepViewIfSnap(CameraManager __instance, CameraMode __0, int __1, bool __2)
    {
        try
        {
            var source = Scope.Active(Time.frameCount);

            if (source == SnapSource.None && !_toggles.OnlyCenterKeyFollows) return true;

            var isFollow = __0 == CameraMode.Follow;
            var isCameraFree = __instance._cameraMode_k__BackingField == CameraMode.Free;
            var asker = _toggles.OnlyCenterKeyFollows && isFollow && isCameraFree ? PlayerAsker(__instance) : null;
            var request = new SnapRequest(source, isFollow, __2, isCameraFree, __1 == __instance._mapConfigId_k__BackingField, asker != null);
            var verdict = SnapRules.Judge(request, _toggles);

            Report(verdict, source, asker, __1);

            return verdict != SnapVerdict.Kept;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("checking a camera snap", ex);

            return true;
        }
    }

    /// <summary>
    /// Who asked for the camera moments ago, or null. The center key is also read directly, since the game may act on
    /// it before this frame's key reading is noted.
    /// </summary>
    private static string? PlayerAsker(CameraManager camera)
    {
        var now = Time.unscaledTime;

        if (CameraKeys.IsCenterFollowAsk(camera) || SnapRules.IsWithinAskWindow(CameraKeys.CenterPressedAt, now)) return "you pressed the center key";

        return SnapRules.IsWithinAskWindow(_buttonPressedAt, now) ? "you pressed the camera button" : null;
    }

    private static void Report(SnapVerdict verdict, SnapSource source, string? asker, int floor)
    {
        var from = source == SnapSource.None ? "unmarked" : source.ToString();
        var message = verdict switch
        {
            SnapVerdict.Kept => $"kept the view instead of snapping to your character ({from})",
            SnapVerdict.PlayerAsked => $"followed your character: {asker} ({from})",
            SnapVerdict.GameNeedsCamera => $"followed your character: the game needs the camera ({from})",
            SnapVerdict.AnotherFloor => $"followed your character: the game asked for another floor or map ({from}, floor {floor})",
            _ => null,
        };

        if (message != null) _logFileOnly(message);
    }
}
