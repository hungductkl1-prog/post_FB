using AutoAndroid;
using Sunny.Subdy.Data.Models;
using System.IO;
using System.Text.RegularExpressions;

namespace Sunny.Subdy.Common
{
    public class BackupRestoreHelper
    {
        private readonly ADBClient _Android;
        private const string Package_Facebook = "com.facebook.katana";
        private const string PackageInstagram = "com.instagram.android";
        private const string PackageTikTok = "com.ss.android.ugc.trill";
        private const string PackageThreads = "com.instagram.barcelona";

        public BackupRestoreHelper(DeviceModel device)
        {
            _Android = new ADBClient(device);
        }
        public bool BackupFacebook(string file)
        {
            for (int i = 0; i < 10; i++)
            {
                _Android.ADB.Shell("su -c 'rm -rf /data/data/" + Package_Facebook + "/*.tar.gz'");
                _Android.ADB.Shell("su -c 'rm -rf /sdcard/*.tar.gz'");
                _Android.Device.Status = "Đang nén dữ liệu Facebook...";
                _Android.ADB.Shell("su root sh -c 'tar -czvpf /data/data/" + Package_Facebook + "/backup.tar.gz /data/data/" + Package_Facebook + "/databases /data/data/" + Package_Facebook + "/app_light_prefs /data/data/" + Package_Facebook + "/shared_prefs /data/data/" + Package_Facebook + "/files/mobileconfig'");
                _Android.ADB.Shell("su -c 'cp /data/data/" + Package_Facebook + "/backup.tar.gz /sdcard/backup.tar.gz'");
                _Android.Device.Status = "Đang tải file backup về máy...";
                _Android.ADB.CMD($"pull /sdcard/backup.tar.gz \"{file}\"", 300);
                _Android.ADB.Shell("su -c 'rm -rf /data/data/" + Package_Facebook + "/*.tar.gz'");
                _Android.ADB.Shell("su -c 'rm -rf /sdcard/*.tar.gz'");
                if (!File.Exists(file))
                {
                    continue;
                }
                _Android.Device.Status = "Đã backup facebook thành công";
                return true;
            }
            return false;
        }
        public bool RestoreFacebook(string file)
        {
            if (!File.Exists(file) || Path.GetExtension(file) != ".gz") return false;

            string fileName = Path.GetFileName(file);
            string escapedFile = file.Replace("\\", "/");

            for (int i = 0; i < 2; i++)
            {
                string output = _Android.ADB.Shell($"cmd package list packages -U {Package_Facebook}", 30).Trim();

                string appUid = Regex.Match(output, @"uid:(\d+)").Groups[1].Value;
                if (string.IsNullOrWhiteSpace(appUid)) continue;
                _Android.Device.Status = "Đang đẩy file vào thiết bị...";
                _Android.ADB.CMD($"push \"{escapedFile}\" /data/local/tmp/{fileName}", 100);

                // DÙNG ĐƯỜNG PROCESS adb.exe (_Android.ADB.Shell) CÓ TIMEOUT THẬT + KILL,
                // GIỐNG BackupFacebook ở trên và giống tool đối thủ — KHÔNG dùng đường socket
                // (_Android.Shell). Lý do: đường socket chỉ có ReceiveTimeout 30s trên socket;
                // `tar -zxvf` có -v in liên tục nên ĐỒNG HỒ 30s BỊ RESET MÃI → lệnh chạy gần như
                // VÔ HẠN; còn `cp`/`chown` im lặng quá 30s thì ném SocketException → ADBClient.Shell
                // RETRY 3 lần, MỖI LẦN CHẠY LẠI TOÀN BỘ lệnh (tới 90s+) rồi gọi Connect() reconnect.
                // 26 máy cùng vào bước này → bão I/O + reconnect storm → TREO MÁY.
                // Đường process WaitForExit(timeout) rồi TryKillProcess → CÓ CHẶN TRÊN, không treo vô hạn.
                // Timeout đặt RỘNG (chỉ nổ khi lệnh THẬT SỰ đứng, không phải khi chạy chậm hợp lệ);
                // WaitForExit trả ngay khi lệnh xong nên không làm chậm trường hợp bình thường.
                //
                // ── FIX C1 (v19) — BỎ `-v` VÀ BỎ `cp -af` DOUBLE-WRITE ──
                // (1) `-v` (verbose): tar in tên TỪNG file ra stdout. Trên đường socket đây CHÍNH
                //     là thứ reset đồng hồ ReceiveTimeout 30s làm lệnh chạy gần như VÔ HẠN (bug v17).
                //     Trên đường process nó vẫn tốn: adb.exe phải bơm hàng chục nghìn dòng tên file
                //     qua pipe về host cho 26 máy song song = I/O + CPU vô ích. Tool đối thủ dùng
                //     `tar -xpf` QUIET (2 hit @40610008/@46269140), KHÔNG có -v.
                // (2) `cp -af` + `rm -rf .../data`: backup ở :27 được tar với đường dẫn TUYỆT ĐỐI
                //     (`/data/data/PKG/databases` ...). tar (GNU lẫn toybox) STRIP dấu `/` dẫn đầu
                //     rồi nối vào `-C`, nên giải nén với `-C /data/data/PKG/` đặt file vào
                //     `/data/data/PKG/data/data/PKG/...` (lồng 2 lần) — và `cp -af` tồn tại CHỈ để
                //     dời chúng về đúng chỗ, tức GHI TOÀN BỘ DATA FB XUỐNG FLASH LẦN THỨ HAI
                //     trên eMMC yếu. ĐỔI `-C /` (đúng precedent RestoreInstagram:141) → file rơi
                //     thẳng vào `/data/data/PKG/...`, KHÔNG cần cp -af, KHÔNG cần rm -rf .../data.
                //     `-C /` AN TOÀN CẢ HAI HÀNH VI tar: nếu tar strip `/` thì `/`+`data/data/PKG`
                //     = đúng chỗ; nếu tar GIỮ path tuyệt đối thì nó đã extract thẳng đúng chỗ và
                //     `-C` bị bỏ qua. Tương thích ngược với MỌI file .gz đã tạo từ trước.
                // Net: 6 lệnh nặng → 4, và bỏ hẳn một lần ghi full-size xuống flash.
                _Android.ADB.Shell($"su -c 'rm -rf /data/data/{Package_Facebook}/*'", 120);
                _Android.ADB.Shell($"su -c 'tar -zxf /data/local/tmp/{fileName} -C / --exclude=\\\"*cache*\\\"'", 600);

                // Verify giải nén ĐÃ rơi đúng chỗ. Không có thiết bị nối lúc build nên giữ một
                // fallback tường minh thay vì tin mù: nếu `databases` vắng mặt (backup dị dạng /
                // member là path tương đối) thì giải nén lại theo kiểu CŨ (lồng rồi cp -af) để
                // không bao giờ restore ra app rỗng. Chi phí 1 lệnh `ls` ở đường HAPPY (98%+).
                string landed = _Android.ADB.Shell($"su -c 'ls /data/data/{Package_Facebook}/databases 2>/dev/null'", 60);
                if (string.IsNullOrWhiteSpace(landed))
                {
                    _Android.Device.Status = "Giải nén lệch chỗ, đang dọn lại...";
                    _Android.ADB.Shell($"su -c 'tar -zxf /data/local/tmp/{fileName} -C /data/data/{Package_Facebook}/ --exclude=\\\"*cache*\\\"'", 600);
                    _Android.ADB.Shell($"su -c 'cp -af /data/data/{Package_Facebook}/data/data/{Package_Facebook}/. /data/data/{Package_Facebook}/'", 300);
                    _Android.ADB.Shell($"su -c 'rm -rf /data/data/{Package_Facebook}/data'", 60);
                }

                _Android.ADB.Shell($"su -c 'chown -R {appUid}:{appUid} /data/data/{Package_Facebook}'", 300);
                _Android.ADB.Shell($"su -c 'rm -f /data/local/tmp/{fileName}'", 60);
                _Android.LogHelper.SUCCESS("Restore Facebook thành công");
                return true;
            }

            return false;
        }


