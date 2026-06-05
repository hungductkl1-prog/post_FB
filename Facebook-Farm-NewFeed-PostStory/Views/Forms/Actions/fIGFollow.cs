using Sunny.Subdy.Common.Models;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    public class fIGFollow : fIGActionStub
    {
        public fIGFollow(string scriptId, string actionId = "")
            : base(scriptId, InstagramFarmingType.IGFollow, actionId)
        {
        }
    }
}
