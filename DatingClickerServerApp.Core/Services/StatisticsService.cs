using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;
using DatingClickerServerApp.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DatingClickerServerApp.Core.Services
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

        public StatisticsService(IDbContextFactory<AppDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<StatisticsResult> GetStatistics(DateTime startDate, bool isVerified, bool includeInactive)
        {
            using var dbContext = _dbContextFactory.CreateDbContext();

            var endDate = DateTime.UtcNow.AddDays(-7);
            var query = $@"
            SELECT 
                COUNT(*) AS ""{nameof(StatisticsResult.TotalUsers)}"",
                COUNT(CASE WHEN ""HasChildren"" = true THEN 1 END) AS ""{nameof(StatisticsResult.UsersWithChildren)}"",
                COUNT(CASE WHEN 'it' = ANY(""Interests"") THEN 1 END) AS ""{nameof(StatisticsResult.UsersWithITInterests)}"",
                COUNT(CASE WHEN 'it' = ANY(""Interests"") AND ""HasChildren"" = false THEN 1 END) AS ""{nameof(StatisticsResult.UsersWithITInterestsNoChildren)}""
            FROM public.""DatingUsers""
            WHERE ""CreatedDate"" >= @p0 
                {(includeInactive ? "" : $@"AND (""JsonData""->>'last_active_at')::timestamp >= '{endDate:yyyy-MM-dd HH:mm:ss}'")}
                {(isVerified ? "AND \"IsVerified\" = true" : "")}";

            return await dbContext.Database
                .SqlQueryRaw<StatisticsResult>(query, startDate)
                .FirstOrDefaultAsync();
        }
    }
}
