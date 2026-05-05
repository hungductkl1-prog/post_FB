using Sunny.Subdy.Common.Models;

namespace LamToolAutoPhonePrime.Views.Forms.Actions
{
    public class fTRFollow : fThreadsActionStub
    {
        public fTRFollow(string scriptId, string actionId = "")
            : base(scriptId, ThreadsFarmingType.TRFollow, actionId)
        {
        }
    }
}
