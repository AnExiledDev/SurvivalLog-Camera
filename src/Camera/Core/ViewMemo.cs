namespace Camera.Core;

/// <summary>
/// The last two results worked out from a view. A turned view that stays put asks for the same turn every frame,
/// and the free camera asks for this frame's view and last frame's, so two kept results answer both without working
/// either out again. <see cref="Forget"/> drops them when what they were worked out from changes (a new world).
/// </summary>
public sealed class ViewMemo<T> where T : struct
{
    private View _newestView;
    private T _newest;
    private bool _hasNewest;
    private View _olderView;
    private T _older;
    private bool _hasOlder;

    public bool TryGet(View view, out T value)
    {
        if (_hasNewest && _newestView == view)
        {
            value = _newest;

            return true;
        }

        if (_hasOlder && _olderView == view)
        {
            value = _older;

            return true;
        }

        value = default;

        return false;
    }

    public void Keep(View view, T value)
    {
        _olderView = _newestView;
        _older = _newest;
        _hasOlder = _hasNewest;
        _newestView = view;
        _newest = value;
        _hasNewest = true;
    }

    public void Forget()
    {
        _hasNewest = false;
        _hasOlder = false;
    }
}
