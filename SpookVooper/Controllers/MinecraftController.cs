using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Data.Services;
using SpookVooper.Web.Services;
using System.Threading.Tasks;
using System.Linq;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;

namespace SpookVooper.Web.Controllers
{

    public class MinecraftController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly VooperContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IConnectionHandler _connectionHandler;

        public MinecraftController(
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

        [TempData]
        public string StatusMessage { get; set; }

        /// <summary>
        /// Returns SpookVooper ID for Minecraft uuid
        /// </summary>
        public async Task<ActionResult> GetUserID(string uuid)
        {
            User user = _context.Users.FirstOrDefault(u => u.minecraft_id == uuid);

            if (user == null)
            {
                return NotFound("Failed to find user for Minecraft UUID " + uuid);
            }

            return Ok(user.Id);
        }
    }
}