using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Web.Entities;
using SpookVooper.Data.Services;
using SpookVooper.Web.DB;
using SpookVooper.Web.Models.LeaderboardViewModels;
using SpookVooper.Web.Services;
using System.Threading.Tasks;
using System.Linq;

namespace SpookVooper.Web.Controllers
{
    public class LeaderboardController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly VooperContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IConnectionHandler _connectionHandler;

        public LeaderboardController(
            VooperContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IConnectionHandler connectionHandler)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _connectionHandler = connectionHandler;
            _context = context;
        }

        public async Task<IActionResult> Index(int id)
        {
            LeaderboardIndexModel model = null;

            await Task.Run(() =>
            {
                using (VooperContext c = new VooperContext(VooperContext.DBOptions)) {
                    model = new LeaderboardIndexModel()
                    {
                        // TODO: Fix this.
                        users = c.Users.OrderByDescending(x => x.discord_message_xp).ToList(),
                        page = id,
                        amount = 25
                    };
                }
            });

             return View(model);

        }
    }
}
