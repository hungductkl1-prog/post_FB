using Facebook_Farm_NewFeed_PostStory.Utils;
using Sunny.Subdy.Common.Helper;
using System.Diagnostics;
using System.Text;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms
{
    public partial class fUpdateAuto : Form
    {
        private string _link;
        private string _zipPath;
        private string _updateFolder;
        private string _version;

        public fUpdateAuto(string link, string version)
        {
            InitializeComponent();
            _link = link;
            _version = version;
            _updateFolder = Path.Combine(AppContext.BaseDirectory, "UpdateTemp");
            _zipPath = Path.Combine(_updateFolder, "update.zip");
            this.Load += FUpdate_Load;
            FontUtil.ApplyFontToAllControls(this);
        }

        private void FUpdate_Load(object? sender, EventArgs e)
        {
            _ = StartUpdateProcessAsync();
        }

        private async Task StartUpdateProcessAsync()
        {
            try
            {
                SetProgress(0, "Đang tải bản cập nhật...");
                Directory.CreateDirectory(_updateFolder);

                await DownloadFileAsync(_link, _zipPath).ConfigureAwait(false);

                // back to UI thread to update text briefly
                this.Invoke(() => divider1.Text = "Chuẩn bị cập nhật...");
                await Task.Delay(500).ConfigureAwait(false);

                CreateUpdateBatAndRestart();
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                // marshal to UI to show message
                if (this.IsHandleCreated)
                    this.Invoke(() => MessageBox.Show("Lỗi cập nhật: " + ex.Message));
                else
                    MessageBox.Show("Lỗi cập nhật: " + ex.Message);
            }
        }

        private async Task DownloadFileAsync(string url, string destinationPath)
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, useAsync: true);

            byte[] buffer = new byte[8192];
            long totalRead = 0;
            int read;
            int lastPercent = -1;
            var sw = Stopwatch.StartNew();

            if (totalBytes <= 0)
            {
                // unknown size -> indeterminate
                SetProgress(-1, "Đang tải...");
            }
            else
            {
                SetProgress(0, "Đang tải... 0%");
            }

            while ((read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length)).ConfigureAwait(false)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read)).ConfigureAwait(false);
                totalRead += read;

                if (totalBytes > 0)
                {
                    int percent = (int)(totalRead * 100 / totalBytes);
                    // throttle updates: update when percent changed and either at least 1% or 500ms elapsed
                    if (percent != lastPercent && (percent - lastPercent >= 1 || sw.ElapsedMilliseconds > 500))
                    {
                        sw.Restart();
                        lastPercent = percent;
                        SetProgress(percent, $"Đang tải... {percent}%");
                    }
                }
            }

            // finalize
            SetProgress(100, "Tải xong");
        }

        private void SetProgress(int percent, string text = null)
        {
            if (progress5.InvokeRequired || divider1.InvokeRequired)
            {
                try
                {
                    this.Invoke(() => SetProgress(percent, text));
                }
                catch
                {
                    // if invoke fails (form closing), ignore
                }
                return;
            }

            try
            {
                if (percent < 0)
                {
                    progress5.Loading = true; // Set the 'Loading' property to true for indeterminate progress
                }
                else
                {
                    progress5.Loading = false; // Set the 'Loading' property to false for determinate progress
                    progress5.Value = Math.Clamp(percent, 0, 100);
                }

                if (!string.IsNullOrEmpty(text))
                    divider1.Text = text;
            }
            catch
            {
                // ignore UI update errors (e.g., control disposed)
            }
        }

        private void CreateUpdateBatAndRestart()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string folderPath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
                string exeName = Path.GetFileName(exePath);
                string renamedExe = Path.GetFileNameWithoutExtension(exeName) + "-" + _version + ".exe";
                string newExeName = exeName; // keep same exe name after update
                string batPath = Path.Combine(Path.GetTempPath(), $"update_{Guid.NewGuid():N}.bat");

                // Use single-quoted PowerShell literals to preserve backslashes and spaces
                string batContent =
$"@echo off\r\n" +
$"cd /d \"{folderPath}\"\r\n" +
$"timeout /t 2 >nul\r\n\r\n" +
$"rem Rename old exe (if exists)\r\n" +
$"if exist \"{exeName}\" (\r\n" +
$"    rename \"{exeName}\" \"{renamedExe}\"\r\n" +
$")\r\n\r\n" +
$"rem Extract update.zip into application folder\r\n" +
$"powershell -NoProfile -ExecutionPolicy Bypass -Command \"Expand-Archive -LiteralPath '{_zipPath}' -DestinationPath '{folderPath}' -Force\" \r\n\r\n" +
$"rem Log before running\r\n" +
$"echo Running new exe >> %TEMP%\\update_log.txt\r\n\r\n" +
$"rem Start new exe elevated (will prompt UAC)\r\n" +
$"powershell -NoProfile -ExecutionPolicy Bypass -Command \"Start-Process -FilePath '{newExeName}' -Verb RunAs\" \r\n\r\n" +
$"rem Cleanup update folder\r\n" +
$"if exist \"{_updateFolder}\" rd /s /q \"{_updateFolder}\"\r\n\r\n" +
$"rem Delete this batch file\r\n" +
$"del \"%~f0\"\r\n";

                // Write UTF8 without BOM to avoid BOM issues with cmd while preserving Unicode paths
                File.WriteAllText(batPath, batContent, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

                var psi = new ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                if (this.IsHandleCreated)
                    this.Invoke(() => MessageBox.Show("Không thể tạo/khởi chạy tệp cập nhật: " + ex.Message));
                else
                    MessageBox.Show("Không thể tạo/khởi chạy tệp cập nhật: " + ex.Message);
            }
        }
    }
}
