using AutoMapper;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SpookVooper.Api.Entities;
using SpookVooper.Web.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpookVooper.Web.Entities.Groups
{
    public class Group : ITradeable, Entity
    {
        // Id of group
        [Key]
        [Required]
        [Display(Name = "Group ID")]
        public string Id { get; set; }

        // Name of group
        [Required]
        [MaxLength(64, ErrorMessage = "Name should be under 64 characters.")]
        [RegularExpression("^[a-zA-Z0-9, ]*$", ErrorMessage = "Please use only letters, numbers, and commas.")]
        [Display(Name = "Name")]
        public string Name { get; set; }

        // Group page description
        [Required]
        [Display(Name = "Group Description")]
        [MaxLength(3000, ErrorMessage = "Description should be under 3000 characters.")]
        public string Description { get; set; }

        // If the group is open to the public
        [Display(Name = "Open")]
        public bool Open { get; set; }

        [Display(Name = "Credits")]
        public decimal Credits { get; set; }

        // URL for group image
        [Display(Name = "Group Icon URL")]
        [RegularExpression(@"(^(.*)(\.jpg|\.jpeg|\.png|\.gif|\.PNG|\.JPG|\.JPEG))$", ErrorMessage = "Link should end in an image file [.png, .jpg, .jpeg, .gif, etc.]")]
        public string Image_Url { get; set; }

        // The type of group this is
        [Required]
        [Display(Name = "Group Category")]
        public string Group_Category { get; set; }

        // The owner of this group
        [Display(Name = "Owner ID")]
        public string Owner_Id { get; set; }

        // The district containing this group
        [Display(Name = "District")]
        public string District_Id { get; set; }

        [Display(Name = "Default Role IDs")]
        public string Default_Role_Id { get; set; }

        [Display(Name = "API Key")]
        public string Api_Key { get; set; }

        public decimal Credits_Invested { get; set; }

        public int Amount { get { return 1; } }


        public class GroupTypes
        {
            public const string None = "Groups", Political = "Parties", Company = "Companies", News = "News", District = "District";
        }

        /* Deprecated and added to ITradeable
        public bool IsOwner(User user)
        {
            return user.Id == Owner_Id;
        }
        */

        public decimal GetValue()
        {
            return Credits;
        }

        public async Task<decimal> GetValueAsync()
        {
            return Credits;
        }

        public bool IsOwner(Entity entity)
        {
            return IsOwnerAsync(entity).Result;
        }

        public async Task<bool> IsOwnerAsync(Entity entity)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                Entity owner = await Entity.FindAsync(Owner_Id);

                // While the owner is a group
                while (owner is Group)
                {
                    // Check if it matches
                    if (owner.Id == entity.Id)
                    {
                        return true;
                    }

                    // Move up to next layer of ownership
                    owner = await Entity.FindAsync(((Group)owner).Owner_Id);
                }

                // At this point the owner must be a user
                if (owner != null)
                {
                    return owner.Id == entity.Id;
                }

                return false;
            }
        }

        public async Task<bool> HasPermissionWithKey(string key, string permission)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                if (Api_Key == key) return true;

                User user = await context.Users.AsQueryable().FirstOrDefaultAsync(u => u.Api_Key == key);

                if (user == null) return false;

                return await HasPermissionAsync(user, permission);
            }
        }

        public bool HasPermission(Entity entity, string perm)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return HasPermissionAsync(entity, perm).Result;
            }
        }

        public async Task<bool> HasPermissionAsync(Entity entity, string perm)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                if (await IsOwnerAsync(entity))
                {
                    return true;
                }

                // Get all role membership of the user in the group
                var membership = context.GroupRoleMembers.AsQueryable().Where(x => x.Group_Id == Id && x.User_Id == entity.Id);

                // Get role and check if it has permission for each membership
                foreach (var m in membership)
                {
                    if (await context.GroupRoles.AsQueryable().AnyAsync(x => x.RoleId == m.Role_Id && x.Permissions.Contains(perm.ToLower())))
                    {
                        return true;
                    }
                }

                // Default to no permissions
                return false;
            }
        }


        public static List<SelectListItem> GetCategoryListForDropdown()
        {
            List<SelectListItem> items = new List<SelectListItem>();

            items.Add(new SelectListItem() { Text = "None", Value = "Groups" });
            items.Add(new SelectListItem() { Text = "Political Party", Value = "Parties" });
            items.Add(new SelectListItem() { Text = "Corporation", Value = "Companies" });
            items.Add(new SelectListItem() { Text = "News", Value = "News" });

            return items;
        }

        public async Task AddMember(User user)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                // Cancel if the member is already there
                if (context.GroupMembers.Any(x => x.Group_Id == Id && x.User_Id == user.Id))
                {
                    return;
                }

                GroupMember member = new GroupMember()
                {
                    Id = Guid.NewGuid().ToString(),
                    Group_Id = Id,
                    User_Id = user.Id
                };

                await context.GroupMembers.AddAsync(member);
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveMember(User user)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                GroupMember member = await context.GroupMembers.AsQueryable().FirstOrDefaultAsync(x => x.Group_Id == Id && x.User_Id == user.Id);

                if (member != null)
                {
                    context.GroupMembers.Remove(member);
                    await context.SaveChangesAsync();
                }
            }
        }

        public async Task ClearRoles()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                context.GroupRoles.RemoveRange(context.GroupRoles.AsQueryable().Where(x => x.GroupId == Id));
                await context.SaveChangesAsync();
            }
        }

        public IEnumerable<GroupMember> GetMemberList()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return context.GroupMembers.AsQueryable().Where(x => x.Group_Id == Id);
            }
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                var members = context.GroupMembers.AsQueryable().Where(x => x.Group_Id == Id);

                List<User> users = new List<User>();

                foreach (var member in members)
                {
                    users.Add(await context.Users.FindAsync(member.User_Id));
                }

                return users;
            }
        }

        public async Task<int> GetAuthority(User user)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                if (user == null)
                {
                    return int.MinValue;
                }

                if (await IsOwnerAsync(user))
                {
                    return int.MaxValue;
                }

                List<GroupRole> roles = await GetRoles(user);

                if (roles == null || roles.Count == 0)
                {
                    return int.MinValue;
                }

                return roles.Max(r => r.Weight);
            }
        }

        public async Task<GroupRole> GetMainRole(User user)
        {
            List<GroupRole> roles = await GetRoles(user);

            if (roles == null || roles.Count == 0)
            {
                return GroupRole.Default;
            }

            return roles.OrderByDescending(r => r.Weight).First();
        }

        public async Task<List<GroupRole>> GetRoles(User user)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                var role_members = context.GroupRoleMembers.AsQueryable().Where(x => x.User_Id == user.Id && x.Group_Id == Id);

                List<GroupRole> roles = new List<GroupRole>();

                foreach (GroupRoleMember member in role_members)
                {
                    var role = await context.GroupRoles.AsQueryable().FirstOrDefaultAsync(x => x.RoleId == member.Role_Id);

                    if (role != null)
                    {
                        roles.Add(role);
                    }
                }

                return roles;
            }
        }

        public IEnumerable<GroupRole> GetRoleList()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return context.GroupRoles.AsQueryable().Where(x => x.GroupId == Id).ToList();
            }
        }

        public async Task<bool> IsInGroup(User user)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.GroupMembers.AsQueryable().AnyAsync(x => x.Group_Id == Id && x.User_Id == user.Id);
            }
        }

        public List<GroupRole> GetRolesForPerm(string permission)
        {
            List<GroupRole> list = new List<GroupRole>();

            foreach (GroupRole role in GetRoleList())
            {
                if (role.Permissions.Contains(permission))
                {
                    list.Add(role);
                }
            }

            return list;
        }
        public async Task<bool> HasPermission(User user, string permission)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                if (await IsOwnerAsync(user))
                {
                    return true;
                }

                if (!(await IsInGroup(user)))
                {
                    return false;
                }

                foreach (GroupRole role in GetRolesForPerm(permission))
                {
                    if (await context.GroupRoleMembers.AsQueryable().AnyAsync(x => x.Role_Id == role.RoleId && user.Id == x.User_Id))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public async Task<bool> IsInvited(User user)
        {
            // Null cancel
            if (user == null)
            {
                return false;
            }

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.GroupInvites.AsQueryable().AnyAsync(x => x.User_Id == user.Id && x.Group_Id == Id);
            }
        }

        public async Task<bool> IsBanned(User user)
        {
            // Null cancel
            if (user == null)
            {
                return true;
            }

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.GroupBans.AsQueryable().AnyAsync(x => x.User_Id == user.Id && x.Group_Id == Id);
            }
        }

        public async Task<Entity> GetOwner()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await Entity.FindAsync(Owner_Id);
            }
        }

        /// <summary>
        /// Sets the owner of this group to the specified new owner
        /// </summary>
        public async Task SetOwnerAsync(Entity newOwner)
        {
            // Change locally
            this.Owner_Id = newOwner.Id;

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                // Change in DB
                Group dummy = new Group { Id = this.Id, Owner_Id = newOwner.Id };
                context.Groups.Attach(dummy);

                // Set to update owner field
                context.Entry(dummy).Property(x => x.Owner_Id).IsModified = true;
                await context.SaveChangesAsync();
            }

            // Set top owner to new owner
            Entity topOwner = await Entity.FindAsync(newOwner.Id);

            // Scale up to top owner
            while (topOwner is Group)
            {
                topOwner = await ((Group)topOwner).GetOwner();
            }

            // By this point the top owner should be a user
            // Add that user to this group
            await AddMember((User)topOwner);
        }

        public GroupSnapshot MapToSnapshot(IMapper mapper)
        {
            GroupSnapshot snapshot = mapper.Map<GroupSnapshot>(this);
            return snapshot;
        }

        public async Task<IEnumerable<Group>> GetOwnedGroupsAsync()
        {
            List<Group> groups = new List<Group>();

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                var owned = context.Groups.AsQueryable().Where(x => x.Owner_Id == Id);

                foreach (Group g in owned)
                {
                    groups.Add(g);
                    groups.AddRange(await g.GetOwnedGroupsAsync());
                }
            }

            return groups;
        }
    }
}