        public bool BackupInstagram(string file)
        {
            for (int i = 0; i < 10; i++)
            {
                _Android.LogHelper.SUCCESS("Đang backup instagram...");
                _Android.ADB.Shell($"su -c \"rm -rf /data/data/{PackageInstagram}/*.tar.gz\"");
                _Android.ADB.Shell("su -c \"rm -rf /sdcard/*.tar.gz\"");
                _Android.Device.Status = "Đang nén dữ liệu Instagram...";
                _Android.ADB.Shell($"su -c 'tar -czvpf /sdcard/backup.tar.gz -C / /data/misc/keystore/user_0 /data/data/{PackageInstagram}'", 300);
                _Android.Device.Status = "Đang tải file backup về máy...";
                _Android.ADB.CMD($"pull /sdcard/backup.tar.gz \"{file}\"", 300);
                _Android.ADB.Shell($"su -c \"rm -rf /data/data/{PackageInstagram}/*.tar.gz\"");
                _Android.ADB.Shell("su -c \"rm -rf /sdcard/*.tar.gz\"");
                if (File.Exists(file))
                {
                    _Android.LogHelper.SUCCESS("Đã backup instagram thành công");
                    return true;
                }
            }
            return false;
        }
        public async Task<bool> RestoreInstagram(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                _Android.LogHelper.ERROR("File không tồn tại!");
                return false;
            }
            _Android.ADB.Shell($"am force-stop {PackageInstagram}");
            _Android.ADB.Shell($"su -c 'killall -9 {PackageInstagram}'");
            string fileName = Path.GetFileName(file);
            string escapedFile = file.Replace("\\", "/");
            string sdcardTar = "/sdcard/profile.tar.gz";
            string uidnew = _Android.ADB.Shell($"su -c \"ls -l /data/data | grep " + PackageInstagram + " | awk '{print $3}'\"", 30);
            string numberUid = uidnew.Replace("u0_a", "");
            _Android.ADB.Shell($"su -c \"cd /data/misc/keystore/user_0 && for f in \\$(ls | grep {numberUid}); do rm -f \\\"\\$f\\\"; done\"", 30);
             _Android.Delay(1);


