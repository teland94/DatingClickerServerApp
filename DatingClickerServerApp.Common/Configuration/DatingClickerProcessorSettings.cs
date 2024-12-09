namespace DatingClickerServerApp.Common.Configuration
{
    public class DatingClickerProcessorSettings
    {
        public IDictionary<string, string> SignIn { get; set; }

        public bool UseChatBot { get; set; }

        public DatingUserCriteriesSettings LikeCriteries { get; set; }

        public bool DislikeIfUserHasExistingSuperLikeAction { get; set; }
    }
}
