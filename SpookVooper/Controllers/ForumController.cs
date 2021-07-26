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
using SpookVooper.Web.Models.ForumViewModels;
using SpookVooper.Web.Models.ManageViewModels;
using SpookVooper.Web.Helpers;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.Forums;
using SpookVooper.Web.Entities.Groups;

namespace SpookVooper.Web.Controllers
{
    public class ForumController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private RoleManager<IdentityRole> _roleManager;
        private readonly VooperContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IConnectionHandler _connectionHandler;

        public ForumController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            VooperContext context,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IConnectionHandler connectionHandler,
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

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> Index(string id, int page)
        {
            if (String.IsNullOrEmpty(id))
            {
                id = "Root";
            }

            ForumIndexViewModel model = new ForumIndexViewModel()
            {
                Category = id,
                userManager = _userManager,
                amount = 10,
                page = page
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Admin()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AddCategory()
        {
            CategoryViewModel model = new CategoryViewModel();

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            _context.ForumCategories.Add(new ForumCategory()
            {
                CategoryID = model.Name,
                Description = model.Description,
                Tags = model.Tags,
                Parent = model.Parent
            });

            StatusMessage = $"Successfully added the category {model.Name}.";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> EditCategory(string id)
        {

            ForumCategory current = _context.ForumCategories.FirstOrDefault(c => c.CategoryID == id);

            if (current == null)
            {
                StatusMessage = $"Error: The category {id} does not exist!";
                return RedirectToAction(nameof(Index));
            }

            CategoryViewModel model = new CategoryViewModel()
            {
                Name = current.CategoryID,
                Description = current.Description,
                Tags = current.Tags,
                Parent = current.Parent,
                RoleAccess = current.RoleAccess
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(CategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ForumCategory category = _context.ForumCategories.FirstOrDefault(c => c.CategoryID == model.Name);

            category.CategoryID = model.Name;
            category.Description = model.Description;
            category.Tags = model.Tags;
            category.Parent = model.Parent;
            category.RoleAccess = model.RoleAccess;

            StatusMessage = $"Successfully edited the category {model.Name}.";

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AddPost(string id, bool picpost)
        {

            ForumCategory category = _context.ForumCategories.FirstOrDefault(c => c.CategoryID == id);

            if (category == null)
            {
                StatusMessage = $"Failed to find the category {id}.";
                return RedirectToAction(nameof(Index));
            }

            User user = null;

            if (User != null)
            {
                user = await _userManager.GetUserAsync(User);
            }

            // Group check: Ensure that if this is a group category, the poster is a member!
            Group group = _context.Groups.FirstOrDefault(g => category.CategoryID == g.Name);

            if (group != null)
            {
                if (!await group.IsInGroup(user))
                {
                    StatusMessage = $"Error: You must be a part of the group to post here! Ensure you are logged in.";
                    return RedirectToAction(nameof(Index));
                }
            }

            if (RoleHelper.UserAuthorizedForCategory(_roleManager, category, User))
            {
                PostViewModel model = new PostViewModel();
                model.Category = id;
                model.Picture = picpost;

                return View(model);
            }
            else
            {
                StatusMessage = $"Error: You are not authorized to create a post in {id}.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPost(PostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User user = await _userManager.GetUserAsync(User);

            ForumCategory category = _context.ForumCategories.FirstOrDefault(c => c.CategoryID == model.Category);

            if (category == null)
            {
                StatusMessage = $"Error: Failed to find the category {model.Category}.";
                return RedirectToAction(nameof(Index));
            }

            if (RoleHelper.UserAuthorizedForCategory(_roleManager, category, User))
            {

                // Check for group ownership and permissions
                Group group = _context.Groups.FirstOrDefault(g => category.CategoryID.ToLower() == g.Name.ToLower());

                if (group != null)
                {
                    if (!await group.HasPermissionAsync(user, "post"))
                    {
                        StatusMessage = $"Error: You don't have permission to post in this group!";
                        return RedirectToAction(nameof(Index));
                    }
                }

                model.PostID = (ulong)_context.ForumPosts.LongCount();
                model.TimePosted = DateTime.UtcNow;
                model.Author = user.Id;

                if (model.Picture)
                {
                    model.Content = model.ImageLink;
                }

                _context.ForumPosts.Add(new ForumPost()
                {
                    Author = model.Author,
                    Category = model.Category,
                    Content = model.Content,
                    PostID = model.PostID,
                    Tags = model.Tags,
                    TimePosted = model.TimePosted,
                    Title = model.Title,
                    Picture = model.Picture
                });

                await _context.SaveChangesAsync();

                if (model.Picture)
                {
                    // await VoopAI.forumChannel.SendMessageAsync($"New Picture Post! \n https://SpookVooper.com/Forum/ViewPost/{model.PostID}");
                }
                else { 
                    // await VoopAI.forumChannel.SendMessageAsync($"New Post! \n https://SpookVooper.com/Forum/ViewPost/{model.PostID}");
                }

                StatusMessage = $"Post created successfully!";
                return RedirectToAction(nameof(ViewPost), new { id = model.PostID });
            }
            else
            {
                StatusMessage = $"Error: You are not authorized to create a post in {category.CategoryID}. [POST Request]";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RemovePost(ulong id)
        {

            ForumPost post = _context.ForumPosts.FirstOrDefault(p => p.PostID == id);

            if (post == null)
            {
                StatusMessage = $"Failed to find post with id {id}.";
                return RedirectToAction(nameof(Index));
            }

            User user = await _userManager.GetUserAsync(User);

            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")))
            {
                if (post.Author != user.Id)
                {
                    StatusMessage = $"You are not the author of that post!";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(new RemovePostModel() { PostID = id });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePost(ForumPost model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User user = await _userManager.GetUserAsync(User);

            ForumPost post = _context.ForumPosts.FirstOrDefault(p => p.PostID == model.PostID);

            if (post == null)
            {
                StatusMessage = $"Error: Could not find a post with the id {model.PostID}";
                return RedirectToAction(nameof(Index));
            }

            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")))
            {
                if (post.Author != user.Id)
                {
                    StatusMessage = $"Error: You are not the author of that post!";
                    return RedirectToAction(nameof(Index));
                }
            }

            post.Removed = true;

            await _context.SaveChangesAsync();

            StatusMessage = $"Post removed successfully!";
            return RedirectToAction(nameof(ViewPost), new { id = model.PostID });

        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditPost(ulong id)
        {

            ForumPost post = _context.ForumPosts.FirstOrDefault(p => p.PostID == id);

            if (post == null)
            {
                StatusMessage = $"Failed to find post with id {id}.";
                return RedirectToAction(nameof(Index));
            }

            User user = await _userManager.GetUserAsync(User);

            if (post.Author != user.Id)
            {
                StatusMessage = $"You are not the author of that post!";
                return RedirectToAction(nameof(Index));
            }

            PostViewModel model = new PostViewModel()
            {
                Author = post.Author,
                Category = post.Category,
                Content = post.Content,
                PostID = post.PostID,
                Likes = post.GetLikes(_context),
                Tags = post.Tags,
                TimePosted = post.TimePosted,
                Title = post.Title,
                Picture = post.Picture
            };

            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(PostViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            User user = await _userManager.GetUserAsync(User);

            ForumPost post = _context.ForumPosts.FirstOrDefault(p => p.PostID == model.PostID);

            if (post == null)
            {
                StatusMessage = $"Error: Could not find a post with the id {model.PostID}";
                return RedirectToAction(nameof(Index));
            }

            if (post.Author != user.Id)
            {
                StatusMessage = $"Error: You are not the author of that post!";
                return RedirectToAction(nameof(Index));
            }

            post.Content = model.Content;
            post.Tags = model.Tags;
            post.Title = model.Title;

            await _context.SaveChangesAsync();

            StatusMessage = $"Post edited successfully!";
            return RedirectToAction(nameof(ViewPost), new { id = model.PostID });

        }

        public async Task<IActionResult> ViewPost(ulong id)
        {
            ForumPost post = _context.ForumPosts.FirstOrDefault(p => p.PostID == id);

            if (post == null)
            {
                StatusMessage = $"Error: The post was not found with id {id}";
                return RedirectToAction(nameof(Index));
            }

            return View(post);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddLike(ulong id)
        {
            User user = await _userManager.GetUserAsync(User);

            ForumPost post = _context.ForumPosts.FirstOrDefault(p => p.PostID == id);

            if (post == null)
            {
                return Json("No post!");
            }

            if (user != null)
            {
                if (_context.ForumLikes.Any(l => l.AddedBy == user.Id && l.Post == id))
                {
                    // Cancel because there is already a like
                    return Json("Failure");
                }
                else
                {
                    string likeid = $"{id}-{user.Id}";

                    // Success
                    ForumLike like = new ForumLike()
                    {
                        AddedBy = user.Id,
                        LikeID = likeid,
                        Post = id,
                        GivenTo = post.Author
                    };

                    _context.ForumLikes.Add(like);

                    await _context.SaveChangesAsync();

                    User author = await _userManager.FindByIdAsync(like.GivenTo);

                    author.post_likes += 1;

                    await _userManager.UpdateAsync(author);

                    return Json("Success");
                }
            }
            else
            {
                // There is no user (?)
                return Json("Failure");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLike(ulong id)
        {
            User user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                ForumLike like = _context.ForumLikes.FirstOrDefault(l => l.AddedBy == user.Id && l.Post == id);

                if (like != null)
                {
                    // There is a like, so we will remove it
                    _context.ForumLikes.Remove(like);
                    await _context.SaveChangesAsync();

                    User author = await _userManager.FindByIdAsync(like.GivenTo);

                    author.post_likes -= 1;

                    await _userManager.UpdateAsync(author);

                    return Json("Success");
                }
                else
                {
                    // No like exists
                    return Json("Failure");
                }
            }
            else
            {
                // There is no user (?)
                return Json("Failure");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(ulong postid, bool root, ulong commentid, string content)
        {
            if (content.Length > 1000)
            {
                return Json($"Error: Comment length is over 1000");
            }

            if (String.IsNullOrWhiteSpace(content))
            {
                return Json($"Error: Comment is empty");
            }

            if (!ModelState.IsValid)
            {
                return Json($"Error: Model state is not valid.");
            }

            ForumPost post = _context.ForumPosts.FirstOrDefault(p => p.PostID == postid);

            if (post == null)
            {
                return Json($"Failed to find the post {postid}.");
            }

            User user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json($"Failed to find user!");
            }

            ForumComment oncomment = null;
            ulong? oncommentid = null;

            Notification notification;

            if (!root)
            {
                oncomment = _context.ForumComments.FirstOrDefault(c => c.CommentID == commentid);

                if (oncomment == null)
                {
                    return Json($"Failed to find the comment {commentid}.");
                }

                oncommentid = oncomment.CommentID;

                notification = new Notification()
                {
                    NotificationID = $"{user.Id}-{DateTime.UtcNow}-{post.PostID}",
                    Author = user.Id,
                    Content = content,
                    Source = oncomment.CommentID,
                    TimeSent = DateTime.UtcNow,
                    Title = $"{user.UserName} has replied to your comment!",
                    Type = "Reply",
                    Target = oncomment.UserID,
                    Linkback = $"https://spookvooper.com/forum/viewpost/{post.PostID}"
                };
            }
            else
            {
                notification = new Notification()
                {
                    NotificationID = $"{user.Id}-{DateTime.UtcNow}-{post.PostID}",
                    Author = user.Id,
                    Content = content,
                    Source = post.PostID,
                    TimeSent = DateTime.UtcNow,
                    Title = $"{user.UserName} has commented on your post!",
                    Type = "Comment",
                    Target = post.Author,
                    Linkback = $"https://spookvooper.com/forum/viewpost/{post.PostID}"
                };
            }

            ForumComment comment = new ForumComment()
            {
                CommentOnID = oncommentid,
                PostOnID = postid,
                UserID = user.Id,
                CommentID = (ulong)_context.ForumComments.LongCount(),
                Content = content,
                TimePosted = DateTime.UtcNow,
            };

            _context.ForumComments.Add(comment);
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully added comment!";

            return Json("Success");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddCommentLike(ulong id)
        {
            User user = await _userManager.GetUserAsync(User);

            ForumComment comment = _context.ForumComments.FirstOrDefault(c => c.CommentID == id);

            if (comment == null)
            {
                return Json("Comment not found!");
            }

            if (user != null)
            {
                if (_context.ForumCommentLikes.Any(l => l.UserID == user.Id && l.CommentID == id))
                {
                    // Cancel because there is already a like
                    return Json("Failure");
                }
                else
                {
                    string likeid = $"{id}-{user.Id}";

                    // Success
                    ForumCommentLike like = new ForumCommentLike()
                    {
                        UserID = user.Id,
                        LikeID = likeid,
                        CommentID = id,
                        GivenTo = comment.UserID
                    };

                    _context.ForumCommentLikes.Add(like);

                    await _context.SaveChangesAsync();

                    User author = await _userManager.FindByIdAsync(like.GivenTo);

                    author.comment_likes += 1;

                    await _userManager.UpdateAsync(author);

                    return Json("Success");
                }
            }
            else
            {
                // There is no user (?)
                return Json("Failure");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveCommentLike(ulong id)
        {
            User user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                ForumCommentLike like = _context.ForumCommentLikes.FirstOrDefault(l => l.UserID == user.Id && l.CommentID == id);

                if (like != null)
                {
                    // There is a like, so we will remove it
                    _context.ForumCommentLikes.Remove(like);
                    await _context.SaveChangesAsync();

                    User author = await _userManager.FindByIdAsync(like.GivenTo);

                    author.comment_likes -= 1;

                    await _userManager.UpdateAsync(author);

                    return Json("Success");
                }
                else
                {
                    // No like exists
                    return Json("Failure");
                }
            }
            else
            {
                // There is no user (?)
                return Json("Failure");
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComment(ulong commentid, string content)
        {
            if (content.Length > 1000)
            {
                return Json($"Error: Comment length is over 1000");
            }

            if (String.IsNullOrWhiteSpace(content))
            {
                return Json($"Error: Comment is empty");
            }

            if (!ModelState.IsValid)
            {
                return Json($"Error: Model state is not valid.");
            }

            ForumComment comment = _context.ForumComments.FirstOrDefault(c => c.CommentID == commentid);

            if (comment == null)
            {
                return Json($"Failed to find the comment {commentid}.");
            }

            User user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json($"Failed to find user!");
            }

            if (user.Id != comment.UserID)
            {
                return Json($"This is not your comment!");
            }

            comment.Content = content;

            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully added comment!";

            return Json("Success");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveComment(ulong commentid)
        {
            if (!ModelState.IsValid)
            {
                return Json($"Error: Model state is not valid.");
            }

            ForumComment comment = _context.ForumComments.FirstOrDefault(c => c.CommentID == commentid);

            if (comment == null)
            {
                return Json($"Failed to find the comment {commentid}.");
            }

            User user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return Json($"Failed to find user!");
            }

            if (!(User.IsInRole("Admin") || User.IsInRole("Moderator")))
            {
                if (user.Id != comment.UserID)
                {
                    return Json($"This is not your comment!");
                }
            }

            comment.Removed = true;

            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully removed comment!";

            return Json("Success");
        }
    }
}