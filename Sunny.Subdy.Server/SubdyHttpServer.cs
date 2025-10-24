using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using System.Web;
using AutoAndroid;
using Sunny.Subdy.Common;
using Sunny.Subdy.Common.Logs;
using Sunny.Subdy.Common.Services;
using Sunny.Subdy.Data.Models;

namespace Sunny.Subdy.Server
{
    public class SubdyHttpServer
    {
        private HttpListener _listener;
        private bool _isRunning = false;
        private const string Port = "8686";
        private const string ServerUrl = "http://localhost:" + Port + "/";
        private ApiRouter _router;
        public SubdyHttpServer()
        {
            _router = new ApiRouter();
            RegisterRoutes(); // Đăng ký tất cả các API endpoint
        }

        /// <summary>
        /// Bắt đầu lắng nghe các yêu cầu HTTP.
        /// </summary>
        public async Task StartServer()
        {
            if (_isRunning)
            {
                LogManager.Info("Server is already running.");
                return;
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add(ServerUrl);

            try
            {
                _listener.Start();
                _isRunning = true;
                LogManager.Info($"Listening for requests on {ServerUrl}");

                // Chạy vòng lặp lắng nghe yêu cầu trong một Task riêng
                _ = Task.Run(async () =>
                {
                    while (_isRunning)
                    {
                        HttpListenerContext context = null;
                        try
                        {
                            context = await _listener.GetContextAsync();
                            await _router.RouteRequest(context);
                        }
                        catch (HttpListenerException ex)
                        {
                            if (ex.ErrorCode == 995)
                            {
                                LogManager.Info("HttpListener was closed gracefully.");
                            }
                            else
                            {
                                LogManager.Error(ex);
                            }
                            break;
                        }
                        catch (ObjectDisposedException ex)
                        {
                            LogManager.Info("HttpListener object disposed gracefully.");
                            break;
                        }
                        catch (Exception ex)
                        {
                            LogManager.Error(ex);

                            if (context != null) // Đảm bảo context không null
                            {
                                try
                                {
                                    await ApiRouter.SendJsonResponse(context.Response, ApiResponse<object>.ErrorResponse($"Unhandled server error: {ex.Message}"), HttpStatusCode.InternalServerError);
                                }
                                catch (Exception innerEx) // THÊM LOG Ở ĐÂY
                                {
                                    LogManager.Error(innerEx);
                                }
                                finally
                                {
                                    try { context.Response.Close(); } catch { }
                                }
                            }
                        }
                    }
                });

            }
            catch (HttpListenerException ex)
            {
                LogManager.Error(ex);
                PortKiller.KillPort(8686);
                if (ex.ErrorCode == 5) // Access Denied
                {

                }


            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
            }
        }

        /// <summary>
        /// Dừng lắng nghe các yêu cầu HTTP.
        /// </summary>
        public void StopServer()
        {
            if (_isRunning)
            {
                _isRunning = false;
                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                    LogManager.Info("HTTP Server stopped.");
                }
                _listener = null;
            }
        }

        /// <summary>
        /// Đăng ký tất cả các API endpoint và các hàm xử lý tương ứng.
        /// </summary>
        private async Task RegisterRoutes()
        {
            // Route cho đường dẫn gốc
            _router.Get("/", HandleRootRequest);

            // Các route cho API Devices
            _router.Get("/devices", GetDevices); // Đã đổi tên route

            // Route cho yêu cầu thay đổi thiết bị theo ID (ID dạng chuỗi)
            _router.Get("/{id}/Change", HandleChangeRequest); // VẪN DÙNG HandleChangeRequest



            _router.Post("/{id}/facebook/backup", HandleFacebookBackup);
            _router.Post("/{id}/facebook/restore", HandleFacebookRestore);

            _router.Post("/{id}/instagram/backup", HandleInstagramBackup);
            _router.Post("/{id}/instagram/restore", HandleInstagramRestore);

            _router.Post("/{id}/tiktok/backup", HandleTikTokBackup);
            _router.Post("/{id}/tiktok/restore", HandleTikTokRestore);

            LogManager.Info("All API routes registered.");
        }

