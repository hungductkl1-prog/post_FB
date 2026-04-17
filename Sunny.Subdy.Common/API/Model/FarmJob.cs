using System.Collections.Generic;

namespace Sunny.Subdy.Common.API.Model
{
    public class FarmJobAccount
    {
        public int Id { get; set; }
        public string Uid { get; set; }
    }

    public class FarmJob
    {
        public int JobId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string TargetUrl { get; set; }
        public string Platform { get; set; }
        public string PlatformName { get; set; }
        public string PlatformIcon { get; set; }
        public string JobType { get; set; }
        public string JobTypeName { get; set; }
        public string Server { get; set; }
        public double Price { get; set; }
        public bool RequiresProof { get; set; }
        public int Remaining { get; set; }
        public string Priority { get; set; }
        public List<FarmJobAccount> YourAccounts { get; set; } = new List<FarmJobAccount>();
    }
}
