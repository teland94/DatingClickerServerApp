using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Core;
using DatingClickerServerApp.Core.Interfaces;
using DatingClickerServerApp.Core.Persistence;
using DatingClickerServerApp.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System.Globalization;
using System.Net;
using System.Reflection;

namespace DatingClickerServerApp.UI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration
                .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location))
                .AddJsonFile("appsettings.common.json", optional: false, reloadOnChange: true);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            builder.Services.AddHttpClient();

            builder.Services.AddScoped<IDatingClickerApiService, DatingClickerApiService>();
            builder.Services.AddScoped<IEncryptionService, EncryptionService>();
            builder.Services.AddScoped<IDatingAccountService, DatingAccountService>();

            builder.Services.AddScoped(sp =>
            {
                var httpClient = builder.Configuration.GetValue<bool>("OpenAI:UseProxy")
                    ? new HttpClient(new HttpClientHandler
                    {
                        Proxy = new WebProxy(builder.Configuration["OpenAI:Proxy:Address"])
                        {
                            Credentials = string.IsNullOrEmpty(builder.Configuration["OpenAI:Proxy:Username"]) ? null
                                         : new NetworkCredential(builder.Configuration["OpenAI:Proxy:Username"], builder.Configuration["OpenAI:Proxy:Password"])
                        },
                        UseProxy = true
                    })
                    : new HttpClient();

                return new OpenAIClient(builder.Configuration["OpenAI:ApiKey"], client: httpClient);
            });

            builder.Services.AddScoped<IDatingUserService, DatingUserService>();
            builder.Services.AddScoped<IStatisticsService, StatisticsService>();

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<DatingClickerProcessor>();

            builder.Services.Configure<DatingClickerApiSettings>(builder.Configuration.GetSection("DatingClickerApi"));
            builder.Services.Configure<DatingClickerProcessorSettings>(config =>
            {
                var configuration = builder.Configuration;

                config.SignIn = configuration.GetSection(nameof(DatingClickerProcessorSettings.SignIn)).GetChildren().ToDictionary(s => s.Key, s => s.Value);

                config.UseChatBot = configuration.GetValue<bool>(nameof(DatingClickerProcessorSettings.UseChatBot));

                config.LikeCriteries = configuration.GetSection(nameof(DatingClickerProcessorSettings.LikeCriteries)).Get<DatingUserCriteriesSettings>()
                                      ?? new DatingUserCriteriesSettings(155, [], ["plus size", "plus-size", "мужчину для кое чего интересного", "показать себя", "покажу себя"], null, false);

                config.DislikeIfUserHasExistingSuperLikeAction = configuration.GetValue<bool>(nameof(DatingClickerProcessorSettings.DislikeIfUserHasExistingSuperLikeAction));
            });
            builder.Services.Configure<EncryptionSettings>(builder.Configuration.GetSection("Encryption"));

            var app = builder.Build();

            // Apply migrations automatically
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                dbContext.Database.EnsureCreated();
                dbContext.Database.Migrate();
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}