using Sunny.Subdy.Common.Models;

namespace Facebook_Farm_NewFeed_PostStory.Views.Forms.Actions
{
    public class fIGUnfollow : fIGActionStub
    {
        public fIGUnfollow(string scriptId, string actionId = "")
            : base(scriptId, InstagramFarmingType.IGUnfollow, actionId)
        {
        }
    }
}
