using Camera.Core;

namespace Camera.Adapters;

/// <summary>Everything the config decides, fixed for the session.</summary>
internal sealed record CameraSettings(
    SnapToggles Snap,
    bool IsRotateOn,
    DragButton MouseButton,
    float MouseSensitivity,
    bool IsMouseXInverted,
    bool IsMouseYInverted,
    bool IsRotateAllowedInBuildMode,
    ViewLimits Limits,
    KeySpeeds Speeds,
    float SmoothingRate,
    float ZoomExtraIn,
    float ZoomExtraOut,
    float ScrollSpeed,
    KeyChoice RotateLeftKey,
    KeyChoice RotateRightKey,
    KeyChoice TiltUpKey,
    KeyChoice TiltDownKey,
    KeyChoice RaiseKey,
    KeyChoice LowerKey,
    KeyChoice ResetKey,
    bool DoesCenterKeyResetView);
