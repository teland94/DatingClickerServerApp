using DatingClickerServerApp.Common;
using DatingClickerServerApp.Common.Persistence;
using DatingClickerServerApp.Common.Services;
using DatingClickerServerApp.Runner;
using DatingClickerServerApp.Runner.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.IO;
using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Runner.Configuration;
using DatingClickerServerApp.Common.Services.Interfaces;

namespace DatingClickerConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/log.txt"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            try
            {
                Log.Information("Starting application");

                var host = Host.CreateDefaultBuilder(args)
                    .UseWindowsService()
                    .UseSerilog()
                    .ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddJsonFile("appsettings.common.json", optional: true, reloadOnChange: true);
                    })
                    .ConfigureServices((context, services) =>
                    {
                        var configuration = context.Configuration;

                        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog())
#if DEBUG
                            .AddHostedService<DatingClickerService>()
#endif
                            .AddQuartz(configure =>
                            {
                                var jobKey = new JobKey(nameof(DatingClickerJob));

                                var cronExpressions = configuration.GetSection("SchedulerCronExpressions").Get<IEnumerable<string>>().ToList();

                                configure.AddJob<DatingClickerJob>(opts => opts.WithIdentity(jobKey));

                                foreach (var cronExpression in cronExpressions)
                                {
                                    configure.AddTrigger(trigger => trigger
                                        .ForJob(jobKey)
                                        .WithIdentity($"{nameof(DatingClickerJob)}Trigger-{cronExpression}")
                                        .WithCronSchedule(cronExpression));
                                }
                            })
                            .AddQuartzHostedService(q => q.WaitForJobsToComplete = true)
                            .AddScoped<IDatingClickerService, VkDatingClickerService>()
                            .AddSingleton<IEncryptionService, EncryptionService>()
                            .AddDbContext<AppDbContext>(options =>
                                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")))
                            .AddScoped(provider =>
                            {
                                var datingClickerService = provider.GetRequiredService<IDatingClickerService>();
                                var dbContext = provider.GetRequiredService<AppDbContext>();
                                var encryptionService = provider.GetRequiredService<IEncryptionService>();

                                var settings = new DatingClickerProcessorSettings
                                {
                                    SignIn = configuration.GetRequiredSection(nameof(DatingClickerProcessorSettings.SignIn)).GetChildren().ToDictionary(s => s.Key, s => s.Value),
                                    LikeCriteries = configuration.GetSection(nameof(DatingClickerProcessorSettings.LikeCriteries)).Get<DatingUserCriteriesSettings>()
                                };

                                return new DatingClickerProcessor(datingClickerService, settings, dbContext, encryptionService);
                            })
                            .Configure<DatingClickerJobSettings>(configuration.GetSection("DatingClickerJob"))
                            .Configure<EncryptionSettings>(configuration.GetSection("Encryption"));
                    })
                    .Build();

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
