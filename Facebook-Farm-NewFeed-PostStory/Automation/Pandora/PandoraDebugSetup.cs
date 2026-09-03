using Sunny.Subdy.Common.Models;
using Sunny.Subdy.Data.Context;
using Sunny.Subdy.Data.Models;

namespace Facebook_Farm_NewFeed_PostStory.Automation.Pandora
{
    /// <summary>
    /// Self-debug setup for Pandora platform — configures test device, account,
    /// script, and default URL for quick validation.
    /// </summary>
    public static class PandoraDebugSetup
    {
        // Test credentials provided for self-debug
        public const string TestDeviceSerial = "5200d004b263c49d";
        public const string TestAccountUid = "stolcalspinkie2323@outlook.com";
        public const string TestAccountPassword = "faith20003";
        public const string TestProxy = "65.111.12.6:3129:rfzpmumjnc2q:qy8h973dk5pwi1x";

        // Default Pandora URL for testing
        public const string DefaultPandoraUrl = "https://www.pandora.com/playlist/PL:135084901224429246:78063128696361081";

        /// <summary>
        /// Initialize debug environment: create default script + action if none exist for Pandora.
        /// Sets up debug mode for XML dumps during self-debug.
        /// </summary>
        public static void EnsureDebugSetup()
        {
            try
            {
                // Enable XML debug dumps for Pandora automation
                Sunny.Subd.Core.Pandora.PandoraFarming.DebugMode = true;

                var scriptCtx = new ScriptContext();
                var actionCtx = new ScriptActionContext();

                // Check if any Pandora script exists
                var existing = scriptCtx.GetByPlatform(PlatformModel.Pandora);
                if (existing != null && existing.Count > 0)
                    return; // Already set up

                // Create default debug script
                var script = new Script
                {
                    Id = Guid.NewGuid(),
                    Name = "Pandora Debug Script",
                    Platform = PlatformModel.Pandora,
                };
                scriptCtx.Add(script);

                // Create default action with URL
                string json = "{\"txtPandoraUrl\":\"" + DefaultPandoraUrl.Replace("\"", "\\\"") +
                    "\",\"nudListenMinutesFrom\":30,\"nudListenMinutesTo\":180}";

                var action = new ScriptAction
                {
                    Id = Guid.NewGuid(),
                    Name = "Nghe nhạc (URL)",
                    Type = PandoraFarmingType.HDNgheNhac,
                    Json = json,
                    Platform = PlatformModel.Pandora,
                    ScriptId = script.Id,
                    ByOrder = 1,
                };
                actionCtx.Add(action);

                System.Diagnostics.Debug.WriteLine("[Pandora] Debug setup complete — script + action created.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pandora] Debug setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Register the test account for self-debug if it doesn't exist yet.
        /// </summary>
        public static void EnsureTestAccount()
        {
            try
            {
                var accCtx = new AccountContext();
                var existing = accCtx.GetAll(new List<string>(), PlatformModel.Pandora, true);
                if (existing != null && existing.Any(a => a.Uid == TestAccountUid))
                    return;

                var account = new Account
                {
                    Uid = TestAccountUid,
                    Password = TestAccountPassword,
                    Proxy = TestProxy,
                    Platformt = PlatformModel.Pandora,
                    NameScript = "Pandora Debug Script",
                };
                accCtx.Add(account);

                System.Diagnostics.Debug.WriteLine("[Pandora] Test account registered.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Pandora] Test account setup failed: {ex.Message}");
            }
        }
    }
}
