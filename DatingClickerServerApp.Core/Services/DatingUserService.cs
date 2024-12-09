using DatingClickerServerApp.Common.Extensions;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;
using DatingClickerServerApp.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DatingClickerServerApp.Core.Services
{
    public class DatingUserService : IDatingUserService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public DatingUserService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<List<DatingUser>> GetUsers(bool onlyVerified, bool onlyToday, ICollection<DatingUserActionType> selectedAllActionTypes, ICollection<DatingUserActionType> selectedLastActionTypes, string searchText, int currentPage, int pageSize)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var query = ApplyFilters(dbContext, onlyVerified, onlyToday, selectedAllActionTypes, selectedLastActionTypes, searchText);

            return await query
                .OrderByDescending(u => u.UpdatedDate)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalUsersCount(bool onlyVerified, bool onlyToday, ICollection<DatingUserActionType> selectedAllActionTypes, ICollection<DatingUserActionType> selectedLastActionTypes, string searchText)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var query = ApplyFilters(dbContext, onlyVerified, onlyToday, selectedAllActionTypes, selectedLastActionTypes, searchText);

            return await query.CountAsync();
        }

        public async Task<DatingUser> GetUserByExternalId(string externalId, CancellationToken cancellationToken)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            return await dbContext.DatingUsers
                .Include(u => u.Actions)
                .Include(u => u.BlacklistedDatingUser)
                .FirstOrDefaultAsync(u => u.ExternalId == externalId, cancellationToken);
        }

        public async Task SaveDatingUser(DatingUser datingUser, DatingUserActionType actionType, string superLikeText, Guid datingAccountId, CancellationToken cancellationToken)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var dbDatingUser = await dbContext.DatingUsers.FirstOrDefaultAsync(du => du.ExternalId == datingUser.ExternalId, cancellationToken);

            if (dbDatingUser == null)
            {
                datingUser.CreatedDate = DateTime.UtcNow;
                datingUser.UpdatedDate = DateTime.UtcNow;

                datingUser.Actions =
                [
                    new DatingUserAction
                    {
                        CreatedDate = DateTime.UtcNow,
                        ActionType = actionType,
                        SuperLikeText = superLikeText,
                        DatingAccountId = datingAccountId
                    }
                ];

                await dbContext.DatingUsers.AddAsync(datingUser, cancellationToken);
            }
            else
            {
                dbDatingUser.UpdatedDate = DateTime.UtcNow;
                dbDatingUser.Name = datingUser.Name;
                dbDatingUser.IsVerified = datingUser.IsVerified;
                dbDatingUser.Age = datingUser.Age;
                dbDatingUser.HasChildren = datingUser.HasChildren;
                dbDatingUser.Height = datingUser.Height;
                dbDatingUser.PreviewUrl = datingUser.PreviewUrl;
                dbDatingUser.About = datingUser.About;
                dbDatingUser.Interests = datingUser.Interests;
                dbDatingUser.CityName = datingUser.CityName;
                dbDatingUser.JsonData = datingUser.JsonData;

                dbContext.DatingUsers.Update(dbDatingUser);

                await dbContext.DatingUserActions.AddAsync(new DatingUserAction
                {
                    CreatedDate = DateTime.UtcNow,
                    ActionType = actionType,
                    SuperLikeText = superLikeText,
                    DatingUserId = dbDatingUser.Id,
                    DatingAccountId = datingAccountId
                }, cancellationToken);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        public async Task AddToBlacklist(DatingUser user)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var isAlreadyBlacklisted = await dbContext.BlacklistedDatingUsers
                .AnyAsync(b => b.Id == user.Id);

            if (!isAlreadyBlacklisted)
            {
                var blacklistedUser = new BlacklistedDatingUser
                {
                    Id = user.Id,
                    CreatedDate = DateTime.UtcNow
                };

                await dbContext.BlacklistedDatingUsers.AddAsync(blacklistedUser);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                throw new InvalidOperationException($"{user.Name} уже находится в черном списке.");
            }
        }

        private IQueryable<DatingUser> ApplyFilters(AppDbContext dbContext, bool onlyVerified, bool onlyToday, ICollection<DatingUserActionType> selectedAllActionTypes, ICollection<DatingUserActionType> selectedLastActionTypes, string searchText)
        {
            IQueryable<DatingUser> query = dbContext.DatingUsers
                .Include(u => u.Actions.OrderByDescending(a => a.CreatedDate))
                .ThenInclude(a => a.DatingAccount)
                .Include(u => u.BlacklistedDatingUser);

            if (onlyVerified)
            {
                query = query.Where(u => u.IsVerified);
            }

            if (onlyToday)
            {
                var startOfToday = DateTime.UtcNow.GetStartOfDay();
                query = query.Where(u => u.UpdatedDate >= startOfToday);
            }

            if (selectedAllActionTypes.Count != 0)
            {
                query = query.Where(u => u.Actions.Any(a => selectedAllActionTypes.Contains(a.ActionType)));
            }

            if (selectedLastActionTypes.Count != 0)
            {
                query = query.Where(u => selectedLastActionTypes.Contains(u.Actions.First().ActionType));
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                var lowerSearchText = searchText.ToLower();
                query = query.Where(u => u.ExternalId.ToLower().Contains(lowerSearchText) ||
                                         u.Name.ToLower().Contains(lowerSearchText) ||
                                         u.About.ToLower().Contains(lowerSearchText) ||
                                         u.Interests.Any(i => i.ToLower().Equals(lowerSearchText)));
            }

            return query;
        }       
    }
}
