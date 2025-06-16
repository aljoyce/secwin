using secwin.core;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace secwin.service
{
    public class Program
    {
        public static void Main(string[] args)
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

            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}