using System;
using System.Collections.Generic;
using System.Linq;
using Camera.Core;
using GameCore.HotUpdate;
using GameCore.HotUpdate.Battle.Show;
using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace Camera.Adapters;

/// <summary>
/// The mod's camera keys: which keys they are, whether they are held, and whether the game is somewhere keys
/// should reach the camera at all (no text box being typed in, no menu or panel open, no key being rebound).
/// </summary>
internal static class CameraKeys
{
    /// <summary>A key one setting uses: a fixed one from the config, or whatever the game's key settings give an action.</summary>
    private sealed record ModKey(string Setting, KeyCode Fixed, HotKeyAction? GameAction)
    {
        public KeyCode Current => GameAction is { } action ? HotKeyManager.Bind(action) : Fixed;

        public string Describe() => GameAction is { } action ? $"{Current} (the game's {action} key)" : Fixed.ToString();
    }

    /// <summary>
    /// Keys something answers without going through the game's key settings: the game's hardcoded camera pan and
    /// photo keys, menu keys, mouse clicks, and the keys the other Survival Log mods use.
    /// </summary>
    private static readonly (KeyCode Key, string By)[] AlwaysTaken =
    {
        (KeyCode.W, "the game's camera pan keys"),
        (KeyCode.A, "the game's camera pan keys"),
        (KeyCode.S, "the game's camera pan keys"),
        (KeyCode.D, "the game's camera pan keys"),
        (KeyCode.P, "the game's photo mode key"),
        (KeyCode.Space, "Time Control's pause key"),
        (KeyCode.G, "Time Control's center camera key"),
        (KeyCode.Escape, "closing menus"),
        (KeyCode.Return, "confirming menus"),
        (KeyCode.KeypadEnter, "confirming menus"),
        (KeyCode.F6, "another Survival Log mod"),
        (KeyCode.F7, "another Survival Log mod"),
        (KeyCode.F8, "another Survival Log mod"),
        (KeyCode.F9, "another Survival Log mod"),
        (KeyCode.F10, "another Survival Log mod"),
        (KeyCode.F11, "another Survival Log mod"),
        (KeyCode.F12, "another Survival Log mod"),
        (KeyCode.Mouse0, "clicking in the game"),
        (KeyCode.Mouse1, "clicking in the game"),
        (KeyCode.LeftShift, "a modifier key other mods read"),
        (KeyCode.RightShift, "a modifier key other mods read"),
        (KeyCode.LeftControl, "a modifier key other mods read"),
        (KeyCode.RightControl, "a modifier key other mods read"),
    };

    private static CameraSettings? _settings;
    private static Action<string> _log = _ => { };
    private static ModKey? _rotateLeft;
    private static ModKey? _rotateRight;
    private static ModKey? _tiltUp;
    private static ModKey? _tiltDown;
    private static ModKey? _raise;
    private static ModKey? _lower;
    private static ModKey? _reset;
    private static bool _isSetUp;
    private static bool _areHotKeysFree = true;
    private static int _hotKeyFrame = int.MinValue;
    private static IntPtr _listeningActionField;
    private static IntPtr _listeningActionClass;

    public static void Apply(Harmony harmony, CameraSettings settings, Action<string> log)
    {
        _settings = settings;
        _log = log;
        FindListeningActionField();
        PatchKit.Patch(harmony, typeof(CameraKeys), typeof(HotKeyManager), nameof(HotKeyManager.Update), postfix: nameof(NoteHotKeyState),
            done: "camera keys wait while the game has input switched off or a key is being rebound",
            consequence: "the camera keys may also fire while the game has input switched off or a key is being rebound");
    }