            for (int i = 0; i < 5; i++)
            {
                _Android.LogHelper.SUCCESS("Đang restore instagram...");
                // 1. Dọn dẹp file tạm
                _Android.ADB.Shell($"su -c 'rm -rf {sdcardTar} /data/{fileName}'", 30);
                 _Android.Delay(1);
                // Đẩy file vào thiết bị
                _Android.LogHelper.SUCCESS("Đang đẩy file vào thiết bị...");
                string pushResult = _Android.ADB.CMD($"push \"{escapedFile}\" /sdcard/", 60);
                if (!pushResult.ToLower().Contains("kb/s") && !pushResult.ToLower().Contains("mb/s"))
                {
                    _Android.LogHelper.ERROR("Push file không thành công.");
                    continue;
                }
                // 3. Copy từ /sdcard sang /data/
                _Android.ADB.Shell($"su -c 'cp /sdcard/{fileName} /data/'", 60);
                 _Android.Delay(1);

                // 4. Giải nén
                _Android.ADB.Shell($"su -c 'tar -xzvpf /data/{fileName} -C /'", 300);
                 _Android.Delay(2);
                string uidOld = _Android.ADB.Shell($"su -c \"ls -l /data/data | grep " + PackageInstagram + " | awk '{print $3}'\"", 30);
                string numberUidOld = uidOld.Replace("u0_a", "");
                _Android.ADB.Shell($"su -c 'cd /data/misc/keystore/user_0 && for f in *{numberUidOld}* .*{numberUidOld}*; do [ -e \"$f\" ] && newname=$(echo \"$f\" | sed \"s/{numberUidOld}/{numberUid}/g\"); mv \"$f\" \"$newname\"; done'");
                // 7. Lấy UID:GID
                _Android.ADB.Shell($"su -c 'chown -R {uidnew}:{uidnew} /data/data/{PackageInstagram}'", 60);
                 _Android.Delay(1);
                // 9. Xoá file tạm
                _Android.ADB.Shell($"su -c 'rm -rf /data/{fileName}'", 30);
                _Android.AppStart(PackageInstagram);
                _Android.LogHelper.SUCCESS("Đã restore instagram thành công");
                return true;
            }

