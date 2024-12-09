using DatingClickerServerApp.Common.Model;

namespace DatingClickerServerApp.Core.Interfaces
{
    public interface IDatingUserService
    {
        Task<List<DatingUser>> GetUsers(bool onlyVerified, bool onlyToday, ICollection<DatingUserActionType> selectedAllActionTypes, ICollection<DatingUserActionType> selectedLastActionTypes, string searchText, int currentPage, int pageSize);
        
        Task<int> GetTotalUsersCount(bool onlyVerified, bool onlyToday, ICollection<DatingUserActionType> selectedAllActionTypes, ICollection<DatingUserActionType> selectedLastActionTypes, string searchText);

        Task<DatingUser> GetUserByExternalId(string externalId, CancellationToken cancellationToken);

        Task SaveDatingUser(DatingUser datingUser, DatingUserActionType actionType, string superLikeText, Guid datingAccountId, CancellationToken cancellationToken);

        Task AddToBlacklist(DatingUser user);
    }
}
