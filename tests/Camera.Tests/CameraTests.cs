using Camera.Core;
using Xunit;

namespace Camera.Tests;

internal static class Near
{
    public static void Equal(double expected, float actual) => Assert.Equal(expected, actual, 3);
}

public class ViewLimitsTests
{
    [Fact]
    public void Hand_edited_limits_outside_the_safe_range_are_clamped_rather_than_refused()
    {
        var limits = ViewLimits.From(0f, 1f, 120f, -100f, 500f);

        Assert.Equal(ViewLimits.FlattestPitch, limits.PitchMin);
        Assert.Equal(ViewLimits.SteepestPitch, limits.PitchMax);
        Assert.Equal(ViewLimits.LowestHeight, limits.HeightMin);
        Assert.Equal(ViewLimits.HighestHeight, limits.HeightMax);
    }

    [Fact]
    public void Pitch_limits_written_the_wrong_way_round_are_put_in_order()
    {
        var limits = ViewLimits.From(0f, 80f, 30f, 0f, 0f);

        Assert.Equal(30f, limits.PitchMin);
        Assert.Equal(80f, limits.PitchMax);
    }

    [Fact]
    public void A_yaw_limit_of_zero_or_a_half_turn_lets_the_camera_turn_all_the_way_round()
    {
        Assert.False(ViewLimits.From(0f, 25f, 85f, 0f, 0f).IsYawLimited);
        Assert.False(ViewLimits.From(180f, 25f, 85f, 0f, 0f).IsYawLimited);
        Assert.False(ViewLimits.From(-5f, 25f, 85f, 0f, 0f).IsYawLimited);
        Assert.True(ViewLimits.From(45f, 25f, 85f, 0f, 0f).IsYawLimited);
    }

    [Fact]
    public void Limits_that_are_not_numbers_fall_back_to_the_defaults()
    {
        var limits = ViewLimits.From(float.NaN, float.NaN, float.NaN, float.NaN, float.NaN);

        Assert.False(limits.IsYawLimited);
        Assert.Equal(25f, limits.PitchMin);
        Assert.Equal(85f, limits.PitchMax);
        Assert.Equal(0f, limits.HeightMin);
        Assert.Equal(0f, limits.HeightMax);
    }
}

public class OrbitTests
{
    private static readonly ViewLimits AllRound = ViewLimits.From(0f, 25f, 85f, -4f, 20f);

    [Fact]
    public void Without_a_yaw_limit_the_camera_keeps_turning_past_a_full_circle()
    {
        var view = Orbit.Aim(View.Default, new ViewPush(400f, 0f, 0f), AllRound, 40f);

        Near.Equal(400, view.Yaw);
    }

    [Fact]
    public void A_yaw_limit_stops_the_turn_at_the_limit_either_way()
    {
        var limits = ViewLimits.From(45f, 25f, 85f, 0f, 0f);

        Near.Equal(45, Orbit.Aim(View.Default, new ViewPush(100f, 0f, 0f), limits, 40f).Yaw);
        Near.Equal(-45, Orbit.Aim(View.Default, new ViewPush(-100f, 0f, 0f), limits, 40f).Yaw);
    }

    [Fact]
    public void Tilting_stops_at_the_real_angle_limits_whatever_the_games_own_angle()
    {
        Near.Equal(45, Orbit.Aim(View.Default, new ViewPush(0f, 100f, 0f), AllRound, 40f).Pitch);
        Near.Equal(-15, Orbit.Aim(View.Default, new ViewPush(0f, -100f, 0f), AllRound, 40f).Pitch);
    }

    [Fact]
    public void The_games_own_angle_stays_reachable_when_it_is_outside_the_limits()
    {
        var view = Orbit.Aim(View.Default, new ViewPush(0f, -10f, 0f), AllRound, 20f);

        Near.Equal(0, view.Pitch);
        Assert.Equal((0f, 65f), Orbit.PitchRange(AllRound, 20f));
    }

