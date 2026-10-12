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
/// Holds back three of the game's automatic snaps to the character while the player is looking at another floor.
/// Each snap is the game asking SetCameraMode for Follow from inside one known method, on the same call stack, so
/// that method is marked while it runs and only a request made inside it is dropped. The center key, map moves,
/// cutscenes and photo mode make their requests from elsewhere and always go through.
/// </summary>
internal static class SnapPatches
{
    private static readonly SnapScope Scope = new();

    private static SnapToggles _toggles;
    private static Action<string> _logFileOnly = _ => { };

    public static void Apply(Harmony harmony, SnapToggles toggles, Action<string> log, Action<string> logFileOnly)
    {
        _toggles = toggles;
        _logFileOnly = logFileOnly;

        if (!toggles.IsAnyOn)
        {
            log("all automatic camera snaps left as the game has them");

            return;
        }

        var isGuardHooked = PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.SetCameraMode), prefix: nameof(KeepViewIfSnap),
            done: "camera snap guard hooked", consequence: "the camera still snaps to your character on stairs, after menus and after build mode");

        if (!isGuardHooked) return;

        if (toggles.StayOnStairs)
        {
            PatchKit.Patch(harmony, typeof(SnapPatches), typeof(AE_FloorMove), nameof(AE_FloorMove.Run), prefix: nameof(EnterStairs), postfix: nameof(LeaveStairs),
                done: "the camera stays put when your character takes stairs or a ladder", consequence: "the camera still snaps to your character on stairs and ladders");
        }

        if (toggles.StayAfterMenuClose)
        {
            PatchKit.Patch(harmony, typeof(SnapPatches), typeof(Reducer_Web_SelectMenu), nameof(Reducer_Web_SelectMenu.TryReturnToPlayerFloor), prefix: nameof(EnterMenuClose), postfix: nameof(LeaveMenuClose),
                done: "the camera stays on the floor you were viewing when an object menu closes", consequence: "the camera still snaps to your character's floor when an object menu closes");
        }

        if (toggles.StayAfterBuildMode)
        {
            PatchKit.Patch(harmony, typeof(SnapPatches), typeof(CameraManager), nameof(CameraManager.OnExitInstallPlanMode), prefix: nameof(EnterBuildExit), postfix: nameof(LeaveBuildExit),
                done: "the camera stays where it was when build mode ends", consequence: "the camera still snaps to your character when build mode ends");
        }
    }

    private static void EnterStairs() => Scope.Enter(SnapSource.Stairs, Time.frameCount);

    private static void LeaveStairs() => Scope.Leave(SnapSource.Stairs);

    private static void EnterMenuClose() => Scope.Enter(SnapSource.MenuClose, Time.frameCount);

    private static void LeaveMenuClose() => Scope.Leave(SnapSource.MenuClose);

    private static void EnterBuildExit() => Scope.Enter(SnapSource.BuildExit, Time.frameCount);

    private static void LeaveBuildExit() => Scope.Leave(SnapSource.BuildExit);

    /// <returns>False drops the game's request, so the camera stays where the player put it.</returns>
    private static bool KeepViewIfSnap(CameraManager __instance, CameraMode __0, bool __2)
    {
        try
        {
            var source = Scope.Active(Time.frameCount);

            if (source == SnapSource.None) return true;

            var isKept = SnapRules.ShouldKeepView(source, __0 == CameraMode.Follow, __2, __instance.cameraMode == CameraMode.Free, _toggles);

            if (isKept) _logFileOnly($"kept the view instead of snapping to your character ({source})");

            return !isKept;
        }
        catch (Exception ex)
        {
            PatchKit.Fail("checking a camera snap", ex);

            return true;
        }
    }
}
