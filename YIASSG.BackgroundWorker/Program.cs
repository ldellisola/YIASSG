using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace YIASSG.BackgroundWorker;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSystemd()
            .ConfigureServices((_, services) =>
            {
                services.AddHostedService<Worker>();
            });
    }
}