namespace Sunny.Subdy.Common.Models
{
    public class PandoraFarmingType
    {
        public const string HDNgheNhac = "HDNgheNhac";

        public readonly static Dictionary<string, string> DictionariesAction = new Dictionary<string, string>
        {
            { HDNgheNhac, "Nghe nhạc (tìm kiếm & phát)" },
        };

        public readonly static Dictionary<string, string> DescriptionAction = new Dictionary<string, string>
        {
            { HDNgheNhac, "Mở URL playlist/bài hát trên Pandora, phát và nghe nhạc trong khoảng thời gian cấu hình." },
        };
    }
}