            return false;
        }

        public bool BackupThreads(string file)
        {
            for (int i = 0; i < 10; i++)
            {
                _Android.LogHelper.SUCCESS("Đang backup threads...");
                _Android.ADB.Shell($"su -c \"rm -rf /data/data/{PackageThreads}/*.tar.gz\"");
                _Android.ADB.Shell("su -c \"rm -rf /sdcard/*.tar.gz\"");
                _Android.ADB.Shell($"su -c 'tar -czvpf /sdcard/backup.tar.gz -C / /data/misc/keystore/user_0 /data/data/{PackageThreads}'", 300);
                _Android.ADB.CMD($"pull /sdcard/backup.tar.gz \"{file}\"", 300);
                _Android.ADB.Shell($"su -c \"rm -rf /data/data/{PackageThreads}/*.tar.gz\"");
                _Android.ADB.Shell("su -c \"rm -rf /sdcard/*.tar.gz\"");
                if (File.Exists(file))
                {
                    _Android.LogHelper.SUCCESS("Đã backup threads thành công");
                    return true;
                }
            }
            return false;
        }
        public async Task<bool> RestoreThreads(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                _Android.LogHelper.ERROR("File không tồn tại!");
                return false;
            }
            _Android.ADB.Shell($"am force-stop {PackageThreads}");
            _Android.ADB.Shell($"su -c 'killall -9 {PackageThreads}'");
            string fileName = Path.GetFileName(file);
            string escapedFile = file.Replace("\\", "/");
            string sdcardTar = "/sdcard/profile.tar.gz";
            string uidnew = _Android.ADB.Shell($"su -c \"ls -l /data/data | grep " + PackageThreads + " | awk '{print $3}'\"", 30);
            string numberUid = uidnew.Replace("u0_a", "");
            _Android.ADB.Shell($"su -c \"cd /data/misc/keystore/user_0 && for f in \\$(ls | grep {numberUid}); do rm -f \\\"\\$f\\\"; done\"", 30);
             _Android.Delay(1);


            for (int i = 0; i < 5; i++)
            {
                _Android.LogHelper.SUCCESS("Đang restore threads...");
                // 1. Dọn dẹp file tạm
                _Android.ADB.Shell($"su -c 'rm -rf {sdcardTar} /data/{fileName}'", 30);
                 _Android.Delay(1);
                // Đẩy file vào thiết bị
                string pushResult = _Android.ADB.CMD($"push \"{escapedFile}\" /sdcard/", 60);
                if (!pushResult.ToLower().Contains("kb/s") && !pushResult.ToLower().Contains("mb/s"))
                {
                    _Android.LogHelper.ERROR("Push file không thành công.");
                    continue;
                }
                // 3. Copy từ /sdcard sang /data/
                _Android.ADB.Shell($"su -c 'cp /sdcard/{fileName} /data/'", 60);
                 _Android.Delay(1);

                // 4. Giải nén
                _Android.ADB.Shell($"su -c 'tar -xzvpf /data/{fileName} -C /'", 300);
                 _Android.Delay(2);
                string uidOld = _Android.ADB.Shell($"su -c \"ls -l /data/data | grep " + PackageThreads + " | awk '{print $3}'\"", 30);
                string numberUidOld = uidOld.Replace("u0_a", "");
                _Android.ADB.Shell($"su -c 'cd /data/misc/keystore/user_0 && for f in *{numberUidOld}* .*{numberUidOld}*; do [ -e \"$f\" ] && newname=$(echo \"$f\" | sed \"s/{numberUidOld}/{numberUid}/g\"); mv \"$f\" \"$newname\"; done'");
                // 7. Lấy UID:GID
                _Android.ADB.Shell($"su -c 'chown -R {uidnew}:{uidnew} /data/data/{PackageThreads}'", 60);
                 _Android.Delay(1);
                // 9. Xoá file tạm
                _Android.ADB.Shell($"su -c 'rm -rf /data/{fileName}'", 30);
                _Android.AppStart(PackageThreads);
                _Android.LogHelper.SUCCESS("Đã restore threads thành công");
                return true;
            }

