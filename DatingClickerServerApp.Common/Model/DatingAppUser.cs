using System.Text.Json;

namespace DatingClickerServerApp.Common.Model
{
    public class DatingAppUser
    {
        public string UserId { get; set; }

        public int SuperLikeCount { get; set; }

        public JsonElement JsonData { get; set; }
    }
}
