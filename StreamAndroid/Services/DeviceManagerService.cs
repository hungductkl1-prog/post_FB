using AntdUI;
using AutoAndroid;
using Serilog.Core;
using SharpAdbClient;
using StreamAndroid.Helper;
using StreamAndroid.Models;
using StreamAndroid.View;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace StreamAndroid.Services
{
    public class DeviceManagerService : IDisposable
    {
        private readonly object lockObj = new();
        private readonly DeviceMonitor monitor;
        private readonly ConcurrentDictionary<string, DeviceView> instances = new();
        private readonly AdbClient adb = new();
        private readonly DeviceModelContext deviceModelContext = new();
        private SelectableFlowLayoutPanel selectableFlowLayoutPanel;
        private ucDataGridViewDevice ucDataGridView;
        private ucManagerDevices ucManagerDevices;
        public DeviceManagerService(ucManagerDevices ucManagerDevices)
        {
            ADBHelper.StartServer();
            var endPoint = new IPEndPoint(IPAddress.Loopback, 5037);
            monitor = new DeviceMonitor(new AdbSocket(endPoint));
            this.selectableFlowLayoutPanel = ucManagerDevices.flControlAndroid;
            this.ucDataGridView = ucManagerDevices.dataGridViewDevice;
            this.ucManagerDevices = ucManagerDevices;
        }

        public async Task HookDeviceEvents()
        {
            // Tải danh sách thiết bị từ DB khi khởi tạo
            foreach (var device in deviceModelContext.GetAll())
            {
                Debug.WriteLine($"{device.Id} - {device.NameDevice}");
                if (!ADBHelper.GetDevices().Contains(device.Serial)) continue;
                await EnsureDeviceConnectedAsync(device.Serial);
            }
            await Task.Delay(1000);
            // Khi có thiết bị mới kết nối
            monitor.DeviceConnected += async (s, e) =>
            {
                Debug.WriteLine($"📱 Kết nối: {e.Device.Serial}");
                if (!instances.ContainsKey(e.Device.Serial))
                {
                    await EnsureDeviceConnectedAsync(e.Device.Serial);
                }
                else
                {
                    var device = instances[e.Device.Serial];
                    if (device == null)
                    {
                        return;
                    }
                    if (device.UCControlAndroid != null)
                    {
                        return;
                    }
                    try
                    {
                        selectableFlowLayoutPanel?.Invoke(new Action(() =>
                        {
                            int oldIndex = selectableFlowLayoutPanel.Controls.GetChildIndex(device.ucThongBaoDeviceView);
                            var client = new ADBClient(device.DeviceModel);
                            client.Connect();
                            device.DeviceData = adb.GetDevices().Find(d => d.Serial == e.Device.Serial);
                            device.Scrcpy = new Scrcpy(device.DeviceData, device.DeviceModel.Port);
                            device.Scrcpy.Start();
                            device.UCControlAndroid = new ucControlAndroid(device);
                            device.UCControlAndroid.Width = device.ucThongBaoDeviceView.Width;
                            device.UCControlAndroid.Height = device.ucThongBaoDeviceView.Height;
                            device.UCControlAndroid.Margin = device.ucThongBaoDeviceView.Margin;
                            selectableFlowLayoutPanel.Controls.RemoveAt(oldIndex);
                            device.ucThongBaoDeviceView.Dispose();
                            selectableFlowLayoutPanel.Controls.Add(device.UCControlAndroid);
                            selectableFlowLayoutPanel.Controls.SetChildIndex(device.UCControlAndroid, oldIndex);
                           
                        }));
                    }
                    catch
                    {

                    }

                }
            };

            // Khi thiết bị bị ngắt
            monitor.DeviceDisconnected += (s, e) =>
            {
                Debug.WriteLine($"❌ Ngắt: {e.Device.Serial}");
                StopDeviceBySerial(e.Device.Serial);
            };

            // Bắt đầu theo dõi
            monitor.Start();
        }

        private async Task EnsureDeviceConnectedAsync(string serial)
        {
            try
            {
                instances[serial] = Connect(serial);
                ucManagerDevices.SetRenderSize(ucManagerDevices.slider3.Value);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"⚠️ Lỗi kết nối {serial}: {ex.Message}");
            }
        }

        private DeviceView Connect(string serial)
        {
            lock (lockObj)
            {
                try
                {
                    var client = new ADBClient(serial);
                    if (client?.Device == null)
                        return null;

                    if (deviceModelContext.GetBySerial(serial) == null)
                    {
                        deviceModelContext.Add(client.Device);
                    }

                    var deviceData = adb.GetDevices().Find(d => d.Serial == serial);
                    if (deviceData == null)
                        return null;

                    var view = new DeviceView
                    {
                        DeviceModel = client.Device,
                        DeviceData = deviceData,
                        Scrcpy = new Scrcpy(deviceData, client.Device.Port)

                    };

                    view.Scrcpy.Start();

                    selectableFlowLayoutPanel?.Invoke(new Action(() =>
                    {
                        bool check = false;
                        if (selectableFlowLayoutPanel.Controls.Count > 0)
                        {
                            foreach (ucControlAndroid ctrl in selectableFlowLayoutPanel.Controls)
                            {
                                if (ctrl.device.Serial == serial)
                                {
                                    check = true;
                                    break;
                                }
                            }
                        }
                        if (!check)
                        {
                            var control = new ucControlAndroid(view);
                            selectableFlowLayoutPanel.Controls.Add(control);
                            view.UCControlAndroid = control;
                           
                        }

                    }));

                    view.DeviceModel.IsScrcpy = true;
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
            lock (lockObj)
            {
                try
                {
                    var device = instances[serial];
                    device?.Scrcpy?.Stop();
                    device.DeviceModel.IsScrcpy = false;
                    HideControlAndroid(serial);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"⚠️ Lỗi dừng thiết bị {serial}: {ex.Message}");
                }
            }

        }
        private void HideControlAndroid(string serial)
        {
            try
            {
                selectableFlowLayoutPanel?.Invoke(new Action(() =>
                {
                    var device = instances[serial];
                    int oldIndex = selectableFlowLayoutPanel.Controls.GetChildIndex(device.UCControlAndroid);
                    selectableFlowLayoutPanel.Controls.RemoveAt(oldIndex);
                    device.ucThongBaoDeviceView = new ucThongBaoDeviceView();
                    device.ucThongBaoDeviceView.Width = device.UCControlAndroid.Width;
                    device.ucThongBaoDeviceView.Height = device.UCControlAndroid.Height;
                    device.ucThongBaoDeviceView.Margin = device.UCControlAndroid.Margin;
                    device.UCControlAndroid.Dispose();
                    device.UCControlAndroid = null;
                    selectableFlowLayoutPanel.Controls.Add(device.ucThongBaoDeviceView);
                    selectableFlowLayoutPanel.Controls.SetChildIndex(device.ucThongBaoDeviceView, oldIndex);

                }));

            }
            catch
            {

            }
            finally
            {
            }

        }
        public void Dispose()
        {
            monitor?.Dispose();

            foreach (var pair in instances)
            {
                try
                {
                    pair.Value?.Scrcpy?.Stop();
                }
                catch { }
            }

            instances.Clear();
        }
    }
}
