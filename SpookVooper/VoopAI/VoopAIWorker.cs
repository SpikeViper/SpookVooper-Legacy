using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using SpookVooper.Data.Services;
using SpookVooper.Web.Entities;

namespace SpookVooper.VoopAIService
{
    public class VoopAIWorker : BackgroundService
    {
        public IServiceProvider Services { get; }
        public readonly ILogger<VoopAIWorker> _logger;
        public readonly UserManager<User> _userManager;
        public readonly IConnectionHandler _connectionHandler;
        public VoopAI voopAI;

        public VoopAIWorker(IServiceProvider services, ILogger<VoopAIWorker> logger,
                             UserManager<User> userManager,
                            SignInManager<User> signInManager, IConnectionHandler connectionHandler)
        {
            Services = services;
            _logger = logger;
            _userManager = userManager;
            _connectionHandler = connectionHandler;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                voopAI = new VoopAI();
                VoopAI.service = this;
                VoopAI.logger = _logger;

                Task task = Task.Run(voopAI.MainAsync);

                while (!task.IsCompleted)
                {
                    _logger.LogInformation("VoopAI running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(60000, stoppingToken);
                }

                _logger.LogInformation("VoopAI task stopped at: {time}", DateTimeOffset.Now);
                _logger.LogInformation("Restarting.", DateTimeOffset.Now);
            }
        }
    }
}
