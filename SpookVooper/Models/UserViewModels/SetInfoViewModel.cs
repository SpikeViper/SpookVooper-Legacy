using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Models.UserViewModels
{
    [Bind("Description")]
    public class SetInfoViewModel
    {
        public string Description { get; set; }
        public UserManager<User> userManager;
        public string StatusMessage { get; set; }
    }
}
