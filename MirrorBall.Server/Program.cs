namespace MirrorBall.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOptions();
            
            builder.Host.ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Trace);
            });

            var hostName = System.Net.Dns.GetHostName();
            var hostOptions = builder.Configuration.GetSection(hostName);

            if (string.IsNullOrWhiteSpace(hostOptions.Get<MirrorOptions>()?.PeerName))
            {
                throw new InvalidOperationException($@"
                    Missing configuration for host {hostName}
                ");
            }

            builder.Services.Configure<MirrorOptions>(hostOptions);

            var app = builder.Build();
            app.UseStaticFiles();
            app.MapControllers();
            app.Run();
        }
    }
}