    /// <summary>
    /// Done on the first frame the game handles keys or moves the camera rather than at plugin load: the game's key
    /// settings are read through its SettingManager, and reading them at load would build it before the game does.
    /// </summary>
    public static void SetUpOnce()
    {
        if (_isSetUp || _settings is not { } settings) return;

        _isSetUp = true;

        try
        {
            var rotateOn = settings.IsRotateOn;
            var keys = new[]
            {
                rotateOn ? Resolve("RotateLeftKey", settings.RotateLeftKey, KeyCode.None, HotKeyAction.CameraRotateLeft) : null,
                rotateOn ? Resolve("RotateRightKey", settings.RotateRightKey, KeyCode.None, HotKeyAction.CameraRotateRight) : null,
                rotateOn ? Resolve("TiltUpKey", settings.TiltUpKey, KeyCode.Insert, null) : null,
                rotateOn ? Resolve("TiltDownKey", settings.TiltDownKey, KeyCode.Delete, null) : null,
                Resolve("RaiseKey", settings.RaiseKey, KeyCode.PageUp, null),
                Resolve("LowerKey", settings.LowerKey, KeyCode.PageDown, null),
                Resolve("ResetKey", settings.ResetKey, KeyCode.Home, null),
            }.OfType<ModKey>().ToList();
            var usable = KeepUsable(keys, Taken(settings));

            _rotateLeft = usable.GetValueOrDefault("RotateLeftKey");
            _rotateRight = usable.GetValueOrDefault("RotateRightKey");
            _tiltUp = usable.GetValueOrDefault("TiltUpKey");
            _tiltDown = usable.GetValueOrDefault("TiltDownKey");
            _raise = usable.GetValueOrDefault("RaiseKey");
            _lower = usable.GetValueOrDefault("LowerKey");
            _reset = usable.GetValueOrDefault("ResetKey");
            _log("camera keys: " + (usable.Count == 0 ? "none" : string.Join(", ", usable.Values.Select(key => $"{key.Setting} {key.Describe()}"))));
        }
        catch (Exception ex)
        {
            _log("could not set up the camera keys, so they are off (mouse turning still works): " + ex.Message);
        }
    }

    public static HeldKeys Held() =>
        new(IsHeld(_rotateLeft), IsHeld(_rotateRight), IsHeld(_tiltUp), IsHeld(_tiltDown), IsHeld(_raise), IsHeld(_lower));

    public static bool IsResetDown() => _reset != null && IsDown(_reset.Current);

    public static bool IsCenterDown() => IsDown(HotKeyManager.Bind(HotKeyAction.CameraLock));

    /// <summary>
    /// The game's own conditions for its camera rotate keys (ReadCameraYawInput: keyboard camera moves allowed, no key
    /// being rebound, no panel open), plus no text box being typed in and the game taking input at all.
    /// </summary>
    public static bool IsKeyboardFree(CameraManager camera) =>
        AreHotKeysFree() &&
        !camera.isKeyboardMoveBlocked &&
        !camera.isRebindListening &&
        !HotKeyManager.IsUGUIInputFocused() &&
        !ContainerTabHotKey.IsAnyPanelOpen();

    /// <summary>A drag can only start on the game world, so typing does not matter; an open panel or a rebind still does.</summary>
    public static bool IsMouseFree(CameraManager camera) =>
        AreHotKeysFree() &&
        !camera.isRebindListening &&
        !ContainerTabHotKey.IsAnyPanelOpen();

    /// <summary>
    /// The game's key manager may update before or after the camera within a frame, so its last reading counts for
    /// one frame more. When it has stopped updating, keys are not held back on its account.
    /// </summary>
    private static bool AreHotKeysFree() => Time.frameCount - _hotKeyFrame > 1 || _areHotKeysFree;

    private static void NoteHotKeyState(HotKeyManager __instance)
    {
        SetUpOnce();

        try
        {
            _areHotKeysFree = !__instance._isInputDisabled && !IsRebindingAKey(__instance);
            _hotKeyFrame = Time.frameCount;
        }
        catch (Exception ex)
        {
            _areHotKeysFree = true;
            PatchKit.Fail("reading whether the game takes keys", ex);
        }
    }

    private static ModKey? Resolve(string setting, KeyChoice choice, KeyCode fallback, HotKeyAction? gameAction)
    {
        switch (choice.Kind)
        {
            case KeyChoiceKind.None:
                return null;
            case KeyChoiceKind.Game:
                return new ModKey(setting, KeyCode.None, gameAction);
        }

        if (Enum.TryParse<KeyCode>(choice.Name, ignoreCase: true, out var key) && Enum.IsDefined(key) && key != KeyCode.None) return new ModKey(setting, key, null);

        var useInstead = gameAction is { } action ? new ModKey(setting, KeyCode.None, action) : new ModKey(setting, fallback, null);

        _log($"{setting} '{choice.Name}' is not a Unity key name, using {useInstead.Describe()}");

        return useInstead;
    }

