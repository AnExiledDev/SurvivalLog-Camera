using System;
using System.IO;
using System.Threading;
using BepInEx.Logging;
using Camera.Core;

namespace Camera.Adapters;

/// <summary>
/// Writes to the BepInEx log and to an append-only file that survives restarts
/// (LogOutput.log and Player.log are both overwritten on the next launch).
/// The file is written by a worker thread, so the camera's hooks never wait on the disk; the worker never writes to
/// the BepInEx console. Never throws: it is called from inside Load and the camera hooks, where an escaping exception
/// would reach the game.
/// </summary>
internal sealed class CameraLog
{
    private const long TrimAboveBytes = 1024 * 1024;
    private const int KeepBytes = 200 * 1024;
    private const int QuitWaitMilliseconds = 2000;

    private readonly ManualLogSource _bepinex;
    private readonly string _path;
    private readonly object _gate = new();
    private readonly LogLines _lines = new();
    private readonly AutoResetEvent _wake = new(false);

    /// <summary>Starts the worker, which trims an oversized file before it writes anything.</summary>
    public CameraLog(ManualLogSource bepinex, string path)
    {
        _bepinex = bepinex;
        _path = path;

        try
        {
            new Thread(WriteLoop) { IsBackground = true, Name = "Camera log" }.Start();
            AppDomain.CurrentDomain.ProcessExit += WriteOnQuit;
        }
        catch (Exception)
        {
            // Without the worker the lines wait in memory; the camera itself is unaffected.
        }
    }

    public void Write(string message)
    {
        if (HookFailure.Is(message)) _bepinex.LogError(message);
        else _bepinex.LogInfo(message);

        WriteFileOnly(message);
    }

    /// <summary>For chatty lines (every snap the mod holds back) that would flood LogOutput.log.</summary>
    public void WriteFileOnly(string message)
    {
        try
        {
            _lines.Add(DateTime.UtcNow, message);
            _wake.Set();
        }
        catch (Exception)
        {
            // A line lost from the file copy costs nothing else.
        }
    }

    private void WriteLoop()
    {
        try
        {
            TrimIfLarge();

            while (true)
            {
                _wake.WaitOne();
                WriteWaiting();
            }
        }
        catch (Exception)
        {
            // The file copy stops; the BepInEx log still has every line but the chatty ones.
        }
    }

    /// <summary>The game is closing: the lines still waiting are written, giving up after a short wait for a write in progress.</summary>
    private void WriteOnQuit(object? sender, EventArgs e)
    {
        if (!Monitor.TryEnter(_gate, QuitWaitMilliseconds)) return;

        try
        {
            WriteWaitingLocked();
        }
        finally
        {
            Monitor.Exit(_gate);
        }
    }

    private void WriteWaiting()
    {
        lock (_gate) WriteWaitingLocked();
    }

    private void WriteWaitingLocked()
    {
        try
        {
            var text = _lines.Take();

            if (text != null) File.AppendAllText(_path, text);
        }
        catch (Exception)
        {
            // A locked or read-only file costs the file copy, never the plugin.
        }
    }

    /// <summary>Held-back snaps add up over a long session, so past 1 MB only the newest ~200 KB of whole lines is kept.</summary>
    private void TrimIfLarge()
    {
        lock (_gate)
        {
            try
            {
                var file = new FileInfo(_path);

                if (!file.Exists || file.Length <= TrimAboveBytes) return;

                var bytes = File.ReadAllBytes(_path);
                var cut = bytes.Length - KeepBytes;
                var nextLine = Array.IndexOf(bytes, (byte)'\n', cut) + 1;

                File.WriteAllBytes(_path, bytes[(nextLine > 0 ? nextLine : cut)..]);
            }
            catch (Exception)
            {
                // An oversized log is harmless; failing to trim it must not stop the file being written.
            }
        }
    }
}