    [Fact]
    public void Tilting_does_nothing_when_the_games_angle_could_not_be_read()
    {
        Near.Equal(0, Orbit.Aim(View.Default, new ViewPush(0f, 30f, 0f), AllRound, float.NaN).Pitch);
        Near.Equal(0, Orbit.Aim(View.Default, new ViewPush(0f, 30f, 0f), AllRound, 95f).Pitch);
    }

    [Fact]
    public void Raising_and_lowering_stop_at_the_height_limits()
    {
        Near.Equal(20, Orbit.Aim(View.Default, new ViewPush(0f, 0f, 50f), AllRound, 40f).Height);
        Near.Equal(-4, Orbit.Aim(View.Default, new ViewPush(0f, 0f, -50f), AllRound, 40f).Height);
    }

    [Fact]
    public void A_push_that_is_not_a_number_leaves_the_view_where_it_was()
    {
        var target = new View(10f, 5f, 2f);

        Assert.Equal(target, Orbit.Aim(target, new ViewPush(float.NaN, float.PositiveInfinity, float.NaN), AllRound, 40f));
    }

    [Fact]
    public void Turning_past_a_half_turn_rewraps_both_views_together_so_the_turn_continues_the_short_way()
    {
        var (current, target) = Orbit.Rewrap(new View(170f, 0f, 0f), new View(190f, 0f, 0f));

        Near.Equal(-170, target.Yaw);
        Near.Equal(-190, current.Yaw);
    }
}

public class SmoothingTests
{
    [Fact]
    public void A_smoothing_rate_of_zero_moves_the_view_instantly()
    {
        var target = new View(90f, 10f, 3f);

        Assert.Equal(target, Smoothing.Toward(View.Default, target, 0f, 0.016f));
    }

    [Fact]
    public void Easing_covers_the_same_distance_whatever_the_frame_rate()
    {
        var target = new View(90f, 0f, 0f);
        var twoShortFrames = Smoothing.Toward(Smoothing.Toward(View.Default, target, 12f, 0.01f), target, 12f, 0.01f);
        var oneLongFrame = Smoothing.Toward(View.Default, target, 12f, 0.02f);

        Near.Equal(oneLongFrame.Yaw, twoShortFrames.Yaw);
    }

    [Fact]
    public void Easing_lands_exactly_on_the_target_once_close_so_the_default_view_really_is_the_default()
    {
        var almost = new View(0.001f, -0.001f, 0.0001f);

        Assert.True(Smoothing.Toward(almost, View.Default, 12f, 0.016f).IsDefault);
    }

    [Fact]
    public void A_frame_with_no_time_does_not_move_the_view()
    {
        var current = new View(10f, 0f, 0f);

        Assert.Equal(current, Smoothing.Toward(current, new View(90f, 0f, 0f), 12f, 0f));
    }
}

public class ViewDriverTests
{
    private static readonly ViewLimits AllRound = ViewLimits.From(0f, 25f, 85f, -4f, 20f);

    [Fact]
    public void Input_moves_the_view_on_screen()
    {
        var driver = new ViewDriver(AllRound, 0f);

        var view = driver.Step(new ViewPush(30f, 0f, 2f), false, Suspension.None, 40f, 0.016f);

        Assert.Equal(new View(30f, 0f, 2f), view);
    }

    [Fact]
    public void The_reset_key_brings_back_the_games_own_view()
    {
        var driver = new ViewDriver(AllRound, 0f);

        driver.Step(new ViewPush(90f, 10f, 5f), false, Suspension.None, 40f, 0.016f);

        Assert.True(driver.Step(ViewPush.None, true, Suspension.None, 40f, 0.016f).IsDefault);
        Assert.True(driver.Target.IsDefault);
    }

