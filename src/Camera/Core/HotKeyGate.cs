namespace Camera.Core;

/// <summary>
/// Whether the game's key manager last let keys through. It may update before or after the camera within a frame,
/// so a reading that held keys back counts for one frame more; once it stops updating, keys are not held back on its
/// account. A free reading needs no frame number, so the frame is only asked for while keys are held back.
/// </summary>
public sealed class HotKeyGate
{
    private bool _isFree = true;
    private int _blockedFrame;

    /// <summary>Whether <see cref="IsFree"/> needs this frame's number to answer.</summary>
    public bool NeedsFrame => !_isFree;

    public void NoteFree() => _isFree = true;

    public void NoteBlocked(int frame)
    {
        _isFree = false;
        _blockedFrame = frame;
    }

    public bool IsFree(int frame) => _isFree || unchecked(frame - _blockedFrame) > 1;
}
