using System.Collections.Concurrent;
using System.Text;
using AutoAndroid.Monitoring;

namespace AutoAndroid.Services
{
    /// <summary>
    /// Circuit Breaker pattern cho các kết nối (ADB, ATX, JsonRpc).
    /// Trạng thái: Closed → Open (sau N lần fail) → HalfOpen (sau timeout) → Closed/Open.
    /// Mỗi circuit breaker có tên riêng để phân biệt trong metric.
    /// </summary>
    public class CircuitBreaker
    {
        private readonly object _lock = new();
        private readonly string _name;
        private readonly int _failureThreshold;
        private readonly int _halfOpenMaxTests;
        private readonly int _openTimeoutMs;
        private readonly int _halfOpenTimeoutMs;

        private CircuitState _state = CircuitState.Closed;
        private int _failureCount;
        private DateTime _lastFailureTime = DateTime.MinValue;
        private int _successCountAfterHalfOpen;
        private int _testCount;
        private DateTime _stateChangedAt = DateTime.MinValue;

        /// <summary>
        /// </summary>
        /// <param name="name">Tên circuit (vd "adb:serial123", "atx:serial456").</param>
        /// <param name="failureThreshold">Số lần fail liên tiếp để mở circuit. Default: 5.</param>
        /// <param name="openTimeoutMs">Thời gian giữ circuit Open trước khi sang HalfOpen. Default: 30s.</param>
        /// <param name="halfOpenMaxTests">Số lần test thành công ở HalfOpen để đóng circuit. Default: 2.</param>
        /// <param name="halfOpenTimeoutMs">Timeout cho HalfOpen nếu không có request nào. Default: 10s.</param>
        public CircuitBreaker(
            string name,
            int failureThreshold = 5,
            int openTimeoutMs = 30_000,
            int halfOpenMaxTests = 2,
            int halfOpenTimeoutMs = 10_000)
        {
            _name = name;
            _failureThreshold = failureThreshold;
            _openTimeoutMs = openTimeoutMs;
            _halfOpenMaxTests = halfOpenMaxTests;
            _halfOpenTimeoutMs = halfOpenTimeoutMs;
        }

        public CircuitState State => _state;

        /// <summary>
        /// Kiểm tra xem có được phép thực hiện request không.
        /// Nếu không (circuit đang Open), throw CircuitBreakerOpenException.
        /// </summary>
        public void Check()
        {
            lock (_lock)
            {
                switch (_state)
                {
                    case CircuitState.Closed:
                        return; // OK

                    case CircuitState.Open:
                        var elapsed = (DateTime.Now - _stateChangedAt).TotalMilliseconds;
                        if (elapsed >= _openTimeoutMs)
                        {
                            // Chuyển sang HalfOpen
                            TransitionTo(CircuitState.HalfOpen);
                            _successCountAfterHalfOpen = 0;
                            _testCount = 0;
                            return;
                        }
                        MetricsCollector.Increment($"circuit.blocked.{_name}");
                        throw new CircuitBreakerOpenException(_name,
                            $"Circuit [{_name}] OPEN — chờ {(int)(_openTimeoutMs - elapsed) / 1000}s");

                    case CircuitState.HalfOpen:
                        var halfElapsed = (DateTime.Now - _stateChangedAt).TotalMilliseconds;
                        if (halfElapsed >= _halfOpenTimeoutMs)
                        {
                            // Hết thời gian HalfOpen mà không có request — quay lại Closed
                            TransitionTo(CircuitState.Closed);
                            _failureCount = 0;
                            return;
                        }
                        // HalfOpen: giới hạn số request test
                        _testCount++;
                        if (_testCount > _halfOpenMaxTests + 2)
                        {
                            MetricsCollector.Increment($"circuit.blocked.{_name}");
                            throw new CircuitBreakerOpenException(_name,
                                $"Circuit [{_name}] HALF_OPEN — quá nhiều request đồng thời");
                        }
                        return;

                    default:
                        return;
                }
            }
        }

