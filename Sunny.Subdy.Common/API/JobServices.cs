using Sunny.Subdy.Common.API.Jobs;
using Sunny.Subdy.Common.Models;

namespace Sunny.Subdy.Common.API
{
    public class JobServices
    {
        public const string SeedingVip = "SeedingVip";
        public const string GoLike = "https://app.golike.net/";
        public const string TuongTacCheo = "https://tuongtaccheo.com/";
        public const string TraoDoiSub = "https://traodoisub.com/";
        public const string VipIG = "https://vipig.net/";
        public static List<string> TypesFacebook = new List<string>
        {
            TuongTacCheo,
         //   TraoDoiSub,
            GoLike,
            SeedingVip,
        };
        public static List<string> TypesTikTok = new List<string>
        {
            TuongTacCheo,
           // TraoDoiSub,
          //  GoLike,
        };
        public static List<string> TypesInstagram = new List<string>
        {
            VipIG,
         //   TraoDoiSub,
          //  GoLike,
        };
        public static List<string> GetTypeJobByPlatformt(string platformt)
        {
            switch (platformt)
            {
                case PlatformModel.Facebook:
                    return new List<string>
            {
                JobTypes.Like,
                JobTypes.Love,
                JobTypes.Care,
                JobTypes.Haha,
                JobTypes.Wow,
                JobTypes.Sad,
                JobTypes.Angry,
                JobTypes.Share,
                JobTypes.LikePage,
                JobTypes.Follow,
                JobTypes.JoinGroup,
                JobTypes.Comment
            };

                case PlatformModel.TikTok:
                case PlatformModel.Instagram:
                    return new List<string>
            {
                JobTypes.Like,
                JobTypes.Follow
            };

                default:
                    return new List<string>();
            }
        }
    }
}
