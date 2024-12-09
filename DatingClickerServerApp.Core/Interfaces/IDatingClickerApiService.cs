using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Common.Model;

namespace DatingClickerServerApp.Core.Interfaces
{
    public interface IDatingClickerApiService
    {
        Task<List<DatingUser>> GetRecommendedUsers(bool onlineOnly, CancellationToken cancellationToken = default);

        Task<DatingUser> GetRecommendedUser(string externalId, CancellationToken cancellationToken = default);

        bool IsUserLikeable(DatingUser user, DatingUserCriteriesSettings datingUserCriteriesInfo);

        bool IsUserSuperLikeable(DatingUser user, DatingUserCriteriesSettings datingUserCriteriesInfo);

        Task<string> LikeUser(string externalId, CancellationToken cancellationToken = default);

        Task<string> DislikeUser(string externalId, CancellationToken cancellationToken = default);

        Task<string> SuperLikeUser(string externalId, string superLikeText = null, CancellationToken cancellationToken = default);

        Task<DatingAppUser> SignIn(IDictionary<string, string> signInSettings, CancellationToken cancellationToken);
    }
}