using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Discord.WebSocket;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Government;
using SpookVooper.VoopAIService;
using SpookVooper.Web.DB;
using Newtonsoft.Json;

namespace SpookVooper.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UserController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly VooperContext _context;
        private readonly IMapper _mapper;

        public UserController(
            VooperContext context,
            UserManager<User> userManager,
            IMapper mapper)
        {
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<SpookVooper.Api.Entities.UserSnapshot>> GetUser(string svid)
        {
            User user = await _context.Users.FindAsync(svid);
            if (user == null) return NotFound($"Could not find {svid}");

            SpookVooper.Api.Entities.UserSnapshot json = user.MapToSnapshot(_mapper);

            return json;
        }

        [HttpGet]
        public async Task<ActionResult<List<SpookVooper.Api.Entities.UserSnapshot>>> GetSenators()
        {
            List<SpookVooper.Api.Entities.UserSnapshot> users = new List<SpookVooper.Api.Entities.UserSnapshot>();

            foreach (District d in _context.Districts)
            {
                if (!string.IsNullOrWhiteSpace(d.Senator))
                {
                    SpookVooper.Api.Entities.UserSnapshot json = (await d.GetSenator(_context)).MapToSnapshot(_mapper);
                    users.Add(json);
                }
            }

            return users;
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetUsername(string svid)
        {
            User user = await _context.Users.FindAsync(svid);
            if (user == null) return NotFound($"Could not find {svid}");

            return user.UserName;
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetSVIDFromUsername(string username)
        {
            User user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == username);

            if (user == null) return NotFound($"Could not find user {username}");

            return user.Id;
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetUsernameFromDiscord(ulong discordid)
        {
            User user = await _context.Users.FirstOrDefaultAsync(x => x.discord_id == discordid);

            if (user == null) return NotFound($"Could not find user with discord {discordid}");

            return user.UserName;
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetSVIDFromDiscord(ulong discordid)
        {
            User user = await _context.Users.FirstOrDefaultAsync(x => x.discord_id == discordid);

            if (user == null) return NotFound($"Could not find user with discord {discordid}");

            return user.Id;
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetUsernameFromMinecraft(string minecraftid)
        {
            User user = await Task.Run(() => _context.Users.FirstOrDefault(x => x.minecraft_id == minecraftid));

            if (user == null) return NotFound($"Could not find user with minecraft {minecraftid}");

            return Ok(user.UserName);
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetSVIDFromMinecraft(string minecraftid)
        {
            User user = await Task.Run(() => _context.Users.FirstOrDefault(x => x.minecraft_id == minecraftid));

            if (user == null) return NotFound($"Could not find user with minecraft {minecraftid}");

            return Ok(user.Id);
        }

        [HttpGet]
        public async Task<ActionResult<bool>> HasDiscordRole(string userid, string role)
        {
            User user = await _userManager.FindByIdAsync(userid);

            if (user == null) return NotFound($"Could not find {userid}");
            else
            {
                SocketGuildUser discordUser = VoopAI.server.Users.FirstOrDefault(u => u.Id == user.discord_id);

                if (discordUser == null) return NotFound($"User has no linked discord account!");

                else return discordUser.Roles.Any(r => r.Name.ToLower() == role.ToLower());
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SpookVooper.Api.Entities.DiscordRoleData>>> GetDiscordRoles(string svid)
        {
            User user = await _userManager.FindByIdAsync(svid);

            if (user == null) return NotFound($"Could not find user {svid}.");

            if (user.discord_id == null) return NotFound($"User does not have a linked discord.");

            List<SpookVooper.Api.Entities.DiscordRoleData> data = new List<SpookVooper.Api.Entities.DiscordRoleData>();

            foreach (var result in VoopAI.server.GetUser((ulong)user.discord_id).Roles.Select(r => (r.Name, r.Id)))
            {
                data.Add(new SpookVooper.Api.Entities.DiscordRoleData(result.Name, result.Id));
            }

            return data;
        }

        [HttpGet]
        public async Task<ActionResult<int>> GetDaysSinceLastMove(string svid)
        {
            User user = await _context.Users.FindAsync(svid);

            if (user == null) return NotFound($"Could not find user with svid {svid}");

            return user.GetDaysSinceLastMove();
        }
    }
}