        /// <summary>Gọi khi request thành công.</summary>
        public void Success()
        {
            lock (_lock)
            {
                switch (_state)
                {
                    case CircuitState.HalfOpen:
                        _successCountAfterHalfOpen++;
                        if (_successCountAfterHalfOpen >= _halfOpenMaxTests)
                        {
                            // Test đủ số lần thành công → đóng circuit
                            TransitionTo(CircuitState.Closed);
                            _failureCount = 0;
                            MetricsCollector.Increment($"circuit.close.{_name}");
                        }
                        break;

                    case CircuitState.Closed:
                        // Reset failure count dần — mỗi success giảm 1
                        if (_failureCount > 0) _failureCount--;
                        break;
                }
            }
        }

        /// <summary>Gọi khi request thất bại.</summary>
        public void Failure()
        {
            lock (_lock)
            {
                _lastFailureTime = DateTime.Now;
                MetricsCollector.Increment($"circuit.fail.{_name}");

                switch (_state)
                {
                    case CircuitState.Closed:
                        _failureCount++;
                        MetricsCollector.GaugeSet($"circuit.failcount.{_name}", _failureCount);
                        if (_failureCount >= _failureThreshold)
                        {
                            TransitionTo(CircuitState.Open);
                            MetricsCollector.Increment($"circuit.open.{_name}");
                            MetricsCollector.Increment($"circuit.open.{_name}.total");
                        }
                        break;

                    case CircuitState.HalfOpen:
                        // Fail trong HalfOpen → quay lại Open ngay
                        TransitionTo(CircuitState.Open);
                        MetricsCollector.Increment($"circuit.reopen.{_name}");
                        break;

                    case CircuitState.Open:
                        // Đã Open rồi, cập nhật thời gian
                        break;
                }
            }
        }

        private void TransitionTo(CircuitState newState)
        {
            _state = newState;
            _stateChangedAt = DateTime.Now;
        }

        /// <summary>Reset circuit về Closed.</summary>
        public void Reset()
        {
            lock (_lock)
            {
                TransitionTo(CircuitState.Closed);
                _failureCount = 0;
                _successCountAfterHalfOpen = 0;
                _testCount = 0;
            }
        }

        public CircuitBreakerStats GetStats()
        {
            lock (_lock)
            {
                return new CircuitBreakerStats
                {
                    Name = _name,
                    State = _state,
                    FailureCount = _failureCount,
                    LastFailureTime = _lastFailureTime,
                    StateChangedAt = _stateChangedAt,
                };
            }
        }
    }

    public enum CircuitState
    {
        Closed,
        Open,
        HalfOpen
    }

    public class CircuitBreakerOpenException : Exception
    {
        public string CircuitName { get; }
        public CircuitBreakerOpenException(string circuitName, string message) : base(message)
        {
            CircuitName = circuitName;
        }
    }

    public class CircuitBreakerStats
    {
        public string Name { get; init; } = "";
        public CircuitState State { get; init; }
        public int FailureCount { get; init; }
        public DateTime LastFailureTime { get; init; }
        public DateTime StateChangedAt { get; init; }
    }

    /// <summary>
    /// Quản lý tất cả CircuitBreaker instances. Singleton.
    /// </summary>
    public static class CircuitBreakerRegistry
    {
        private static readonly ConcurrentDictionary<string, CircuitBreaker> _breakers = new(StringComparer.OrdinalIgnoreCase);

        public static CircuitBreaker GetOrCreate(string name, int failureThreshold = 5, int openTimeoutMs = 30_000)
        {
            return _breakers.GetOrAdd(name, n => new CircuitBreaker(n, failureThreshold, openTimeoutMs));
        }

        public static void ResetAll()
        {
            foreach (var cb in _breakers.Values) cb.Reset();
        }

        public static string DumpAll()
        {
            var sb = new StringBuilder(2048);
            sb.AppendLine("=== CIRCUIT BREAKERS ===");
            foreach (var kv in _breakers.OrderBy(k => k.Key))
            {
                var s = kv.Value.GetStats();
                sb.AppendLine($"  {s.Name}: state={s.State} failCount={s.FailureCount}");
            }
            sb.AppendLine("=== END ===");
            return sb.ToString();
        }
    }
}
