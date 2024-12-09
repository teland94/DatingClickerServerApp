using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;
using DatingClickerServerApp.Core.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DatingClickerServerApp.Core.Services
{
    public class DatingAccountService : IDatingAccountService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
        private readonly IEncryptionService _encryptionService;

        public DatingAccountService(
            IDbContextFactory<AppDbContext> dbContextFactory,
            IEncryptionService encryptionService) 
        {
            _dbContextFactory = dbContextFactory;
            _encryptionService = encryptionService;
        }

        public async Task<DatingAccount> SaveDatingAccount(DatingAppUser user, IDictionary<string, string> signIn, CancellationToken cancellationToken)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var dbDatingAccount = await dbContext.DatingAccounts.FirstOrDefaultAsync(da => da.AppUserId == user.UserId, cancellationToken);

            if (dbDatingAccount != null)
            {
                dbDatingAccount.UpdatedDate = DateTime.UtcNow;
                dbDatingAccount.JsonProfileData = user.JsonData;

                dbContext.DatingAccounts.Update(dbDatingAccount);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var datingAccount = new DatingAccount
                {
                    AppUserId = user.UserId,
                    AppName = DatingAppNameType.MainDating,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow,
                    JsonAuthData = _encryptionService.Encrypt(JsonSerializer.Serialize(signIn)),
                    JsonProfileData = user.JsonData
                };

                var entityEntry = await dbContext.DatingAccounts.AddAsync(datingAccount, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                dbDatingAccount = entityEntry.Entity;
            }

            return dbDatingAccount;
        }
    }
}
