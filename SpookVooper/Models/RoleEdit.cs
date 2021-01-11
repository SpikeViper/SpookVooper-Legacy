using Microsoft.AspNetCore.Identity;
using SpookVooper.Web.Entities;
using System.Collections.Generic;

namespace SpookVooper.Web.Models
{
    public class RoleEdit
    {
        public IdentityRole Role { get; set; }
        public IEnumerable<User> Members { get; set; }
        public IEnumerable<User> NonMembers { get; set; }
    }
}
