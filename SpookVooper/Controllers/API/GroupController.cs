using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.DB;

namespace SpookVooper.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class GroupController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly VooperContext _context;
        private readonly IMapper _mapper;

        public GroupController(
            VooperContext context,
            UserManager<User> userManager,
            IMapper mapper)
        {
            _userManager = userManager;
            _context = context;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<SpookVooper.Api.Entities.GroupSnapshot>> GetGroup(string svid)
        {
            Group group = await _context.Groups.FindAsync(svid);
            if (group == null) return NotFound($"Could not find {svid}");

            SpookVooper.Api.Entities.GroupSnapshot json = group.MapToSnapshot(_mapper);

            return json;
        }

        [HttpGet]
        public async Task<ActionResult<decimal>> GetBalance(string svid)
        {
            Entity account = await Entity.FindAsync(svid);
            if (account == null) return NotFound($"Could not find {svid}");

            return account.Credits;
        }

        [HttpGet]
        public async Task<ActionResult<bool>> DoesGroupExist(string svid)
        {
            return await _context.Groups.AsQueryable().AnyAsync(g => g.Id == svid);
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetGroupMembers(string svid)
        {
            return JsonConvert.SerializeObject(_context.GroupMembers.AsQueryable().Where(x => x.Group_Id == svid).Select(x => x.User_Id));
        }

        [HttpGet]
        public async Task<ActionResult<bool>> HasGroupPermission(string svid, string usersvid, string permission)
        {
            Group group = await _context.Groups.FindAsync(svid);

            if (group == null) return NotFound($"Could not find group {svid}");

            User user = await _userManager.FindByIdAsync(usersvid);

            if (user == null) return NotFound($"Could not find user {usersvid}");

            return await group.HasPermissionAsync(user, permission);
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetSVIDFromName(string name)
        {
            Group group = await _context.Groups.AsQueryable().FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower());
            if (group != null) return group.Id;

            return NotFound($"Could not find {name}");
        }

        [HttpGet]
        public async Task<ActionResult<string>> GetName(string svid)
        {
            Group group = await _context.Groups.FindAsync(svid);
            if (group != null) return Ok(group.Name);

            return NotFound($"Could not find {svid}");
        }

        [HttpGet]
        public async Task<ActionResult<TaskResult>> TransferOwnership(string user_svid, string group_svid, string to_svid, string auth)
        {
            // Get caller
            User user = await _context.Users.FindAsync(user_svid);

            // Get group 
            Group group = await _context.Groups.FindAsync(group_svid);

            if (group == null)
            {
                return NotFound($"Could not find the group {group_svid}");
            }

            // Get entity to send ownership to
            Entity entity = await Entity.FindAsync(to_svid);

            if (entity == null)
            {
                return new TaskResult(false, $"Could not find the entity {to_svid}");
            }

            // Require owner to transfer ownership
            if (!(await group.IsOwnerAsync(user)))
            {
                return new TaskResult(false, $"You do not have permission to do this");
            }

            //  Check auth key
            if (!(await user.HasPermissionWithKey(auth, "groups")))
            {
                return new TaskResult(false, $"The authentication supplied was not valid for group permissions.");
            }

            if (group_svid == to_svid)
            {
                return new TaskResult(false, $"You cannot give a group to itself!");
            }

            // Case for crazy people who want to watch the world burn
            if (entity is Group)
            {
                if (await group.IsOwnerAsync(entity))
                {
                    return new TaskResult(false, $"You cannot give a group to a group it owns, because that would give me a severe headache.");
                }
            }

            Entity owner = entity;

            // Detect ownership loops
            while (owner is Group)
            {
                if (owner.Id == group.Id)
                {
                    return new TaskResult(false, $"This would result in an ownership loop.");
                }

                owner = await ((Group)owner).GetOwner();
            }

            // Set the owner
            await group.SetOwnerAsync(entity);

            return new TaskResult(true, $"Successfully transferred group ownership to {entity.Name}");
        }

        //public async Task<ActionResult<string>> GetTopOwner(string svid)
        //{

        //}
    }
}
