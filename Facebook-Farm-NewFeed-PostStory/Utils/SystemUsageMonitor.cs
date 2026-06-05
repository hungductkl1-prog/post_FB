using Microsoft.VisualBasic.Devices;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Facebook_Farm_NewFeed_PostStory.Utils
{
    public class SystemUsageMonitor
    {
        public static string Ram = "0%";

        // Ngưỡng cảnh báo + cooldown để không spam toast.
        public const float CpuWarnThreshold = 85f;
        public const float RamWarnThreshold = 90f;
        public const int SustainedSeconds = 30;     // phải vượt liên tục 30s mới cảnh báo
        public const int CooldownSeconds = 300;     // 5 phút giữa hai lần cảnh báo

        private static DateTime? _cpuOverThresholdSince;
        private static DateTime? _ramOverThresholdSince;
        private static DateTime _lastCpuAlert = DateTime.MinValue;
        private static DateTime _lastRamAlert = DateTime.MinValue;

        public static event Action<string, float>? OverloadDetected;

        public static async Task<float> GetCpuUsage()
        {
            using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"))
            {
                cpuCounter.NextValue();
                await Task.Delay(500);
                return cpuCounter.NextValue();
            }
        }

        public static float GetRamUsage()
        {
            using (var ramCounter = new PerformanceCounter("Memory", "Available MBytes"))
            {
                float availableMb = ramCounter.NextValue();
                float totalMb = GetTotalRamInMb();
                return 100f - (availableMb / totalMb * 100f);
            }
        }

        private static float GetTotalRamInMb()
        {
            var computerInfo = new ComputerInfo();
            return computerInfo.TotalPhysicalMemory / (1024f * 1024f);
        }

        /// <summary>
        /// Gọi mỗi lần cập nhật CPU/RAM. Nếu vượt ngưỡng liên tục SustainedSeconds
        /// và đã qua CooldownSeconds kể từ lần cảnh báo gần nhất, raise event
        /// <see cref="OverloadDetected"/> để UI hiện toast.
        /// </summary>
        public static void CheckOverload(float cpu, float ram)
        {
            var now = DateTime.UtcNow;

            if (cpu >= CpuWarnThreshold)
            {
                _cpuOverThresholdSince ??= now;
                bool sustained = (now - _cpuOverThresholdSince.Value).TotalSeconds >= SustainedSeconds;
                bool cooledDown = (now - _lastCpuAlert).TotalSeconds >= CooldownSeconds;
                if (sustained && cooledDown)
                {
                    _lastCpuAlert = now;
                    OverloadDetected?.Invoke("CPU", cpu);
                }
            }
            else
            {
                _cpuOverThresholdSince = null;
            }

            if (ram >= RamWarnThreshold)
            {
                _ramOverThresholdSince ??= now;
                bool sustained = (now - _ramOverThresholdSince.Value).TotalSeconds >= SustainedSeconds;
                bool cooledDown = (now - _lastRamAlert).TotalSeconds >= CooldownSeconds;
                if (sustained && cooledDown)
                {
                    _lastRamAlert = now;
                    OverloadDetected?.Invoke("RAM", ram);
                }
            }
            else
            {
                _ramOverThresholdSince = null;
            }
        }
    }
}
