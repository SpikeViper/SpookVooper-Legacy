using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Web.Entities;
using SpookVooper.Data.Services;
using SpookVooper.Web.DB;
using SpookVooper.Web.Models.NationViewModels;
using SpookVooper.Web.Services;
using System.Net.Http;
using System.Threading.Tasks;

namespace SpookVooper.Web.Controllers
{
    public class NationController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private readonly VooperContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IConnectionHandler _connectionHandler;

        [TempData]
        public string StatusMessage { get; set; }

        public NationController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IConnectionHandler connectionHandler,
            VooperContext context,
            RoleManager<IdentityRole> roleManager)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _connectionHandler = connectionHandler;
            _context = context;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Connect()
        {
            NationConnectModel model = new NationConnectModel();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                StatusMessage = "Failed to find SpookVooper user. Try logging in?";
                return View();
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Connect(NationConnectModel model)
        {

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                StatusMessage = "Error: Failed to find SpookVooper user. Try logging in?";
                return RedirectToAction("Connect");
            }

            using (HttpClient client = new HttpClient())
            {
                // client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("SpookVooper"));

                using (var response = await client.GetAsync($"https://www.nationstates.net/cgi-bin/api.cgi?a=verify&nation={model.NationName}&checksum={model.Password}"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string resp = await response.Content.ReadAsStringAsync();

                        if (resp.Contains("1") && !resp.Contains("0"))
                        {

                            user.nationstate = model.NationName;

                            await _userManager.UpdateAsync(user);

                            StatusMessage = $"Successfully connected to {model.NationName}!";

                            return RedirectToAction("Index", controllerName: "Home");
                        }
                        else
                        {
                            StatusMessage = $"Error: Failed to connect to {model.NationName}.";
                            return RedirectToAction("Connect");
                        }
                    }
                    else
                    {
                        StatusMessage = $"Error: Failed to connect to {model.NationName}.";
                        return RedirectToAction("Connect");
                    }
                }
            }
        }

    }
}
