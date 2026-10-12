using System;
using HarmonyLib;

namespace Camera.Adapters;

/// <summary>Hooking and failure logging shared by every patch class. A hook that cannot be placed costs its feature, never the mod.</summary>
internal static class PatchKit
{
    private const int MaxLoggedFailures = 3;

    private static int _failures;

    public static Action<string> Log { get; set; } = _ => { };

    /// <returns>Whether the hook was placed.</returns>
    public static bool Patch(Harmony harmony, Type hooks, Type type, string method, string? prefix = null, string? postfix = null, string done = "", string consequence = "")
    {
        try
        {
            harmony.Patch(
                AccessTools.Method(type, method),
                prefix: prefix == null ? null : new HarmonyMethod(hooks, prefix),
                postfix: postfix == null ? null : new HarmonyMethod(hooks, postfix));
            Log(done);

            return true;
        }
        catch (Exception ex)
        {
            Log($"could not hook {type.Name}.{method}, {consequence}: {ex.Message}");

            return false;
        }
    }

    /// <summary>Runs from per-frame hooks, so only the first few failures are written.</summary>
    public static void Fail(string doing, Exception ex)
    {
        _failures++;

        if (_failures <= MaxLoggedFailures) Log($"failed {doing} (logged {_failures} of {MaxLoggedFailures}): {ex}");
    }
}
