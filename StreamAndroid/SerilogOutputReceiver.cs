using Serilog;
using SharpAdbClient;
using System.Collections.Generic;

namespace StreamAndroid
{
    public class SerilogOutputReceiver : MultiLineReceiver
    {
        protected override void ProcessNewLines(IEnumerable<string> lines)
        {
            foreach (var line in lines)
                Log.Information("[server] {@LogLine}", line);
        }
    }
}