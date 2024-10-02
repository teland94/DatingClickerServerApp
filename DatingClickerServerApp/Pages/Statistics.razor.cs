using DatingClickerServerApp.Common.Persistence;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using DatingClickerServerApp.Common.Extensions;

namespace DatingClickerServerApp.Pages
{
    public partial class Statistics : ComponentBase
    {
        private StatisticsResult _statistics = new();
        private StatisticsResult _verifiedStatistics = new();

        [Inject] AppDbContext DbContext { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await LoadStatistics(DateTime.UtcNow.GetStartOfDay());
        }

        private async Task SetPeriod(DateTime startDate)
        {
            await LoadStatistics(startDate);
        }

        private async Task LoadStatistics(DateTime startDate)
        {
            _statistics = await GetStatistics(startDate, false, false);
            _verifiedStatistics = await GetStatistics(startDate, true, false);
        }

        private Task<StatisticsResult> GetStatistics(DateTime startDate, bool isVerified, bool includeInactive)
        {
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

            return DbContext.Database
                .SqlQueryRaw<StatisticsResult>(query, startDate)
                .FirstOrDefaultAsync();
        }
    }

    public class StatisticsResult
    {
        public int TotalUsers { get; set; }
        public int UsersWithChildren { get; set; }
        public int UsersWithITInterests { get; set; }
        public int UsersWithITInterestsNoChildren { get; set; }
    }
}
