namespace Camera.Core;

/// <summary>
/// Whenever the game asks for follow mode it hands the follow camera the free camera's zoom. That is right coming
/// back from the free camera, which started out with the follow camera's zoom. It is wrong when the camera was
/// following all along: the free zoom is then whatever it was the last time the player looked at another floor, so
/// the zoom would pop back to it every time the character takes stairs or a ladder.
/// </summary>
public static class FollowZoom
{
    /// <summary>
    /// Whether the follow camera's zoom should be copied into the free camera's just before the game hands it back,
    /// so the zoom handed over is the one already on screen.
    /// </summary>
    public static bool ShouldCarry(bool isFollowRequest, bool isFollowing) => isFollowRequest && isFollowing;
}
