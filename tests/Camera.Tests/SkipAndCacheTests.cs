using Camera.Core;
using Xunit;

namespace Camera.Tests;

/// <summary>A stand-in for the game's key settings that counts how often it is asked.</summary>
internal sealed class FakeGameKeys
{
    public Dictionary<int, int> Keys { get; } = new();

    public int Reads { get; private set; }

    public int Read(int action)
    {
        Reads++;

        return Keys.TryGetValue(action, out var key) ? key : 0;
    }
}

public class KeyBindsTests
{
    private const int RotateLeft = 1;
    private const int RotateRight = 2;
    private const int Period = 46;
    private const int Comma = 44;

    [Fact]
    public void A_key_already_read_is_not_asked_of_the_game_again_while_nothing_changed()
    {
        var game = new FakeGameKeys();
        var binds = new KeyBinds(game.Read) { IsWatched = true };

        game.Keys[RotateLeft] = Period;

        Assert.Equal(Period, binds.KeyFor(RotateLeft));
        Assert.Equal(Period, binds.KeyFor(RotateLeft));
        Assert.Equal(Period, binds.KeyFor(RotateLeft));
        Assert.Equal(1, game.Reads);
    }

    [Fact]
    public void A_key_rebound_in_the_game_is_seen_once_its_settings_are_saved()
    {
        var game = new FakeGameKeys();
        var binds = new KeyBinds(game.Read) { IsWatched = true };

        game.Keys[RotateLeft] = Period;
        binds.KeyFor(RotateLeft);
        game.Keys[RotateLeft] = Comma;

        Assert.Equal(Period, binds.KeyFor(RotateLeft));

        binds.Forget();

        Assert.Equal(Comma, binds.KeyFor(RotateLeft));
        Assert.Equal(2, game.Reads);
    }

    [Fact]
    public async Task A_forget_from_another_thread_is_seen_on_the_next_ask()
    {
        var game = new FakeGameKeys();
        var binds = new KeyBinds(game.Read) { IsWatched = true };

        game.Keys[RotateLeft] = Period;
        binds.KeyFor(RotateLeft);
        game.Keys[RotateLeft] = Comma;
        await Task.Run(() => binds.Forget());

        Assert.Equal(Comma, binds.KeyFor(RotateLeft));
    }

    [Fact]
    public void Every_kept_key_is_dropped_by_one_forget()
    {
        var game = new FakeGameKeys();
        var binds = new KeyBinds(game.Read) { IsWatched = true };

        binds.KeyFor(RotateLeft);
        binds.KeyFor(RotateRight);
        binds.Forget();
        binds.KeyFor(RotateLeft);
        binds.KeyFor(RotateRight);

        Assert.Equal(4, game.Reads);
    }

    [Fact]
    public void Each_action_is_read_and_kept_on_its_own()
    {
        var game = new FakeGameKeys();
        var binds = new KeyBinds(game.Read) { IsWatched = true };

        game.Keys[RotateLeft] = Period;
        game.Keys[RotateRight] = Comma;

        Assert.Equal(Period, binds.KeyFor(RotateLeft));
        Assert.Equal(Comma, binds.KeyFor(RotateRight));
        Assert.Equal(Period, binds.KeyFor(RotateLeft));
        Assert.Equal(Comma, binds.KeyFor(RotateRight));
        Assert.Equal(2, game.Reads);
    }

    [Fact]
    public void Without_the_saves_watched_every_ask_reads_the_game_so_a_rebind_is_never_missed()
    {
        var game = new FakeGameKeys();
        var binds = new KeyBinds(game.Read);

        game.Keys[RotateLeft] = Period;
        binds.KeyFor(RotateLeft);
        game.Keys[RotateLeft] = Comma;

        Assert.Equal(Comma, binds.KeyFor(RotateLeft));
        Assert.Equal(2, game.Reads);
    }
}

public class HotKeyGateTests
{
    [Fact]
    public void Keys_are_free_before_the_game_has_said_anything_and_no_frame_is_needed()
    {
        var gate = new HotKeyGate();

        Assert.False(gate.NeedsFrame);
        Assert.True(gate.IsFree(0));
    }

    [Fact]
    public void A_free_reading_needs_no_frame_number()
    {
        var gate = new HotKeyGate();

        gate.NoteBlocked(10);
        gate.NoteFree();

        Assert.False(gate.NeedsFrame);
        Assert.True(gate.IsFree(10));
    }

    [Fact]
    public void A_reading_that_holds_keys_back_counts_for_its_frame_and_the_next_then_lets_go()
    {
        var gate = new HotKeyGate();

        gate.NoteBlocked(100);

        Assert.True(gate.NeedsFrame);
        Assert.False(gate.IsFree(100));
        Assert.False(gate.IsFree(101));
        Assert.True(gate.IsFree(102));
    }

    [Fact]
    public void A_newer_reading_that_holds_keys_back_moves_the_frame_on()
    {
        var gate = new HotKeyGate();

        gate.NoteBlocked(100);
        gate.NoteBlocked(105);

        Assert.False(gate.IsFree(106));
        Assert.True(gate.IsFree(107));
    }
}