            return false;
        }

        public async Task<bool> BackupTikTok(string file)
        {
            _Android.StopApp(PackageTikTok);
            for (int i = 0; i < 10; i++)
            {
                _Android.Device.Status = "Đang backup TikTok...";
                _Android.Shell($"su -c \"rm -rf /data/data/{PackageTikTok}/cache\"");
                _Android.Shell($"su -c \"rm -rf /data/data/{PackageTikTok}/*.tar.gz\"");
                _Android.Shell("su -c \"rm -rf /sdcard/*.tar.gz\"");
                 _Android.Delay(1);

                // 2. Nén thư mục TikTok
                _Android.Device.Status = "Đang nén dữ liệu TikTok...";
                _Android.ADB.Shell($"su root sh -c 'tar -czvpf /sdcard/backup.tar.gz -C /data/data/{PackageTikTok} .'", 300);
                 _Android.Delay(1);

                // 5. Pull về máy tính
                _Android.Device.Status = "Đang tải file backup về máy...";
                _Android.ADB.CMD($"pull /sdcard/backup.tar.gz \"{file}\"", 300);
                 _Android.Delay(1);

                // 6. Dọn dẹp
                _Android.ADB.Shell($"su -c \"rm -rf /data/data/ {PackageTikTok}/*.tar.gz\"");
                _Android.ADB.Shell("su -c \"rm -rf /sdcard/*.tar.gz\"");

                // 7. Kiểm tra file thành công
                if (File.Exists(file))
                {
                    _Android.Device.Status = "Đã backup TikTok thành công";
                    return true;
                }

                 _Android.Delay(3);
            }
            return false;
        }
        public async Task<bool> RestoreTikTok(string file)
        {
            string shellOutput = string.Empty;
            _Android.StopApp(PackageTikTok);
            string fileName = Path.GetFileName(file);
            string escapedFile = file.Replace("\\", "/");
            bool flag = false;
            for (int i = 0; i < 2; i++)
            {
                _Android.Device.Status = "Đang đẩy file vào thiết bị...";
                //    _Android.Push(file, $"/sdcard/{fileName}");
                shellOutput = _Android.ADB.CMD($"push \"{escapedFile}\" /sdcard/{fileName}", 100);

                shellOutput = _Android.ADB.Shell($"su -c 'cp /sdcard/{fileName} /data/data/" + PackageTikTok + $"/{fileName}'", 300);
                 _Android.Delay(1);
                shellOutput = _Android.ADB.Shell("su -c 'tar -xpf /data/data/" + PackageTikTok + $"/{fileName}'", 300);
                 _Android.Delay(1);
                shellOutput = _Android.ADB.Shell($"su -c \"sh -c 'tar -xzf /data/data/{PackageTikTok}/{fileName} -C /data/data/{PackageTikTok}/'\"", 300);
                 _Android.Delay(1);
                shellOutput = _Android.Shell("su -c \"ls -l /data/data | grep " + PackageTikTok + " | awk '{print $3\\\":\\\"$4}'\"", 30);
                flag = shellOutput != "";
                 _Android.Delay(1);
                shellOutput = _Android.Shell("su -c chown -R " + shellOutput + " /data/data/" + PackageTikTok, 30);
                if (!flag)
                {
                    continue;
                }
                _Android.Device.Status = "Đã restore TikTok thành công";
                return true;
            }
            return false;
        }


    }
}
