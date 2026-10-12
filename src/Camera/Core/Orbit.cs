using System;

namespace Camera.Core;

/// <summary>Where the player's input takes the view, kept inside the limits.</summary>
public static class Orbit
{
    /// <summary>
    /// The view after this frame's push. Yaw is left unwrapped here (see <see cref="Rewrap"/>) so a turn past 180
    /// degrees keeps going the way the player is turning it.
    /// </summary>
    public static View Aim(View target, ViewPush push, ViewLimits limits, float basePitch)
    {
        var yaw = target.Yaw + Finite(push.Yaw);
        var (pitchLow, pitchHigh) = PitchRange(limits, basePitch);

        if (limits.IsYawLimited) yaw = Math.Clamp(yaw, -limits.YawLimit, limits.YawLimit);

        return new View(
            yaw,
            Math.Clamp(target.Pitch + Finite(push.Pitch), pitchLow, pitchHigh),
            Math.Clamp(target.Height + Finite(push.Height), limits.HeightMin, limits.HeightMax));
    }

    /// <summary>
    /// The pitch changes that keep the camera's real angle inside the limits. The game's own angle (a change of 0)
    /// is always allowed, even when the limits leave it out, so the default view can always be reached. When the
    /// game's angle could not be read, pitch stays at the game's.
    /// </summary>
    public static (float Low, float High) PitchRange(ViewLimits limits, float basePitch)
    {
        if (!float.IsFinite(basePitch) || basePitch <= 0f || basePitch >= 90f) return (0f, 0f);

        return (Math.Min(limits.PitchMin, basePitch) - basePitch, Math.Max(limits.PitchMax, basePitch) - basePitch);
    }

    /// <summary>
    /// Keeps yaw inside (-180, 180] by shifting the current and target views together, so the numbers never grow
    /// and the smoothing between them still turns the short way.
    /// </summary>
    public static (View Current, View Target) Rewrap(View current, View target)
    {
        var shift = target.Yaw > 180f ? -360f : target.Yaw <= -180f ? 360f : 0f;

        if (shift == 0f) return (current, target);

        return (current with { Yaw = current.Yaw + shift }, target with { Yaw = target.Yaw + shift });
    }

    private static float Finite(float value) => float.IsFinite(value) ? value : 0f;
}
