using System.Collections.Concurrent;
using System.Threading.Tasks;
using AutoAndroid.Monitoring;

namespace AutoAndroid.Services
{
    /// <summary>
    /// Self-healing multi-level recovery:
    /// Level 1: ATX mất kết nối → Reconnect ATX
    /// Level 2: ATX không phản hồi → Restart UIAutomator
    /// Level 3: UIAutomator không khởi động → Reinstall Agent
    /// Level 4: ADB lỗi → adb reconnect
    /// Level 5: ADB daemon lỗi → Restart Device Session
    /// Level 6: Device thật sự offline → Mark Offline
    ///
    /// KHÔNG restart toàn bộ hệ thống chỉ vì một device.
    /// </summary>
    public class SelfHealingService
    {
        private readonly ADBClient _client;
        private readonly DeviceConnectionState _state;
        private readonly string _serial;
        private readonly CircuitBreaker _adbCircuit;
        private readonly CircuitBreaker _atxCircuit;

        // Tránh loop healing: mỗi device chỉ chạy 1 healing session tại 1 thời điểm
        private static readonly ConcurrentDictionary<string, byte> _inFlight = new();

        public SelfHealingService(ADBClient client)
        {
            _client = client;
            _serial = client.Device.Serial;
            _state = DeviceConnectionManager.GetOrCreate(_serial);
            _adbCircuit = CircuitBreakerRegistry.GetOrCreate($"adb:{_serial}", failureThreshold: 5);
            _atxCircuit = CircuitBreakerRegistry.GetOrCreate($"atx:{_serial}", failureThreshold: 5);
        }

        /// <summary>
        /// Entry point: gọi khi phát hiện ATX hoặc ADB lỗi khi đang chạy script.
        /// Trả về true nếu đã phục hồi thành công, false nếu device thực sự offline.
        /// </summary>
        public async Task<bool> HealAsync(string failedComponent, string? error = null)
        {
            if (!_inFlight.TryAdd(_serial, 0))
            {
                // Đang healing rồi — bỏ qua
                return false;
            }

            try
            {
                MetricsCollector.Increment($"heal.triggered.{failedComponent}.{_serial}");

                // Level 1: ATX reconnect
                if (failedComponent == "atx" || failedComponent == "jsonrpc")
                {
                    if (await HealLevel1_ReconnectAtx()) return true;
                }

                // Level 2: ATX không phản hồi → Restart UIAutomator
                if (failedComponent == "atx" || failedComponent == "jsonrpc")
                {
                    if (await HealLevel2_RestartUiAutomator()) return true;
                }

                // Level 3: UIAutomator không khởi động → Reinstall agent
                if (failedComponent == "atx" || failedComponent == "uiautomator")
                {
                    if (await HealLevel3_ReinstallAgent()) return true;
                }

                // Level 4: ADB lỗi
                if (failedComponent == "adb" || failedComponent == "shell")
                {
                    if (await HealLevel4_AdbReconnect()) return true;
                }

                // Level 5: ADB daemon
                if (failedComponent == "adb" || failedComponent == "daemon")
                {
                    if (await HealLevel5_RestartDeviceSession()) return true;
                }

                // Level 6: Offline thật sự
                HealLevel6_MarkOffline(error);
                return false;
            }
            finally
            {
                _inFlight.TryRemove(_serial, out _);
            }
        }

