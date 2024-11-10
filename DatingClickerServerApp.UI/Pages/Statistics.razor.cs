using Microsoft.AspNetCore.Components;
using DatingClickerServerApp.Common.Extensions;
using DatingClickerServerApp.Common.Model;
using DatingClickerServerApp.Core.Interfaces;

namespace DatingClickerServerApp.UI.Pages
{
    public partial class Statistics : ComponentBase
    {
        private StatisticsResult _statistics = new();
        private StatisticsResult _verifiedStatistics = new();

        [Inject] private IStatisticsService StatisticsService { get; set; }

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
            _statistics = await StatisticsService.GetStatistics(startDate, false, false);
            _verifiedStatistics = await StatisticsService.GetStatistics(startDate, true, false);
        }
    }    
}
