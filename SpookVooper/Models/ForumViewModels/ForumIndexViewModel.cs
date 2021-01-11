using Microsoft.AspNetCore.Identity;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Models.ForumViewModels
{
    public class ForumIndexViewModel
    {
        public string Category { get; set; }
        public UserManager<User> userManager { get; set; }
        public int page { get; set; }

        public int amount { get; set; }
    }
}
