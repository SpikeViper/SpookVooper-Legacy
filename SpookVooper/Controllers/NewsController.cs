using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Data.Services;
using SpookVooper.Web.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SpookVooper.Web.Helpers;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.News;
using SpookVooper.Web.Entities.Groups;

namespace SpookVooper.Web.Controllers
{
    public class NewsController : Controller
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

        public NewsController(
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
        public async Task<IActionResult> Index()
        {
            return await Task.Run(() => View());
        }

        public async Task<IActionResult> View(string postid)
        {
            NewsPost post = await _context.NewsPosts.FindAsync(postid);

            return await Task.Run(() => View(post));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create (string groupid)
        {
            User user = await _userManager.GetUserAsync(User);
            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null) return await RedirectBack($"Error: Could not find group {groupid}");

            if (! await PressPass.HasPressPass(group, _context)) return await RedirectBack($"Error: Group does not have a press pass!");

            if (!await group.HasPermissionAsync(user, "news")) return await RedirectBack($"Error: You do not have permission!");

            NewsPost post = new NewsPost()
            {
                PostID = Guid.NewGuid().ToString(),
                GroupID = groupid,
                AuthorID = user.Id
            };

            return View(post);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(NewsPost model)
        {
            if (!ModelState.IsValid) return View(model);

            User user = await _userManager.GetUserAsync(User);
            Group group = await _context.Groups.FindAsync(model.GroupID);

            if (group == null) return await RedirectBack($"Error: Could not find group {model.GroupID}");

            if (!await PressPass.HasPressPass(group, _context)) return await RedirectBack($"Error: Group does not have a press pass!");

            if (!await group.HasPermissionAsync(user, "news")) return await RedirectBack($"Error: You do not have permission!");

            model.Timestamp = DateTime.UtcNow;

            await _context.NewsPosts.AddAsync(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("View", new { postid = model.PostID });
        }

        [Authorize]
        [AuthorizeDiscord("Minister of Journalism")]
        public async Task<IActionResult> AddPressPass(string groupid)
        {
            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null) return await RedirectBack($"Failed to find group {groupid}");

            PressPass pass = new PressPass()
            {
                GroupID = groupid
            };

            await _context.PressPasses.AddAsync(pass);
            await _context.SaveChangesAsync();

            StatusMessage = $"Gave press pass to {group.Name}";
            return RedirectToAction("View", "Group", new { groupid = groupid });
        }

        [Authorize]
        [AuthorizeDiscord("Minister of Journalism")]
        public async Task<IActionResult> RemovePressPass(string groupid)
        {
            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null) return await RedirectBack($"Failed to find group {groupid}");

            PressPass pass = await _context.PressPasses.FindAsync(groupid);

            if (pass == null) return await RedirectBack($"Failed: Group does not have a press pass!");

            _context.PressPasses.Remove(pass);
            await _context.SaveChangesAsync();

            StatusMessage = $"Removed press pass from {group.Name}";
            return RedirectToAction("View", "Group", new { groupid = groupid });
        }

        public async Task<IActionResult> RedirectBack(string reason)
        {
            StatusMessage = reason;
            return Redirect(Request.Headers["Referer"].ToString());
        }


    }
}
