namespace DatingClickerServerApp.Common.Configuration
{
    public class DatingUserCriteriesSettings
    {
        public int Height { get; set; }
        public ICollection<string> Interests { get; set; }
        public ICollection<string> ExclusionWords { get; set; }
        public bool? IsVerified { get; set; }
        public bool? HasChildren { get; set; }

        public DatingUserCriteriesSettings() { }

        public DatingUserCriteriesSettings(int height,
                                           ICollection<string> interests = null,
                                           ICollection<string> exclusionWords = null,
                                           bool? isVerified = null,
                                           bool? hasChildren = null)
        {
            Height = height;
            Interests = interests;
            ExclusionWords = exclusionWords;
            IsVerified = isVerified;
            HasChildren = hasChildren;
        }
    }

}