public class ViewMemoTests
{
    private static readonly View Turned = new(30f, 10f, 0f);
    private static readonly View TurnedMore = new(31f, 10f, 0f);
    private static readonly View Raised = new(30f, 10f, 2f);

    [Fact]
    public void A_view_that_has_not_moved_gets_the_kept_result_back()
    {
        var memo = new ViewMemo<float>();

        memo.Keep(Turned, 1.5f);

        Assert.True(memo.TryGet(new View(30f, 10f, 0f), out var kept));
        Assert.Equal(1.5f, kept);
    }

    [Fact]
    public void A_view_that_moved_in_any_way_is_worked_out_again()
    {
        var memo = new ViewMemo<float>();

        memo.Keep(Turned, 1.5f);

        Assert.False(memo.TryGet(TurnedMore, out _));
        Assert.False(memo.TryGet(Raised, out _));
        Assert.False(memo.TryGet(Turned with { ZoomOut = 0.1f }, out _));
    }

    [Fact]
    public void This_frame_and_last_frame_are_both_kept_but_not_the_frame_before()
    {
        var memo = new ViewMemo<float>();

        memo.Keep(Turned, 1f);
        memo.Keep(TurnedMore, 2f);

        Assert.True(memo.TryGet(Turned, out var older));
        Assert.Equal(1f, older);
        Assert.True(memo.TryGet(TurnedMore, out var newest));
        Assert.Equal(2f, newest);

        memo.Keep(Raised, 3f);

        Assert.False(memo.TryGet(Turned, out _));
        Assert.True(memo.TryGet(TurnedMore, out _));
    }

    [Fact]
    public void A_new_world_drops_every_kept_result()
    {
        var memo = new ViewMemo<float>();

        memo.Keep(Turned, 1f);
        memo.Keep(TurnedMore, 2f);
        memo.Forget();

        Assert.False(memo.TryGet(Turned, out _));
        Assert.False(memo.TryGet(TurnedMore, out _));
    }

    [Fact]
    public void A_result_kept_after_a_forget_is_found_again()
    {
        var memo = new ViewMemo<float>();

        memo.Keep(Turned, 1f);
        memo.Forget();
        memo.Keep(Turned, 5f);

        Assert.True(memo.TryGet(Turned, out var kept));
        Assert.Equal(5f, kept);
    }
}

public class LogLinesTests
{
    private const string StampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    [Fact]
    public void Lines_come_out_oldest_first_each_stamped_with_its_own_local_time()
    {
        var lines = new LogLines();
        var first = new DateTime(2026, 10, 26, 8, 30, 1, 250, DateTimeKind.Utc);
        var second = first.AddSeconds(2);

        lines.Add(first, "camera angle read");
        lines.Add(second, "kept the view");

        var expected =
            first.ToLocalTime().ToString(StampFormat) + " camera angle read" + Environment.NewLine +
            second.ToLocalTime().ToString(StampFormat) + " kept the view" + Environment.NewLine;

        Assert.Equal(expected, lines.Take());
    }

    [Fact]
    public void Nothing_waiting_gives_nothing_to_write()
    {
        Assert.Null(new LogLines().Take());
    }

    [Fact]
    public void Taken_lines_are_not_written_twice()
    {
        var lines = new LogLines();

        lines.Add(DateTime.UtcNow, "one");
        lines.Take();

        Assert.Null(lines.Take());

        lines.Add(DateTime.UtcNow, "two");

        Assert.EndsWith(" two" + Environment.NewLine, lines.Take());
    }
}

public class HeldKeysTests
{
    [Fact]
    public void No_key_held_is_nothing_to_act_on()
    {
        Assert.False(new HeldKeys(false, false, false, false, false, false).IsAny);
    }

    [Theory]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(false, true, false, false, false, false)]
    [InlineData(false, false, true, false, false, false)]
    [InlineData(false, false, false, true, false, false)]
    [InlineData(false, false, false, false, true, false)]
    [InlineData(false, false, false, false, false, true)]
    public void Any_one_camera_key_held_is_acted_on(bool rotateLeft, bool rotateRight, bool tiltUp, bool tiltDown, bool raise, bool lower)
    {
        var held = new HeldKeys(rotateLeft, rotateRight, tiltUp, tiltDown, raise, lower);

        Assert.True(held.IsAny);
        Assert.NotEqual(ViewPush.None, ViewInput.FromKeys(held, new KeySpeeds(90f, 45f, 6f), 0.016f));
    }

    [Fact]
    public void Skipping_the_keys_with_none_held_pushes_the_view_exactly_as_reading_them_would()
    {
        var none = new HeldKeys(false, false, false, false, false, false);

        Assert.Equal(ViewPush.None, ViewInput.FromKeys(none, new KeySpeeds(90f, 45f, 6f), 0.016f));
    }
}

public class WarmUpTests
{
    [Fact]
    public void The_warm_up_goes_all_the_way_through_the_camera_code_it_compiles()
    {
        Assert.Equal("view 90 0 0, kept 10 snaps, 2 keys usable, 2 key reads, gate right, log taken", WarmUp.Run());
    }

    [Fact]
    public void The_warm_up_touches_nothing_shared_so_running_it_again_gives_the_same()
    {
        Assert.Equal(WarmUp.Run(), WarmUp.Run());
    }
}
