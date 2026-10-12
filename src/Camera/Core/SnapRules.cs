using System.Collections.Generic;

namespace Camera.Core;

/// <summary>Which of the game's automatic snaps back to the character a camera request came from.</summary>
public enum SnapSource
{
    None,

    /// <summary>The player's character took stairs or a ladder to another floor.</summary>
    Stairs,

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
    /// only when it would take the camera to another floor, and only when that snap's setting is on. A forced request
    /// always goes through, and so does one for the floor already on screen: the character came to the player's floor.
    /// </summary>
    public static bool ShouldKeepView(SnapRequest request, SnapToggles toggles)
    {
        if (!request.IsFollow || request.IsForced || !request.IsCameraFree || request.IsViewedFloor) return false;

        return request.Source switch
        {
            SnapSource.Stairs => toggles.StayOnStairs,
            SnapSource.MenuClose => toggles.StayAfterMenuClose,
            SnapSource.BuildExit => toggles.StayAfterBuildMode,
            _ => false,
        };
    }
}
