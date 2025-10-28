using AutoAndroid;
using SharpAdbClient;
using StreamAndroid.Models;
using StreamAndroid.View;
using Sunny.Subdy.Common.Helper;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;

namespace StreamAndroid.Services
{
    public class DeviceManagerService : IDisposable
    {
        private readonly object _lockObj = new();
        private readonly DeviceMonitor _monitor;
        private readonly ConcurrentDictionary<string, DeviceView> _instances = new();
        private readonly AdbClient _adb = new();
        private readonly DeviceModelContext _deviceModelContext = new();
        private readonly SelectableFlowLayoutPanel _flowPanel;
        private readonly ucDataGridViewDevice _dataGridView;
        private readonly ucManagerDevices _managerDevices;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingOperations = new();
        private readonly ConcurrentDictionary<string, DateTime> _lastEventTime = new();
        private const int DEBOUNCE_MS = 500;
        private readonly SemaphoreSlim _uiSemaphore = new(1, 1);

        private readonly System.Windows.Forms.Timer _statsUpdateTimer;
        private volatile bool _statsUpdatePending = false;

        public static SortableBindingList<DeviceModel> DeviceModels { get; private set; } = new SortableBindingList<DeviceModel>(new List<DeviceModel>());

        public DeviceManagerService(ucManagerDevices ucManagerDevices)
        {
            ADBHelper.StartServer();

            var endPoint = new IPEndPoint(IPAddress.Loopback, 5037);
            _monitor = new DeviceMonitor(new AdbSocket(endPoint));

            _flowPanel = ucManagerDevices.flControlAndroid;
            _dataGridView = ucManagerDevices.dataGridViewDevice;
            _managerDevices = ucManagerDevices;
            _dataGridView.bindingList = DeviceModels;
            _dataGridView.dataGridView1.DataSource = DeviceModels;
            _dataGridView.SelectionChangedEvent += OnDataGridViewSelectionChanged;
            _dataGridView.CheckedChangedEvent += OnDataGridViewCheckChanged;

            _statsUpdateTimer = new System.Windows.Forms.Timer { Interval = 200 };
            _statsUpdateTimer.Tick += (s, e) =>
            {
                if (_statsUpdatePending)
                {
                    _statsUpdatePending = false;
                    UpdateUIStatsSync();
                }
            };
            _statsUpdateTimer.Start();
        }

        public async Task HookDeviceEvents()
        {
            await LoadExistingDevicesAsync();
            await Task.Delay(1000);

            _monitor.DeviceConnected += OnDeviceConnected;
            _monitor.DeviceDisconnected += OnDeviceDisconnected;
            _monitor.Start();
        }

        private async Task LoadExistingDevicesAsync()
        {
            var devices = _deviceModelContext.GetAll();
            var connectedSerials = ADBHelper.GetDevices();

            foreach (var device in devices)
            {
                Debug.WriteLine($"{device.Id} - {device.NameDevice}");

                if (connectedSerials.Contains(device.Serial))
                {
                    await EnsureDeviceConnectedAsync(device.Serial);
                }
                else
                {
                    await AddDisconnectedDeviceAsync(device);
                }
            }

            _managerDevices.SetRenderSize(_managerDevices.slider3.Value);
        }

        private async Task AddDisconnectedDeviceAsync(DeviceModel device)
        {
            device.IsScrcpy = false;
            device.State = "Ngắt kết nối";
            device.TypeColor = 1;
            device.Status = "Ngắt kết nối";

            var deviceView = new DeviceView
            {
                DeviceModel = device
            };

            _instances[device.Serial] = deviceView;

            await InvokeUIAsync(() =>
            {
                var control = new ucControlAndroid(deviceView)
                {
                    Name = $"ucControlAndroid_{device.Serial}",
                    Width = 300,
                    Height = 533
                };
                control.Load += (s, e2) => control.SetCenterText("Điện thoại đã ngắt kết nối, vui lòng kiểm tra.", Color.OrangeRed);
                control.SetCenterText("Điện thoại đã ngắt kết nối, vui lòng kiểm tra.", Color.OrangeRed);
                _flowPanel.Controls.Add(control);
                deviceView.UCControlAndroid = control;

                DeviceModels.Add(device);
            });
        }

