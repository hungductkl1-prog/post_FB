using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System.Net;

namespace AutoAndroid
{
    public class FileHelper
    {
        public static bool UnzipTgz(string zipPath, string goalFolder)
        {
            System.IO.Stream inStream = null;
            System.IO.Stream gzipStream = null;
            TarArchive tarArchive = null;
            try
            {
                using (inStream = File.OpenRead(zipPath))
                {
                    using (gzipStream = new GZipInputStream(inStream))
                    {
                        tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
                        tarArchive.ExtractContents(goalFolder);
                        tarArchive.Close();
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                tarArchive?.Close();
                gzipStream?.Close();
                inStream?.Close();
            }
        }
        public static bool DeleteFile(string path)
        {
            if (!File.Exists(path))
            {
                return true;
            }
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        public static bool DownImage(string url, string file)
        {
            try
            {
                using (var client = new WebClient())
                {
                    client.Headers.Add("user-agent", "Hell");
                    client.DownloadFile(url, file);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