        /// <summary>
        /// Xử lý yêu cầu GET tới "/{id}/Change".
        /// </summary>
        private async Task HandleChangeRequest(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("Handling /{id}/Change request.");

            if (!routeParams.TryGetValue("id", out string idValue))
            {
                await ApiRouter.SendJsonResponse(
                    context.Response,
                    ApiResponse<object>.ErrorResponse("ID parameter not found in URL."),
                    HttpStatusCode.BadRequest
                );
                return;
            }

            LogManager.Info($"Extracted ID: {idValue}");

            try
            {
                var client = new ADBClient(idValue);
                client.Connect();

                if (!await client.ChangInfo("", false, "samsung", "Random"))
                {
                    await ApiRouter.SendJsonResponse(
                        context.Response,
                        ApiResponse<object>.ErrorResponse("Thay đổi thông tin thiết bị thất bại!"),
                        HttpStatusCode.InternalServerError
                    );
                    return;
                }

                await Task.Delay(3000); // Chờ thiết bị cập nhật

                var deviceName = client.GetDeviceName();
                if (string.IsNullOrEmpty(deviceName))
                {
                    await ApiRouter.SendJsonResponse(
                        context.Response,
                        ApiResponse<object>.ErrorResponse("Thay đổi thiết bị thất bại! (Tên thiết bị trống)"),
                        HttpStatusCode.InternalServerError
                    );
                    return;
                }

                // Trả về thông tin thiết bị (tùy chỉnh thêm nếu cần)
                var responseData = new DeviceRespone
                {
                    Serial = idValue,
                    NameDevice = client.Device?.NameDevice,
                    Status = deviceName,
                    OS = client.Device?.OS ?? "Unknown"
                };

                await JsonSerializer.SerializeAsync(
                    context.Response.OutputStream,
                    ApiResponse<DeviceRespone>.SuccessResponse(responseData, "Thay đổi thông tin thiết bị thành công!"),
                    MyJsonContext.Default.ApiResponseDeviceRespone
                );
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(
                    context.Response,
                    ApiResponse<object>.ErrorResponse($"Lỗi xử lý yêu cầu thay đổi thiết bị: {ex.Message}"),
                    HttpStatusCode.InternalServerError
                );
            }
        }

        private async Task HandleRootRequest(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("Handling GET / request.");
            await ApiRouter.SendJsonResponse(context.Response, ApiResponse<string>.SuccessResponse("Welcome to the C# HTTP Server!", "Server is running."));
        }

