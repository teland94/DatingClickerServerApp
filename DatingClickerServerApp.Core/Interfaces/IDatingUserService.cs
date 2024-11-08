using DatingClickerServerApp.Common.Model;

namespace DatingClickerServerApp.Core.Interfaces
{
    public interface IDatingUserService
    {
        Task<List<DatingUser>> GetUsers(bool onlyVerified, bool onlyToday, ICollection<DatingUserActionType> selectedAllActionTypes, ICollection<DatingUserActionType> selectedLastActionTypes, string searchText, int currentPage, int pageSize);
        
        Task<int> GetTotalUsersCount(bool onlyVerified, bool onlyToday, ICollection<DatingUserActionType> selectedAllActionTypes, ICollection<DatingUserActionType> selectedLastActionTypes, string searchText);
        
        Task AddToBlacklist(DatingUser user);
    }
}
