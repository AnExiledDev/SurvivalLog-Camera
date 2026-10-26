using System;

namespace Camera.Core;

/// <summary>
/// Runs the camera's own per-frame and first-frame code once, on throwaway objects, so the runtime compiles it at
/// load instead of on the frame the player first turns the camera, presses a key or meets a snap. Pure .NET and
/// touches nothing shared, so it can run on any thread.
/// </summary>
public static class WarmUp
{
    private const int RotateLeftAction = 1;
    private const int PeriodKey = 46;
    private const int SpaceKey = 32;
    private const int PageUpKey = 280;

    /// <returns>What the throwaway camera ended up with; for the test that it went all the way through.</returns>
    public static string Run()
    {
        var view = TurnTheView();
        var kept = JudgeEverySnap();
        var usable = CheckKeys();
        var reads = ReadKeysTwice();
        var isGateRight = CheckGate();
        var log = new LogLines();

        log.Add(DateTime.UtcNow, "warm-up");

        var hasLog = log.Take() != null;

        return $"view {(int)view.Yaw} {(int)view.Pitch} {(int)view.Height}, kept {kept} snaps, {usable} keys usable, {reads} key reads, gate {(isGateRight ? "right" : "wrong")}, log {(hasLog ? "taken" : "empty")}";
    }

    /// <summary>A second of the right rotate key held with no smoothing, then every kind of suspension and a reset.</summary>
    private static View TurnTheView()
    {
        var limits = ViewLimits.From(0f, 25f, 85f, -4f, 20f, 0.5f, 3f);
        var driver = new ViewDriver(limits, 0f);
        var held = new HeldKeys(false, true, false, false, false, false);
        var push = held.IsAny ? ViewInput.FromKeys(held, new KeySpeeds(90f, 45f, 6f), 1f) : ViewPush.None;

        push += ViewInput.FromMouse(0f, 0f, 0.25f, false, false);
        push += ViewInput.FromScroll(0f, 1f);

        var view = driver.Step(push, false, Suspensions.Of(new CameraFlags(false, false, false, false), true), 45f, 0.1f);
        var spare = new ViewDriver(limits, 12f);

        spare.Step(push, false, Suspension.None, 45f, 0.016f);
        spare.Step(ViewPush.None, false, Suspensions.Of(new CameraFlags(false, false, false, true), false), 45f, 0.016f);
        spare.Step(ViewPush.None, false, Suspensions.Of(new CameraFlags(true, false, false, false), true), 45f, 0.016f);
        spare.Step(ViewPush.None, true, Suspension.None, 45f, 0.016f);
        spare.Use(limits, 0f);
        spare.Forget();

        return view;
    }

    private static int JudgeEverySnap()
    {
        var scope = new SnapScope();
        var kept = 0;

        scope.Enter(SnapSource.Stairs, 1);

        var active = scope.Active(1);

        scope.Leave(SnapSource.Stairs);

        foreach (var source in Enum.GetValues<SnapSource>())
        {
            for (var isOnlyCenterKey = 0; isOnlyCenterKey < 2; isOnlyCenterKey++)
            {
                var request = new SnapRequest(source, true, false, true, false);

                if (SnapRules.Judge(request, new SnapToggles(true, true, true, isOnlyCenterKey == 1)) == SnapVerdict.Kept) kept++;
            }
        }

        var isAsked = SnapRules.IsWithinAskWindow(0f, 0.1f) &&
            SnapRules.IsCameraButtonFollowAsk(active, true) &&
            SnapRules.IsCenterKeyFollowAsk(true, true, true) &&
            FollowZoom.ShouldCarry(true, true) &&
            HookFailure.Is(HookFailure.Prefix + " warm-up");

        return isAsked ? kept : -1;
    }

    /// <summary>The setup's key check: a borrowed game key, a free key, and one Time Control already has.</summary>
    private static int CheckKeys()
    {
        var names = TimeControlKeys.Read(new[] { "[Keys]", "PauseToggleKey = Space", "CenterCameraKey = G" });
        var choices = KeyChoice.Parse("Game", true).Kind == KeyChoiceKind.Game &&
            KeyChoice.Parse("PageUp", false).Kind == KeyChoiceKind.Named &&
            DragButtons.Parse("Middle") == DragButton.Middle;
        var wanted = new[]
        {
            new WantedKey("RotateLeftKey", PeriodKey, RotateLeftAction),
            new WantedKey("RaiseKey", PageUpKey),
            new WantedKey("TiltUpKey", SpaceKey),
        };
        var taken = new[]
        {
            new TakenKey(PeriodKey, "the game's CameraRotateLeft key", RotateLeftAction),
            new TakenKey(SpaceKey, "Time Control's " + names.PauseToggleKey + " key"),
        };
        var usable = 0;

        foreach (var verdict in KeyClash.Check(wanted, taken))
        {
            if (verdict.IsUsable) usable++;
        }

        return choices ? usable : -1;
    }

    /// <summary>Two asks for one key, a forget, then one more: the game is read twice.</summary>
    private static int ReadKeysTwice()
    {
        var reads = 0;
        var binds = new KeyBinds(action =>
        {
            reads++;

            return action + 1;
        }) { IsWatched = true };

        binds.KeyFor(RotateLeftAction);
        binds.KeyFor(RotateLeftAction);
        binds.Forget();
        binds.KeyFor(RotateLeftAction);

        return reads;
    }

    private static bool CheckGate()
    {
        var gate = new HotKeyGate();

        gate.NoteBlocked(10);

        var isHeldBack = gate.NeedsFrame && !gate.IsFree(11);
        var isLetGo = gate.IsFree(12);

        gate.NoteFree();

        return isHeldBack && isLetGo && !gate.NeedsFrame;
    }
}