        /// <summary>
        /// Xử lý yêu cầu GET tới "/devices" để lấy danh sách thiết bị.
        /// </summary>
        private async Task GetDevices(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("Handling GET /devices request.");
            var devices = new List<DeviceModel>();

            try
            {
                var lines = ADBHelper.GetDevices(); // Giả định ADBHelper.GetDevices trả về IEnumerable<string>
                if (lines != null && lines.Any())
                {
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        var client = new ADBClient(line.Trim()); // trim để loại bỏ khoảng trắng dư thừa
                        devices.Add(client.Device); // Giả định client.Device là một DeviceModel
                    }

                }
                var selectedDevices = devices.Select(d => new DeviceRespone
                {
                    Serial = d.Serial, // Ánh xạ Id của DeviceModel sang Serial
                    NameDevice = d.NameDevice,
                    OS = d.OS // Đảm bảo DeviceModel có thuộc tính OS
                }).ToList();
                await JsonSerializer.SerializeAsync(context.Response.OutputStream,
     ApiResponse<List<DeviceRespone>>.SuccessResponse(selectedDevices, "Danh sách thiết bị đã lấy thành công."),
     MyJsonContext.Default.ApiResponseListDeviceRespone);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(context.Response, ApiResponse<object>.ErrorResponse($"Lỗi khi lấy danh sách thiết bị: {ex.Message}"), HttpStatusCode.InternalServerError);
            }
        }
        private async Task HandleFacebookBackup(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("[HandleFacebookBackup] Bắt đầu xử lý");

            try
            {
                string id = routeParams.ContainsKey("id") ? routeParams["id"] : "(null)";
                LogManager.Info($"[HandleFacebookBackup] ID: {id}");

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                LogManager.Info($"[HandleFacebookBackup] Raw Body: {body}");

                var device = DeviceServices.DeviceModels.FirstOrDefault(d => d.Serial == id);
                if (device == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Thiết bị không tồn tại."), HttpStatusCode.NotFound);
                    return;
                }
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<DataRequest>(body, jsonOptions);

                if (request == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Lỗi parse JSON."), HttpStatusCode.BadRequest);
                    return;
                }
                if (new BackupRestoreHelper(device).BackupFacebook(request.File) == false)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Backup facebook thất bại"), HttpStatusCode.InternalServerError);
                    return;
                }

                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<string>.SuccessResponse(null, "Backup facebook thành công."), HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<object>.ErrorResponse(ex.Message), HttpStatusCode.InternalServerError);
            }
        }
        private async Task HandleFacebookRestore(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("[HandleFacebookRestore] Bắt đầu xử lý");

            try
            {
                string id = routeParams.ContainsKey("id") ? routeParams["id"] : "(null)";
                LogManager.Info($"[HandleFacebookRestore] ID: {id}");

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                LogManager.Info($"[HandleFacebookRestore] Raw Body: {body}");

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<DataRequest>(body, jsonOptions);

                if (request == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Lỗi parse JSON."), HttpStatusCode.BadRequest);
                    return;
                }
                if (!File.Exists(request.File))
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                      ApiResponse<object>.ErrorResponse("Không tồn tại file"), HttpStatusCode.BadRequest);
                    return;
                }
                var device = DeviceServices.DeviceModels.FirstOrDefault(d => d.Serial == id);
                if (device == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Thiết bị không tồn tại."), HttpStatusCode.NotFound);
                    return;
                }
                if (new BackupRestoreHelper(device).RestoreFacebook(request.File) == false)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Restore facebook thất bại"), HttpStatusCode.InternalServerError);
                    return;
                }

                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<string>.SuccessResponse(null, "Restore facebook thành công."), HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<object>.ErrorResponse(ex.Message), HttpStatusCode.InternalServerError);
            }
        }

        private async Task HandleInstagramBackup(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("[HandleInstagramBackup] Bắt đầu xử lý");

            try
            {
                string id = routeParams.ContainsKey("id") ? routeParams["id"] : "(null)";
                LogManager.Info($"[HandleInstagramBackup] ID: {id}");

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                LogManager.Info($"[HandleInstagramBackup] Raw Body: {body}");

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<DataRequest>(body, jsonOptions);
                var device = DeviceServices.DeviceModels.FirstOrDefault(d => d.Serial == id);
                if (device == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Thiết bị không tồn tại."), HttpStatusCode.NotFound);
                    return;
                }
                if (request == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Lỗi parse JSON."), HttpStatusCode.BadRequest);
                    return;
                }
                if (new BackupRestoreHelper(device).BackupInstagram(request.File) == false)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Backup instagram thất bại"), HttpStatusCode.InternalServerError);
                    return;
                }

                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<string>.SuccessResponse(null, "Backup instagram thành công."), HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<object>.ErrorResponse(ex.Message), HttpStatusCode.InternalServerError);
            }
        }
        private async Task HandleInstagramRestore(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("[HandleInstagramRestore] Bắt đầu xử lý");

            try
            {
                string id = routeParams.ContainsKey("id") ? routeParams["id"] : "(null)";
                LogManager.Info($"[HandleInstagramRestore] ID: {id}");

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                LogManager.Info($"[HandleInstagramRestore] Raw Body: {body}");

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<DataRequest>(body, jsonOptions);

                if (request == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Lỗi parse JSON."), HttpStatusCode.BadRequest);
                    return;
                }
                if (!File.Exists(request.File))
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                      ApiResponse<object>.ErrorResponse("Không tồn tại file"), HttpStatusCode.BadRequest);
                    return;
                }
                var device = DeviceServices.DeviceModels.FirstOrDefault(d => d.Serial == id);
                if (device == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Thiết bị không tồn tại."), HttpStatusCode.NotFound);
                    return;
                }
                if (await new BackupRestoreHelper(device).RestoreInstagram(request.File) == false)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Restore instagram thất bại"), HttpStatusCode.InternalServerError);
                    return;
                }

                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<string>.SuccessResponse(null, "Restore instagram thành công."), HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<object>.ErrorResponse(ex.Message), HttpStatusCode.InternalServerError);
            }
        }

        private async Task HandleTikTokBackup(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("[HandleTikTokBackup] Bắt đầu xử lý");

            try
            {
                string id = routeParams.ContainsKey("id") ? routeParams["id"] : "(null)";
                LogManager.Info($"[HandleTikTokBackup] ID: {id}");

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                LogManager.Info($"[HandleTikTokBackup] Raw Body: {body}");

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<DataRequest>(body, jsonOptions);

                if (request == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Lỗi parse JSON."), HttpStatusCode.BadRequest);
                    return;
                }
                var device = DeviceServices.DeviceModels.FirstOrDefault(d => d.Serial == id);
                if (device == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Thiết bị không tồn tại."), HttpStatusCode.NotFound);
                    return;
                }
                if (await new BackupRestoreHelper(device).BackupTikTok(request.File) == false)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Backup tiktok thất bại"), HttpStatusCode.InternalServerError);
                    return;
                }

                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<string>.SuccessResponse(null, "Backup tiktok thành công."), HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<object>.ErrorResponse(ex.Message), HttpStatusCode.InternalServerError);
            }
        }
        private async Task HandleTikTokRestore(HttpListenerContext context, Dictionary<string, string> routeParams)
        {
            LogManager.Info("[HandleTikTokRestore] Bắt đầu xử lý");

            try
            {
                string id = routeParams.ContainsKey("id") ? routeParams["id"] : "(null)";
                LogManager.Info($"[HandleTikTokRestore] ID: {id}");

                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                LogManager.Info($"[HandleTikTokRestore] Raw Body: {body}");

                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var request = JsonSerializer.Deserialize<DataRequest>(body, jsonOptions);

                if (request == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Lỗi parse JSON."), HttpStatusCode.BadRequest);
                    return;
                }
                if (!File.Exists(request.File))
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                      ApiResponse<object>.ErrorResponse("Không tồn tại file"), HttpStatusCode.BadRequest);
                    return;
                }
                var device = DeviceServices.DeviceModels.FirstOrDefault(d => d.Serial == id);
                if (device == null)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Thiết bị không tồn tại."), HttpStatusCode.NotFound);
                    return;
                }
                if (await new BackupRestoreHelper(device).RestoreTikTok(request.File) == false)
                {
                    await ApiRouter.SendJsonResponse(context.Response,
                        ApiResponse<object>.ErrorResponse("Restore tiktok thất bại"), HttpStatusCode.InternalServerError);
                    return;
                }

                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<string>.SuccessResponse(null, "Restore tiktok thành công."), HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                LogManager.Error(ex);
                await ApiRouter.SendJsonResponse(context.Response,
                    ApiResponse<object>.ErrorResponse(ex.Message), HttpStatusCode.InternalServerError);
            }
        }
    }
}
