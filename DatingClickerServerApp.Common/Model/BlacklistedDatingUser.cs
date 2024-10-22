namespace DatingClickerServerApp.Common.Model
{
    public class BlacklistedDatingUser
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }

        public DatingUser DatingUser { get; set; }
    }
}
