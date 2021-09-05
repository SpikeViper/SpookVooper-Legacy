using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Data.Services;
using SpookVooper.Web.Services;
using System.Linq;
using System.Threading.Tasks;
using SpookVooper.Web.Models.UserViewModels;
using Microsoft.AspNetCore.Authorization;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.Managers;
using Microsoft.EntityFrameworkCore;

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

        [TempData]
        public string StatusMessage { get; set; }

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

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(UserPayModel model)
        {
            User user = await _userManager.GetUserAsync(User);

            Entity target = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == model.Target.ToLower());
            if (target == null) await _context.Groups.FirstOrDefaultAsync(x => x.Name.ToLower() == model.Target.ToLower());

            if (target == null)
            {
                return await RedirectBack($"Error: Could not find {model.Target}");
            }

            TaskResult result = await new TransactionRequest(user.Id, target.Id, model.Amount, "Group Direct Payment", ApplicableTax.None, false).Execute();

            if (!result.Succeeded)
            {
                return await RedirectBack(result.Info);
            }

            StatusMessage = "Successfully sent direct payment.";

            return RedirectToAction("Info", new { svid = user.Id });
        }

        public async Task<IActionResult> RedirectBack(string reason)
        {
            StatusMessage = reason;
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}