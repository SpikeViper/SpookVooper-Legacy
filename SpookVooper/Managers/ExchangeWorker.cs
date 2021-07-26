using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.Managers;

namespace SpookVooper.Web.Workers
{
    public class ExchangeWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly ILogger<ExchangeWorker> _logger;
        public readonly UserManager<User> _userManager;
        public DateTime lastValueMinuteUpdate = DateTime.UtcNow;
        public DateTime lastValueHourUpdate = DateTime.UtcNow;
        public DateTime lastValueDayUpdate = DateTime.UtcNow;

        public ExchangeWorker(ILogger<ExchangeWorker> logger,
                            UserManager<User> userManager,
                            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _userManager = userManager;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Task task = Task.Run(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await ExchangeManager.RunTrades();
                        }
                        catch(System.Exception e)
                        {
                            Console.WriteLine("FATAL EXCHANGE ERROR:");
                            Console.WriteLine(e.Message);
                        }
                    }
                });

                while (!task.IsCompleted)
                {
                    _logger.LogInformation("Exchange running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(60000, stoppingToken);
                }

                _logger.LogInformation("Exchange task stopped at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Restarting.", DateTimeOffset.Now);

            }
        }
    }
}