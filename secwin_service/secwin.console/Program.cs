using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using secwin.core;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace console
{
    /// <summary>
    /// This console application is used for testing
    /// </summary>
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            builder.Services.Configure<SecwinAppSettings>(builder.Configuration.GetSection("SecwinAppSettings"));

            var settings = new SecwinAppSettings();
            builder.Configuration.GetSection("SecwinAppSettings").Bind(settings);

            builder.Logging
                .AddConsole()
                .SetMinimumLevel(LogLevel.Debug);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    if (!EventLog.SourceExists(settings.EventSource))
                        EventLog.CreateEventSource(settings.EventSource, settings.EventLog);

                    builder.Logging.AddEventLog(options =>
                    {
                        options.LogName = settings.EventLog;
                        options.SourceName = settings.EventSource;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating event log source: {ex.Message}");
                }
            }

            builder.Services.AddSingleton<IDashboardAdapter, DashboardAdapter>();
            builder.Services.AddSingleton<SearchService>();

            using var host = builder.Build();

            var dashboardAdapter = host.Services.GetRequiredService<IDashboardAdapter>();
            await dashboardAdapter.Connect();

            // removing this causes the console write line to appear before the connection message
            // race condition, find out why
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Press any key to exit.");
            // Console.WriteLine("Press any key to exit.");
            Console.ReadKey();

            dashboardAdapter.Disconnect();
        }
    }
}