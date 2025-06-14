using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace secwin_lib
{
    public sealed class LoggerSingleton
    {
        private static readonly Lazy<LoggerSingleton> lazy = new Lazy<LoggerSingleton>(
                () => new LoggerSingleton(),
                LazyThreadSafetyMode.ExecutionAndPublication
            );

        public static LoggerSingleton Instance { get { return lazy.Value; } }

        private readonly ILogger _logger;

        private LoggerSingleton()
        {
            string eventSource = "SecWin Service";
            string eventLog = "Centripetal";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!EventLog.SourceExists(eventSource))
                {
                    EventLog.CreateEventSource(eventSource, eventLog);
                }
            }
            
            var loggerFactory = LoggerFactory.Create(builder => {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
                
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    builder.AddEventLog(options =>
                    {
                        options.SourceName = eventSource;
                        options.LogName = eventLog;
                    });
                 }
            });

            _logger = loggerFactory.CreateLogger<LoggerSingleton>();
        }

        public ILogger Logger => _logger;
    }
}
