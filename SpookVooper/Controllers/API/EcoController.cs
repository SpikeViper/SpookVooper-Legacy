using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SpookVooper.Web;
using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Government;
using SpookVooper.Web.DB;
using SpookVooper.Web.Extensions;
using SpookVooper.Web.Hubs;
using SpookVooper.Web.Managers;

namespace SpookVooper.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EcoController : ControllerBase
    {
        private readonly VooperContext _context;

        public EcoController(VooperContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetBalance(string svid)
        {
            Entity account = await Entity.FindAsync(svid);

            if (account == null)
            {
                return NotFound($"Could not find entity with svid {svid}");
            }

            return account.Credits;
        }

        [HttpGet]
        public async Task<ActionResult<TaskResult>> SendTransactionByIDS(string from, string to, decimal amount, string auth, string detail)
        {
            if (string.IsNullOrWhiteSpace(auth)) return NotFound("Please specify authorization.");

            Entity fromAccount = await Entity.FindAsync(from);

            if (fromAccount == null) return NotFound("Could not find " + from);

            if (!(await fromAccount.HasPermissionWithKey(auth, "eco")))
            {
                return Unauthorized("You do not have permission to do that!");
            }

            ApplicableTax tax = ApplicableTax.None;

            Entity toAccount = await Entity.FindAsync(to);

            if (toAccount == null) return NotFound("Could not find " + to);

            if (toAccount is Group)
            {
                tax = ApplicableTax.Corporate;
            }
            else
            {
                if (detail.ToLower().Contains("sale"))
                {
                    tax = ApplicableTax.Sales;
                }
            }

            if (string.IsNullOrWhiteSpace(detail))
            {
                detail = "Undefined API Call";
            }

            TaskResult result = await new TransactionRequest(from, to, amount, detail, tax).Execute();

            if (result.Succeeded)
            {
                return Ok(result);
            }
            else
            {
                return NotFound(result);
            }
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetStockValue(string ticker)
        {
            StockDefinition stock = await _context.StockDefinitions.FindAsync(ticker);

            if (stock == null) return NotFound($"Could not find ticker {ticker}");

            return stock.Current_Value;
        }

        [HttpGet]
        public async Task<ActionResult<List<decimal>>> GetStockHistory(string ticker, string type, int count = 60, int interval = 1)
        {
            return _context.ValueHistory.AsQueryable().Where(h => h.Account_Id == ticker && h.Type == type)
                                                      .OrderByDescending(h => h.Time)
                                                      .Select(h => h.Value)
                                                      .AsEnumerable()
                                                      .Where((h, x) => x % interval == 0)
                                                      .Take(count)
                                                      .Reverse()
                                                      .ToList();
        }

        [HttpGet]
        public async Task<ActionResult<List<int>>> GetStockVolumeHistory(string ticker, string type, int count = 60, int interval = 1)
        {
            return _context.ValueHistory.AsQueryable().Where(h => h.Account_Id == ticker && h.Type == type)
                                                      .OrderByDescending(h => h.Time)
                                                      .Select(h => h.Volume)
                                                      .AsEnumerable()
                                                      .Where((h, x) => x % interval == 0)
                                                      .Take(count)
                                                      .Reverse()
                                                      .ToList();
        }


        [HttpGet]
        public async Task<ActionResult<TaskResult>> SubmitStockBuy(string ticker, int count, decimal price, string accountid, string auth)
        {
            // Account logic first
            Entity account = await Entity.FindAsync(accountid);
            if (account == null) return new TaskResult(false, "Failed to find account " + accountid);

            User authUser = await _context.Users.AsQueryable().FirstOrDefaultAsync(u => u.Api_Key == auth);
            if (authUser == null) return new TaskResult(false, "Failed to find auth account.");

            StockDefinition stockDef = await _context.StockDefinitions.FindAsync(ticker.ToUpper());
            if (stockDef == null) return new TaskResult(false, "Failed to find stock with ticker " + ticker);

            // Authority check
            if (!await account.HasPermissionAsync(authUser, "eco"))
            {
                return new TaskResult(false, "You do not have permission to trade for this entity!");
            }

            // At this point the account is authorized //
            if (count < 1) return new TaskResult(false, "You must buy a positive number of stocks!");

            if (price == 0)
            {
                StockOffer lowOffer = await ExchangeManager.GetLowestSellOffer(ticker, _context);
                if (lowOffer != null) price = lowOffer.Target;
                else return new TaskResult(false, "There is no market rate!");
            }

            if (price < 0)
            {
                return new TaskResult(false, "Negatives are not allowed!");
            }

            decimal totalPrice = count * price;

            if (totalPrice > account.Credits)
            {
                return new TaskResult(false, "You cannot afford this!");
            }

            TransactionRequest transaction = new TransactionRequest(account.Id, EconomyManager.VooperiaID, totalPrice, $"Stock purchase: {count} {ticker}@{price}", ApplicableTax.None, false);
            TaskResult transResult = await transaction.Execute();

            if (!transResult.Succeeded) return transResult;

            // Create offer
            StockOffer buyOffer = new StockOffer()
            {
                Id = Guid.NewGuid().ToString(),
                Amount = count,
                Order_Type = "BUY",
                Owner_Name = account.Name,
                Owner_Id = account.Id,
                Target = price.Round(),
                Ticker = ticker
            };

            _context.StockOffers.Add(buyOffer);
            await _context.SaveChangesAsync();

            string json = JsonConvert.SerializeObject(buyOffer);

            await ExchangeHub.Current.Clients.All.SendAsync("StockOffer", json);

            return new TaskResult(true, $"Successfully posted BUY order {buyOffer.Id}");
        }

        [HttpGet]
        public async Task<ActionResult<TaskResult>> SubmitStockSell(string ticker, int count, decimal price, string accountid, string auth)
        {
            // Account logic first
            Entity account = await Entity.FindAsync(accountid);
            if (account == null) return new TaskResult(false, "Failed to find account " + accountid);

            User authUser = await _context.Users.AsQueryable().FirstOrDefaultAsync(u => u.Api_Key == auth);
            if (authUser == null) return new TaskResult(false, "Failed to find auth account.");

            StockDefinition stockDef = await _context.StockDefinitions.FindAsync(ticker.ToUpper());
            if (stockDef == null) return new TaskResult(false, "Failed to find stock with ticker " + ticker);

            // Authority check
            if (!await account.HasPermissionAsync(authUser, "eco"))
            {
                return new TaskResult(false, "You do not have permission to trade for this entity!");
            }

            if (count < 1) return new TaskResult(false, "You must sell a positive number of stocks!");

            // At this point the account is authorized //
            // Find currently owned stock
            var owned = await _context.StockObjects.AsQueryable().FirstOrDefaultAsync(s => s.Owner_Id == accountid && s.Ticker == ticker);

            if (owned == null || count > owned.Amount) return new TaskResult(false, "You don't own that many stocks!");

            if (price < 0)
            {
                return new TaskResult(false, "Negatives are not allowed!");
            }

            if (price == 0)
            {
                StockOffer highOffer = await ExchangeManager.GetHighestBuyOffer(ticker, _context);
                if (highOffer != null) price = highOffer.Target;
                else return new TaskResult(false, "There is no market rate!");
            }

            decimal totalPrice = count * price;

            // Perform transaction
            owned.Amount -= count;

            // If amount hit 0, remove the object
            if (owned.Amount == 0)
            {
                _context.StockObjects.Remove(owned);
            }
            else
            {
                _context.StockObjects.Update(owned);
            }

            // Create offer
            StockOffer sellOffer = new StockOffer()
            {
                Id = Guid.NewGuid().ToString(),
                Amount = count,
                Order_Type = "SELL",
                Owner_Name = account.Name,
                Owner_Id = account.Id,
                Target = price.Round(),
                Ticker = ticker
            };

            _context.StockOffers.Add(sellOffer);
            await _context.SaveChangesAsync();

            string json = JsonConvert.SerializeObject(sellOffer);

            await ExchangeHub.Current.Clients.All.SendAsync("StockOffer", json);

            return new TaskResult(true, $"Successfully posted SELL order {sellOffer.Id}");
        }

        [HttpGet]
        public async Task<ActionResult<TaskResult>> CancelOrder(string orderid, string accountid, string auth)
        {
            // Account logic first
            Entity account = await Entity.FindAsync(accountid);
            if (account == null) return new TaskResult(false, "Failed to find account " + accountid);

            User authUser = await _context.Users.AsQueryable().FirstOrDefaultAsync(u => u.Api_Key == auth);
            if (authUser == null) return new TaskResult(false, "Failed to find auth account.");

            StockOffer stockOffer = await _context.StockOffers.FindAsync(orderid);
            if (stockOffer == null) return new TaskResult(false, "Failed to find offer " + orderid);

            // Authority check
            if (!await account.HasPermissionAsync(authUser, "eco"))
            {
                return new TaskResult(false, "You do not have permission to trade for this entity!");
            }

            if (stockOffer.Order_Type == "BUY")
            {
                _context.StockOffers.Remove(stockOffer);

                await _context.SaveChangesAsync();

                // Refund user and delete offer
                decimal refund = stockOffer.Amount * stockOffer.Target;

                TransactionRequest transaction = new TransactionRequest(EconomyManager.VooperiaID, account.Id, refund, $"Stock buy cancellation: {stockOffer.Amount} {stockOffer.Ticker}@{stockOffer.Target}", ApplicableTax.None, true);
                TaskResult transResult = await transaction.Execute();

                if (!transResult.Succeeded) return transResult;
            }
            else if (stockOffer.Order_Type == "SELL")
            {
                _context.StockOffers.Remove(stockOffer);

                await _context.SaveChangesAsync();

                // Refund user stock and delete offer
                await ExchangeManager.AddStock(stockOffer.Ticker, stockOffer.Amount, accountid, _context);
            }

            string json = JsonConvert.SerializeObject(stockOffer);

            await ExchangeHub.Current.Clients.All.SendAsync("StockOfferCancel", json);

            return new TaskResult(true, "Successfully removed order.");
        }

        // Returns cheapest buy price on the market
        [HttpGet]
        public async Task<ActionResult<decimal>> GetStockBuyPrice(string ticker)
        {
            decimal lowest = 0;
            StockOffer lowOffer = await ExchangeManager.GetLowestSellOffer(ticker, _context);
            if (lowOffer != null) lowest = lowOffer.Target;

            if (lowest == 0) return NotFound($"No stocks with ticker {ticker} are for sale.");

            return lowest;
        }

        public class OfferInfo
        {
            [JsonProperty("Target")]
            public decimal Target { get; set; }

            [JsonProperty("Amount")]
            public int Amount { get; set; }
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetQueueInfo(string ticker, string type)
        {
            Dictionary<decimal, OfferInfo> queueInfo = new Dictionary<decimal, OfferInfo>();

            IEnumerable<StockOffer> query = _context.StockOffers.AsQueryable().Where(s => s.Ticker == ticker && s.Order_Type == type).OrderByDescending(s => s.Target);

            foreach (StockOffer offer in query)
            {
                if (queueInfo.ContainsKey(offer.Target))
                {
                    queueInfo[offer.Target].Amount += offer.Amount;
                }
                else
                {
                    OfferInfo info = new OfferInfo()
                    {
                        Amount = offer.Amount,
                        Target = offer.Target
                    };

                    queueInfo.Add(info.Target, info);
                }
            }

            IEnumerable<OfferInfo> infoList = null;

            if (type == "SELL")
            {
                infoList = queueInfo.Values.TakeLast(10);
            }
            else
            {
                infoList = queueInfo.Values.Take(10);
            }

            return JsonConvert.SerializeObject(infoList);
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetUserStockOffers(string ticker, string svid)
        {
            return JsonConvert.SerializeObject(await _context.StockOffers.AsQueryable().Where(s => s.Ticker == ticker && s.Owner_Id == svid).OrderBy(s => s.Order_Type + s.Target).ToListAsync());
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetDistrictWealth(string id)
        {
            District district = await _context.Districts.FindAsync(id);

            if (district == null) return NotFound("Could not find " + id);

            decimal userWealth = _context.Users.AsQueryable().Where(u => u.district.ToLower() == id.ToLower()).Sum(u => u.Credits);
            decimal groupWealth = _context.Groups.AsQueryable().Where(g => g.District_Id.ToLower() == id.ToLower()).Sum(g => g.Credits);

            return userWealth + groupWealth;
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetDistrictUserWealth(string id)
        {
            District district = await _context.Districts.FindAsync(id);

            if (district == null) return NotFound("Could not find " + id);

            decimal userWealth = _context.Users.AsQueryable().Where(u => u.district.ToLower() == id.ToLower()).Sum(u => u.Credits);

            return userWealth;
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetDistrictGroupWealth(string id)
        {
            District district = await _context.Districts.FindAsync(id);

            if (district == null) return NotFound("Could not find " + id);

            decimal groupWealth = _context.Groups.AsQueryable().Where(g => g.District_Id.ToLower() == id.ToLower()).Sum(g => g.Credits);

            return groupWealth;
        }

        public class OwnershipData
        {
            [JsonProperty]
            public string OwnerId { get; set; }
            [JsonProperty]
            public string OwnerName { get; set; }
            [JsonProperty]
            public int Amount { get; set; }
        }

        [HttpGet]
        public async Task<List<OwnershipData>> GetOwnerData(string ticker)
        {
            var query = _context.StockObjects.AsQueryable().Where(x => x.Ticker == ticker).OrderBy(x => x.Amount).Select(x => new { x.Owner_Id, x.Amount });

            List<OwnershipData> data = new List<OwnershipData>();

            foreach (var obj in query)
            {
                Entity owner = await Entity.FindAsync(obj.Owner_Id);

                if (owner != null)
                {
                    data.Add(new OwnershipData() { OwnerId = owner.Id, OwnerName = owner.Name, Amount = obj.Amount });
                }
            }

            return data;
        }
    }
}
