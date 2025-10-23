using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sunny.Subdy.Server
{
    public static class PortKiller
    {
        public static void KillPort(int port)
        {
            try
            {
                Console.WriteLine($"🔍 Đang kiểm tra cổng {port}...");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Tìm PID từ dòng có port
                    string pattern = $@"\s+TCP\s+\S+:{port}\s+\S+\s+LISTENING\s+(\d+)";
                    var match = Regex.Match(output, pattern);
                    if (match.Success)
                    {
                        int pid = int.Parse(match.Groups[1].Value);
                        Process.Start("taskkill", $"/PID {pid} /F")?.WaitForExit();
                    }
                    else
                    {
                    }
                }
            }
            catch
            {
            }
        }
    }
}
