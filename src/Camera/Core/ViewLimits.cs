using System;

namespace Camera.Core;

/// <summary>
/// How far the player may move the view. PitchMin and PitchMax are the camera's real downward angle in degrees
/// (90 looks straight down), not a change from the game's angle, so they mean the same thing whatever the game's
/// own angle is. A YawLimit of 0 lets the camera turn all the way round. ZoomOutMin and ZoomOutMax are in the units
/// of <see cref="View.ZoomOut"/>; both 0 leaves the scroll wheel to the game.
/// </summary>
public sealed record ViewLimits(float YawLimit, float PitchMin, float PitchMax, float HeightMin, float HeightMax, float ZoomOutMin = 0f, float ZoomOutMax = 0f)
{
    /// <summary>Straight down would flip the camera over the top; one degree short keeps its up direction defined.</summary>
    public const float SteepestPitch = 89f;

    /// <summary>Flatter than this the camera looks along the floor and through walls the game never hides.</summary>
    public const float FlattestPitch = 5f;

    public const float LowestHeight = -50f;
    public const float HighestHeight = 100f;

    /// <summary>Closer than this, as a share of the game's distance, the camera ends up inside the character.</summary>
    public const float NearestDistance = 0.2f;

    /// <summary>Farther than this, as a multiple of the game's distance, the character is a speck.</summary>
    public const float FarthestDistance = 10f;

    public bool IsYawLimited => YawLimit > 0f;

    /// <summary>Whether the scroll wheel moves the camera at all; when it cannot, the game's own zoom is left alone.</summary>
    public bool CanZoom => ZoomOutMin < 0f || ZoomOutMax > 0f;

    /// <summary>
    /// Hand-edited config is clamped rather than refused, so a typo costs a limit, never the mod. Closest and farthest
    /// are multiples of the game's distance, and the game's own distance (1) always stays inside them.
    /// </summary>
    public static ViewLimits From(float yawLimit, float pitchMin, float pitchMax, float heightMin, float heightMax, float closest = 1f, float farthest = 1f)
    {
        var yaw = Finite(yawLimit, 0f);
        var flattest = Math.Clamp(Finite(pitchMin, 25f), FlattestPitch, SteepestPitch);
        var steepest = Math.Clamp(Finite(pitchMax, 85f), FlattestPitch, SteepestPitch);

        return new ViewLimits(
            yaw <= 0f || yaw >= 180f ? 0f : yaw,
            Math.Min(flattest, steepest),
            Math.Max(flattest, steepest),
            Math.Clamp(Finite(heightMin, 0f), LowestHeight, 0f),
            Math.Clamp(Finite(heightMax, 0f), 0f, HighestHeight),
            MathF.Log(Math.Clamp(Finite(closest, 1f), NearestDistance, 1f)),
            MathF.Log(Math.Clamp(Finite(farthest, 1f), 1f, FarthestDistance)));
    }

    private static float Finite(float value, float fallback) => float.IsFinite(value) ? value : fallback;
}
