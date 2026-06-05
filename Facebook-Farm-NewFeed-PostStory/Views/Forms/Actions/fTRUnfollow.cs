using Sunny.Subdy.Common.Models;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    public class fTRUnfollow : fThreadsActionStub
    {
        public fTRUnfollow(string scriptId, string actionId = "")
            : base(scriptId, ThreadsFarmingType.TRUnfollow, actionId)
        {
        }
    }
}
