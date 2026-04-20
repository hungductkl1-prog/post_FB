using System.Collections.Concurrent;
using System.ComponentModel;

namespace Sunny.Subdy.Data.Models;

/// <summary>
/// Marshals PropertyChanged events from background threads to the UI thread via BeginInvoke.
/// Uses a 150ms dedup window per (owner, propertyName) to avoid flooding the UI thread
/// when worker threads update properties in tight loops.
///
/// Design:
///   - MarkDirty: called from any thread, just sets a flag per (owner, property)
///   - Timer fires every 150ms on a background thread, snapshots dirty pairs, clears them,
///     then BeginInvokes RaiseAll on the UI control.
///   - RaiseAll runs on the UI thread — safe for BindingList/DataGridView.
///   - Unregister: called synchronously on the UI thread (from BindingList.RemoveItem via
///     OnBeforeRemove hook) BEFORE the item exits BindingList's Child_PropertyChanged
///     subscription. After Unregister, no further events will be raised for that item.
///
/// Race condition eliminated:
///   Previously: Flush snapshot → BeginInvoke → meanwhile ApplyFilter calls Clear()+Unregister()
///   → snapshot still contains removed items → RaisePropertyChanged on removed item → crash.
///
///   Fix: _active tracks which owners are currently registered. RaiseAll checks _active before
///   raising. Unregister removes from _active synchronously. Even if a stale snapshot is in flight,
///   RaiseAll will skip unregistered owners.
/// </summary>
public sealed class ThrottledPropertyNotifier
{
    public const int IntervalMs = 150;

    // Pending (owner, propertyName) pairs — set by any thread, cleared by Flush
    private static readonly ConcurrentDictionary<(INotifyPropertyChanged, string), byte> _dirty = new();

    // Set of currently active owners — items are added on first MarkDirty, removed by Unregister.
    // RaiseAll checks this before firing to skip owners removed between snapshot and invocation.
    private static readonly ConcurrentDictionary<INotifyPropertyChanged, byte> _active = new();

    private static readonly System.Threading.Timer _timer;
    // Captured from the UI thread in Initialize(). Used to Post RaiseAll back to the UI thread.
    private static SynchronizationContext? _uiContext;

    static ThrottledPropertyNotifier()
    {
        _timer = new System.Threading.Timer(Flush, null, IntervalMs, IntervalMs);
    }

    /// <summary>
    /// Call once from the UI thread after the main form is loaded (e.g. in Form.Load).
    /// At that point WindowsFormsSynchronizationContext is guaranteed to be installed.
    /// </summary>
    public static void Initialize()
    {
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException(
                "ThrottledPropertyNotifier.Initialize() must be called on the UI thread after WinForms is running.");
    }

    /// <summary>
    /// Mark a property as dirty. Safe to call from any thread.
    /// </summary>
    public static void MarkDirty(INotifyPropertyChanged owner, string propertyName)
    {
        _active.TryAdd(owner, 0);
        _dirty.TryAdd((owner, propertyName), 0);
    }

    /// <summary>
    /// Remove owner from active set so no further PropertyChanged events will be raised for it.
    /// Must be called on the UI thread (from BindingList.RemoveItem → OnBeforeRemove hook)
    /// before the item is unsubscribed from BindingList.Child_PropertyChanged.
    /// This eliminates the race: even if a stale snapshot is already queued in BeginInvoke,
    /// RaiseAll will skip this owner.
    /// </summary>
    public static void Unregister(INotifyPropertyChanged owner)
    {
        _active.TryRemove(owner, out _);
        // Also clear any pending dirty entries to avoid unnecessary work
        foreach (var key in _dirty.Keys)
        {
            if (ReferenceEquals(key.Item1, owner))
                _dirty.TryRemove(key, out _);
        }
    }

    private static void Flush(object? state)
    {
        if (_dirty.IsEmpty) return;

        // Snapshot and clear dirty set
        var keys = _dirty.Keys.ToArray();
        foreach (var key in keys)
            _dirty.TryRemove(key, out _);

        var ctx = _uiContext;
        if (ctx == null) return;

        // Post (not Send) — async, does not block the timer thread
        ctx.Post(_ => RaiseAll(keys), null);
    }

    private static void RaiseAll((INotifyPropertyChanged owner, string propertyName)[] keys)
    {
        foreach (var (owner, propertyName) in keys)
        {
            // Skip if Unregister was called between Flush snapshot and this invocation.
            // This is the key guard that prevents raising PropertyChanged on removed items.
            if (!_active.ContainsKey(owner)) continue;

            if (owner is IThrottledNotify t)
            {
                try { t.RaisePropertyChanged(propertyName); }
                catch (ObjectDisposedException)
                {
                    // Bound control was disposed between snapshot and raise — drop the owner.
                    _active.TryRemove(owner, out _);
                }
                catch (InvalidOperationException)
                {
                    // Bound control's handle not created yet (e.g. tab never shown).
                    // The control will read the current value when its handle is created.
                }
            }
        }
    }
}

/// <summary>
/// Implemented by models that use throttled notifications.
/// </summary>
public interface IThrottledNotify
{
    void RaisePropertyChanged(string propertyName);
}
