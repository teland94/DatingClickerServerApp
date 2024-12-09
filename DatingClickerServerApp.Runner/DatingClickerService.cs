using DatingClickerServerApp.Runner.Jobs;
using Microsoft.Extensions.Hosting;
using Quartz;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace DatingClickerServerApp.Runner
{
    internal class DatingClickerService(ISchedulerFactory schedulerFactory, ILogger<DatingClickerService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting DatingClickerService...");

            var scheduler = await schedulerFactory.GetScheduler(cancellationToken);

            var jobKey = new JobKey(nameof(DatingClickerJob));

            logger.LogInformation("Triggering job: {JobKey}", jobKey);

            await scheduler.TriggerJob(jobKey, cancellationToken);

            logger.LogInformation("Job triggered successfully.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping DatingClickerService...");

            return Task.CompletedTask;
        }
    }
}