        private async void OnDeviceConnected(object sender, DeviceDataEventArgs e)
        {
            var serial = e.Device.Serial;
            Debug.WriteLine($"📱 Kết nối event: {serial}");

            if (_pendingOperations.TryGetValue(serial, out var existingCts))
            {
                existingCts.Cancel();
                _pendingOperations.TryRemove(serial, out _);
            }

            var cts = new CancellationTokenSource();
            _pendingOperations[serial] = cts;

            try
            {
                await Task.Delay(DEBOUNCE_MS, cts.Token);

                _lastEventTime.TryGetValue(serial, out var lastTime);
                var now = DateTime.Now;
                if ((now - lastTime).TotalMilliseconds < DEBOUNCE_MS)
                {
                    Debug.WriteLine($"⏭️ Bỏ qua event cũ: {serial}");
                    return;
                }
                _lastEventTime[serial] = now;

                await ProcessDeviceConnectedAsync(serial);
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"⏹️ Hủy operation: {serial}");
            }
            finally
            {
                _pendingOperations.TryRemove(serial, out _);
            }
        }

        private async void OnDeviceDisconnected(object sender, DeviceDataEventArgs e)
        {
            var serial = e.Device.Serial;
            Debug.WriteLine($"❌ Ngắt kết nối event: {serial}");

            if (_pendingOperations.TryGetValue(serial, out var existingCts))
            {
                existingCts.Cancel();
                _pendingOperations.TryRemove(serial, out _);
            }

            var cts = new CancellationTokenSource();
            _pendingOperations[serial] = cts;

            try
            {
                await Task.Delay(DEBOUNCE_MS, cts.Token);

                _lastEventTime.TryGetValue(serial, out var lastTime);
                var now = DateTime.Now;
                if ((now - lastTime).TotalMilliseconds < DEBOUNCE_MS)
                {
                    Debug.WriteLine($"⏭️ Bỏ qua disconnect event cũ: {serial}");
                    return;
                }
                _lastEventTime[serial] = now;

                await ProcessDeviceDisconnectedAsync(serial);
            }
            catch (TaskCanceledException)
            {
                Debug.WriteLine($"⏹️ Hủy disconnect operation: {serial}");
            }
            finally
            {
                _pendingOperations.TryRemove(serial, out _);
            }
        }

        private async Task ProcessDeviceConnectedAsync(string serial)
        {
            Debug.WriteLine($"✅ Xử lý kết nối: {serial}");

            await _uiSemaphore.WaitAsync();
            try
            {
                if (!_instances.ContainsKey(serial))
                {
                    await EnsureDeviceConnectedAsync(serial);
                }
                else
                {
                    await ReconnectExistingDeviceAsync(serial);
                }

                ScheduleStatsUpdate();
            }
            finally
            {
                _uiSemaphore.Release();
            }
        }

        private async Task ProcessDeviceDisconnectedAsync(string serial)
        {
            Debug.WriteLine($"✅ Xử lý ngắt kết nối: {serial}");

            await _uiSemaphore.WaitAsync();
            try
            {
                //if (_instances.TryGetValue(serial, out var deviceView))
                //{
                //    if (deviceView.UCControlAndroid != null)
                //    {
                //        try
                //        {
                //            await InvokeUIAsync(() =>
                //            {
                //                deviceView.UCControlAndroid?.OnDeviceDisconnected();
                //            });

                //            await Task.Delay(50);
                //        }
                //        catch (Exception ex)
                //        {
                //            Debug.WriteLine($"⚠️ Error notifying disconnect: {ex.Message}");
                //        }
                //    }
                //}

                StopDeviceBySerial(serial);
                ScheduleStatsUpdate();
            }
            finally
            {
                _uiSemaphore.Release();
            }
        }

        private void ScheduleStatsUpdate()
        {
            _statsUpdatePending = true;
        }

