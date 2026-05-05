using Sunny.Subdy.Common.Models;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public class fTRUnfollow : fThreadsActionStub
    {
        public fTRUnfollow(string scriptId, string actionId = "")
            : base(scriptId, ThreadsFarmingType.TRUnfollow, actionId)
        {
        }
    }
}
