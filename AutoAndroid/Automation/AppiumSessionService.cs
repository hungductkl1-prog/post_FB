using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AutoAndroid
{
    public static class AppiumSessionService
    {
        private const string ServerHost = "127.0.0.1";
        private const int ServerPort = 4723;
        private static readonly Uri ServerRootUri = new Uri($"http://{ServerHost}:{ServerPort}");
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly object ServerLock = new object();
        private static readonly ConcurrentDictionary<string, SessionState> DeviceSessions = new ConcurrentDictionary<string, SessionState>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, object> DeviceLocks = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        private static Process? AppiumServerProcess;
        private static string BasePath = string.Empty;

        private sealed class SessionState
        {
            public SessionState(string sessionId, int systemPort)
            {
                SessionId = sessionId;
                SystemPort = systemPort;
            }

            public string SessionId { get; set; }
            public int SystemPort { get; }
        }

        public static bool EnsureSession(ADBClient client)
        {
            if (client?.Device == null || string.IsNullOrWhiteSpace(client.Device.Serial))
            {
                return false;
            }

            string serial = client.Device.Serial;
            object deviceGate = DeviceLocks.GetOrAdd(serial, _ => new object());
            lock (deviceGate)
            {
                if (!EnsureServerReady(client))
                {
                    return false;
                }

                if (TryGetSession(serial, out SessionState? currentSession)
                    && currentSession != null
                    && IsSessionAlive(currentSession.SessionId))
                {
                    return true;
                }

                DeviceSessions.TryRemove(serial, out _);

                int systemPort = ComputeSystemPort(serial);
                string? sessionId = CreateSession(client, serial, systemPort);
                if (string.IsNullOrWhiteSpace(sessionId))
                {
                    return false;
                }

                DeviceSessions[serial] = new SessionState(sessionId, systemPort);
                return true;
            }
        }

        public static string GetPageSource(ADBClient client)
        {
            if (!EnsureSession(client))
            {
                return string.Empty;
            }

            string serial = client.Device.Serial;
            return ExecuteWithSessionRetry(client, serial, sessionId =>
            {
                var response = SendJson(HttpMethod.Post, $"/session/{sessionId}/source", "{}");
                if (response.IsInvalidSession)
                {
                    return CommandResult<string>.Invalid();
                }

                if (!response.Success)
                {
                    client.LogHelper.Log($"Appium /source fail: {response.Content}");
                    return CommandResult<string>.Fail(string.Empty);
                }

                string source = ReadValueAsString(response.Content);
                return CommandResult<string>.Ok(source ?? string.Empty);
            });
        }

        public static bool Tap(ADBClient client, int x, int y)
        {
            if (!EnsureSession(client))
            {
                return false;
            }

            string serial = client.Device.Serial;
            return ExecuteWithSessionRetry(client, serial, sessionId =>
            {
                string payload = JsonSerializer.Serialize(new
                {
                    actions = new object[]
                    {
                        new
                        {
                            type = "pointer",
                            id = "finger1",
                            parameters = new { pointerType = "touch" },
                            actions = new object[]
                            {
                                new { type = "pointerMove", duration = 0, x, y },
                                new { type = "pointerDown", button = 0 },
                                new { type = "pause", duration = 40 },
                                new { type = "pointerUp", button = 0 }
                            }
                        }
                    }
                });

                var response = SendJson(HttpMethod.Post, $"/session/{sessionId}/actions", payload);
                if (response.IsInvalidSession)
                {
                    return CommandResult<bool>.Invalid();
                }

                ReleaseActions(sessionId);
                if (!response.Success)
                {
                    client.LogHelper.Log($"Appium tap fail: {response.Content}");
                }

                return CommandResult<bool>.Ok(response.Success);
            });
        }

        public static bool LongPress(ADBClient client, int x, int y, int durationMs)
        {
            if (!EnsureSession(client))
            {
                return false;
            }

            int holdMs = Math.Max(80, durationMs);
            string serial = client.Device.Serial;
            return ExecuteWithSessionRetry(client, serial, sessionId =>
            {
                string payload = JsonSerializer.Serialize(new
                {
                    actions = new object[]
                    {
                        new
                        {
                            type = "pointer",
                            id = "finger1",
                            parameters = new { pointerType = "touch" },
                            actions = new object[]
                            {
                                new { type = "pointerMove", duration = 0, x, y },
                                new { type = "pointerDown", button = 0 },
                                new { type = "pause", duration = holdMs },
                                new { type = "pointerUp", button = 0 }
                            }
                        }
                    }
                });

                var response = SendJson(HttpMethod.Post, $"/session/{sessionId}/actions", payload);
                if (response.IsInvalidSession)
                {
                    return CommandResult<bool>.Invalid();
                }

                ReleaseActions(sessionId);
                if (!response.Success)
                {
                    client.LogHelper.Log($"Appium long-press fail: {response.Content}");
                }

                return CommandResult<bool>.Ok(response.Success);
            });
        }

        public static bool Swipe(ADBClient client, int startX, int startY, int endX, int endY, int durationMs)
        {
            if (!EnsureSession(client))
            {
                return false;
            }

            int moveDuration = Math.Max(30, durationMs);
            string serial = client.Device.Serial;
            return ExecuteWithSessionRetry(client, serial, sessionId =>
            {
                string payload = JsonSerializer.Serialize(new
                {
                    actions = new object[]
                    {
                        new
                        {
                            type = "pointer",
                            id = "finger1",
                            parameters = new { pointerType = "touch" },
                            actions = new object[]
                            {
                                new { type = "pointerMove", duration = 0, x = startX, y = startY },
                                new { type = "pointerDown", button = 0 },
                                new { type = "pause", duration = 40 },
                                new { type = "pointerMove", duration = moveDuration, x = endX, y = endY },
                                new { type = "pointerUp", button = 0 }
                            }
                        }
                    }
                });

                var response = SendJson(HttpMethod.Post, $"/session/{sessionId}/actions", payload);
                if (response.IsInvalidSession)
                {
                    return CommandResult<bool>.Invalid();
                }

                ReleaseActions(sessionId);
                if (!response.Success)
                {
                    client.LogHelper.Log($"Appium swipe fail: {response.Content}");
                }

                return CommandResult<bool>.Ok(response.Success);
            });
        }

        public static Bitmap? Screenshot(ADBClient client)
        {
            if (!EnsureSession(client))
            {
                return null;
            }

            string serial = client.Device.Serial;
            return ExecuteWithSessionRetry(client, serial, sessionId =>
            {
                var response = SendJson(HttpMethod.Post, $"/session/{sessionId}/screenshot", "{}");
                if (response.IsInvalidSession)
                {
                    return CommandResult<Bitmap?>.Invalid();
                }

                if (!response.Success)
                {
                    client.LogHelper.Log($"Appium screenshot fail: {response.Content}");
                    return CommandResult<Bitmap?>.Fail(null);
                }

                string base64 = ReadValueAsString(response.Content) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(base64))
                {
                    return CommandResult<Bitmap?>.Fail(null);
                }

                try
                {
                    byte[] bytes = Convert.FromBase64String(base64);
                    using MemoryStream ms = new MemoryStream(bytes);
                    using Bitmap temp = new Bitmap(ms);
                    return CommandResult<Bitmap?>.Ok(new Bitmap(temp));
                }
                catch
                {
                    return CommandResult<Bitmap?>.Fail(null);
                }
            });
        }

        public static void InvalidateSession(string serial)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                return;
            }

            DeviceSessions.TryRemove(serial, out _);
        }

        private static bool EnsureServerReady(ADBClient client)
        {
            if (IsServerAlive())
            {
                return true;
            }

            lock (ServerLock)
            {
                if (IsServerAlive())
                {
                    return true;
                }

                try
                {
                    if (AppiumServerProcess == null || AppiumServerProcess.HasExited)
                    {
                        StartAppiumServerProcess();
                    }

                    Stopwatch sw = Stopwatch.StartNew();
                    while (sw.Elapsed < TimeSpan.FromSeconds(30))
                    {
                        if (IsServerAlive())
                        {
                            return true;
                        }

                        Thread.Sleep(500);
                    }
                }
                catch (Exception ex)
                {
                    client.LogHelper.Log($"Start appium server lỗi: {ex.Message}");
                    return false;
                }
            }

            client.LogHelper.Log("Appium server không phản hồi /status.");
            return false;
        }

        private static void StartAppiumServerProcess()
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C appium --address {ServerHost} --port {ServerPort} --log-level error",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            Process process = new Process
            {
                StartInfo = info
            };
            process.OutputDataReceived += (_, __) => { };
            process.ErrorDataReceived += (_, __) => { };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            AppiumServerProcess = process;
        }

        private static bool IsServerAlive()
        {
            if (ProbeStatus(string.Empty))
            {
                BasePath = string.Empty;
                return true;
            }

            if (ProbeStatus("/wd/hub"))
            {
                BasePath = "/wd/hub";
                return true;
            }

            return false;
        }

        private static bool ProbeStatus(string basePath)
        {
            var response = SendJsonDirect(HttpMethod.Get, $"{basePath}/status", null);
            if (!response.Success)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(response.Content))
            {
                return false;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(response.Content);
                if (doc.RootElement.TryGetProperty("value", out JsonElement value))
                {
                    if (value.ValueKind == JsonValueKind.Object
                        && value.TryGetProperty("ready", out JsonElement ready)
                        && ready.ValueKind == JsonValueKind.True)
                    {
                        return true;
                    }
                }
            }
            catch
            {
            }

            // Nhiều bản appium vẫn trả 200 mà không có "ready=true".
            return true;
        }

        private static bool IsSessionAlive(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return false;
            }

            var response = SendJson(HttpMethod.Get, $"/session/{sessionId}", null);
            if (!response.Success)
            {
                return false;
            }

            if (response.IsInvalidSession)
            {
                return false;
            }

            return true;
        }

        private static string? CreateSession(ADBClient client, string serial, int systemPort)
        {
            var payload = new
            {
                capabilities = new
                {
                    alwaysMatch = new Dictionary<string, object>
                    {
                        ["platformName"] = "Android",
                        ["appium:automationName"] = "UiAutomator2",
                        ["appium:udid"] = serial,
                        ["appium:deviceName"] = serial,
                        ["appium:noReset"] = true,
                        ["appium:newCommandTimeout"] = 300,
                        ["appium:systemPort"] = systemPort
                    },
                    firstMatch = new object[] { new { } }
                }
            };

            string body = JsonSerializer.Serialize(payload);
            var response = SendJson(HttpMethod.Post, "/session", body);
            if (!response.Success)
            {
                client.LogHelper.Log($"Create appium session fail: {response.Content}");
                return null;
            }

            string? sessionId = ReadSessionId(response.Content);
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                client.LogHelper.Log($"Không parse được appium sessionId: {response.Content}");
                return null;
            }

            return sessionId;
        }

        private static void ReleaseActions(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }

            _ = SendJson(HttpMethod.Delete, $"/session/{sessionId}/actions", null);
        }

        private static bool TryGetSession(string serial, out SessionState? state)
        {
            if (DeviceSessions.TryGetValue(serial, out SessionState existing))
            {
                state = existing;
                return true;
            }

            state = null;
            return false;
        }

        private static int ComputeSystemPort(string serial)
        {
            int hash = Math.Abs(serial.GetHashCode(StringComparison.OrdinalIgnoreCase));
            return 8200 + (hash % 1000);
        }

        private static string GetUrl(string relativePath)
        {
            string prefix = BasePath;
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return $"{ServerRootUri}{relativePath}";
            }

            return $"{ServerRootUri}{prefix}{relativePath}";
        }

        private static HttpResult SendJson(HttpMethod method, string relativePath, string? jsonBody)
        {
            try
            {
                using HttpRequestMessage request = new HttpRequestMessage(method, GetUrl(relativePath));
                if (jsonBody != null)
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                using HttpResponseMessage response = Http.Send(request);
                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                bool success = response.IsSuccessStatusCode;
                bool invalidSession = IsInvalidSessionResponse(response.StatusCode, content);
                return new HttpResult(success, invalidSession, content);
            }
            catch (Exception ex)
            {
                return new HttpResult(false, false, ex.Message);
            }
        }

        private static HttpResult SendJsonDirect(HttpMethod method, string absolutePath, string? jsonBody)
        {
            try
            {
                string url = $"{ServerRootUri.ToString().TrimEnd('/')}{absolutePath}";
                using HttpRequestMessage request = new HttpRequestMessage(method, url);
                if (jsonBody != null)
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                using HttpResponseMessage response = Http.Send(request);
                string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                bool success = response.IsSuccessStatusCode;
                bool invalidSession = IsInvalidSessionResponse(response.StatusCode, content);
                return new HttpResult(success, invalidSession, content);
            }
            catch (Exception ex)
            {
                return new HttpResult(false, false, ex.Message);
            }
        }

        private static bool IsInvalidSessionResponse(HttpStatusCode statusCode, string content)
        {
            if (statusCode != HttpStatusCode.NotFound && statusCode != HttpStatusCode.InternalServerError)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            return content.Contains("invalid session id", StringComparison.OrdinalIgnoreCase)
                || content.Contains("no such driver", StringComparison.OrdinalIgnoreCase)
                || content.Contains("session does not exist", StringComparison.OrdinalIgnoreCase);
        }

        private static string? ReadSessionId(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(content);
                JsonElement root = doc.RootElement;
                if (root.TryGetProperty("sessionId", out JsonElement sessionRoot)
                    && sessionRoot.ValueKind == JsonValueKind.String)
                {
                    return sessionRoot.GetString();
                }

                if (!root.TryGetProperty("value", out JsonElement value))
                {
                    return null;
                }

                if (value.ValueKind == JsonValueKind.Object
                    && value.TryGetProperty("sessionId", out JsonElement nestedSession)
                    && nestedSession.ValueKind == JsonValueKind.String)
                {
                    return nestedSession.GetString();
                }
            }
            catch
            {
            }

            return null;
        }

        private static string? ReadValueAsString(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty("value", out JsonElement value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
            catch
            {
            }

            return null;
        }

        private static T ExecuteWithSessionRetry<T>(ADBClient client, string serial, Func<string, CommandResult<T>> action)
        {
            if (!TryGetSession(serial, out SessionState? state))
            {
                return default!;
            }

            CommandResult<T> first = action(state.SessionId);
            if (!first.InvalidSession)
            {
                return first.Value;
            }

            InvalidateSession(serial);
            if (!EnsureSession(client) || !TryGetSession(serial, out SessionState? refreshed))
            {
                return default!;
            }

            CommandResult<T> second = action(refreshed.SessionId);
            return second.Value;
        }

        private readonly struct HttpResult
        {
            public HttpResult(bool success, bool isInvalidSession, string content)
            {
                Success = success;
                IsInvalidSession = isInvalidSession;
                Content = content ?? string.Empty;
            }

            public bool Success { get; }
            public bool IsInvalidSession { get; }
            public string Content { get; }
        }

        private readonly struct CommandResult<T>
        {
            public CommandResult(T value, bool invalidSession)
            {
                Value = value;
                InvalidSession = invalidSession;
            }

            public T Value { get; }
            public bool InvalidSession { get; }

            public static CommandResult<T> Ok(T value) => new CommandResult<T>(value, false);
            public static CommandResult<T> Fail(T value) => new CommandResult<T>(value, false);
            public static CommandResult<T> Invalid() => new CommandResult<T>(default!, true);
        }
    }
}
