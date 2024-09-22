using DatingClickerServerApp.Common.Model;

namespace DatingClickerServerApp.Common.Services
{
    public interface IDatingClickerService
    {
        Task<List<DatingUser>> GetRecommendedUsers(bool onlineOnly, CancellationToken cancellationToken = default);

        Task<DatingUser> GetRecommendedUser(string externalId, CancellationToken cancellationToken = default);

        bool IsUserLikeable(DatingUser user);

        bool IsUserSuperLikeable(DatingUser user, DatingUserCriteriesInfo datingUserCriteriesInfo = null);

        Task<string> LikeUser(string externalId, CancellationToken cancellationToken = default);

        Task<string> DislikeUser(string externalId, CancellationToken cancellationToken = default);

        Task<string> SuperLikeUser(string externalId, string superLikeText = null, CancellationToken cancellationToken = default);

        Task<User> SignIn(IDictionary<string, string> signInSettings, CancellationToken cancellationToken);
    }
}