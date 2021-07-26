using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Government;
using SpookVooper.Web.DB;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;

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
        public async Task<ActionResult<int>> GetDaysSinceLastMove(string svid)
        {
            User user = await _context.Users.FindAsync(svid);

            if (user == null) return NotFound($"Could not find user with svid {svid}");

            return user.GetDaysSinceLastMove();
        }
    }
}