    [Fact]
    public void Cutscenes_and_photo_mode_get_the_games_view_at_once_and_the_players_view_returns_after()
    {
        var driver = new ViewDriver(AllRound, 12f);

        driver.Step(new ViewPush(90f, 0f, 0f), false, Suspension.None, 40f, 1f);

        Assert.True(driver.Step(new ViewPush(30f, 0f, 0f), false, Suspension.Hard, 40f, 0.016f).IsDefault);
        Near.Equal(90, driver.Target.Yaw);

        driver.Step(ViewPush.None, false, Suspension.None, 40f, 1f);

        Near.Equal(90, driver.Current.Yaw);
    }

    [Fact]
    public void Build_mode_ignores_input_and_eases_back_toward_the_games_view()
    {
        var driver = new ViewDriver(AllRound, 12f);

        driver.Step(new ViewPush(90f, 0f, 0f), false, Suspension.None, 40f, 1f);

        var view = driver.Step(new ViewPush(30f, 0f, 0f), false, Suspension.Soft, 40f, 0.05f);

        Near.Equal(90, driver.Target.Yaw);
        Assert.InRange(view.Yaw, 1f, 89f);
    }

    [Fact]
    public void Turning_on_past_a_half_turn_keeps_turning_the_same_way_on_screen()
    {
        var driver = new ViewDriver(AllRound, 12f);

        driver.Step(new ViewPush(170f, 0f, 0f), false, Suspension.None, 40f, 1f);

        var view = driver.Step(new ViewPush(20f, 0f, 0f), false, Suspension.None, 40f, 0.05f);

        Near.Equal(-170, driver.Target.Yaw);
        Assert.InRange(view.Yaw, -189.9f, -170.1f);
    }

    [Fact]
    public void A_new_world_starts_from_the_games_view()
    {
        var driver = new ViewDriver(AllRound, 0f);

        driver.Step(new ViewPush(90f, 10f, 5f), false, Suspension.None, 40f, 0.016f);
        driver.Forget();

        Assert.True(driver.Current.IsDefault);
        Assert.True(driver.Target.IsDefault);
    }
}

public class SuspensionTests
{
    [Fact]
    public void Expeditions_photo_mode_cutscenes_and_the_death_camera_hand_the_camera_straight_back()
    {
        Assert.Equal(Suspension.Hard, Suspensions.Of(new CameraFlags(true, false, false, false, false), false));
        Assert.Equal(Suspension.Hard, Suspensions.Of(new CameraFlags(false, true, false, false, false), false));
        Assert.Equal(Suspension.Hard, Suspensions.Of(new CameraFlags(false, false, true, false, false), false));
        Assert.Equal(Suspension.Hard, Suspensions.Of(new CameraFlags(false, false, false, true, false), false));
    }

    [Fact]
    public void Build_mode_eases_back_to_the_games_view_unless_allowed()
    {
        var building = new CameraFlags(false, false, false, false, true);

        Assert.Equal(Suspension.Soft, Suspensions.Of(building, false));
        Assert.Equal(Suspension.None, Suspensions.Of(building, true));
    }

    [Fact]
    public void A_cutscene_during_build_mode_still_hands_the_camera_straight_back()
    {
        Assert.Equal(Suspension.Hard, Suspensions.Of(new CameraFlags(false, false, true, false, true), true));
    }

    [Fact]
    public void Ordinary_play_leaves_the_camera_to_the_player()
    {
        Assert.Equal(Suspension.None, Suspensions.Of(default, false));
    }
}

public class ViewInputTests
{
    private static readonly KeySpeeds Speeds = new(90f, 45f, 6f);

    [Fact]
    public void Rotate_left_turns_the_way_the_games_own_left_key_does_and_right_the_other_way()
    {
        Near.Equal(90, ViewInput.FromKeys(new HeldKeys(false, true, false, false, false, false), Speeds, 1f).Yaw);
        Near.Equal(-90, ViewInput.FromKeys(new HeldKeys(true, false, false, false, false, false), Speeds, 1f).Yaw);
    }