        /// <summary>
        /// Level 1: ATX mất kết nối → Reconnect ATX
        /// Đơn giản: gọi ATX.Connect() + IsAlive()
        /// </summary>
        private async Task<bool> HealLevel1_ReconnectAtx()
        {
            try
            {
                _client.LogHelper.Log($"[SelfHeal][L1] ATX reconnect cho {_serial}...");
                MetricsCollector.Increment("heal.level1.atx_reconnect");
                bool ok = await Task.Run(() => _client.ATX.Connect());
                if (ok && _client.ATX.IsAlive())
                {
                    _state.MarkAtxConnected();
                    _state.IncrementAtxReconnect();
                    _atxCircuit.Reset();
                    _client.LogHelper.Log($"[SelfHeal][L1] ATX reconnect thành công cho {_serial}");
                    MetricsCollector.Increment("heal.level1.success");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _state.MarkAtxDisconnected(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Level 2: ATX không phản hồi → Restart UIAutomator service
        /// </summary>
        private async Task<bool> HealLevel2_RestartUiAutomator()
        {
            try
            {
                _client.LogHelper.Log($"[SelfHeal][L2] Restart UIAutomator cho {_serial}...");
                MetricsCollector.Increment("heal.level2.restart_uia");
                bool ok = await Task.Run(() => _client.ATX.RunUiautomator());
                if (ok)
                {
                    _state.MarkUiAutomatorConnected();
                    _state.MarkAtxConnected();
                    _atxCircuit.Reset();
                    _client.LogHelper.Log($"[SelfHeal][L2] UIAutomator restart thành công cho {_serial}");
                    MetricsCollector.Increment("heal.level2.success");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _state.MarkUiAutomatorDisconnected(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Level 3: UIAutomator không khởi động → Reinstall ATX agent
        /// </summary>
        private async Task<bool> HealLevel3_ReinstallAgent()
        {
            try
            {
                _client.LogHelper.Log($"[SelfHeal][L3] Reinstall ATX agent cho {_serial}...");
                MetricsCollector.Increment("heal.level3.reinstall");
                bool ok = await Task.Run(() => _client.ATX.SetupATX());
                if (ok)
                {
                    _state.MarkAtxConnected();
                    _state.MarkUiAutomatorConnected();
                    _atxCircuit.Reset();
                    _client.LogHelper.Log($"[SelfHeal][L3] Reinstall ATX agent thành công cho {_serial}");
                    MetricsCollector.Increment("heal.level3.success");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _state.MarkUiAutomatorDisconnected(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Level 4: ADB lỗi → adb reconnect
        /// </summary>
        private async Task<bool> HealLevel4_AdbReconnect()
        {
            try
            {
                _client.LogHelper.Log($"[SelfHeal][L4] ADB reconnect cho {_serial}...");
                MetricsCollector.Increment("heal.level4.adb_reconnect");
                bool ok = await Task.Run(() => _client.ConnectAdb());
                if (ok)
                {
                    _state.MarkAdbConnected();
                    _state.IncrementAdbReconnect();
                    _adbCircuit.Reset();
                    _client.LogHelper.Log($"[SelfHeal][L4] ADB reconnect thành công cho {_serial}");
                    MetricsCollector.Increment("heal.level4.success");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _state.MarkAdbDisconnected(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Level 5: ADB daemon lỗi → Restart Device Session
        /// Forward port + reconnect ATX
        /// </summary>
        private async Task<bool> HealLevel5_RestartDeviceSession()
        {
            try
            {
                _client.LogHelper.Log($"[SelfHeal][L5] Restart device session cho {_serial}...");
                MetricsCollector.Increment("heal.level5.restart_session");
                bool ok = await Task.Run(() =>
                {
                    // Forward port lại
                    using var socket = new ADBSocket(_serial);
                    int port = socket.ForwardPort(7912);
                    if (port <= 0) return false;
                    _client.Device.Port = port;

                    // Kết nối lại ATX
                    return _client.ATX.Connect();
                });
                if (ok)
                {
                    _state.MarkAdbConnected();
                    _state.MarkAtxConnected();
                    _adbCircuit.Reset();
                    _atxCircuit.Reset();
                    _client.LogHelper.Log($"[SelfHeal][L5] Device session khôi phục cho {_serial}");
                    MetricsCollector.Increment("heal.level5.success");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _state.MarkAdbDisconnected(ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Level 6: Device thật sự offline → báo lỗi, không retry thêm
        /// </summary>
        private void HealLevel6_MarkOffline(string? error)
        {
            _client.LogHelper.Log($"[SelfHeal][L6] Device {_serial} thực sự offline: {error}");
            _state.MarkAdbDisconnected(error);
            _state.MarkAtxDisconnected(error);
            _state.MarkUiAutomatorDisconnected(error);
            _client.Device.IsLive = false;
            _client.Device.IsAdbOnline = false;
            _client.Device.Status = "Offline";
            MetricsCollector.Increment("heal.level6.offline");
        }
    }
}
