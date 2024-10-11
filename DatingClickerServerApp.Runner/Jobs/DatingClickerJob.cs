using DatingClickerServerApp.Common;
using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DatingClickerServerApp.Runner.Jobs
{
    public class DatingClickerJob : IJob
    {
        private readonly DatingClickerProcessor _datingClickerProcessor;
        private readonly ILogger<DatingClickerJob> _logger;

        public DatingClickerJob(DatingClickerProcessor datingClickerProcessor,
            ILogger<DatingClickerJob> logger)
        {
            _datingClickerProcessor = datingClickerProcessor;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("Executing DatingClickerJob...");

            try
            {
                _datingClickerProcessor.OnResultUpdated += result =>
                {
                    _logger.LogInformation("Result updated: {Result}", result);

                    return Task.CompletedTask;
                };

                await _datingClickerProcessor.ProcessDatingUsers(onlineOnly: false, repeatCount: 5, CancellationToken.None);

                _logger.LogInformation("DatingClickerJob execution completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing DatingClickerJob.");
            }
        }
    }
}