    [Fact]
    public void Tilt_up_makes_the_view_steeper_and_raise_lifts_it()
    {
        var push = ViewInput.FromKeys(new HeldKeys(false, false, true, false, true, false), Speeds, 1f);

        Near.Equal(45, push.Pitch);
        Near.Equal(6, push.Height);
    }

    [Fact]
    public void Opposite_keys_held_together_cancel()
    {
        Assert.Equal(ViewPush.None, ViewInput.FromKeys(new HeldKeys(true, true, true, true, true, true), Speeds, 1f));
    }

    [Fact]
    public void Held_keys_move_the_view_by_their_speed_times_the_frame_time()
    {
        Near.Equal(4.5, ViewInput.FromKeys(new HeldKeys(false, true, false, false, false, false), Speeds, 0.05f).Yaw);
    }

    [Fact]
    public void Dragging_right_turns_right_and_dragging_up_looks_flatter()
    {
        var push = ViewInput.FromMouse(10f, 10f, 0.5f, false, false);

        Near.Equal(5, push.Yaw);
        Near.Equal(-5, push.Pitch);
    }

    [Fact]
    public void Inverted_axes_turn_and_tilt_the_other_way()
    {
        var push = ViewInput.FromMouse(10f, 10f, 0.5f, true, true);

        Near.Equal(-5, push.Yaw);
        Near.Equal(5, push.Pitch);
    }
}

public class ZoomGuardTests
{
    [Fact]
    public void The_games_zoom_range_is_widened_by_the_extra_each_way()
    {
        Assert.Equal((20f, 75f), new ZoomGuard(10f, 15f).Next(30f, 60f));
    }

    [Fact]
    public void A_range_already_widened_is_not_widened_again()
    {
        var zoom = new ZoomGuard(10f, 15f);

        zoom.Next(30f, 60f);

        Assert.Null(zoom.Next(20f, 75f));
    }

    [Fact]
    public void A_new_range_written_by_the_game_is_widened_from_the_games_values()
    {
        var zoom = new ZoomGuard(10f, 15f);

        zoom.Next(30f, 60f);

        Assert.Equal((25f, 65f), zoom.Next(35f, 50f));
    }

    [Fact]
    public void Zoom_never_goes_past_the_narrowest_and_widest_safe_angles()
    {
        Assert.Equal((ZoomGuard.NarrowestFov, ZoomGuard.WidestFov), new ZoomGuard(10f, 15f).Next(8f, 100f));
    }

    [Fact]
    public void The_games_own_range_is_never_narrowed()
    {
        Assert.Null(new ZoomGuard(10f, 15f).Next(3f, 150f));
    }

    [Fact]
    public void A_range_the_game_could_not_mean_is_left_alone()
    {
        var zoom = new ZoomGuard(10f, 15f);

        Assert.Null(zoom.Next(0f, 60f));
        Assert.Null(zoom.Next(60f, 30f));
        Assert.Null(zoom.Next(float.NaN, 60f));
        Assert.Null(zoom.Next(30f, 200f));
    }

    [Fact]
    public void Extra_zoom_that_is_negative_or_not_a_number_widens_nothing()
    {
        Assert.False(new ZoomGuard(-5f, float.NaN).WidensAnything);
        Assert.True(new ZoomGuard(0f, 1f).WidensAnything);
    }
}

public class SnapTests
{
    private static readonly SnapToggles AllOn = new(true, true, true);

    [Fact]
    public void A_request_made_inside_a_snapping_path_is_traced_to_it()
    {
        var scope = new SnapScope();

        scope.Enter(SnapSource.Stairs, 5);

        Assert.Equal(SnapSource.Stairs, scope.Active(5));
    }

