using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Web.Entities;
using SpookVooper.Data.Services;
using SpookVooper.Web.DB;
using SpookVooper.Web.Services;
using System.Threading.Tasks;

namespace SpookVooper.Web.Controllers
{
    public class NerdcraftController : Controller
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

        public NerdcraftController(
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
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View();
        }

        public async Task<IActionResult> RulesGeneral()
        {
            return View();
        }

        public async Task<IActionResult> Laws()
        {
            return View();
        }

        public async Task<IActionResult> Jobs()
        {
            return View();
        }

        public async Task<IActionResult> Guide()
        {
            return View();
        }
    }
}
