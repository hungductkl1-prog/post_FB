namespace Sunny.Subdy.Common.Helper
{
    public class FileHelper
    {
        public static bool CreateFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return true;
            }
            return false;
        }
    }
}
