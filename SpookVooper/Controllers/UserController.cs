using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Data.Services;
using SpookVooper.VoopAIService;
using SpookVooper.Web.Services;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using SpookVooper.Web.Models.UserViewModels;
using Microsoft.AspNetCore.Authorization;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;

namespace SpookVooper.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly VooperContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IConnectionHandler _connectionHandler;

        public UserController(
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

        [HttpGet]
        public async Task<IActionResult> Search(string id)
        {
            UserSearchModel model = new UserSearchModel()
            {
                userManager = _userManager,
                search = id
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Search(UserSearchModel model)
        {
            model.userManager = _userManager;

            return RedirectToAction(nameof(Search), routeValues: new { id = model.search });
        }

        [HttpGet]
        public async Task<IActionResult> Info(string svid)
        {
            if (svid == null)
            {
                return View(null);
            }

            User webUser = await _userManager.FindByIdAsync(svid);

            return View(webUser);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> SetInfo()
        {
            User webuser = await _userManager.GetUserAsync(User);

            if (webuser == null)
            {
                return RedirectToAction(nameof(Info));
            }

            SetInfoViewModel model = new SetInfoViewModel()
            {
                Description = webuser.description,
                userManager = _userManager
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetInfo(SetInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                model.StatusMessage = "Failed to set: User instance not found!";
                return View(model);
            }

            if (model.Description.Length < 1000)
            {
                user.description = model.Description;
            }
            else
            {
                model.StatusMessage = "Error: The entered description is over 1000 characters!";
                return View(model);
            }

            await _userManager.UpdateAsync(user);

            return RedirectToAction(nameof(SetInfo));
        }

        public async Task<IActionResult> GetSVIDFromUsername(string username)
        {
            User user = await Task.Run(() => _context.Users.FirstOrDefault(x => x.UserName == username));

            if (user == null) return NotFound($"Could not find user {username}");

            return Ok(user.Id);
        }

        public async Task<IActionResult> GetUsernameFromDiscord(ulong discordid)
        {
            User user = await Task.Run(() => _context.Users.FirstOrDefault(x => x.discord_id == discordid));

            if (user == null) return NotFound($"Could not find user with discord {discordid}");

            return Ok(user.UserName);
        }

        public async Task<IActionResult> GetSVIDFromDiscord(ulong discordid)
        {
            User user = await Task.Run(() => _context.Users.FirstOrDefault(x => x.discord_id == discordid));

            if (user == null) return NotFound($"Could not find user with discord {discordid}");

            return Ok(user.Id);
        }

        public async Task<IActionResult> GetUsernameFromMinecraft(string minecraftid)
        {
            User user = await Task.Run(() => _context.Users.FirstOrDefault(x => x.minecraft_id == minecraftid));

            if (user == null) return NotFound($"Could not find user with minecraft {minecraftid}");

            return Ok(user.UserName);
        }

        public async Task<IActionResult> GetSVIDFromMinecraft(string minecraftid)
        {
            User user = await Task.Run(() => _context.Users.FirstOrDefault(x => x.minecraft_id == minecraftid));

            if (user == null) return NotFound($"Could not find user with minecraft {minecraftid}");

            return Ok(user.Id);
        }

        public async Task<IActionResult> HasDiscordRole(string userid, string role)
        {
            User user = await _userManager.FindByIdAsync(userid);

            if (user == null) return NotFound($"Could not find {userid}");
            else
            {
                SocketGuildUser discordUser = VoopAI.server.Users.FirstOrDefault(u => u.Id == user.discord_id);

                if (discordUser == null) return NotFound($"User has no linked discord account!");

                else return Ok(discordUser.Roles.Any(r => r.Name.ToLower() == role.ToLower()));
            }
        }

        public async Task<IActionResult> GetDiscordRoles(string userid)
        {
            User user = await _userManager.FindByIdAsync(userid);

            if (user == null) return NotFound($"Could not find user {userid}.");

            if (user.discord_id == null) return NotFound($"User does not have a linked discord.");

            var roles = VoopAI.server.GetUser((ulong)user.discord_id).Roles;

            string data = "";

            if (roles.Count > 0)
            {
                foreach (var role in roles)
                {
                    data += role.Name + "|";
                }

                data = data.Substring(0, data.Length - 1);
            }

            return Ok(data);

        }
    }
}