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
using SpookVooper.Web.Entities.Groups;

namespace SpookVooper.Web.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class EntityController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly VooperContext _context;
        private readonly IMapper _mapper;

        public EntityController(
            VooperContext context,
            UserManager<User> userManager,
            IMapper mapper)
        {
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetEntity(string svid)
        {
            Entity entity = await Entity.FindAsync(svid);

            if (entity is User)
            {
                return ((User)entity).MapToSnapshot(_mapper);
            }
            else if (entity is Group)
            {
                return ((Group)entity).MapToSnapshot(_mapper);
            }

            if (entity == null)
            {
                return $"Could not find entity {svid}";
            }

            return $"Could not map {svid} to an entity";
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetName(string svid)
        {
            Entity entity = await Entity.FindAsync(svid);

            if (entity == null)
            {
                return $"Could not find entity {svid}";
            }

            return entity.Name;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SpookVooper.Api.Entities.EntitySnapshot>>> Search(string name, int amount = 20)
        {
            List<Entity> entities = new List<Entity>();
            List<SpookVooper.Api.Entities.EntitySnapshot> snaps = new List<SpookVooper.Api.Entities.EntitySnapshot>();

            // Cap at 20
            if (amount > 20)
            {
                amount = 20;
            }

            if (name == null)
            {
                return snaps;
            }

            name = name.ToLower();

            var users = _context.Users.AsEnumerable().Where(x => x.Name.ToLower().Contains(name));
            var groups = _context.Groups.AsEnumerable().Where(x => x.Name.ToLower().Contains(name));

            entities.AddRange(users);
            entities.AddRange(groups);

            var top = entities.OrderBy(x => x.Name.ToLower().StartsWith(name.ToLower())).TakeLast(amount).ToList();

            foreach (Entity e in top)
            {
                snaps.Add(e.GetSnapshot(_mapper));
            }

            //snaps.Reverse();

            return snaps;
        }
    }
}
