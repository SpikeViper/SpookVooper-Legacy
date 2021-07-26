using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Data.Services;
using SpookVooper.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SpookVooper.Web.Helpers;
using SpookVooper.Web.Models.GroupViewModels;
using SpookVooper.Common.Managers;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web;
using SpookVooper.Web.Forums;
using SpookVooper.Web.Managers;
using SpookVooper.Web.Controllers;
using Microsoft.EntityFrameworkCore;

namespace SpookVooper.Web.Api.Controllers
{
    public class GroupController : Controller
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

        public GroupController(
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

        public async Task<IActionResult> View(string groupid)
        {

            Group model = await _context.Groups.FindAsync(groupid);

            if (model == null)
            {
                StatusMessage = $"Error: Could not find the group {groupid}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> MyGroups()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Invite(string groupid)
        {
            // Get current user
            User user = await _userManager.GetUserAsync(User);

            GroupInviteModel model = new GroupInviteModel() { Group = groupid };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(GroupInviteModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get current user
            User user = await _userManager.GetUserAsync(User);
            Group group = _context.Groups.FirstOrDefault(g => g.Id == model.Group);

            User inviteuser = await _userManager.FindByNameAsync(model.InviteUser);

            TaskResult result = await GroupManager.AddInvite(group, user, inviteuser);

            StatusMessage = result.Info;

            return RedirectToAction(nameof(View), new { groupid = group.Id });
        }

        [HttpGet]
        public async Task<IActionResult> ViewMemberRoles(string groupid, string userid)
        {
            Group group = await _context.Groups.FindAsync(groupid);
            User targetuser = await _userManager.FindByIdAsync(userid);

            return View(new ViewMemberRolesModel() { Group = group, Target = targetuser });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RoleMembership(string roleid)
        {
            GroupRole role = _context.GroupRoles.FirstOrDefault(r => r.RoleId.ToLower() == roleid.ToLower());

            return View(role);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DeleteRole(string roleid)
        {
            GroupRole role = _context.GroupRoles.FirstOrDefault(r => r.RoleId.ToLower() == roleid.ToLower());

            if (role == null)
            {
                StatusMessage = "Error: The Role was null!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            Group group = _context.Groups.FirstOrDefault(g => g.Id == role.GroupId);

            if (group == null)
            {
                StatusMessage = "Error: The Role's Group was null!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            User user = await _userManager.GetUserAsync(User);

            if (!await group.HasPermissionAsync(user, "createrole"))
            {
                StatusMessage = "Error: You don't have permission!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            _context.GroupRoles.Remove(role);
            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully deleted the role {role.Name}!";
            return Redirect(Request.Headers["Referer"].ToString());
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> CreateRole(string groupid, string roleid)
        {
            Group group = await _context.Groups.FindAsync(groupid);

            User user = await _userManager.GetUserAsync(User);

            if (string.IsNullOrWhiteSpace(roleid)) { roleid = ""; }

            GroupRole role = await _context.GroupRoles.FindAsync(roleid);

            CreateRoleModel model;

            if (role == null)
            {
                model = new CreateRoleModel()
                {
                    GroupId = group.Id
                };
            }
            else
            {
                model = CreateRoleModel.FromExisting(role);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(CreateRoleModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string perms = "";

            if (model.CreateRole) { perms += "createrole|"; }
            if (model.RemoveRole) { perms += "removerole|"; }
            if (model.AddRole) { perms += "addrole|"; }
            if (model.Invite) { perms += "addinvite|"; }
            if (model.Uninvite) { perms += "uninvite|"; }
            if (model.Kick) { perms += "kick|"; }
            if (model.Ban) { perms += "ban|"; }
            if (model.Edit) { perms += "edit|"; }
            if (model.Description) { perms += "description|"; }
            if (model.Post) { perms += "post|"; }
            if (model.Eco) { perms += "eco|"; }
            if (model.Plots) { perms += "plot|"; }
            if (model.News) { perms += "news|"; }

            model.Permissions = perms;

            if (string.IsNullOrWhiteSpace(model.RoleId))
            {
                model.RoleId = Guid.NewGuid().ToString();
            }

            Group group = await _context.Groups.FindAsync(model.GroupId);

            User user = await _userManager.GetUserAsync(User);

            GroupRole role = new GroupRole()
            {
                Name = model.Name,
                Color = model.Color,
                GroupId = model.GroupId,
                Permissions = model.Permissions,
                RoleId = model.RoleId,
                Weight = model.Weight,
                Salary = model.Salary,
            };

            if (_context.GroupRoles.Any(r => r.RoleId == role.RoleId))
            {
                _context.GroupRoles.Update(role);
            }
            else
            {
                _context.GroupRoles.Add(role);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Edit", new { groupid = role.GroupId });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Create()
        {
            // Get current user
            User user = await _userManager.GetUserAsync(User);

            // Ensure user is logged in
            if (user == null)
            {
                StatusMessage = $"Error: Please log in!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            // Ensure the user doesn't already own 3 groups
            int priorGroupCount = _context.Groups.AsQueryable().Where(g => g.Owner_Id == user.Id).Count();

            //if (!user.IsSupporter())
            //{
            //    if (priorGroupCount > 2)
            //    {
            //        StatusMessage = $"Error: You are at the 3 group limit! Consider becoming a patron!";
            //        return RedirectToAction("Index", controllerName: "Home");
            //    }
            //}
            //else
            //{
                if (priorGroupCount > 9)
                {
                    StatusMessage = $"Error: You are at the 10 group limit!";
                    return RedirectToAction("Index", controllerName: "Home");
                }
            //}

            Group group = new Group()
            {
                Owner_Id = user.Id,
                // Add groupid
                Id = "g-" + Guid.NewGuid().ToString()
            };

            return View(group);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Group model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Whitespace fix
            model.Name = model.Name.Trim();

            if (_context.Groups.Any(g => g.Name.ToLower() == model.Name.ToLower()))
            {
                StatusMessage = $"Error: A group already has this name!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (_context.ForumCategories.Any(g => g.CategoryID.ToLower() == model.Name.ToLower()))
            {
                StatusMessage = $"Error: A forum Category is already using this name!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (String.IsNullOrWhiteSpace(model.Owner_Id))
            {
                StatusMessage = $"Error: Owner could not be found! Try again.";
                return RedirectToAction("Index", controllerName: "Home");
            }

            model.Api_Key = Guid.NewGuid().ToString();

            _context.Add(model);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(View), new { groupid = model.Id });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Delete(string groupid)
        {
            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null)
            {
                return await RedirectBack($"Could not find the group {groupid}");
            }

            User user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return await RedirectBack($"Could not verify user!");
            }

            if (!group.IsOwner(user))
            {
                return await RedirectBack($"You don't own {groupid}!");
            }

            return View(new DeleteGroupModel() { groupid = groupid });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(DeleteGroupModel model)
        {
            Group group = await _context.Groups.FindAsync(model.groupid);

            if (group == null)
            {
                return await RedirectBack($"Could not find the group {model.groupid}");
            }

            User user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return await RedirectBack($"Could not verify user!");
            }

            if (!group.IsOwner(user))
            {
                return await RedirectBack($"You don't own {group.Name}!");
            }

            // Remove category
            ForumCategory category = _context.ForumCategories.FirstOrDefault(c => c.CategoryID == group.Name);

            if (category != null) _context.ForumCategories.Remove(category);

            await _context.SaveChangesAsync();

            /*
            Stock stock = _stockContext.Stocks.FirstOrDefault(s => s.GroupID == model.groupid);

            // Handle stocks

            if (stock != null)
            {
                int publicOwners = stock.Total - stock.ExchangeOwned;

                foreach (var ownership in stock.GetAllOwnership())
                {
                    User stockowner = await _userManager.FindByIdAsync(ownership.Item1);

                    decimal ret = ((ownership.Item2 * 1.0m) / publicOwners) * group.Credits;

                    stockowner.Credits += ret;

                    await _userManager.UpdateAsync(stockowner);
                }

                _stockContext.Stocks.Remove(stock);

                await _stockContext.SaveChangesAsync();
            }
            */

            await group.ClearRoles();

            _context.Remove(group);

            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully deleted {group.Name}!";
            return RedirectToAction("Index", controllerName: "Home");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(string groupid)
        {
            // Get current user
            User user = await _userManager.GetUserAsync(User);

            // Ensure user is logged in
            if (user == null)
            {
                StatusMessage = $"Error: Please log in!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null)
            {
                StatusMessage = $"Error: Can't find group {groupid}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (!await group.HasPermissionAsync(user, "edit"))
            {
                StatusMessage = $"Error: You don't have permission to edit {group.Id}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            return View(group);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Group model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Get current user
            User user = await _userManager.GetUserAsync(User);

            // Ensure user is logged in
            if (user == null)
            {
                StatusMessage = $"Error: Please log in!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            Group lastmodel = await _context.Groups.FindAsync(model.Id);

            if (lastmodel == null)
            {
                StatusMessage = $"Error: Group {model.Name} does not exist!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            //if (lastmodel.Name != model.Name)
            //{
            //    StatusMessage = $"Error: Name cannot be changed!";
            //    return RedirectToAction("Index", controllerName: "Home");
            //}

            if (model.Name != lastmodel.Name)
            {
                if (_context.Groups.Any(x => x.Name.ToLower() == model.Name.ToLower()))
                {
                    StatusMessage = $"Error: Name {model.Name} is already taken!";
                    return RedirectToAction("Index", controllerName: "Home");
                }
            }

            if (!await lastmodel.HasPermissionAsync(user, "edit"))
            {
                StatusMessage = $"Error: You don't have permission to edit {lastmodel.Name}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (lastmodel.Group_Category != model.Group_Category)
            {
                StatusMessage = $"Error: Category cannot be changed!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (await lastmodel.HasPermissionAsync(user, "description"))
            {
                lastmodel.Description = model.Description;
            }
            if (lastmodel.Owner_Id == user.Id)
            {
                lastmodel.Name = model.Name;
                lastmodel.Image_Url = model.Image_Url;
                lastmodel.Open = model.Open;
                lastmodel.District_Id = model.District_Id;
            }

            _context.Update(lastmodel);

            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully edited {model.Name}!";

            return RedirectToAction(nameof(View), new { groupid = model.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Search(string id)
        {
            GroupSearchModel model = new GroupSearchModel()
            {
                search = id
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Search(GroupSearchModel model)
        {
            return RedirectToAction(nameof(Search), routeValues: new { id = model.search });
        }

        [Authorize]
        public async Task<IActionResult> Join(string groupid)
        {
            // Get current user
            User user = await _userManager.GetUserAsync(User);

            // Ensure user is logged in
            if (user == null)
            {
                StatusMessage = $"Error: Please log in!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null)
            {
                StatusMessage = $"Error: Can't find group {groupid}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (await _context.GroupMembers.AnyAsync(x => x.User_Id == user.Id && x.Group_Id == group.Id))
            {
                StatusMessage = $"Error: You already joined {group.Name}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            TaskResult result = await GroupManager.AddToGroup(user, group);

            StatusMessage = result.Info;

            if (!result.Succeeded)
            {
                return RedirectToAction("Index", controllerName: "Home");
            }

            return RedirectToAction(nameof(View), new { groupid = groupid });
        }

        [Authorize]
        public async Task<IActionResult> Leave(string groupid)
        {
            // Get current user
            User user = await _userManager.GetUserAsync(User);

            // Ensure user is logged in
            if (user == null)
            {
                StatusMessage = $"Error: Please log in!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null)
            {
                StatusMessage = $"Error: Can't find group {groupid}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (!await group.IsInGroup(user))
            {
                StatusMessage = $"Error: You haven't joined {group.Name}!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            if (group.IsOwner(user))
            {
                StatusMessage = $"Error: You cannot leave a group you own!";
                return RedirectToAction("Index", controllerName: "Home");
            }

            TaskResult result = await GroupManager.RemoveFromGroup(user, group);

            StatusMessage = result.Info;

            if (!result.Succeeded)
            {
                return RedirectToAction("Index", controllerName: "Home");
            }

            return RedirectToAction(nameof(View), new { groupid = groupid });
        }

        [Authorize]
        public async Task<IActionResult> ManageUser(string groupid, string userid, string operation)
        {
            User user = await _userManager.GetUserAsync(User);
            User targetuser = await _userManager.FindByIdAsync(userid);
            Group group = await _context.Groups.FindAsync(groupid);

            // Null checks
            if (user == null)
            {
                StatusMessage = $"Error: Please log in!";
                return RedirectToAction("Edit");
            }
            if (group == null)
            {
                StatusMessage = $"Error: Can't find group {groupid}!";
                return RedirectToAction("Edit");
            }
            if (targetuser == null)
            {
                StatusMessage = $"Error: Can't find user {userid}!";
                return RedirectToAction("Edit");
            }
            if (string.IsNullOrWhiteSpace(operation))
            {
                StatusMessage = $"Error: Operation is null!";
                return RedirectToAction("Edit");
            }


            string[] split = operation.Split('|');
            string action = split[0];

            TaskResult result = new TaskResult(false, "The action was unknown.");

            if (action == "kick")
            {
                result = await GroupManager.KickFromGroup(group, user, targetuser);
            }
            else if (action == "ban")
            {
                result = await GroupManager.BanFromGroup(group, user, targetuser);
            }
            else if (action == "addrole")
            {
                if (split.Length < 2)
                {
                    StatusMessage = $"Error: Operation is missing an argument!";
                    return RedirectToAction("Edit", new { groupid = groupid });
                }

                GroupRole role = _context.GroupRoles.FirstOrDefault(r => r.RoleId.ToLower() == split[1].ToLower());

                result = await GroupManager.AddToRole(group, user, targetuser, role);

                StatusMessage = result.Info;

                return Redirect(Request.Headers["Referer"].ToString());
            }
            else if (action == "removerole")
            {
                if (split.Length < 2)
                {
                    StatusMessage = $"Error: Operation is missing an argument!";
                    return RedirectToAction("Edit", new { groupid = groupid });
                }

                GroupRole role = _context.GroupRoles.FirstOrDefault(r => r.RoleId.ToLower() == split[1].ToLower());

                result = await GroupManager.RemoveFromRole(group, user, targetuser, role);

                StatusMessage = result.Info;

                return Redirect(Request.Headers["Referer"].ToString());
            }
            else if (action == "invite")
            {
                result = await GroupManager.AddInvite(group, user, targetuser);
            }
            else if (action == "uninvite")
            {
                result = await GroupManager.RemoveInvite(group, user, targetuser);
            }

            StatusMessage = result.Info;

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize]
        public async Task<IActionResult> Pay(string groupid)
        {
            User user = await _userManager.GetUserAsync(User);

            Group group = await _context.Groups.FindAsync(groupid);

            if (group == null)
            {
                return await RedirectBack($"Could not find the group {groupid}");
            }

            if (!await group.HasPermissionAsync(user, "eco"))
            {
                return await RedirectBack($"You don't have eco permissions!");
            }

            GroupPayModel model = new GroupPayModel()
            {
                Group = groupid
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(GroupPayModel model)
        {
            User user = await _userManager.GetUserAsync(User);

            Group group = await _context.Groups.FindAsync(model.Group);

            if (group == null)
            {
                return await RedirectBack($"Error: Could not find the group {model.Group}");
            }

            if (!await group.HasPermissionAsync(user, "eco"))
            {
                return await RedirectBack($"Error: You don't have eco permissions!");
            }

            if (model.Amount > group.Credits)
            {
                return await RedirectBack($"Error: The group doesn't have that much money!");
            }

            Entity target = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == model.User.ToLower());
            if (target == null) await _context.Groups.FirstOrDefaultAsync(x => x.Name.ToLower() == model.User.ToLower());

            if (target == null)
            {
                return await RedirectBack($"Error: Could not find {model.User}");
            }

            TaskResult result = await new TransactionRequest(group.Id, target.Id, model.Amount, "Group Direct Payment", ApplicableTax.None, false).Execute();

            if (!result.Succeeded)
            {
                return await RedirectBack(result.Info);
            }

            StatusMessage = "Successfully sent direct payment.";

            return RedirectToAction("View", new { groupid = model.Group });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> TransferGroup(string group_svid)
        {
            User user = await _userManager.GetUserAsync(User);

            // Retrieve group
            Group group = await _context.Groups.FindAsync(group_svid);

            if (group == null)
            {
                return NotFound($"Error: Could not find {group_svid}");
            }

            if (!(await group.IsOwnerAsync(user)))
            {
                return Forbid($"Error: You do not own {group.Name}");
            }

            TransferGroupModel model = new TransferGroupModel()
            {
                User = user,
                Group = group
            };

            return View(model);
        }
        

        /*
        [Authorize]
        public async Task<IActionResult> IssueStock(string groupid)
        {
            Group group = _context.Groups.FirstOrDefault(g => g.group_category == GroupTypes.Company && g.id == groupid);

            User user = await _userManager.GetUserAsync(User);

            if (group == null)
            {
                return await RedirectBack($"Could not find the company {groupid}");
            }

            if (group.HasPermission(user, "eco", _context))
            {
                if (_stockContext.Stocks.FirstOrDefault(s => s.GroupID == group.id) == null)
                {
                    return RedirectToAction("DoIPO", new { groupid = groupid });
                }

                return View(new IssueStockModel() { GroupID = group.id });
            }
            else
            {
                return await RedirectBack("You do not have group Eco permissions!");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueStock(IssueStockModel model)
        {
            Group group = _context.Groups.FirstOrDefault(g => g.group_category == GroupTypes.Company && g.id == model.GroupID);

            User user = await _userManager.GetUserAsync(User);

            if (group == null)
            {
                return await RedirectBack($"Could not find the company {model.GroupID}");
            }

            if (group.HasPermission(user, "eco", _context))
            {
                Stock stock = _stockContext.Stocks.FirstOrDefault(s => s.GroupID == model.GroupID);

                if (stock == null)
                {
                    return RedirectToAction("DoIPO", new { name = model.GroupID });
                }

                int maxStock = (int)Math.Floor(group.credits);
                int currentStock = stock.Total;

                if (model.Amount > maxStock - currentStock)
                {
                    return await RedirectBack($"Cannot issue that much stock! Value would go under ¢1!");
                }

                await stock.IssueStock(model.Amount, _stockContext);

                // Purchase stock
                new StockTransactionRequest(stock.Ticker, model.Purchase, user.Id, StockTransactionType.buy).Execute();

                await VoopAI.ecoChannel.SendMessageAsync($":moneybag: {stock.Ticker} ({group.name}) has issued {model.Amount} new stock!");
                StatusMessage = $"Successfully issued {model.Amount} stock!";
                return RedirectToAction("Index", controllerName: "Home");
            }
            else
            {
                return await RedirectBack("You do not have group Eco permissions!");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> DoIPO(string groupid)
        {
            Group group = _context.Groups.FirstOrDefault(g => g.group_category == GroupTypes.Company && g.id == groupid.ToLower());

            User user = await _userManager.GetUserAsync(User);

            if (group == null)
            {
                return await RedirectBack($"Could not find the company {groupid}");
            }

            if (group.HasPermission(user, "eco", _context))
            {
                if (group.credits < 1000)
                {
                    StatusMessage = "Error: Your group must be worth at least 1000 credits!";
                    return RedirectToAction("Index", controllerName: "Home");
                }

                // Already IPOed
                if (_stockContext.Stocks.FirstOrDefault(s => s.GroupID == groupid) != null)
                {
                    StatusMessage = "You already issued your first stock!";
                    return RedirectToAction("IssueStock", new { groupid = groupid });
                }

                IssueIPOModel model = new IssueIPOModel()
                {
                    Group = group.id
                };

                return View(model);
            }
            else
            {
                return await RedirectBack("You do not have group Eco permissions!");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoIPO(IssueIPOModel model)
        {
            Group group = _context.Groups.FirstOrDefault(g => g.group_category == GroupTypes.Company && g.id == model.Group);
            User user = await _userManager.GetUserAsync(User);

            if (group == null)
            {
                return await RedirectBack($"Could not find the company!");
            }

            if (group.HasPermission(user, "eco", _context))
            {
                if (group.credits < 1000)
                {
                    StatusMessage = "Error: Your group must be worth at least 1000 credits!";
                    return RedirectToAction("Index", controllerName: "Home");
                }

                // Already IPOed
                if (_stockContext.Stocks.FirstOrDefault(s => s.GroupID == model.Group) != null)
                {
                    StatusMessage = "You already issued your first stock!";
                    return RedirectToAction("IssueStock", new { groupname = group.name });
                }

                // Ticker already taken
                if (_stockContext.Stocks.Find(model.Ticker) != null)
                {
                    StatusMessage = "That ticker is already taken!";
                    return RedirectToAction("DoIPO", new { name = model.Group });
                }

                int max = (int)Math.Floor(group.credits);

                int amount = model.Amount;
                int keep = model.Keep;

                if (amount > max)
                {
                    return await RedirectBack("You cannot issue that much stock!");
                }
                if (keep > amount)
                {
                    return await RedirectBack("You cannot keep more stock than you issue!");
                }
                if (amount < 1)
                {
                    return await RedirectBack("You cannot issue zero or negative stock!");
                }
                if (amount < 0)
                {
                    return await RedirectBack("You cannot keep negative stock!");
                }

                decimal value = group.credits / amount;

                Stock stock = new Stock()
                {
                    GroupID = group.id,
                    Ticker = model.Ticker,
                    Total = amount,
                    PublicOwnership = $"{user.Id}:{keep}",
                    ExchangeOwned = amount - keep
                };

                await _stockContext.Stocks.AddAsync(stock);
                await _stockContext.SaveChangesAsync();

                StatusMessage = $"Successfully issued {amount} ${model.Ticker}";

                await VoopAI.ecoChannel.SendMessageAsync($":new: Welcome new company {model.Ticker}'s ({group.name}) IPO with {model.Amount} stock added to the market!");
                return RedirectToAction("Index", controllerName: "Home");
            }
            else
            {
                return await RedirectBack("You do not have group Eco permissions!");
            }

        }
        */

        public async Task<IActionResult> RedirectBack(string reason)
        {
            StatusMessage = reason;
            return Redirect(Request.Headers["Referer"].ToString());
        }
    }
}
