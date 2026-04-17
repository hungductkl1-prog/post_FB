using AntdUI;
using System.Runtime.InteropServices;

namespace SubdyAutoUpdate;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        AppContext.SetSwitch("System.Windows.Forms.Binding.IsSupported", true);

        ComWrappers.RegisterForMarshalling(WinFormsComInterop.WinFormsComWrappers.Instance);
        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        Application.Run(new MainForm());
    }
}
