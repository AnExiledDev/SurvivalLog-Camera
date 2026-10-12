using System;

namespace Camera.Core;

public enum KeyChoiceKind
{
    /// <summary>The setting is off.</summary>
    None,

    /// <summary>Use whatever key the game's own key settings give the matching action.</summary>
    Game,

    /// <summary>A key named in the config, still to be checked against Unity's key names.</summary>
    Named,
}

/// <summary>What a key setting in the config asks for.</summary>
public readonly record struct KeyChoice(KeyChoiceKind Kind, string Name)
{
    public static KeyChoice Off => new(KeyChoiceKind.None, "");

    public static KeyChoice Game => new(KeyChoiceKind.Game, "");

    /// <summary>"Game" only means the game's key where the game has a matching action; elsewhere it is just a name that will not parse.</summary>
    public static KeyChoice Parse(string? text, bool canUseGameKey)
    {
        var trimmed = (text ?? "").Trim();

        if (trimmed.Length == 0 || trimmed.Equals("None", StringComparison.OrdinalIgnoreCase)) return Off;

        if (canUseGameKey && trimmed.Equals("Game", StringComparison.OrdinalIgnoreCase)) return Game;

        return new KeyChoice(KeyChoiceKind.Named, trimmed);
    }
}

/// <summary>The mouse button that turns the camera while dragged.</summary>
public enum DragButton
{
    None,
    Right,
    Middle,
}

public static class DragButtons
{
    /// <returns>The button named, or null when the text names none of them.</returns>
    public static DragButton? Parse(string? text)
    {
        var trimmed = (text ?? "").Trim();

        if (trimmed.Length == 0 || trimmed.Equals("None", StringComparison.OrdinalIgnoreCase)) return DragButton.None;

        if (trimmed.Equals("Middle", StringComparison.OrdinalIgnoreCase)) return DragButton.Middle;

        if (trimmed.Equals("Right", StringComparison.OrdinalIgnoreCase)) return DragButton.Right;

        return null;
    }
}
