using SpookVooper.Web;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Forums;
using SpookVooper.Web.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace SpookVooper.Common.Managers
{

    public static class GroupManager
    {
        public async static Task<TaskResult> AddToRole(Group group, User user, User target, GroupRole role)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                // Validate arguments
                TaskResult validate = await TargetedCommandValidate(group, user, target, "addrole");
                if (!validate.Succeeded) { return validate; }

                // Authority check
                if (role.Weight > await group.GetAuthority(user))
                {
                    return new TaskResult(false, $"{role.Name} has more authority than you!");
                }

                if (role == null)
                {
                    return new TaskResult(false, "Error: The role value was empty.");
                }

                if (await context.GroupRoleMembers.AnyAsync(x => x.Role_Id == role.RoleId && x.User_Id == target.Id))
                {
                    return new TaskResult(false, "Error: The user already has this role.");
                }

                if (role.GroupId != group.Id)
                {
                    return new TaskResult(false, "Error: The role does not belong to this group!");
                }

                // Add pipe-delimited group name
                GroupRoleMember member = new GroupRoleMember()
                {
                    Id = Guid.NewGuid().ToString(),
                    Role_Id = role.RoleId,
                    Group_Id = role.GroupId,
                    User_Id = target.Id
                };

                await context.GroupRoleMembers.AddAsync(member);
                await context.SaveChangesAsync();

                return new TaskResult(true, $"Successfully added {target.UserName} to {role.Name}");
            }
        }

        public async static Task<TaskResult> RemoveFromRole(Group group, User user, User target, GroupRole role)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                // Validate arguments
                TaskResult validate = await TargetedCommandValidate(group, user, target, "removerole");
                if (!validate.Succeeded) { return validate; }

                // Authority check
                if (await group.GetAuthority(target) > await group.GetAuthority(user))
                {
                    return new TaskResult(false, $"{target.UserName} has more authority than you!");
                }

                if (role == null)
                {
                    return new TaskResult(false, "Error: The role value was empty.");
                }

                if (group.Id != role.GroupId)
                {
                    return new TaskResult(false, "Error: The role does not belong to this group!");
                }

                GroupRoleMember member = await context.GroupRoleMembers.FirstOrDefaultAsync(x => x.Role_Id == role.RoleId && x.User_Id == target.Id);

                if (member != null)
                {
                    context.GroupRoleMembers.Remove(member);
                    await context.SaveChangesAsync();

                    return new TaskResult(true, $"Successfully removed from {role.Name}");
                }
                else
                {
                    return new TaskResult(true, $"Error: That user did not have the role {role.Name}");
                }
            }
        }

        public static async Task<TaskResult> AddToGroup(User user, Group group)
        {
            if (user == null)
            {
                return new TaskResult(false, "Error: The user value was null.");
            }

            if (group == null)
            {
                return new TaskResult(false, "Error: The group value was empty.");
            }

            if (await CanJoinGroup(user, group))
            {
                await group.AddMember(user);

                return new TaskResult(true, $"Successfully added to {group.Name}");
            }
            else
            {
                return new TaskResult(false, "Error: You are not invited to this group.");
            }
        }

        public static async Task<TaskResult> RemoveFromGroup(User user, Group group)
        {
            if (user == null)
            {
                return new TaskResult(false, "Error: The user value was null.");
            }

            if (group == null)
            {
                return new TaskResult(false, "Error: The group value was empty.");
            }

            await group.RemoveMember(user);

            return new TaskResult(true, "Successfully removed from the group.");
        }

        public static async Task<bool> CanJoinGroup(User user, Group group)
        {
            // Null cancel
            if (user == null || group == null)
            {
                return false;
            }

            // Can always join an open group
            if (group.Open)
            {
                return true;
            }

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                // Invited users can always join
                return await context.GroupInvites.AnyAsync(x => x.Group_Id == group.Id && x.User_Id == user.Id);
            }
        }

        public async static Task<TaskResult> AddInvite(Group group, User user, User target)
        {
            // Validate arguments
            TaskResult validate = await TargetedCommandValidate(group, user, target, "addinvite");
            if (!validate.Succeeded) { return validate; }

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                if (await context.GroupInvites.AnyAsync(x => x.User_Id == target.Id && x.Group_Id == group.Id))
                {
                    return new TaskResult(false, $"Error: That user is already invited!");
                }

                GroupInvite invite = new GroupInvite()
                {
                    Id = Guid.NewGuid().ToString(),
                    Group_Id = group.Id,
                    User_Id = target.Id
                };

                await context.GroupInvites.AddAsync(invite);

                // Send a notification
                Notification notification = new Notification()
                {
                    NotificationID = Guid.NewGuid().ToString(),
                    Author = user.Id,
                    Content = $"Click above to see and join the group!",
                    Source = 0,
                    Linkback = $"https://spookvooper.com/Group/View?groupid={group.Id}",
                    Target = target.Id,
                    TimeSent = DateTime.UtcNow,
                    Title = $"You were invited to {group.Name}!",
                    Type = "Group Invite"
                };

                await context.Notifications.AddAsync(notification);

                await context.SaveChangesAsync();

                return new TaskResult(true, $"Invited {target.UserName} to {group.Name}.");
            }
        }

        public async static Task<TaskResult> RemoveInvite(Group group, User user, User target)
        {
            // Validate arguments
            TaskResult validate = await TargetedCommandValidate(group, user, target, "invite");
            if (!validate.Succeeded) { return validate; }

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                GroupInvite invite = await context.GroupInvites.FirstOrDefaultAsync(x => x.Group_Id == group.Id && x.User_Id == target.Id);

                if (invite == null)
                {
                    return new TaskResult(false, $"Error: User hasn't been invited!");
                }

                context.GroupInvites.Remove(invite);

                // Send a notification
                Notification notification = new Notification()
                {
                    NotificationID = Guid.NewGuid().ToString(),
                    Author = user.Id,
                    Content = $"You can no longer join this group.",
                    Source = 0,
                    Linkback = $"https://spookvooper.com/Group/View?groupid={group.Id}",
                    Target = target.Id,
                    TimeSent = DateTime.UtcNow,
                    Title = $"{group.Name} has removed your invite!",
                    Type = "Group Uninvite"
                };

                context.Notifications.Add(notification);
                await context.SaveChangesAsync();

                return new TaskResult(true, $"{user.UserName} has been uninvited!");
            }
        }

        public async static Task<TaskResult> KickFromGroup(Group group, User user, User target)
        {
            // Validate arguments
            TaskResult validate = await TargetedCommandValidate(group, user, target, "kick");
            if (!validate.Succeeded) { return validate; }

            // Authority check
            if (await group.GetAuthority(target) > await group.GetAuthority(user))
            {
                return new TaskResult(false, $"{target.UserName} has more authority than you!");
            }

            // Remove user from group
            TaskResult remove = await RemoveFromGroup(target, group);
            if (!remove.Succeeded) { return remove; }

            // Send a notification
            Notification notification = new Notification()
            {
                NotificationID = Guid.NewGuid().ToString(),
                Author = user.Id,
                Content = $"You have been kicked from this group.",
                Source = 0,
                Linkback = $"https://spookvooper.com/Group/View?groupid={group.Id}",
                Target = target.Id,
                TimeSent = DateTime.UtcNow,
                Title = $"{group.Name} kicked you!",
                Type = "Group Kick"
            };

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                context.Notifications.Add(notification);
                await context.SaveChangesAsync();
            }

            return new TaskResult(true, $"Successfully kicked {target.UserName}!");
        }

        public async static Task<TaskResult> BanFromGroup(Group group, User user, User target)
        {
            // Validate arguments
            TaskResult validate = await TargetedCommandValidate(group, user, target, "ban");
            if (!validate.Succeeded) { return validate; }

            // Authority check
            if (await group.GetAuthority(target) > await group.GetAuthority(user))
            {
                return new TaskResult(false, $"{target.UserName} has more authority than you!");
            }

            // Remove user from group
            TaskResult remove = await RemoveFromGroup(target, group);
            if (!remove.Succeeded) { return remove; }

            if (await group.IsBanned(target))
            {
                return new TaskResult(false, $"{target.UserName} is already banned!");
            }

            GroupBan ban = new GroupBan()
            {
                Id = Guid.NewGuid().ToString(),
                User_Id = target.Id,
                Group_Id = group.Id
            };

            

            // Send a notification
            Notification notification = new Notification()
            {
                NotificationID = Guid.NewGuid().ToString(),
                Author = user.Id,
                Content = $"You have been banned from this group.",
                Source = 0,
                Linkback = $"https://spookvooper.com/Group/View?groupid={group.Id}",
                Target = target.Id,
                TimeSent = DateTime.UtcNow,
                Title = $"{group.Name} banned you!",
                Type = "Group Ban"
            };

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                await context.GroupBans.AddAsync(ban);
                context.Notifications.Add(notification);
                await context.SaveChangesAsync();
            }

            return new TaskResult(true, $"Successfully banned {target.UserName}!");
        }

        public async static Task<TaskResult> TargetedCommandValidate(Group group, User user, User target, string perm)
        {
            if (user == null)
            {
                return new TaskResult(false, $"Error: Please log in!");
            }

            if (group == null)
            {
                return new TaskResult(false, $"Error: Group does not exist!");
            }

            if (target == null)
            {
                return new TaskResult(false, $"Error: Target user does not exist!");
            }

            if (!(await group.HasPermissionAsync(user, perm)))
            {
                return new TaskResult(false, $"Error: You don't have permission to do that!");
            }

            return new TaskResult(true, $"Validated!");
        }
    }
}
