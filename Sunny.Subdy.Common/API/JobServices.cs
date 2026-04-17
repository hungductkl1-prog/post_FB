using Sunny.Subdy.Common.API.Jobs;
using Sunny.Subdy.Common.Models;

namespace Sunny.Subdy.Common.API
{
    public class JobServices
    {
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

                default:
                    return new List<string>();
            }
        }
    }
}
