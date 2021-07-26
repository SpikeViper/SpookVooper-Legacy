using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SpookVooper.Web.Helpers;
using SpookVooper.Web.Models.ExchangeViewModels;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Entities.Groups;

namespace SpookVooper.Web.Controllers
{
    public class ExchangeController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private readonly VooperContext _context;

        [TempData]
        public string StatusMessage { get; set; }

        public ExchangeController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            VooperContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index(int page, string sort, string account)
        {
            IQueryable<StockDefinition> stocks = null;

            if (sort == null) sort = "Price";

            int view = 14;

            if (sort == "Name")
            {
                stocks = _context.StockDefinitions.AsQueryable().OrderBy(s => s.Ticker).Skip(page * view).Take(view);
            }
            else if (sort == "Price")
            {
                stocks = _context.StockDefinitions.AsQueryable().OrderByDescending(s => s.Current_Value).Skip(page * view).Take(view);
            }

            // Allow an account to be specified
            Entity chosen = null;

            if (string.IsNullOrWhiteSpace(account))
            {
                chosen = await _userManager.GetUserAsync(User);
            }
            else {
                chosen = await _context.Users.FindAsync(account);
                if (chosen == null) chosen = await _context.Groups.FindAsync(account);
            }

            ExchangeIndexModel model = new ExchangeIndexModel()
            {
                Stock_List = stocks,
                Chosen_Account = chosen
            };

            return View(model);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SelectAccount(string account)
        {
            return await Task.Run(() =>
            {
                return View((object)account);
            });
        }

        [HttpGet]
        public async Task<IActionResult> Trade(string ticker, string account)
        {
            if (ticker != null) ticker = ticker.ToUpper();

            // Allow an account to be specified
            Entity chosen = null;

            if (string.IsNullOrWhiteSpace(account))
            {
                chosen = await _userManager.GetUserAsync(User);
            }
            else
            {
                chosen = await _context.Users.FindAsync(account);
                if (chosen == null) chosen = await _context.Groups.FindAsync(account);
            }

            StockDefinition stock = await _context.StockDefinitions.FindAsync(ticker);

            if (stock == null)
            {
                StatusMessage = $"Failed to find the ticker {ticker}";
                return RedirectToAction("Index");
            }

            return await Task.Run(() =>
            {
                return View(new ExchangeTradeModel() { Chosen_Account = chosen, Stock = stock });
            });
        }

        [AuthorizeDiscord("Board of MOF")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ListNewStock()
        {
            return await Task.Run(() => { return View(); });
        }

        [AuthorizeDiscord("Board of MOF")]
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ListNewStock(CreateStockModel model)
        {
            // Validate model
            if (!ModelState.IsValid) return View();

            // Additional validations
            Group group = await _context.Groups.FindAsync(model.Group_Id);

            if (group == null)
            {
                StatusMessage = $"Failed: Could not find group {model.Group_Id}";
                return View();
            }

            // Check if group already has a stock
            if (_context.StockDefinitions.Any(s => s.Group_Id == model.Group_Id))
            {
                StatusMessage = $"Failed: {group.Name} already has a stock!";
                return View();
            }

            if (model.Amount < 0)
            {
                StatusMessage = $"Failed: Amount must be positive!";
                return View();
            }

            if (model.Keep < 0)
            {
                StatusMessage = $"Failed: Keep must be positive!";
                return View();
            }

            // Check if ticker is taken
            if (_context.StockDefinitions.Any(s => s.Ticker == model.Ticker))
            {
                StatusMessage = $"Failed: A ticker {model.Ticker} already exists!";
                return View();
            }

            if (model.Initial_Value < 1)
            {
                StatusMessage = $"Failed: Initial value must be greater or equal to 1!";
                return View();
            }

            if (model.Keep > model.Amount)
            {
                StatusMessage = $"Failed: Keep must be less than Amount!";
                return View();
            }

            // Create stock definition
            StockDefinition stockDef = new StockDefinition()
            {
                Ticker = model.Ticker,
                Group_Id = model.Group_Id,
                Current_Value = model.Initial_Value
            };

            // Add stock definition to database
            await _context.StockDefinitions.AddAsync(stockDef);

            // Create stock object for keeping
            StockObject keepStock = new StockObject()
            {
                Id = Guid.NewGuid().ToString(),
                Amount = model.Keep,
                Owner_Id = model.Group_Id,
                Ticker = model.Ticker,
            };

            // Add
            await _context.StockObjects.AddAsync(keepStock);

            // Create stock sale for issued part
            StockOffer sellOffer = new StockOffer()
            {
                Id = Guid.NewGuid().ToString(),
                Order_Type = "SELL",
                Target = model.Initial_Value,
                Ticker = model.Ticker,
                Amount = model.Amount - model.Keep,
                Owner_Id = model.Group_Id
            };

            // Add
            await _context.StockOffers.AddAsync(sellOffer);

            // Save changes if successful
            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully issued {model.Amount} ${model.Ticker}";
            // await VoopAI.ecoChannel.SendMessageAsync($":new: Welcome {model.Amount} {model.Ticker}, from {group.Name} to the market at ¢{model.Initial_Value}, with an initial {sellOffer.Amount} on the market!");
            return RedirectToAction("Index");
        }

        /*

        [HttpGet]
        public async Task<IActionResult> GetMinuteData(string id, int minutes)
        {
            if (minutes == 0)
            {
                minutes = 1;
            }

            if (id != null)
            {
                id = id.ToUpper();
            }

            Stock stock = await _context.Stocks.FindAsync(id);

            if (stock == null)
            {
                return NotFound();
            }

            List<decimal> data = new List<decimal>();

            if (minutes == 1)
            {
                 data = stock.FormatData(stock.OneMinuteData);
            }
            else if (minutes == 15)
            {
                data = stock.FormatData(stock.FifteenMinuteData);
            }
            else if (minutes == 30)
            {
                data = stock.FormatData(stock.ThirtyMinuteData);
            }
            else if (minutes == 60)
            {
                data = stock.FormatData(stock.HourData);
            }
            else if (minutes == 1440)
            {
                data = stock.FormatData(stock.DayData);
            }
            else if (minutes == 43200)
            {
                data = stock.FormatData(stock.MonthData);
            }

            string response = "";

            foreach (var history in data)
            {
                response += $"{history},";
            }

            response = response.TrimEnd(',');

            return Ok(response);
        }

        [HttpGet]
        public async Task<IActionResult> GetCurrentData(string id)
        {
            if (id != null)
            {
                id = id.ToUpper();
            }

            decimal now = 0;

            Stock stock = await _context.Stocks.FindAsync(id);

            if (stock != null) {
                now = await stock.GetValue(_context);
            }

            return Ok(now);
        }

        [HttpGet]
        public async Task<IActionResult> Available(string id)
        {
            if (id != null)
            {
                id = id.ToUpper();
            }

            Stock stock = await _context.Stocks.FindAsync(id);

            int available = 0;

            if (stock != null)
            {
                available = stock.ExchangeOwned;
            }

            return Ok(available);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BuyStock(string ticker, int amount)
        {
            string id = (await _userManager.GetUserAsync(User)).Id;
            TaskResult result = await new StockTransactionRequest(ticker, amount, id, StockTransactionType.buy).Execute();
            return Ok(result.Info);
        }

        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SellStock(string ticker, int amount)
        {
            string id = (await _userManager.GetUserAsync(User)).Id;
            TaskResult result = await new StockTransactionRequest(ticker, amount, id, StockTransactionType.sell).Execute();
            return Ok(result.Info);
        }

        [HttpGet]
        public async Task<IActionResult> CalculateBuy(string ticker, int amount)
        {
            if (ticker != null)
            {
                ticker = ticker.ToUpper();
            }

            Stock stock = await _context.Stocks.FindAsync(ticker);
            Group group = await _context.Groups.FindAsync(stock.GroupID);

            return Ok(StockManager.CalculateBuy(stock, group, amount));
        }

        [HttpGet]
        public async Task<IActionResult> CalculateSell(string ticker, int amount)
        {
            if (ticker != null)
            {
                ticker = ticker.ToUpper();
            }

            Stock stock = await _context.Stocks.FindAsync(ticker);
            Group group = await _context.Groups.FindAsync(stock.GroupID);

            return Ok(StockManager.CalculateSell(stock, group, amount));
        }

        [HttpGet]
        public async Task<IActionResult> BuyStockWithKey(string ticker, int amount, string key)
        {
            TaskResult result = await new StockTransactionRequest(ticker, amount, null, StockTransactionType.buy, key).Execute();
            return Ok(result.Info);
        }

        [HttpGet]
        public async Task<IActionResult> SellStockWithKey(string ticker, int amount, string key)
        {
            TaskResult result = await new StockTransactionRequest(ticker, amount, null, StockTransactionType.sell, key).Execute();
            return Ok(result.Info);
        }

        */
    }
}
