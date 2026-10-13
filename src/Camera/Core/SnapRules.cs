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

    /// <summary>The game's camera mode toggle, which its center key, its rotate keys and the camera button on the HUD all go through.</summary>
    CameraToggle,

    /// <summary>One of the game's camera keys (the center key or a rotate key) asked for the camera mode toggle.</summary>
    GameCameraKey,

    /// <summary>A cutscene took the camera.</summary>
    Cutscene,

    /// <summary>Photo mode started or ended.</summary>
    PhotoMode,

    /// <summary>The world started and the game put the camera on the character.</summary>
    WorldStart,

    /// <summary>The tutorial's door scene moved the camera.</summary>
    TutorialDoor,

    /// <summary>The character travelled to another map.</summary>
    MapTravel,
}

/// <summary>
/// Which automatic snaps the player wants held back. Each of the first three is its own setting;
/// OnlyCenterKeyFollows holds back every one of them, and more, while it is on.
/// </summary>
public readonly record struct SnapToggles(bool StayOnStairs, bool StayAfterMenuClose, bool StayAfterBuildMode, bool OnlyCenterKeyFollows = false)
{
    public bool IsAnyOn => StayOnStairs || StayAfterMenuClose || StayAfterBuildMode || OnlyCenterKeyFollows;
}

/// <summary>What the snap guard made of one camera request.</summary>
public enum SnapVerdict
{
    /// <summary>Not a request to follow the character, or the camera already follows them.</summary>
    NotAJump,

    /// <summary>A snap whose own setting is off, or one it lets through.</summary>
    Allowed,

    /// <summary>Dropped: the camera stays where the player put it.</summary>
    Kept,

    /// <summary>The player asked for it with the center key or the camera button.</summary>
    PlayerAsked,

    /// <summary>A cutscene, photo mode, the world starting, the tutorial door or map travel needs the camera.</summary>
    GameNeedsCamera,

    /// <summary>An unmarked request for a floor or map other than the one on screen.</summary>
    AnotherFloor,
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
/// asks for is the one the camera already shows. IsPlayerAsking says the player pressed the center key or the camera
/// button moments before.
/// </summary>
public readonly record struct SnapRequest(SnapSource Source, bool IsFollow, bool IsForced, bool IsCameraFree, bool IsViewedFloor, bool IsPlayerAsking = false);

public static class SnapRules
{
    /// <summary>
    /// How long after the center key or the camera button a follow request still counts as the player's. The game
    /// hands the press through its menu system before the camera sees it, which may take a few frames.
    /// </summary>
    public const float AskWindowSeconds = 0.5f;

    /// <summary>Whether a press at askedAt still covers a request at now (both unscaled seconds). No press yet is negative infinity.</summary>
    public static bool IsWithinAskWindow(float askedAt, float now)
    {
        var elapsed = now - askedAt;

        return elapsed >= 0f && elapsed <= AskWindowSeconds;
    }

    /// <summary>
    /// Whether the game's camera toggle action, as it is sent, is the player asking for Follow with the camera button.
    /// The game's camera keys send the same action from inside their own path (active is GameCameraKey), so only a
    /// send from outside it is the button. From Follow the button lets the camera go, which asks for nothing.
    /// </summary>
    public static bool IsCameraButtonFollowAsk(SnapSource active, bool isCameraFree) =>
        isCameraFree && active != SnapSource.GameCameraKey;

    /// <summary>
    /// Whether a center key press is the player asking for Follow. The game only acts on it while keys reach the
    /// camera (nothing being typed, no panel open, no key being rebound, not in photo mode) and the camera is free;
    /// from Follow the key only ends a peek.
    /// </summary>
    public static bool IsCenterKeyFollowAsk(bool isKeyDown, bool areKeysFree, bool isCameraFree) =>
        isKeyDown && areKeysFree && isCameraFree;

    public static bool ShouldKeepView(SnapRequest request, SnapToggles toggles) => Judge(request, toggles) == SnapVerdict.Kept;

    /// <summary>
    /// What to do with one camera request. With OnlyCenterKeyFollows on, a free camera only goes back to the
    /// character when the player asks (center key or camera button), when the game needs the camera (cutscenes,
    /// photo mode, the world starting, the tutorial door, map travel), or when an unmarked request asks for another
    /// floor or map. Everything else is dropped, forced or not, on any floor: stairs, menus, build mode and the rotate
    /// keys. With it off, each snap follows its own setting (<see cref="IsHeldBackBySetting"/>).
    /// </summary>
    public static SnapVerdict Judge(SnapRequest request, SnapToggles toggles)
    {
        if (!request.IsFollow || !request.IsCameraFree) return SnapVerdict.NotAJump;

        if (!toggles.OnlyCenterKeyFollows) return IsHeldBackBySetting(request, toggles) ? SnapVerdict.Kept : SnapVerdict.Allowed;

        if (request.IsPlayerAsking) return SnapVerdict.PlayerAsked;

        return request.Source switch
        {
            SnapSource.Cutscene or SnapSource.PhotoMode or SnapSource.WorldStart or SnapSource.TutorialDoor or SnapSource.MapTravel => SnapVerdict.GameNeedsCamera,
            SnapSource.None => request.IsViewedFloor ? SnapVerdict.Kept : SnapVerdict.AnotherFloor,
            _ => SnapVerdict.Kept,
        };
    }

    /// <summary>
    /// Whether a snap is held back by its own setting. Only a follow request made inside one of the
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
    private static bool IsHeldBackBySetting(SnapRequest request, SnapToggles toggles)
    {
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
