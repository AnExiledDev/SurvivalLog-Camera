using System;
using System.Collections.Generic;

namespace Camera.Core;

/// <summary>The key names Time Control's [Keys] settings hold, as written in its config file.</summary>
public readonly record struct TimeControlKeyNames(string PauseToggleKey, string CenterCameraKey)
{
    /// <summary>Time Control's own defaults, used for any setting its file does not have.</summary>
    public static TimeControlKeyNames Defaults => new("Space", "G");
}

public static class TimeControlKeys
{
    private const string KeysSection = "[Keys]";

    /// <summary>
    /// Reads the two key settings from the lines of Time Control's BepInEx config file. Only lines under its [Keys]
    /// section count; comment lines and the same names in other sections are skipped.
    /// </summary>
    public static TimeControlKeyNames Read(IEnumerable<string> lines)
    {
        var names = TimeControlKeyNames.Defaults;
        var isInKeys = false;

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.StartsWith('['))
            {
                isInKeys = line.Equals(KeysSection, StringComparison.OrdinalIgnoreCase);

                continue;
            }

            if (!isInKeys || line.StartsWith('#')) continue;

            var equals = line.IndexOf('=');

            if (equals < 0) continue;

            var name = line[..equals].Trim();
            var value = line[(equals + 1)..].Trim();

            if (name.Equals("PauseToggleKey", StringComparison.OrdinalIgnoreCase)) names = names with { PauseToggleKey = value };
            else if (name.Equals("CenterCameraKey", StringComparison.OrdinalIgnoreCase)) names = names with { CenterCameraKey = value };
        }

        return names;
    }
}
