using AutoAndroid;
using Sunny.Subd.Core.Models;
using Sunny.Subd.Core.Pandora;
using Sunny.Subd.Core.Services;
using Sunny.Subdy.Common.Json;
using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;
using System.Diagnostics;
using System.Text;

namespace PandoraSelfTest
{
    internal static class Program
    {
        private const string DefaultSerial = "5200d004b263c49d";
        private const string Uid = "stolcalspinkie2323@outlook.com";
        private const string Pwd = "faith20003";
        private const string ScriptName = "Pandora URL Test";
        private const string TestUrl = "https://www.pandora.com/playlist/PL:135084901224429246:78063128696361081";

        private static StreamWriter _log;
        private static CancellationTokenSource _cts;
        private static ADBClient _client;

        private static void Emit(string line)
        {
            string stamped = $"[{DateTime.Now:HH:mm:ss}] {line}";
            Console.WriteLine(stamped);
            try { _log?.WriteLine(stamped); _log?.Flush(); } catch { }
        }

        private static async Task<int> Main(string[] args)
        {
            try { Console.OutputEncoding = Encoding.UTF8; } catch { }

            string serial = args.Length > 0 ? args[0] : DefaultSerial;
            string logPath = Path.Combine(AppContext.BaseDirectory,
                $"pandora_url_test_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            _log = new StreamWriter(logPath, false, Encoding.UTF8) { AutoFlush = true };

            // Capture Debug.WriteLine output (used by PandoraFarming.Log)
            Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
            Trace.Listeners.Add(new TextWriterTraceListener(_log));
            Trace.AutoFlush = true;

            Emit($"=== Pandora URL Quick Test ===");
            Emit($"serial={serial} | url={TestUrl}");

            PandoraFarming.DebugMode = true;

            _cts = new CancellationTokenSource();

            // Connect to device
            Emit("Ket noi thiet bi...");
            var probe = new ADBClient(serial);
            _client = new ADBClient(probe.Device);
            _client.Running = true;
            Emit($"Device: {_client.Device?.NameDevice} | OS {_client.Device?.OS}");

            bool atx = false;
            try { atx = await _client.ATX.SetupATX(); } catch (Exception ex) { Emit("SetupATX loi: " + ex.Message); }
            Emit($"ATX ready = {atx}");
            if (!atx) { Emit("Thieu ATX - dung."); return 2; }

            try { await _client.TurnOnADBKeyboard(); Emit("ADB Keyboard: on"); }
            catch (Exception ex) { Emit("ADB Keyboard loi: " + ex.Message); }

            // Force close Pandora app
            _client.ForcePortraitOrientation();
            Emit("Stop Pandora app...");
            _client.StopApp("com.pandora.android");
            _client.Delay(2, 3);

            // Create script + action in DB
            var scriptCtx = new ScriptContext();
            var actionCtx = new ScriptActionContext();
            var old = scriptCtx.GetByName(ScriptName, PlatformModel.Pandora);
            if (old != null)
            {
                foreach (var a in actionCtx.GetByScriptId(old.Id) ?? new())
                    actionCtx.DeleteById(a.Id);
                scriptCtx.DeleteById(old.Id);
            }
            var script = new Script
            {
                Id = Guid.NewGuid(),
                Name = ScriptName,
                Platform = PlatformModel.Pandora,
                DateCreate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            };
            scriptCtx.Add(script);
            var json = new System.Text.Json.Nodes.JsonObject
            {
                ["txtPandoraUrl"] = TestUrl,
                ["nudListenMinutesFrom"] = 5,
                ["nudListenMinutesTo"] = 10,
            };
            actionCtx.Add(new ScriptAction
            {
                Id = Guid.NewGuid(),
                Name = "Nghe nhac URL",
                Type = PandoraFarmingType.HDNgheNhac,
                Json = json.ToJsonString(),
                Platform = PlatformModel.Pandora,
                ScriptId = script.Id,
                ByOrder = 1,
            });
            Emit("DB setup done.");

            // Setup MainService
            var config = new ConfigModel
            {
                Platform = PlatformModel.Pandora,
                SettingGeneral = new JsonHelper("{}", true),
                SettingJob = new JsonHelper("{}", true),
            };
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Uid = Uid,
                Password = Pwd,
                Platformt = PlatformModel.Pandora,
                NameScript = ScriptName,
                Serial = serial,
            };
            var main = new MainService(PlatformModel.Pandora, _client, config, _cts.Token);
            main._account = account;

            // Run the automation
            Emit("--- Chay PandoraFarming.ExecuteAsync() ---");
            var sw = Stopwatch.StartNew();
            int exit = 0;
            try
            {
                await new PandoraFarming(main).ExecuteAsync();
                Emit("--- ExecuteAsync ket thuc ---");
            }
            catch (Exception ex)
            {
                Emit($"!!! Loi: {ex.GetType().Name}: {ex.Message}");
                exit = 1;
            }
            sw.Stop();

            Emit($"Tong thoi gian: {sw.Elapsed.TotalSeconds:F0}s");
            _log?.Flush();
            return exit;
        }
    }
}