using System;

namespace Camera.Core;

/// <summary>Eases the view toward where the player sent it, at the same speed whatever the frame rate.</summary>
public static class Smoothing
{
    public const float CloseDegrees = 0.01f;
    public const float CloseMetres = 0.001f;

    /// <summary>The share of the remaining distance to cover this frame. A rate of 0 or less is instant.</summary>
    public static float Blend(float rate, float deltaTime)
    {
        if (!float.IsFinite(rate) || rate <= 0f) return 1f;

        if (!float.IsFinite(deltaTime) || deltaTime <= 0f) return 0f;

        return 1f - MathF.Exp(-rate * deltaTime);
    }

    /// <summary>One frame's step toward the target, landing exactly on it once close, so "back to default" really is the default.</summary>
    public static View Toward(View current, View target, float rate, float deltaTime)
    {
        var blend = Blend(rate, deltaTime);
        var next = new View(
            Lerp(current.Yaw, target.Yaw, blend),
            Lerp(current.Pitch, target.Pitch, blend),
            Lerp(current.Height, target.Height, blend));

        return IsClose(next, target) ? target : next;
    }

    private static float Lerp(float from, float to, float blend) => from + (to - from) * blend;

    private static bool IsClose(View a, View b) =>
        MathF.Abs(a.Yaw - b.Yaw) < CloseDegrees &&
        MathF.Abs(a.Pitch - b.Pitch) < CloseDegrees &&
        MathF.Abs(a.Height - b.Height) < CloseMetres;
}
