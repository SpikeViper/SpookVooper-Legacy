using Microsoft.AspNetCore.Identity;
using SpookVooper.Web.Forums;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace SpookVooper.Web.Helpers
{
    public static class RoleHelper
    {
        public static List<IdentityRole> GetRolesFromString(RoleManager<IdentityRole> manager, string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return null;
            }
            
            List<IdentityRole> results = new List<IdentityRole>();

            string[] split = input.Split(',');

            foreach (string s in split)
            {
                IdentityRole role = manager.FindByNameAsync(s).Result;

                if (role != null)
                {
                    results.Add(role);
                }
            }

            // SECURITY ADDITION //
            // In the case of a misspelling of a role, we want to use the most strict policy possible.
            if (split.Length != results.Count)
            {
                IdentityRole admin = manager.FindByNameAsync("Admin").Result;
                Console.WriteLine("There was a role error for a placed request.");
                return new List<IdentityRole>() { admin };
            }
            else
            {
                return results;
            }
        }

        public static bool UserAuthorizedForCategory(RoleManager<IdentityRole> manager, ForumCategory category, ClaimsPrincipal User)
        {
            List<IdentityRole> roles = GetRolesFromString(manager, category.RoleAccess);

            if (roles == null)
            {
                return true;
            }

            foreach (IdentityRole role in roles)
            {
                if (User.IsInRole(role.Name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
