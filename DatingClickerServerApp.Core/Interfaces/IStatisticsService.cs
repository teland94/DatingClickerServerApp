using DatingClickerServerApp.Common.Model;

namespace DatingClickerServerApp.Core.Interfaces
{
    public interface IStatisticsService
    {
        Task<StatisticsResult> GetStatistics(DateTime startDate, bool isVerified, bool includeInactive);
    }    
}
