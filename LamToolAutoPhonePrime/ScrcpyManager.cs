using ScrcpyNet;
using Serilog;
using SharpAdbClient;
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

        public Scrcpy StartForDevice(DeviceData device, int port)
        {
            if (instances.ContainsKey(device.Serial))
            {
                log.Warning("[{Serial}] Already running.", device.Serial);
                return instances[device.Serial];
            }

            var scrcpy = new Scrcpy(device, port);
            scrcpy.Start();
            instances[device.Serial] = scrcpy;
            log.Information("[{Serial}] Scrcpy started.", device.Serial);
            return scrcpy;
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
