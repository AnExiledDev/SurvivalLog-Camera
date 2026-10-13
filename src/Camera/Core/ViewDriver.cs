namespace Camera.Core;

/// <summary>Whether the game is somewhere the free camera should stand back.</summary>
public enum Suspension
{
    /// <summary>The player's view applies and input moves it.</summary>
    None,

    /// <summary>Build mode: the view eases back to the game's and returns afterwards; input is ignored.</summary>
    Soft,

    /// <summary>Expeditions, photo mode, cutscene cameras and the death camera: the game's view at once, no easing.</summary>
    Hard,
}

/// <summary>What the camera manager is doing this frame, as far as the free camera cares.</summary>
public readonly record struct CameraFlags(bool IsExploring, bool IsPhotoMode, bool IsSubCameraActive, bool IsDeadZooming, bool IsBuilding);

public static class Suspensions
{
    /// <summary>The turned view stays in build mode unless the player turns that off.</summary>
    public const bool AllowInBuildModeByDefault = true;

    public static Suspension Of(CameraFlags flags, bool allowInBuildMode)
    {
        if (flags.IsExploring || flags.IsPhotoMode || flags.IsSubCameraActive || flags.IsDeadZooming) return Suspension.Hard;

        return flags.IsBuilding && !allowInBuildMode ? Suspension.Soft : Suspension.None;
    }
}

/// <summary>
/// The view the player asked for (Target) and the one on screen (Current), stepped once a frame. The target
/// survives a suspension, so the view comes back once the game is done with the camera.
/// </summary>
public sealed class ViewDriver
{
    private readonly ViewLimits _limits;
    private readonly float _smoothingRate;

    public ViewDriver(ViewLimits limits, float smoothingRate)
    {
        _limits = limits;
        _smoothingRate = smoothingRate;
    }

    public View Target { get; private set; } = View.Default;

    public View Current { get; private set; } = View.Default;

    /// <summary>A new world or chapter starts from the game's view.</summary>
    public void Forget()
    {
        Target = View.Default;
        Current = View.Default;
    }

    public View Step(ViewPush push, bool isResetPressed, Suspension suspension, float basePitch, float deltaTime)
    {
        if (suspension == Suspension.Hard)
        {
            Current = View.Default;

            return Current;
        }

        if (suspension == Suspension.None)
        {
            Target = isResetPressed ? View.Default : Orbit.Aim(Target, push, _limits, basePitch);
            (Current, Target) = Orbit.Rewrap(Current, Target);
        }

        Current = Smoothing.Toward(Current, suspension == Suspension.None ? Target : View.Default, _smoothingRate, deltaTime);

        return Current;
    }
}
