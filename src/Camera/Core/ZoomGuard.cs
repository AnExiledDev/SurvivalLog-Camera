using System;

namespace Camera.Core;

/// <summary>
/// Widens the game's zoom range (its field-of-view limits) by a few degrees each way. The game's own limits are
/// remembered and re-read whenever the game writes new ones, so the extra is always added to the game's range and
/// never stacked on top of a range this mod already widened.
/// </summary>
public sealed class ZoomGuard
{
    /// <summary>Narrower than this the picture turns into a telescope and floors behind the camera clip.</summary>
    public const float NarrowestFov = 5f;

    /// <summary>Wider than this the edges of the screen stretch badly.</summary>
    public const float WidestFov = 110f;

    public const float MostExtra = 60f;

    private readonly float _extraIn;
    private readonly float _extraOut;
    private (float Min, float Max)? _gameRange;
    private (float Min, float Max)? _written;

    public ZoomGuard(float extraIn, float extraOut)
    {
        _extraIn = Extra(extraIn);
        _extraOut = Extra(extraOut);
    }

    public bool WidensAnything => _extraIn > 0f || _extraOut > 0f;

    /// <returns>The range to write in place of the current one, or null when the current one should stay.</returns>
    public (float Min, float Max)? Next(float currentMin, float currentMax)
    {
        if (!IsSane(currentMin, currentMax)) return null;

        if (_written is not { } written || written.Min != currentMin || written.Max != currentMax) _gameRange = (currentMin, currentMax);

        var game = _gameRange ?? (currentMin, currentMax);
        var widened = (
            Min: Math.Min(game.Min, Math.Max(NarrowestFov, game.Min - _extraIn)),
            Max: Math.Max(game.Max, Math.Min(WidestFov, game.Max + _extraOut)));

        _written = widened;

        return widened.Min == currentMin && widened.Max == currentMax ? null : widened;
    }

    /// <summary>A range the game could really mean; anything else is left alone rather than widened into nonsense.</summary>
    public static bool IsSane(float min, float max) =>
        float.IsFinite(min) && float.IsFinite(max) && min > 0f && max > min && max < 180f;

    private static float Extra(float degrees) => float.IsFinite(degrees) ? Math.Clamp(degrees, 0f, MostExtra) : 0f;
}
