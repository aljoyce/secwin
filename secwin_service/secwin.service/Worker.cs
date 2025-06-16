using secwin.core;

namespace secwin.service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly IDashboardAdapter _dashboardAdapter;

        public Worker(ILogger<Worker> logger, IDashboardAdapter dashboardAdapter)

        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _dashboardAdapter = dashboardAdapter ?? throw new ArgumentNullException(nameof(dashboardAdapter));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _dashboardAdapter.Connect();
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping the service");
            _dashboardAdapter.Disconnect();
            await base.StopAsync(cancellationToken);
        }
    }
}
