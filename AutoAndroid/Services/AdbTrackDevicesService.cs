using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoAndroid.Monitoring;

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
        private int _consecutiveEmptyResults;

        public event Action<List<string>>? DevicesChanged;

        public bool IsRunning => _trackTask != null && !_trackTask.IsCompleted;

        public void Start()
        {
            if (IsRunning) return;
            _cts?.Dispose();   // giải phóng CTS của lần chạy trước (nếu đã Stop rồi Start lại)
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
                using var _ = MetricsCollector.Measure("trackdevices.poll.ms");
                try
                {
                    var current = ADBHelper.GetDevices();
                    var currentSet = new HashSet<string>(current);

                    // Nếu kết quả rỗng nhưng trước đó có thiết bị: có thể là transient timeout
                    // do semaphore bão hòa, không phải thiết bị thực sự mất kết nối.
                    // Yêu cầu 2 lần rỗng liên tiếp mới chấp nhận là thiết bị đã ngắt toàn bộ.
                    if (currentSet.Count == 0 && _lastSerials.Count > 0)
                    {
                        _consecutiveEmptyResults++;
                        if (_consecutiveEmptyResults < 2)
                        {
                            // Bỏ qua lần này, giữ nguyên _lastSerials, đợi poll tiếp
                            try { Task.Delay(5000, token).Wait(token); }
                            catch (OperationCanceledException) { break; }
                            continue;
                        }
                    }
                    else
                    {
                        _consecutiveEmptyResults = 0;
                    }

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
