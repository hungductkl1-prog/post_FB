using Sunny.Subdy.Common.API;
using Sunny.Subdy.Common.API.Jobs;
using Sunny.Subdy.Common.ControlMethod;
using Sunny.Subdy.Common.Logs;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sunny.Subdy.Common.Helper
{
    public class ControlHelper
    {
        public static List<Control> GetControls(Control control)
        {
            List<Control> controls = new List<Control>();
            foreach (Control c in control.Controls)
            {
                controls.Add(c);
                if (c.Controls.Count > 0)
                {
                    controls.AddRange(GetControls(c));
                }
            }
            return controls;
        }
        public static void LoadFormatFromFile(string FormatFileName, List<ComboBox> cbxs)
        {
            try
            {
                string FormatFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", FormatFileName);
                if (File.Exists(FormatFilePath))
                {
                    string formattedString = File.ReadAllText(FormatFilePath);
                    string[] listFormats = formattedString.Split('|');

                    for (int i = 0; i < Math.Min(listFormats.Length, cbxs.Count); i++)
                    {
                        try
                        {
                            cbxs[i].Text = listFormats[i];
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
        }
        public static void SaveFormatToFile(string FormatFileName, List<ComboBox> cbxs)
        {
            try
            {
                List<string> listFormats = cbxs.Select(cbx => cbx.Text).ToList();
                string formattedString = string.Join("|", listFormats);
                string FormatFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs", FormatFileName);
                if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs")))
                {
                    Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs"));
                }
                File.WriteAllText(FormatFilePath, formattedString);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving format: {ex.Message}");
            }
        }
        public static void LoadConfigColums(DataGridView dgv, List<string> listHide)
        {
            var configFile = $"configs\\{dgv.Name}.txt";
            Dictionary<string, bool> configLines = new();

            if (File.Exists(configFile))
            {
                configLines = File.ReadAllLines(configFile)
                                  .Where(line => !string.IsNullOrWhiteSpace(line) && line.Contains("|"))
                                  .Select(line => line.Split('|'))
                                  .ToDictionary(
                                      parts => parts[0].Trim(),
                                      parts => parts[1].Trim().ToLower() == "true"
                                  );
            }

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                // Mặc định là true nếu không có trong config
                bool visible = configLines.TryGetValue(col.HeaderText.Trim(), out bool value) ? value : true;
                col.Visible = visible;
                if (listHide.Contains(col.HeaderText))
                {
                    col.Visible = false;
                }
            }
        }
        public static void SetToolStripMenuItemTextSafe(ToolStripMenuItem label, string text)
        {
            try
            {
                if (label.GetCurrentParent()?.InvokeRequired ?? false)
                {
                    label.GetCurrentParent()?.BeginInvoke(new Action(() => label.Text = text));
                }
                else
                {
                    label.Text = text;
                }
                label.GetCurrentParent()?.Refresh();
            }
            catch
            {
            }

        }
        public static void SetToolStripLabelTextSafe(ToolStripLabel label, string text)
        {
            try
            {
                if (label.GetCurrentParent()?.InvokeRequired ?? false)
                {
                    label.GetCurrentParent()?.BeginInvoke(new Action(() => label.Text = text));
                }
                else
                {
                    label.Text = text;
                }
                label.GetCurrentParent()?.Refresh();
            }
            catch
            {
            }

        }
        public static void SetLabelTextSafe(Label label, string text)
        {
            if (label == null || label.IsDisposed) return;

            try
            {
                if (label.InvokeRequired) // đúng cách kiểm tra thread
                {
                    label.BeginInvoke(new Action(() =>
                    {
                        label.Text = text;
                        label.Refresh();
                    }));
                }
                else
                {
                    label.Text = text;
                    label.Refresh();
                }
            }
            catch
            {
                // ignore
            }

        }
    }
}
