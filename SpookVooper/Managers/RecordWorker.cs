using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Managers;

namespace SpookVooper.Web.Workers
{
    public class RecordWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        public readonly ILogger<RecordWorker> _logger;
        public readonly UserManager<User> _userManager;
        public DateTime lastValueMinuteUpdate;
        public DateTime lastValueHourUpdate;
        public DateTime lastValueDayUpdate;

        public RecordWorker(ILogger<RecordWorker> logger,
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

                lastValueMinuteUpdate = (await tempc.ValueHistory.AsQueryable().Where(x => x.Type == "MINUTE").OrderByDescending(x => x.Time).FirstOrDefaultAsync()).Time;
                lastValueHourUpdate = (await tempc.ValueHistory.AsQueryable().Where(x => x.Type == "HOUR").OrderByDescending(x => x.Time).FirstOrDefaultAsync()).Time;
                lastValueDayUpdate = (await tempc.ValueHistory.AsQueryable().Where(x => x.Type == "DAY").OrderByDescending(x => x.Time).FirstOrDefaultAsync()).Time;
            }

            try
            {

                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        VooperContext context = scope.ServiceProvider.GetRequiredService<VooperContext>();

                        DateTime time = DateTime.UtcNow;

                        if (time.Subtract(lastValueMinuteUpdate).TotalMinutes >= 1)
                        {
                            lastValueMinuteUpdate = time;
                            await RecordValueHistory(context, "MINUTE", time);

                            if (time.Subtract(lastValueHourUpdate).TotalHours >= 1)
                            {
                                lastValueHourUpdate = time;
                                await RecordValueHistory(context, "HOUR", time);

                                if (time.Subtract(lastValueDayUpdate).TotalDays >= 1)
                                {
                                    lastValueDayUpdate = time;
                                    await RecordValueHistory(context, "DAY", time);
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Console.WriteLine("FATAL RECORD ERROR: " + e.StackTrace);
            }
        }

        public async Task RecordValueHistory(VooperContext context, string type, DateTime time)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            // First clear outdated records
            if (type == "MINUTE")
            {
                context.RemoveRange(context.ValueHistory.AsQueryable().Where(v => v.Type == "MINUTE" && EF.Functions.DateDiffMinute(v.Time, time) > 2000));
            }
            else if (type == "HOUR")
            {
                context.RemoveRange(context.ValueHistory.AsQueryable().Where(v => v.Type == "HOUR" && EF.Functions.DateDiffHour(v.Time, time) > 2000));
            }
            else if (type == "DAY")
            {
                context.RemoveRange(context.ValueHistory.AsQueryable().Where(v => v.Type == "DAY" && EF.Functions.DateDiffDay(v.Time, time) > 2000));
            }

            // Add new records

            List<ValueHistory> additions = new List<ValueHistory>();

            // User Portfolios

            foreach (Entity user in context.Users)
            {
                decimal portValue = await user.GetPortfolioValue();

                if (portValue > 0)
                {
                    ValueHistory hist = new ValueHistory()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Account_Id = user.Id,
                        Time = time,
                        Type = type,
                        Value = Math.Min(portValue, 9999999999M)
                    };

                    additions.Add(hist);
                }
            }

            // Group Portfolios

            foreach (Entity group in context.Groups)
            {
                decimal portValue = await group.GetPortfolioValue();

                if (portValue > 0)
                {
                    ValueHistory hist = new ValueHistory()
                    {
                        Id = Guid.NewGuid().ToString(),
                        Account_Id = group.Id,
                        Time = time,
                        Type = type,
                        Value = Math.Min(portValue, 9999999999M)
                    };

                    additions.Add(hist);
                }
            }

            // Stocks

            foreach (StockDefinition stock in context.StockDefinitions)
            {
                decimal value = stock.Current_Value;

                int volume = 0;

                Dictionary<string, int> volumesDict = null;

                if (type == "MINUTE")
                {
                    volumesDict = ExchangeManager.VolumesMinute;
                }
                else if (type == "HOUR")
                {
                    volumesDict = ExchangeManager.VolumesHour;
                }
                else
                {
                    volumesDict = ExchangeManager.VolumesDay;
                }

                if (volumesDict.ContainsKey(stock.Ticker))
                {
                    volume = volumesDict[stock.Ticker];
                    volumesDict[stock.Ticker] = 0;
                }

                ValueHistory hist = new ValueHistory()
                {
                    Id = Guid.NewGuid().ToString(),
                    Account_Id = stock.Ticker,
                    Time = time,
                    Type = type,
                    Value = Math.Min(value, 9999999999M),
                    Volume = volume
                };

                additions.Add(hist);
            }

            await context.AddRangeAsync(additions);
            await context.SaveChangesAsync();

            sw.Stop();

            _logger.LogInformation($"Added {additions.Count} financial records in {sw.Elapsed.Seconds} seconds.");
        }

    }
}
