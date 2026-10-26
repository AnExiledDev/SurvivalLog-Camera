using System;
using System.Collections.Generic;

namespace Camera.Core;

/// <summary>
/// The game's key for each of its actions, read once and kept. Asking the game costs a trip into its key settings,
/// and the rotate keys are asked about every frame a key is held. The game only changes a key as it saves its
/// settings, so the adapter calls <see cref="Forget"/> then, at each world load and at each change of this mod's
/// settings. Until those saves are watched (<see cref="IsWatched"/>), every ask reads the game again.
/// </summary>
public sealed class KeyBinds
{
    private readonly Dictionary<int, int> _known = new();
    private readonly Func<int, int> _read;
    private volatile int _version;
    private int _knownVersion;

    /// <param name="read">Reads the game's key for an action, both as plain numbers.</param>
    public KeyBinds(Func<int, int> read) => _read = read;

    /// <summary>Whether the game's settings saves are watched, so a kept key can be trusted.</summary>
    public bool IsWatched { get; set; }

    public int KeyFor(int action)
    {
        if (!IsWatched) return _read(action);

        if (_knownVersion != _version)
        {
            _known.Clear();
            _knownVersion = _version;
        }

        if (_known.TryGetValue(action, out var key)) return key;

        key = _read(action);
        _known[action] = key;

        return key;
    }

    /// <summary>Safe from any thread: the keys kept are dropped on the next ask.</summary>
    public void Forget() => _version++;
}
