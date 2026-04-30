using Sunny.Subdy.Common.Models;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public class fIGUnfollow : fIGActionStub
    {
        public fIGUnfollow(string scriptId, string actionId = "")
            : base(scriptId, InstagramFarmingType.IGUnfollow, actionId)
        {
        }
    }
}