    private static Dictionary<string, ModKey> KeepUsable(List<ModKey> keys, List<TakenKey> taken)
    {
        var wanted = keys.Select(key => new WantedKey(key.Setting, (int)key.Current, key.GameAction is { } action ? (int)action : null)).ToList();
        var usable = new Dictionary<string, ModKey>();

        foreach (var verdict in KeyClash.Check(wanted, taken))
        {
            var key = keys.First(candidate => candidate.Setting == verdict.Setting);

            if (verdict.IsUsable)
            {
                usable[verdict.Setting] = key;
            }
            else
            {
                _log(verdict.ClashesWith == null
                    ? $"{verdict.Setting} has no key (the game gives {key.GameAction} none), so it is off: set a key name in the config"
                    : $"{verdict.Setting} {key.Describe()} is already used by {verdict.ClashesWith}, so it is off: pick another key in the config");
            }
        }

        return usable;
    }

    /// <summary>Every key already answered: the fixed list, the drag button, and each of the game's actions as its key settings bind them.</summary>
    private static List<TakenKey> Taken(CameraSettings settings)
    {
        var taken = AlwaysTaken.Select(entry => new TakenKey((int)entry.Key, entry.By)).ToList();

        if (settings.IsRotateOn && settings.MouseButton == DragButton.Middle) taken.Add(new TakenKey((int)KeyCode.Mouse2, "the Rotate.MouseButton drag"));

        foreach (HotKeyAction action in Enum.GetValues(typeof(HotKeyAction)))
        {
            try
            {
                var key = HotKeyManager.Bind(action);

                if (key != KeyCode.None) taken.Add(new TakenKey((int)key, $"the game's {action} key", (int)action));
            }
            catch (Exception)
            {
                // An action the game has no binding for answers no key.
            }
        }

        return taken;
    }

    private static bool IsHeld(ModKey? key)
    {
        if (key == null) return false;

        var current = key.Current;

        return current != KeyCode.None && Input.GetKey(current);
    }

    private static bool IsDown(KeyCode key) => key != KeyCode.None && Input.GetKeyDown(key);

    /// <summary>
    /// The game's getter for HotKeyManager._listeningAction boxes the nullable, and il2cpp boxes an empty nullable to
    /// null, so the getter throws whenever no key is being rebound. The field and the nullable's class are looked up
    /// once here so <see cref="IsRebindingAKey"/> can box it itself and check for null.
    /// </summary>
    private static void FindListeningActionField()
    {
        try
        {
            _listeningActionField = IL2CPP.GetIl2CppField(Il2CppClassPointerStore<HotKeyManager>.NativeClassPtr, "_listeningAction");
            _listeningActionClass = Il2CppClassPointerStore<Il2CppSystem.Nullable<HotKeyAction>>.NativeClassPtr;
        }
        catch (Exception ex)
        {
            _listeningActionField = IntPtr.Zero;
            _log("could not look up the key rebinding state: " + ex.Message);
        }

        if (_listeningActionField != IntPtr.Zero && _listeningActionClass != IntPtr.Zero) return;

        _listeningActionField = IntPtr.Zero;
        _log("the key rebinding state was not found, so only the camera's own rebinding flag holds the camera keys back while a key is being rebound");
    }

    /// <summary>Boxing an empty nullable gives null, so a non-null box means a key is being rebound.</summary>
    private static bool IsRebindingAKey(HotKeyManager keys)
    {
        if (_listeningActionField == IntPtr.Zero) return false;

        var field = IL2CPP.Il2CppObjectBaseToPtrNotNull(keys) + (int)IL2CPP.il2cpp_field_get_offset(_listeningActionField);

        return IL2CPP.il2cpp_value_box(_listeningActionClass, field) != IntPtr.Zero;
    }
}
