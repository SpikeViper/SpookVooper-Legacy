using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SpookVooper.Web.DB;
using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Government;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Hubs;
using SpookVooper.Web.Extensions;

namespace SpookVooper.Web.Managers
{
    public static class ExchangeManager
    {

        public static Dictionary<string, int> VolumesMinute = new Dictionary<string, int>();
        public static Dictionary<string, int> VolumesHour = new Dictionary<string, int>();
        public static Dictionary<string, int> VolumesDay = new Dictionary<string, int>();

        public static async Task RunTrades()
        {

#if DEBUG
            // Prevent local testing from running the stock exchange
            return;
#endif
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                foreach (StockDefinition def in context.StockDefinitions)
                {
                    StockOffer sell = await GetLowestSellOffer(def.Ticker, context);
                    StockOffer buy = await GetHighestBuyOffer(def.Ticker, context);

                    // If buy > sell, trade occurs
                    if (buy != null &&
                        sell != null &&
                        buy.Target >= sell.Target)
                    {
                        GovControls gov = await context.GovControls.AsQueryable().FirstAsync();

                        int remainder = buy.Amount - sell.Amount;

                        decimal beforePrice = def.Current_Value;

                        decimal tradePrice = buy.Target;

                        int tradeAmount = Math.Min(buy.Amount, sell.Amount);

                        string buyer = buy.Owner_Id;
                        string seller = sell.Owner_Id;

                        string buyId = buy.Id;
                        string sellId = sell.Id;

                        // If remainder is 0, they cancel out perfectly
                        // If remainder > 0, the buy order is not finished and sell is deleted
                        // If remainder < 0, the sell order is not finished and the buy is deleted
                        if (remainder == 0)
                        {
                            context.StockOffers.Remove(sell);
                            context.StockOffers.Remove(buy);
                        }
                        else if (remainder > 0)
                        {
                            context.StockOffers.Remove(sell);
                            buy.Amount = buy.Amount - sell.Amount;
                            context.StockOffers.Update(buy);
                        }
                        else
                        {
                            context.StockOffers.Remove(buy);
                            sell.Amount = sell.Amount - buy.Amount;
                            context.StockOffers.Update(sell);
                        }

                        // Volume stuff
                        if (VolumesMinute.ContainsKey(def.Ticker))
                        {
                            VolumesMinute[def.Ticker] += tradeAmount;
                            VolumesHour[def.Ticker] += tradeAmount;
                            VolumesDay[def.Ticker] += tradeAmount;
                        }
                        else
                        {
                            VolumesMinute.Add(def.Ticker, tradeAmount);
                            VolumesHour.Add(def.Ticker, tradeAmount);
                            VolumesDay.Add(def.Ticker, tradeAmount);
                        }
                        // End volume stuff

                        decimal totalTrade = tradeAmount * tradePrice;
                        decimal tax = totalTrade * (gov.CapitalGainsTaxRate / 100);

                        await new TransactionRequest(EconomyManager.VooperiaID, sell.Owner_Id, totalTrade - tax, $"Stock sale: {tradeAmount} {def.Ticker}@¢{tradePrice.Round()}", ApplicableTax.None, true).Execute();
                        gov.CapitalGainsTaxRevenue += tax;
                        gov.UBIAccount += (tax * (gov.UBIBudgetPercent / 100.0m));

                        context.GovControls.Update(gov);

                        await context.SaveChangesAsync();

                        await AddStock(def.Ticker, tradeAmount, buyer, context);

                        Console.WriteLine($"Processed Stock sale: {tradeAmount} {def.Ticker}@¢{tradePrice.Round()}");

                        decimal trueValue = 0.0m;

                        StockOffer nextBuy = await GetHighestBuyOffer(def.Ticker, context);
                        StockOffer nextSell = await GetLowestSellOffer(def.Ticker, context);

                        if (nextBuy == null && nextSell == null)
                        {
                            trueValue = tradePrice;
                        }
                        else if (nextBuy == null)
                        {
                            trueValue = nextSell.Target;
                        }
                        else if (nextSell == null)
                        {
                            trueValue = nextBuy.Target;
                        }
                        else
                        {
                            trueValue = (nextBuy.Target + nextSell.Target) / 2;
                        }

                        StockTradeModel noti = new StockTradeModel()
                        {
                            Ticker = def.Ticker,
                            Amount = tradeAmount,
                            Price = tradePrice,
                            True_Price = trueValue,
                            From = sell.Owner_Id,
                            To = buy.Owner_Id,
                            Buy_Id = buyId,
                            Sell_Id = sellId
                        };

                        def.Current_Value = noti.True_Price;
                        context.StockDefinitions.Update(def);

                        Entity sellAccount = await Entity.FindAsync(seller);
                        sellAccount.Credits_Invested -= totalTrade;
                        if (sellAccount is User) context.Users.Update((User)sellAccount);
                        else if (sellAccount is Group) context.Groups.Update((Group)sellAccount);


                        Entity buyAccount = await Entity.FindAsync(buyer);
                        buyAccount.Credits_Invested += totalTrade;
                        if (buyAccount is User) context.Users.Update((User)buyAccount);
                        else if (buyAccount is Group) context.Groups.Update((Group)buyAccount);

                        await context.SaveChangesAsync();

                        await ExchangeHub.Current.Clients.All.SendAsync("StockTrade", JsonConvert.SerializeObject(noti));

                        if (trueValue < beforePrice)
                        {
                            //VoopAI.ecoChannel.SendMessageAsync($":chart_with_downwards_trend: ({def.Ticker}) Trade: {tradeAmount}@{buy.Target}, price drop to ¢{trueValue.Round()}");
                        }
                        else if (trueValue > beforePrice)
                        {
                            //VoopAI.ecoChannel.SendMessageAsync($":chart_with_upwards_trend: ({def.Ticker}) Trade: {tradeAmount}@{buy.Target}, price increase to ¢{trueValue.Round()}");
                        }
                    }

                    // Otherwise move on to the next stock
                }
            }
        }

        public static async Task AddStock(string ticker, int amount, string svid, VooperContext context)
        {
            StockObject stock = await context.StockObjects.AsQueryable().FirstOrDefaultAsync(s => s.Owner_Id == svid && s.Ticker == ticker);

            if (stock != null)
            {
                stock.Amount += amount;
                context.StockObjects.Update(stock);
            }
            else
            {
                stock = new StockObject()
                {
                    Amount = amount,
                    Ticker = ticker,
                    Id = Guid.NewGuid().ToString(),
                    Owner_Id = svid
                };

                await context.StockObjects.AddAsync(stock);
            }

            await context.SaveChangesAsync();
        }

        public static async Task<StockOffer> GetLowestSellOffer(string ticker, VooperContext context)
        {
            StockOffer lowest = await context.StockOffers
                                             .AsQueryable()
                                             .Where(s => s.Ticker == ticker && s.Order_Type == "SELL")
                                             .OrderBy(s => s.Target)
                                             .FirstOrDefaultAsync();

            return lowest;
        }

        public static async Task<StockOffer> GetHighestBuyOffer(string ticker, VooperContext context)
        {
            StockOffer highest = await context.StockOffers
                                              .AsQueryable()
                                              .Where(s => s.Ticker == ticker && s.Order_Type == "BUY")
                                              .OrderByDescending(s => s.Target)
                                              .FirstOrDefaultAsync();

            return highest;
        }
    }
}
