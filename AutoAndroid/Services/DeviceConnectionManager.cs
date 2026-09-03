using System.Collections.Concurrent;
using System.Text;
using AutoAndroid.Monitoring;

namespace AutoAndroid.Services
{
    /// <summary>
    /// Granular trạng thái kết nối cho 1 device.
    /// Thay thế trạng thái "Mất kết nối" gộp chung bằng các state riêng biệt.
    /// </summary>
    public class DeviceConnectionState
    {
        public string Serial { get; }

        // --- ADB ---
        public ComponentState Adb { get; private set; } = ComponentState.Disconnected;
        public int AdbReconnectCount { get; private set; }
        public DateTime? AdbLastConnectedAt { get; private set; }
        public DateTime? AdbLastDisconnectedAt { get; private set; }
        public string? AdbLastError { get; private set; }

        // --- ATX (atx-agent HTTP) ---
        public ComponentState Atx { get; private set; } = ComponentState.Disconnected;
        public int AtxReconnectCount { get; private set; }
        public DateTime? AtxLastConnectedAt { get; private set; }
        public DateTime? AtxLastDisconnectedAt { get; private set; }
        public string? AtxLastError { get; private set; }

        // --- UIAutomator (com.github.uiautomator) ---
        public ComponentState UiAutomator { get; private set; } = ComponentState.Disconnected;
        public int UiAutomatorReconnectCount { get; private set; }
        public DateTime? UiAutomatorLastConnectedAt { get; private set; }
        public string? UiAutomatorLastError { get; private set; }

        // --- Internet ---
        public ComponentState Internet { get; private set; } = ComponentState.Unknown;
        public DateTime? InternetLastCheckedAt { get; private set; }
        public string? InternetLastError { get; private set; }

        // --- Facebook App ---
        public ComponentState FacebookApp { get; private set; } = ComponentState.Disconnected;
        public DateTime? FacebookAppLastOpenedAt { get; private set; }

        // --- Composite ---
        public bool IsOperational => Adb == ComponentState.Connected
                                     && Atx == ComponentState.Connected
                                     && UiAutomator == ComponentState.Connected
                                     && Internet == ComponentState.Connected;

        public DeviceConnectionState(string serial)
        {
            Serial = serial;
        }

        // --- ADB transitions ---

        public void MarkAdbConnected()
        {
            if (Adb != ComponentState.Connected)
            {
                Adb = ComponentState.Connected;
                AdbLastConnectedAt = DateTime.Now;
                MetricsCollector.Increment($"device.adb.online.{Serial}");
            }
        }

        public void MarkAdbDisconnected(string? reason = null)
        {
            if (Adb != ComponentState.Disconnected)
            {
                Adb = ComponentState.Disconnected;
                AdbLastDisconnectedAt = DateTime.Now;
                AdbLastError = reason;
                MetricsCollector.Increment($"device.adb.offline.{Serial}");
            }
        }

        public void IncrementAdbReconnect() => AdbReconnectCount++;

        // --- ATX transitions ---

        public void MarkAtxConnected()
        {
            if (Atx != ComponentState.Connected)
            {
                Atx = ComponentState.Connected;
                AtxLastConnectedAt = DateTime.Now;
                MetricsCollector.Increment($"device.atx.online.{Serial}");
            }
        }

        public void MarkAtxDisconnected(string? reason = null)
        {
            if (Atx != ComponentState.Disconnected)
            {
                Atx = ComponentState.Disconnected;
                AtxLastDisconnectedAt = DateTime.Now;
                AtxLastError = reason;
                MetricsCollector.Increment($"device.atx.offline.{Serial}");
            }
        }

        public void IncrementAtxReconnect() => AtxReconnectCount++;

        // --- UIAutomator transitions ---

        public void MarkUiAutomatorConnected()
        {
            if (UiAutomator != ComponentState.Connected)
            {
                UiAutomator = ComponentState.Connected;
                UiAutomatorLastConnectedAt = DateTime.Now;
            }
        }

        public void MarkUiAutomatorDisconnected(string? reason = null)
        {
            if (UiAutomator != ComponentState.Disconnected)
            {
                UiAutomator = ComponentState.Disconnected;
                UiAutomatorLastError = reason;
            }
        }

        // --- Internet transitions ---

        public void MarkInternetConnected()
        {
            Internet = ComponentState.Connected;
            InternetLastCheckedAt = DateTime.Now;
        }

        public void MarkInternetDisconnected(string? reason = null)
        {
            Internet = ComponentState.Disconnected;
            InternetLastCheckedAt = DateTime.Now;
            InternetLastError = reason;
        }

        // --- Facebook App ---

        public void MarkFacebookAppOpened()
        {
            FacebookApp = ComponentState.Connected;
            FacebookAppLastOpenedAt = DateTime.Now;
        }

        public void MarkFacebookAppClosed()
        {
            FacebookApp = ComponentState.Disconnected;
        }

        /// <summary>Build a human-readable status string (replaces the old "Mất kết nối").</summary>
        public override string ToString()
        {
            var parts = new List<string>();
            parts.Add($"ADB={StateChar(Adb)}");
            parts.Add($"ATX={StateChar(Atx)}");
            parts.Add($"Ui2={StateChar(UiAutomator)}");
            parts.Add($"Net={StateChar(Internet)}");
            if (AdbLastError != null) parts.Add($"ADB-err:{AdbLastError}");
            if (AtxLastError != null) parts.Add($"ATX-err:{AtxLastError}");
            return string.Join(" ", parts);
        }

        private static char StateChar(ComponentState s) => s switch
        {
            ComponentState.Connected => 'Y',
            ComponentState.Disconnected => 'N',
            ComponentState.Unknown => '?',
            _ => '?'
        };
    }

    public enum ComponentState
    {
        Unknown,
        Connected,
        Disconnected
    }

    /// <summary>
    /// Quản lý DeviceConnectionState cho tất cả device.
    /// Singleton — thay thế DeviceMetricsRegistry cho connection state.
    /// </summary>
    public static class DeviceConnectionManager
    {
        private static readonly ConcurrentDictionary<string, DeviceConnectionState> _states = new(StringComparer.OrdinalIgnoreCase);

        public static DeviceConnectionState GetOrCreate(string serial)
        {
            return _states.GetOrAdd(serial, s => new DeviceConnectionState(s));
        }

        public static DeviceConnectionState? Get(string serial)
        {
            return _states.TryGetValue(serial, out var s) ? s : null;
        }

        public static IReadOnlyCollection<DeviceConnectionState> GetAll() => _states.Values.ToList();

        public static void Remove(string serial)
        {
            _states.TryRemove(serial, out _);
        }

        public static void Clear() => _states.Clear();

        /// <summary>Trả về tất cả device đang disconnected ở component nào đó.</summary>
        public static List<DeviceConnectionState> GetUnhealthy()
        {
            return _states.Values.Where(s =>
                s.Adb != ComponentState.Connected ||
                s.Atx != ComponentState.Connected ||
                s.Internet != ComponentState.Connected).ToList();
        }

        public static string DumpAll()
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("=== DEVICE CONNECTION STATES ===");
            foreach (var s in _states.Values.OrderBy(d => d.Serial))
                sb.AppendLine($"  {s.Serial}: {s}");
            sb.AppendLine("=== END ===");
            return sb.ToString();
        }
    }
}
