using DatingClickerServerApp.Common.Model;

namespace DatingClickerServerApp.Core.Interfaces
{
    public interface IDatingAccountService
    {
        Task<DatingAccount> SaveDatingAccount(DatingAppUser user, IDictionary<string, string> signIn, CancellationToken cancellationToken);
    }
}
