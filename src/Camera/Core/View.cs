namespace Camera.Core;

/// <summary>
/// The player's change to the game's own camera. Yaw turns it around the point it looks at and Pitch swings it over
/// the top (positive is steeper, more top-down), both in degrees; Height raises it in metres. All zero is the game's
/// own view, untouched.
/// </summary>
public readonly record struct View(float Yaw, float Pitch, float Height)
{
    public static View Default => new(0f, 0f, 0f);

    public bool IsDefault => Yaw == 0f && Pitch == 0f && Height == 0f;
}

/// <summary>How far the player pushed the view this frame, in the units of <see cref="View"/>.</summary>
public readonly record struct ViewPush(float Yaw, float Pitch, float Height)
{
    public static ViewPush None => new(0f, 0f, 0f);

    public static ViewPush operator +(ViewPush a, ViewPush b) => new(a.Yaw + b.Yaw, a.Pitch + b.Pitch, a.Height + b.Height);
}
