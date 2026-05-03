using System.Collections.Generic;

public enum UIOverlayId
{
    None = 0,
    Map = 1,
    Tab = 2,
    Pause = 3,
    Setting = 4
}

public sealed class UIOverlayStack
{
    private sealed class StackEntry
    {
        public UIOverlayId Id;
        public Dictionary<string, object> Snapshot;
    }

    private readonly List<StackEntry> _stack = new List<StackEntry>();

    public UIOverlayId Top => _stack.Count > 0 ? _stack[_stack.Count - 1].Id : UIOverlayId.None;

    public bool Contains(UIOverlayId overlay)
    {
        if (overlay == UIOverlayId.None)
        {
            return false;
        }

        for (int i = 0; i < _stack.Count; i++)
        {
            if (_stack[i].Id == overlay)
            {
                return true;
            }
        }

        return false;
    }

    public bool HasAnyOverlay => _stack.Count > 0;

    public void Open(UIOverlayId overlay, Dictionary<string, object> snapshot = null)
    {
        if (overlay == UIOverlayId.None)
        {
            return;
        }

        Close(overlay);
        _stack.Add(new StackEntry
        {
            Id = overlay,
            Snapshot = snapshot ?? new Dictionary<string, object>()
        });
    }

    public Dictionary<string, object> Close(UIOverlayId overlay)
    {
        if (overlay == UIOverlayId.None)
        {
            return null;
        }

        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            if (_stack[i].Id == overlay)
            {
                Dictionary<string, object> snapshot = _stack[i].Snapshot;
                _stack.RemoveAt(i);
                return snapshot;
            }
        }

        return null;
    }

    public Dictionary<string, object> PeekSnapshot()
    {
        return _stack.Count > 0 ? _stack[_stack.Count - 1].Snapshot : null;
    }

    public void Clear()
    {
        _stack.Clear();
    }
}