    [Fact]
    public void A_scope_left_open_by_an_error_dies_with_its_frame()
    {
        var scope = new SnapScope();

        scope.Enter(SnapSource.Stairs, 5);

        Assert.Equal(SnapSource.None, scope.Active(6));
    }

    [Fact]
    public void Leaving_a_scope_returns_to_the_one_around_it()
    {
        var scope = new SnapScope();

        scope.Enter(SnapSource.Stairs, 5);
        scope.Enter(SnapSource.MenuClose, 5);
        scope.Leave(SnapSource.MenuClose);

        Assert.Equal(SnapSource.Stairs, scope.Active(5));
    }

    [Fact]
    public void Leaving_a_scope_that_is_not_the_innermost_changes_nothing()
    {
        var scope = new SnapScope();

        scope.Enter(SnapSource.Stairs, 5);
        scope.Leave(SnapSource.BuildExit);

        Assert.Equal(SnapSource.Stairs, scope.Active(5));
    }

    [Fact]
    public void A_scope_left_open_last_frame_does_not_leak_into_a_new_one()
    {
        var scope = new SnapScope();

        scope.Enter(SnapSource.Stairs, 5);
        scope.Enter(SnapSource.MenuClose, 6);
        scope.Leave(SnapSource.MenuClose);

        Assert.Equal(SnapSource.None, scope.Active(6));
    }

    /// <summary>A follow request from the stairs, to another floor, while the player looks around in free mode.</summary>
    private static readonly SnapRequest Elsewhere = new(SnapSource.Stairs, IsFollow: true, IsForced: false, IsCameraFree: true, IsViewedFloor: false);

    [Fact]
    public void Each_snap_is_held_back_while_the_player_is_looking_elsewhere()
    {
        Assert.True(SnapRules.ShouldKeepView(Elsewhere, AllOn));
        Assert.True(SnapRules.ShouldKeepView(Elsewhere with { Source = SnapSource.MenuClose }, AllOn));
        Assert.True(SnapRules.ShouldKeepView(Elsewhere with { Source = SnapSource.BuildExit }, AllOn));
    }

    [Fact]
    public void A_snap_whose_setting_is_off_goes_through()
    {
        Assert.False(SnapRules.ShouldKeepView(Elsewhere, new SnapToggles(false, true, true)));
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { Source = SnapSource.MenuClose }, new SnapToggles(true, false, true)));
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { Source = SnapSource.BuildExit }, new SnapToggles(true, true, false)));
    }

    [Fact]
    public void A_forced_request_always_goes_through()
    {
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { IsForced = true }, AllOn));
    }

    [Fact]
    public void Nothing_is_held_back_while_the_camera_already_follows_the_character()
    {
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { IsCameraFree = false }, AllOn));
    }

    [Fact]
    public void Requests_from_anywhere_else_such_as_the_center_key_always_go_through()
    {
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { Source = SnapSource.None }, AllOn));
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { IsFollow = false }, AllOn));
    }

    [Fact]
    public void A_character_arriving_on_the_floor_being_viewed_is_followed_again()
    {
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { IsViewedFloor = true }, AllOn));
        Assert.False(SnapRules.ShouldKeepView(Elsewhere with { Source = SnapSource.MenuClose, IsViewedFloor = true }, AllOn));
    }
}

public class TimeControlKeysTests
{
    [Fact]
    public void Time_Controls_chosen_keys_are_read_from_its_keys_section()
    {
        var names = TimeControlKeys.Read(new[]
        {
            "[Keys]",
            "",
            "## A key that pauses",
            "# Setting type: String",
            "# Default value: Space",
            "PauseToggleKey = F",
            "",
            "CenterCameraKey = Tab",
        });

        Assert.Equal(new TimeControlKeyNames("F", "Tab"), names);
    }

    [Fact]
    public void Same_names_in_other_sections_and_comments_are_not_taken_for_its_keys()
    {
        var names = TimeControlKeys.Read(new[]
        {
            "[Alerts]",
            "PauseToggleKey = F",
            "[Keys]",
            "# PauseToggleKey = Tab",
        });

        Assert.Equal(TimeControlKeyNames.Defaults, names);
    }

