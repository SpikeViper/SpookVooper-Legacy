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
using SpookVooper.VoopAIService;

namespace SpookVooper.Web.Workers
{
    public class EconomyWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly ILogger<EconomyWorker> _logger;
        public readonly UserManager<User> _userManager;

        public EconomyWorker(ILogger<EconomyWorker> logger,
                            UserManager<User> userManager,
                            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _userManager = userManager;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                VooperContext tempc = scope.ServiceProvider.GetRequiredService<VooperContext>();
            }

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        VooperContext context = scope.ServiceProvider.GetRequiredService<VooperContext>();

                        await EconomyManager.RunQueue(context);
                    }
                }
            }
            catch(System.Exception e)
            {
                Console.WriteLine("FATAL TRANSACTION ERROR: " + e.StackTrace);
            }
        }
    }
}
