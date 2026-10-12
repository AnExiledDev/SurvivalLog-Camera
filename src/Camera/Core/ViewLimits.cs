using System;

namespace Camera.Core;

/// <summary>
/// How far the player may move the view. PitchMin and PitchMax are the camera's real downward angle in degrees
/// (90 looks straight down), not a change from the game's angle, so they mean the same thing whatever the game's
/// own angle is. A YawLimit of 0 lets the camera turn all the way round.
/// </summary>
public sealed record ViewLimits(float YawLimit, float PitchMin, float PitchMax, float HeightMin, float HeightMax)
{
    /// <summary>Straight down would flip the camera over the top; one degree short keeps its up direction defined.</summary>
    public const float SteepestPitch = 89f;

    /// <summary>Flatter than this the camera looks along the floor and through walls the game never hides.</summary>
    public const float FlattestPitch = 5f;

    public const float LowestHeight = -50f;
    public const float HighestHeight = 100f;

    public bool IsYawLimited => YawLimit > 0f;

    /// <summary>Hand-edited config is clamped rather than refused, so a typo costs a limit, never the mod.</summary>
    public static ViewLimits From(float yawLimit, float pitchMin, float pitchMax, float heightMin, float heightMax)
    {
        var yaw = Finite(yawLimit, 0f);
        var flattest = Math.Clamp(Finite(pitchMin, 25f), FlattestPitch, SteepestPitch);
        var steepest = Math.Clamp(Finite(pitchMax, 85f), FlattestPitch, SteepestPitch);

        return new ViewLimits(
            yaw <= 0f || yaw >= 180f ? 0f : yaw,
            Math.Min(flattest, steepest),
            Math.Max(flattest, steepest),
            Math.Clamp(Finite(heightMin, 0f), LowestHeight, 0f),
            Math.Clamp(Finite(heightMax, 0f), 0f, HighestHeight));
    }

    private static float Finite(float value, float fallback) => float.IsFinite(value) ? value : fallback;
}
