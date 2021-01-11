using Microsoft.EntityFrameworkCore;
using SpookVooper.Web.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpookVooper.Web.Entities.Groups
{

    public class GroupRole
    {
        public static GroupRole Default = new GroupRole()
        {
            Color = "",
            GroupId = "NONE",
            RoleId = "DEFAULT",
            Name = "Default Role",
            Weight = int.MinValue,
            Permissions = "post|"
        };

        // The name of the role
        [MaxLength(64, ErrorMessage = "Name should be under 64 characters.")]
        [RegularExpression("^[a-zA-Z0-9, ]*$", ErrorMessage = "Please use only letters, numbers, and commas.")]
        public string Name { get; set; }

        // The ID of the role
        [Key]
        public string RoleId { get; set; }

        // Things this role is allowed to do
        public string Permissions { get; set; }

        // Hexcode for role color
        [MaxLength(6, ErrorMessage = "Color should be a hex code (ex: #ffffff)")]
        public string Color { get; set; }

        // The group this role belongs to
        public string GroupId { get; set; }

        // Salary for role
        public decimal Salary { get; set; }

        // Weight of the role
        [Required]
        [RegularExpression("^[0-9]*$", ErrorMessage = "Numbers only!")]
        public int Weight { get; set; }

        public async Task<IEnumerable<User>> GetUsers()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                var members = context.GroupRoleMembers.AsQueryable().Where(x => x.Role_Id == RoleId);

                List<User> users = new List<User>();

                foreach (GroupRoleMember member in members)
                {
                    User user = await context.Users.FindAsync(member.User_Id);

                    if (user != null)
                    {
                        users.Add(user);
                    }
                }

                return users;
            }
        }

        public async Task<int> GetMemberCount()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.GroupRoleMembers.AsQueryable().CountAsync(x => x.Role_Id == RoleId);
            }
        }

        public async Task<Group> GetGroup()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.Groups.FindAsync(GroupId);
            }
        }

        public async Task<bool> HasRole(User user)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.GroupRoleMembers.AsQueryable().AnyAsync(x => x.User_Id == user.Id && x.Role_Id == RoleId);
            }
        }
    }
}
