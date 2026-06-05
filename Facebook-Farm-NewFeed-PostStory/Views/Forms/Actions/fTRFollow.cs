using Sunny.Subdy.Common.Models;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    public class fTRFollow : fThreadsActionStub
    {
        public fTRFollow(string scriptId, string actionId = "")
            : base(scriptId, ThreadsFarmingType.TRFollow, actionId)
        {
        }
    }
}
