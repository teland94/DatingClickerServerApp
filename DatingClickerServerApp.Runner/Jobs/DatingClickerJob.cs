using DatingClickerServerApp.Core;
using DatingClickerServerApp.Runner.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DatingClickerServerApp.Runner.Jobs
{
    internal class DatingClickerJob : IJob
    {
        private readonly DatingClickerProcessor _datingClickerProcessor;
        private readonly DatingClickerJobSettings _settings;
        private readonly ILogger<DatingClickerJob> _logger;

        public DatingClickerJob(DatingClickerProcessor datingClickerProcessor,
            IOptions<DatingClickerJobSettings> options,
            ILogger<DatingClickerJob> logger)
        {
            _datingClickerProcessor = datingClickerProcessor;
            _settings = options.Value;
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

                await _datingClickerProcessor.ProcessDatingUsers(onlineOnly: false, _settings.ProcessDatingUsersRepeatCount, CancellationToken.None);

                _logger.LogInformation("DatingClickerJob execution completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while executing DatingClickerJob.");
            }
        }
    }
}