        private void UpdateUIStatsSync()
        {
            if (_flowPanel.InvokeRequired)
            {
                try { _flowPanel.Invoke(new Action(UpdateUIStatsSync)); } catch { }
                return;
            }

            try
            {
                var connectedCount = _instances.Count(x => x.Value.DeviceModel.State == "Đã kết nối");
                var disconnectedCount = _instances.Count(x => x.Value.DeviceModel.State == "Ngắt kết nối");

                _managerDevices.label1.Text = $"Tổng thiết bị\r\n{_instances.Count}";
                _managerDevices.label2.Text = $"Đã kết nối\r\n{connectedCount}";
                _managerDevices.label3.Text = $"Ngắt kết nối\r\n{disconnectedCount}";

                var uniqueStates = DeviceModels
                    .Select(d => d.State)
                    .Where(state => !string.IsNullOrWhiteSpace(state))
                    .Distinct()
                    .ToList();

                _managerDevices.select1.Items.Clear();
                _managerDevices.select1.Items.Add("Tất cả");
                _managerDevices.select1.SelectedIndex = 0;

                if (uniqueStates.Any())
                {
                    _managerDevices.select1.Items.AddRange(uniqueStates.ToArray());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ UpdateUIStatsSync error: {ex.Message}");
            }
        }

        private async Task EnsureDeviceConnectedAsync(string serial)
        {
            try
            {
                var deviceView = await Task.Run(() => ConnectDevice(serial));
                if (deviceView == null) return;

                _instances[serial] = deviceView;

                var model = deviceView.DeviceModel;
                model.IsScrcpy = true;
                model.State = "Đã kết nối";
                model.TypeColor = 2;
                model.Status = "Đã kết nối";

                await InvokeUIAsync(() =>
                {
                    if (!DeviceModels.Any(d => d.Serial == serial))
                    {
                        DeviceModels.Add(model);
                    }
                    _managerDevices.SetRenderSize(_managerDevices.slider3.Value);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi kết nối {serial}: {ex.Message}");
            }
        }

        private async Task ReconnectExistingDeviceAsync(string serial)
        {
            var deviceView = _instances[serial];
            if (deviceView?.UCControlAndroid != null)
            {
                Debug.WriteLine($"⏭️ Device {serial} đã có control, refresh renderer...");

                try
                {
                    await InvokeUIAsync(() =>
                    {
                        deviceView.UCControlAndroid?.ClearAllText();
                    });

                    if (deviceView.Scrcpy == null || deviceView.DeviceData == null)
                    {
                        await InvokeUIAsync(() =>
                        {
                            var client = new ADBClient(deviceView.DeviceModel);
                            client.Connect();

                            deviceView.DeviceData = _adb.GetDevices().Find(d => d.Serial == serial);
                            if (deviceView.DeviceData != null)
                            {
                                deviceView.Scrcpy = new Scrcpy(deviceView.DeviceData, deviceView.DeviceModel.Port);
                                deviceView.Scrcpy.Start();
                            }
                        });
                    }

                    await InvokeUIAsync(() =>
                    {
                        deviceView.UCControlAndroid?.AttachInstance(deviceView.Scrcpy);
                        deviceView.UCControlAndroid?.OnDeviceReconnected();
                    });

                    deviceView.DeviceModel.IsScrcpy = true;
                    deviceView.DeviceModel.State = "Đã kết nối";
                    deviceView.DeviceModel.TypeColor = 2;
                    deviceView.DeviceModel.Status = "Đã kết nối";
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Error refreshing renderer: {ex.Message}");
                }

                return;
            }

            try
            {
                await InvokeUIAsync(() =>
                {
                    var client = new ADBClient(deviceView.DeviceModel);
                    client.Connect();

                    deviceView.DeviceData = _adb.GetDevices().Find(d => d.Serial == serial);
                    if (deviceView.DeviceData == null) return;

                    deviceView.Scrcpy = new Scrcpy(deviceView.DeviceData, deviceView.DeviceModel.Port);
                    deviceView.Scrcpy.Start();

                    var control = new ucControlAndroid(deviceView)
                    {
                        Name = $"ucControlAndroid_{serial}",
                        Width = 300,
                        Height = 533
                    };

                    _flowPanel.Controls.Add(control);
                    deviceView.UCControlAndroid = control;

                    control.AttachInstance(deviceView.Scrcpy);
                    control.OnDeviceReconnected();

                    deviceView.DeviceModel.IsScrcpy = true;
                    deviceView.DeviceModel.State = "Đã kết nối";
                    deviceView.DeviceModel.TypeColor = 2;
                    deviceView.DeviceModel.Status = "Đã kết nối";
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi reconnect {serial}: {ex.Message}");
            }
        }

        private DeviceView ConnectDevice(string serial)
        {
            lock (_lockObj)
            {
                try
                {
                    var client = new ADBClient(serial);
                    if (client?.Device == null) return null;

                    if (_deviceModelContext.GetBySerial(serial) == null)
                    {
                        _deviceModelContext.Add(client.Device);
                    }

                    var deviceData = _adb.GetDevices().Find(d => d.Serial == serial);
                    if (deviceData == null) return null;

                    var view = new DeviceView
                    {
                        DeviceModel = client.Device,
                        DeviceData = deviceData,
                        Scrcpy = new Scrcpy(deviceData, client.Device.Port)
                    };

                    view.Scrcpy.Start();

                    InvokeUIAsync(() =>
                    {
                        bool exists = _flowPanel.Controls.OfType<ucControlAndroid>()
                            .Any(ctrl => ctrl.device.Serial == serial);

                        if (!exists)
                        {
                            var control = new ucControlAndroid(view);
                            control.Name = $"ucControlAndroid_{serial}";
                            _flowPanel.Controls.Add(control);
                            view.UCControlAndroid = control;
                            control.AttachInstance(view.Scrcpy);
                        }
                        else
                        {
                            var control = _flowPanel.Controls.OfType<ucControlAndroid>()
                                .First(ctrl => ctrl.device.Serial == serial);
                            view.UCControlAndroid = control;
                            control.AttachInstance(view.Scrcpy);
                        }
                    }).Wait();

                    return view;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Lỗi Connect({serial}): {ex.Message}");
                    return null;
                }
            }
        }

        public void StopDeviceBySerial(string serial)
        {
            lock (_lockObj)
            {
                if (!_instances.TryGetValue(serial, out var deviceView)) return;

                try
                {
                    if (deviceView.UCControlAndroid != null)
                    {
                        if (_flowPanel.InvokeRequired)
                            _flowPanel.Invoke(new Action(() => deviceView.UCControlAndroid.OnDeviceDisconnected()));
                        else
                            deviceView.UCControlAndroid.OnDeviceDisconnected();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Error calling OnDeviceDisconnected: {ex.Message}");
                }

                deviceView.Scrcpy?.Stop();
                deviceView.DeviceModel.IsScrcpy = false;
                deviceView.DeviceModel.State = "Ngắt kết nối";
                deviceView.DeviceModel.TypeColor = 1;
                deviceView.DeviceModel.Status = "Ngắt kết nối";

                ShowDisconnectedText(serial);
            }
        }

        private void ShowDisconnectedText(string serial)
        {
            try
            {
                if (!_instances.TryGetValue(serial, out var deviceView)) return;
                if (deviceView.UCControlAndroid == null) return;

                if (_flowPanel.InvokeRequired)
                {
                    _flowPanel.Invoke(new Action(() => ShowDisconnectedText(serial)));
                    return;
                }

                deviceView.UCControlAndroid.SetCenterText(
                    "Điện thoại đã ngắt kết nối, vui lòng kiểm tra.",
                    Color.OrangeRed
                );

                Debug.WriteLine($"✅ Showed disconnected text for {serial}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi ShowDisconnectedText({serial}): {ex.Message}");
            }
        }

        private Task InvokeUIAsync(Action action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (_flowPanel?.InvokeRequired == true)
            {
                _flowPanel.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        action();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }));
            }
            else
            {
                try
                {
                    action();
                    tcs.SetResult(true);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }

            return tcs.Task;
        }

        public void Dispose()
        {
            _statsUpdateTimer?.Stop();
            _statsUpdateTimer?.Dispose();

            foreach (var cts in _pendingOperations.Values)
            {
                cts?.Cancel();
            }
            _pendingOperations.Clear();

            _monitor?.Dispose();

            foreach (var pair in _instances)
            {
                try
                {
                    pair.Value?.Scrcpy?.Stop();
                    pair.Value?.UCControlAndroid?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Lỗi dispose {pair.Key}: {ex.Message}");
                }
            }

            _instances.Clear();
            DeviceModels.Clear();
            _uiSemaphore?.Dispose();
        }

        private async void OnDataGridViewSelectionChanged(object sender, DeviceSelectionChangedEventArgs e)
        {
            Debug.WriteLine($"🔍 Selection changed: {e.SelectionCount} devices selected");
            _managerDevices.label4.Text = $"Bôi đen\r\n{e.SelectionCount}";
            await HandleSelectionChanged(e.SelectedDevices);
        }

        private async void OnDataGridViewCheckChanged(object sender, DeviceSelectionChangedEventArgs e)
        {
            Debug.WriteLine($"🔍 Selection changed: {e.SelectionCount} devices selected");
            _managerDevices.label5.Text = $"Đã chọn\r\n{e.SelectionCount}";
        }

        public async Task TaskKey(string key)
        {
            switch (key)
            {
                case "SelectDatagridview":
                    {
                        var selectedDevices = _dataGridView.dataGridView1.SelectedRows
                            .Cast<DataGridViewRow>()
                            .Select(row => row.DataBoundItem as DeviceModel)
                            .Where(device => device != null)
                            .ToList();

                        await HandleSelectionChanged(selectedDevices);
                    }
                    break;
            }
        }

        private async Task HandleSelectionChanged(List<DeviceModel> selectedDevices)
        {
            try
            {
                foreach (var device in DeviceModels)
                {
                    device.IsSelectControl = false;
                }

                var selectedSerials = new HashSet<string>(selectedDevices.Select(d => d.Serial));
                foreach (var device in DeviceModels.Where(d => selectedSerials.Contains(d.Serial)))
                {
                    device.IsSelectControl = true;
                }

                await InvokeUIAsync(() =>
                {
                    foreach (Control ctrl in _flowPanel.Controls)
                    {
                        if (ctrl is ucControlAndroid ucDevice)
                        {
                            ucDevice.panel1.BorderColor = ucDevice.device.IsSelectControl
                                ? Color.Green
                                : Color.RoyalBlue;
                        }
                    }
                });

                Debug.WriteLine($"✅ Updated borders for {selectedDevices.Count} devices");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error in HandleSelectionChanged: {ex.Message}");
            }
        }

        public async Task FilterDevices(string state, string search)
        {
            try
            {
                await InvokeUIAsync(() =>
                {
                    bool showAllState = string.IsNullOrEmpty(state) || state == "Tất cả";
                    bool hasSearch = !string.IsNullOrWhiteSpace(search);
                    string searchLower = search?.Trim().ToLowerInvariant() ?? "";

                    foreach (Control ctrl in _flowPanel.Controls)
                    {
                        bool visible = true;

                        if (ctrl is ucControlAndroid ucDevice)
                        {
                            var device = ucDevice.device;
                            visible =
                                (showAllState || device.State == state) &&
                                (!hasSearch ||
                                 (device.Serial?.ToLowerInvariant().Contains(searchLower) == true ||
                                  device.NameDevice?.ToLowerInvariant().Contains(searchLower) == true));
                        }

                        ctrl.Visible = visible;
                    }

                    _dataGridView._suppressSelectionChanged = true;

                    try
                    {
                        var dgv = _dataGridView.dataGridView1;
                        dgv.EndEdit();
                        dgv.CurrentCell = null;
                        dgv.ClearSelection();

                        BindingManagerBase bm = dgv.BindingContext[dgv.DataSource];

                        if (bm != null)
                            bm.SuspendBinding();

                        foreach (DataGridViewRow row in dgv.Rows)
                        {
                            if (row.DataBoundItem is DeviceModel device)
                            {
                                bool visible =
                                    (showAllState || device.State == state) &&
                                    (!hasSearch ||
                                     (device.Serial?.ToLowerInvariant().Contains(searchLower) == true ||
                                      device.NameDevice?.ToLowerInvariant().Contains(searchLower) == true));

                                if (row.Visible != visible)
                                    row.Visible = visible;
                            }
                        }

                        if (bm != null)
                        {
                            bm.ResumeBinding();

                            for (int i = 0; i <= dgv.Rows.Count - 1; i++)
                            {
                                if (dgv.Rows[i].Visible)
                                {
                                    bm.Position = i;
                                    break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        _dataGridView._suppressSelectionChanged = false;
                    }

                    _flowPanel.Refresh();
                    _dataGridView.dataGridView1.Refresh();
                });

                Debug.WriteLine($"✅ Filtered devices by state: {state ?? "All"}, search: '{search}'");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Error filtering devices: {ex.Message}");
            }
        }
    }
}
