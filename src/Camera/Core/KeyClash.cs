using System.Collections.Generic;

namespace Camera.Core;

/// <summary>A key one of the mod's settings wants. OwnAction is the game action whose key it borrows, if any.</summary>
public readonly record struct WantedKey(string Setting, int Key, int? OwnAction = null);

/// <summary>A key something else already answers: one of the game's actions, a key it reads directly, or another mod.</summary>
public readonly record struct TakenKey(int Key, string By, int? Action = null);

public readonly record struct KeyVerdict(string Setting, int Key, string? ClashesWith)
{
    public bool IsUsable => Key != 0 && ClashesWith == null;
}

/// <summary>
/// Decides which of the mod's keys can be used. A key something else answers would fire both, so that setting is
/// turned off rather than the other use broken; the first of two settings wanting one key keeps it.
/// </summary>
public static class KeyClash
{
    /// <summary>Key 0 is Unity's "no key": a setting that is off clashes with nothing.</summary>
    public static IReadOnlyList<KeyVerdict> Check(IReadOnlyList<WantedKey> wanted, IReadOnlyList<TakenKey> taken)
    {
        var verdicts = new List<KeyVerdict>();
        var kept = new List<WantedKey>();

        foreach (var want in wanted)
        {
            var clash = want.Key == 0 ? null : ClashFor(want, kept, taken);

            verdicts.Add(new KeyVerdict(want.Setting, want.Key, clash));

            if (want.Key != 0 && clash == null) kept.Add(want);
        }

        return verdicts;
    }

    private static string? ClashFor(WantedKey want, List<WantedKey> kept, IReadOnlyList<TakenKey> taken)
    {
        foreach (var earlier in kept)
        {
            if (earlier.Key == want.Key) return earlier.Setting;
        }

        foreach (var other in taken)
        {
            if (other.Key != want.Key) continue;

            if (want.OwnAction is { } own && other.Action == own) continue;

            return other.By;
        }

        return null;
    }
}
