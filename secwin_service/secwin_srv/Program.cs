using secwin_lib;

namespace secwin_srv
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.Configure<SecwinAppSettings>(builder.Configuration.GetSection("SecwinAppSettings"));

            builder.Services.AddSingleton<SocketServer>();
            builder.Services.AddSingleton<LoggerSingleton>();

            builder.Services.AddHostedService<Worker>();

            var host = builder.Build();
            host.Run();
        }
    }
}