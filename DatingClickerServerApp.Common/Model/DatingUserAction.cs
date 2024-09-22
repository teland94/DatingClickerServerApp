namespace DatingClickerServerApp.Common.Model
{
    public class DatingUserAction
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
        public DatingUserActionType ActionType { get; set; }

        public DatingUser DatingUser { get; set; }
        public Guid DatingUserId { get; set; }
    }
}
