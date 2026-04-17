namespace Sunny.Subdy.Common.API.Model
{
    public class ClaimResult
    {
        public int AttemptId { get; set; }
        public int JobId { get; set; }
        public string Uid { get; set; }
        public string TargetUrl { get; set; }
        public string JobType { get; set; }
        public bool RequiresProof { get; set; }
        public double PotentialReward { get; set; }
        public string Status { get; set; }
        public string ExpiresAt { get; set; }
        public int RemainingSlots { get; set; }
        public int TimeoutMinutes { get; set; }
        public string Message { get; set; }
    }
}
