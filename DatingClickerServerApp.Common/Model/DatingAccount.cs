using System.Text.Json;

namespace DatingClickerServerApp.Common.Model
{
    public class DatingAccount
    {
        public Guid Id { get; set; }

        public string AppUserId { get; set; }

        public DatingAppNameType AppName { get; set; }

        public string JsonAuthData { get; set; }

        public JsonElement JsonProfileData { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public ICollection<DatingUserAction> Actions { get; set; }
    }
}
