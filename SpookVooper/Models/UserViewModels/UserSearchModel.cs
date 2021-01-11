using Microsoft.AspNetCore.Identity;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Models.UserViewModels
{
    public class UserSearchModel
    {
        public UserManager<User> userManager;
        public string search { get; set; }
    }
}
