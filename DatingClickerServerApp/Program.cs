using DatingClickerServerApp.Common.Configuration;
using DatingClickerServerApp.Common.Persistence;
using DatingClickerServerApp.Common.Services;
using DatingClickerServerApp.Common.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Reflection;

namespace DatingClickerServerApp
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

            builder.Services.AddScoped<IDatingClickerService, VkDatingClickerService>();
            builder.Services.AddSingleton<IEncryptionService, EncryptionService>();

            builder.Services.AddDbContextFactory<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

            app.Use(async (context, next) =>
            {
                var culture = new CultureInfo("ru-RU");
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;

                await next();
            });

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}