    [Fact]
    public void A_setting_missing_from_the_file_keeps_Time_Controls_default()
    {
        var names = TimeControlKeys.Read(new[] { "[Keys]", "PauseToggleKey = None" });

        Assert.Equal(new TimeControlKeyNames("None", "G"), names);
    }
}

public class KeyChoiceTests
{
    [Fact]
    public void An_empty_setting_or_None_turns_the_key_off()
    {
        Assert.Equal(KeyChoice.Off, KeyChoice.Parse("", true));
        Assert.Equal(KeyChoice.Off, KeyChoice.Parse(null, true));
        Assert.Equal(KeyChoice.Off, KeyChoice.Parse(" none ", false));
    }

    [Fact]
    public void Game_borrows_the_games_key_where_the_game_has_a_matching_action()
    {
        Assert.Equal(KeyChoice.Game, KeyChoice.Parse(" game ", true));
    }

    [Fact]
    public void Game_is_just_a_key_name_where_the_game_has_no_matching_action()
    {
        Assert.Equal(new KeyChoice(KeyChoiceKind.Named, "Game"), KeyChoice.Parse("Game", false));
    }

    [Fact]
    public void A_named_key_is_kept_trimmed_for_checking_later()
    {
        Assert.Equal(new KeyChoice(KeyChoiceKind.Named, "PageUp"), KeyChoice.Parse("  PageUp ", false));
    }

    [Fact]
    public void The_drag_button_is_read_by_name_and_an_unknown_name_is_reported()
    {
        Assert.Equal(DragButton.Middle, DragButtons.Parse(" middle "));
        Assert.Equal(DragButton.Right, DragButtons.Parse("Right"));
        Assert.Equal(DragButton.None, DragButtons.Parse(""));
        Assert.Null(DragButtons.Parse("Left"));
    }
}

public class KeyClashTests
{
    private const int PageUp = 280;
    private const int Q = 113;
    private const int RotateLeftAction = 7;

    [Fact]
    public void A_key_the_game_already_uses_is_turned_off_and_says_who_has_it()
    {
        var verdicts = KeyClash.Check(new[] { new WantedKey("RaiseKey", Q) }, new[] { new TakenKey(Q, "the game's Build key", 3) });

        Assert.False(verdicts[0].IsUsable);
        Assert.Equal("the game's Build key", verdicts[0].ClashesWith);
    }

    [Fact]
    public void Borrowing_the_games_own_rotate_key_is_not_a_clash_with_that_action()
    {
        var verdicts = KeyClash.Check(
            new[] { new WantedKey("RotateLeftKey", Q, RotateLeftAction) },
            new[] { new TakenKey(Q, "the game's CameraRotateLeft key", RotateLeftAction) });

        Assert.True(verdicts[0].IsUsable);
    }

    [Fact]
    public void Two_settings_on_one_key_leave_it_with_the_first()
    {
        var verdicts = KeyClash.Check(new[] { new WantedKey("RaiseKey", PageUp), new WantedKey("ResetKey", PageUp) }, Array.Empty<TakenKey>());

        Assert.True(verdicts[0].IsUsable);
        Assert.Equal("RaiseKey", verdicts[1].ClashesWith);
    }

    [Fact]
    public void A_setting_with_no_key_is_unusable_but_takes_the_key_from_nobody()
    {
        var verdicts = KeyClash.Check(new[] { new WantedKey("TiltUpKey", 0), new WantedKey("RaiseKey", PageUp) }, new[] { new TakenKey(0, "nothing") });

        Assert.False(verdicts[0].IsUsable);
        Assert.Null(verdicts[0].ClashesWith);
        Assert.True(verdicts[1].IsUsable);
    }
}
