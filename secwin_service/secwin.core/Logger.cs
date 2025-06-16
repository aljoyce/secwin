using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace secwin.core
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
            var loggerFactory = LoggerFactory.Create(builder => {
                builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<LoggerSingleton>();
        }

        public ILogger Logger => _logger;
    }
}
