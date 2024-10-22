using System.Text.Json;

namespace DatingClickerServerApp.Common.Model
{
    public class DatingUser
    {
        public Guid Id { get; set; }

        public string ExternalId { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime UpdatedDate { get; set; }

        public string Name { get; set; }

        public bool IsVerified { get; set; }

        public int? Age { get; set; }

        public bool? HasChildren { get; set; }

        public int? Height { get; set; }

        public Uri PreviewUrl { get; set; }

        public string About { get; set; }

        public string[] Interests { get; set; }

        public string CityName { get; set; }

        public JsonElement JsonData { get; set; }

        public ICollection<DatingUserAction> Actions { get; set; }

        public BlacklistedDatingUser BlacklistedDatingUser { get; set; }
    }
}
