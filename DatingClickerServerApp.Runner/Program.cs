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
using DatingClickerServerApp.Core.Interfaces;
using DatingClickerServerApp.Core.Services;
using DatingClickerServerApp.Core.Persistence;
using DatingClickerServerApp.Core;
using System.Net.Http;
using System.Net;
using OpenAI;

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
                            .AddHttpClient()
                            .AddScoped<IDatingClickerApiService, DatingClickerApiService>()
                            .AddScoped<IEncryptionService, EncryptionService>()
                            .AddScoped<IDatingAccountService, DatingAccountService>()
                            .AddScoped(sp =>
                            {
                                var httpClient = configuration.GetValue<bool>("OpenAI:UseProxy")
                                    ? new HttpClient(new HttpClientHandler
                                    {
                                        Proxy = new WebProxy(configuration["OpenAI:Proxy:Address"])
                                        {
                                            Credentials = string.IsNullOrEmpty(configuration["OpenAI:Proxy:Username"]) ? null
                                                         : new NetworkCredential(configuration["OpenAI:Proxy:Username"], configuration["OpenAI:Proxy:Password"])
                                        },
                                        UseProxy = true
                                    })
                                    : new HttpClient();

                                return new OpenAIClient(configuration["OpenAI:ApiKey"], client: httpClient);
                            })
                            .AddScoped<IDatingUserService, DatingUserService>()
                            .AddDbContextFactory<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")))
                            .AddScoped<DatingClickerProcessor>()
                            .Configure<DatingClickerApiSettings>(configuration.GetSection("DatingClickerApi"))
                            .Configure<DatingClickerProcessorSettings>(config =>
                            {
                                config.SignIn = configuration.GetSection(nameof(DatingClickerProcessorSettings.SignIn)).GetChildren().ToDictionary(s => s.Key, s => s.Value);

                                config.UseChatBot = configuration.GetValue<bool>(nameof(DatingClickerProcessorSettings.UseChatBot));

                                config.LikeCriteries = configuration.GetSection(nameof(DatingClickerProcessorSettings.LikeCriteries)).Get<DatingUserCriteriesSettings>()
                                                      ?? new DatingUserCriteriesSettings(155, [], ["plus size", "plus-size", "мужчину для кое чего интересного", "показать себя", "покажу себя"], null, false);

                                config.DislikeIfUserHasExistingSuperLikeAction = configuration.GetValue<bool>(nameof(DatingClickerProcessorSettings.DislikeIfUserHasExistingSuperLikeAction));
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
