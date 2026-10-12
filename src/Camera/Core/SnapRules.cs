using System.Collections.Generic;

namespace Camera.Core;

/// <summary>Which of the game's automatic snaps back to the character a camera request came from.</summary>
public enum SnapSource
{
    None,

    /// <summary>The player's character set off for stairs or a ladder to another floor.</summary>
    Stairs,

    /// <summary>The player's character came out on another floor, and the game insists the camera follows them there.</summary>
    StairsArrival,

    /// <summary>An object menu closed while the camera showed another floor.</summary>
    MenuClose,

    /// <summary>Build mode ended.</summary>
    BuildExit,
}

/// <summary>Which automatic snaps the player wants held back. Each one is its own setting.</summary>
public readonly record struct SnapToggles(bool StayOnStairs, bool StayAfterMenuClose, bool StayAfterBuildMode)
{
    public bool IsAnyOn => StayOnStairs || StayAfterMenuClose || StayAfterBuildMode;
}

/// <summary>
/// Remembers which of the game's snapping code paths is running, so the camera request it makes inside can be told
/// apart from the player's own (the center key) and from every other caller. The game makes the request on the same
/// call stack, so a scope only counts on the frame it was entered; one left open by an exception dies with its frame.
/// </summary>
public sealed class SnapScope
{
    private readonly List<SnapSource> _open = new();
    private int _frame = int.MinValue;

    public void Enter(SnapSource source, int frame)
    {
        if (frame != _frame)
        {
            _open.Clear();
            _frame = frame;
        }

        _open.Add(source);
    }

    /// <summary>Only closes the innermost scope, and only if it is the one named, so a mismatched leave changes nothing.</summary>
    public void Leave(SnapSource source)
    {
        if (_open.Count > 0 && _open[^1] == source) _open.RemoveAt(_open.Count - 1);
    }

    public SnapSource Active(int frame) => frame == _frame && _open.Count > 0 ? _open[^1] : SnapSource.None;
}

/// <summary>
/// One request the game makes to change the camera mode, as the snap guard sees it. IsViewedFloor says the floor it
/// asks for is the one the camera already shows.
/// </summary>
public readonly record struct SnapRequest(SnapSource Source, bool IsFollow, bool IsForced, bool IsCameraFree, bool IsViewedFloor);

public static class SnapRules
{
    /// <summary>
    /// Whether to drop the game's request to follow the character. Only a follow request made inside one of the
    /// snapping code paths is dropped, only while the player has the camera looking elsewhere (the game's free mode),
    /// and only when that snap's setting is on. Each snap then has its own exceptions:
    /// <list type="bullet">
    /// <item>Setting off for stairs is held back even on the floor on screen, since following would only drag the
    /// camera across to the stairs the character is about to leave by.</item>
    /// <item>Arriving on another floor is held back even though the game forces it, and even on the floor on screen,
    /// since that is the jump to the character's new floor.</item>
    /// <item>After a menu or build mode a forced request goes through, and so does one for the floor on screen.</item>
    /// </list>
    /// </summary>
    public static bool ShouldKeepView(SnapRequest request, SnapToggles toggles)
    {
        if (!request.IsFollow || !request.IsCameraFree) return false;

        return request.Source switch
        {
            SnapSource.Stairs => toggles.StayOnStairs && !request.IsForced,
            SnapSource.StairsArrival => toggles.StayOnStairs,
            SnapSource.MenuClose => toggles.StayAfterMenuClose && IsElsewhere(request),
            SnapSource.BuildExit => toggles.StayAfterBuildMode && IsElsewhere(request),
            _ => false,
        };
    }

    private static bool IsElsewhere(SnapRequest request) => !request.IsForced && !request.IsViewedFloor;
}
