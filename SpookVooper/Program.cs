using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SpookVooper.Web.Workers;

namespace SpookVooper.Web
{
    public class Program
    {

        public static void Main(string[] args)
        {

            // Start site
            var host = CreateHostBuilder(args).UseDefaultServiceProvider(options =>
                    options.ValidateScopes = false)
                    .Build();

            SetupDB(host);

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:5000", "https://localhost:5001");
                }).ConfigureServices(services =>
                {
                    services.AddHostedService<EconomyWorker>();
                    services.AddHostedService<ExchangeWorker>();
                    services.AddHostedService<RecordWorker>();
                });
        
        public static void SetupDB(IHost host)
        {

        }
    }


}
