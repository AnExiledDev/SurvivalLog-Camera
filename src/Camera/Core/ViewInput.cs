using System;

namespace Camera.Core;

/// <summary>Which of the mod's camera keys are held this frame.</summary>
public readonly record struct HeldKeys(bool RotateLeft, bool RotateRight, bool TiltUp, bool TiltDown, bool Raise, bool Lower);

/// <summary>How fast a held key moves the view: degrees a second for turning and tilting, metres a second for height.</summary>
public readonly record struct KeySpeeds(float RotateDegreesPerSecond, float TiltDegreesPerSecond, float MetresPerSecond);

/// <summary>Turns keys and mouse drags into a push on the view.</summary>
public static class ViewInput
{
    /// <summary>
    /// Left turns the view the way the game's own rotate-left key does (a negative yaw). Tilt up swings the camera up
    /// over the top, so the view gets steeper. Opposite keys held together cancel.
    /// </summary>
    public static ViewPush FromKeys(HeldKeys held, KeySpeeds speeds, float deltaTime) =>
        new(
            Axis(held.RotateRight, held.RotateLeft) * speeds.RotateDegreesPerSecond * deltaTime,
            Axis(held.TiltUp, held.TiltDown) * speeds.TiltDegreesPerSecond * deltaTime,
            Axis(held.Raise, held.Lower) * speeds.MetresPerSecond * deltaTime);

    /// <summary>
    /// The same way round as the game's photo mode: dragging right turns right, dragging up looks up (flatter).
    /// Sensitivity is degrees per pixel dragged.
    /// </summary>
    public static ViewPush FromMouse(float deltaX, float deltaY, float sensitivity, bool isXInverted, bool isYInverted) =>
        new(
            (isXInverted ? -deltaX : deltaX) * sensitivity,
            (isYInverted ? deltaY : -deltaY) * sensitivity,
            0f);

    /// <summary>Each notch moves the camera a tenth of its distance in or out, so a step feels the same near and far.</summary>
    public static readonly float ZoomOutPerNotch = MathF.Log(1.1f);

    /// <summary>The game counts at most this many notches in one scroll step.</summary>
    public const float MostNotches = 5f;

    /// <summary>Scrolling up (forward) brings the camera closer, as the game's own zoom does.</summary>
    public static ViewPush FromScroll(float notches, float scrollSpeed) =>
        float.IsFinite(notches) ? new(0f, 0f, 0f, -Math.Clamp(notches, -MostNotches, MostNotches) * ZoomOutPerNotch * scrollSpeed) : ViewPush.None;

    private static float Axis(bool isPositiveHeld, bool isNegativeHeld) => (isPositiveHeld ? 1f : 0f) - (isNegativeHeld ? 1f : 0f);
}
