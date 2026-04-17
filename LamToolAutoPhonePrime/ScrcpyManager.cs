using AutoAndroid;
using ScrcpyNet;
using Serilog;
using SharpAdbClient;
using Sunny.Subdy.Data.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LamToolAutoPhonePrime
{
    public class ScrcpyManager
    {
        private readonly ConcurrentDictionary<string, Scrcpy> instances = new();
        public readonly AdbClient adb = new();
        private static readonly ILogger log = Log.ForContext<ScrcpyManager>();
        private readonly Dictionary<DeviceData, int> devicePorts = new();

        // Port counter dùng chung — tránh conflict khi nhiều device start cùng lúc
        private static int _nextPort = 27183;
        private static readonly object _portLock = new();

        // Serialize ADB operations: push JAR + reverse forward + TCP handshake
        // Chỉ 1 device được Start tại một thời điểm — tránh ADB flood và forward conflict
        private static readonly SemaphoreSlim _startLock = new(1, 1);

        private static int GetFreePort()
        {
            lock (_portLock)
            {
                // Tăng port cho mỗi instance, tìm port thực sự free
                while (true)
                {
                    int port = _nextPort++;
                    try
                    {
                        var l = new TcpListener(IPAddress.Loopback, port);
                        l.Start();
                        l.Stop();
                        return port;
                    }
                    catch
                    {
                        // port đang dùng, thử port tiếp
                    }
                }
            }
        }

        public Scrcpy StartForDevice(string serial)
        {
            if (!devicePorts.Any())
            {
                log.Warning("[{Serial}] Port not specified for scrcpy.", serial);
                return null;
            }
            var device = devicePorts.Keys.FirstOrDefault(d => d.Serial == serial);
            if (device == null)
            {
                log.Warning("[{Serial}] Device not found for scrcpy.", serial);
                return null;
            }
            if (instances.ContainsKey(device.Serial))
            {
                log.Warning("[{Serial}] Already running.", device.Serial);
                return instances[device.Serial];
            }

            var scrcpy = new Scrcpy(device, devicePorts[device]);
            scrcpy.Start();
            instances[device.Serial] = scrcpy;
            log.Information("[{Serial}] Scrcpy started.", device.Serial);
            return scrcpy;
        }

        /// <summary>
        /// Tự lookup DeviceData từ ADB và auto-assign port trống.
        /// Dùng cho ViewControl khi không có DeviceModel đầy đủ.
        /// </summary>
        /// <summary>
        /// Tạo Scrcpy object (chưa Start) — để caller subscribe events trước khi Start.
        /// </summary>
        public Scrcpy? CreateForSerial(string serial)
        {
            if (instances.TryGetValue(serial, out var existing))
                return existing;

            var allDevices = adb.GetDevices();
            var deviceData = allDevices.FirstOrDefault(d => d.Serial == serial);
            if (deviceData == null)
            {
                log.Warning("[{Serial}] Device not found in ADB. Available: {All}", serial, string.Join(", ", allDevices.Select(d => d.Serial)));
                return null;
            }

            int port = GetFreePort();
            var scrcpy = new Scrcpy(deviceData, port);
            instances[serial] = scrcpy;
            return scrcpy;
        }

        /// <summary>
        /// Start scrcpy đã được tạo bởi CreateForSerial.
        /// </summary>
        public void StartCreated(string serial)
        {
            _startLock.Wait();
            try
            {
                if (instances.TryGetValue(serial, out var scrcpy))
                {
                    // Timeout 15s — scrcpy-server cần thời gian push JAR + khởi động lần đầu
                    scrcpy.Start(timeoutMs: 15000);
                    log.Information("[{Serial}] Scrcpy started.", serial);
                }
            }
            finally
            {
                _startLock.Release();
            }
        }

        public Scrcpy StartForSerial(string serial)
        {
            var scrcpy = CreateForSerial(serial);
            if (scrcpy == null) return null;
            scrcpy.Start();
            return scrcpy;
        }

        public void StartAll(List<DeviceModel> devices)
        {
            foreach (var deviceModel in devices)
            {
                var deviceData = adb.GetDevices().FirstOrDefault(d => d.Serial == deviceModel.Serial);
                if (deviceData == null)
                {
                    log.Warning("[{Serial}] Device not found for scrcpy.", deviceModel.Serial);
                    continue;
                }
                if (!devicePorts.ContainsKey(deviceData))
                {
                    devicePorts[deviceData] = deviceModel.Port;
                }
                StartForDevice(deviceModel.Serial);
            }
        }

        public void Stop(string serial)
        {
            if (instances.TryRemove(serial, out var scrcpy))
            {
                scrcpy.Stop();
                log.Information("[{Serial}] Scrcpy stopped.", serial);
            }
        }

        public void StopAll()
        {
            foreach (var kv in instances)
            {
                kv.Value.Stop();
            }
            instances.Clear();
            log.Information("All scrcpy instances stopped.");
        }


        public IReadOnlyCollection<Scrcpy> GetAll() => instances.Values.ToList().AsReadOnly();
    }
}
