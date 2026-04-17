using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AutoAndroid
{
    /// <summary>
    /// Lightweight poll of 'adb devices' every 5s.
    /// Only fires DevicesChanged when the serial list actually changes.
    /// </summary>
    public class AdbTrackDevicesService : IDisposable
    {
        private CancellationTokenSource? _cts;
        private Task? _trackTask;
        private HashSet<string> _lastSerials = new();

        public event Action<List<string>>? DevicesChanged;

        public bool IsRunning => _trackTask != null && !_trackTask.IsCompleted;

        public void Start()
        {
            if (IsRunning) return;
            _cts = new CancellationTokenSource();
            _trackTask = Task.Run(() => TrackLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _trackTask = null;
        }

        private void TrackLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var current = ADBHelper.GetDevices();
                    var currentSet = new HashSet<string>(current);

                    if (!currentSet.SetEquals(_lastSerials))
                    {
                        _lastSerials = currentSet;
                        DevicesChanged?.Invoke(current);
                    }
                }
                catch { }

                try { Task.Delay(5000, token).Wait(token); }
                catch (OperationCanceledException) { break; }
            }
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }
    }
}
