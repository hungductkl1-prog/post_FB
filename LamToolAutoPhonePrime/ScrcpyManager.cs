using AutoAndroid;
using ScrcpyNet;
using Serilog;
using SharpAdbClient;
using Sunny.Subdy.Data.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LamToolAutoPhonePrime
{
    public class ScrcpyManager
    {
        private readonly ConcurrentDictionary<string, Scrcpy> instances = new();
        public readonly AdbClient adb = new();
        private static readonly ILogger log = Log.ForContext<ScrcpyManager>();
        private readonly Dictionary<DeviceData, int> devicePorts = new();
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
