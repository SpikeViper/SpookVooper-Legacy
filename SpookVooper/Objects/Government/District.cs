using Microsoft.AspNetCore.Mvc.Rendering;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Entities.Groups;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SpookVooper.Web.Government
{
    public class District
    {
        [Key]
        public string Name { get; set; }

        [Display(Name = "Flag Image URL")]
        public string Flag_Url { get; set; }
        public string Description { get; set; }
        public string Senator { get; set; }
        public string Group_Id { get; set; }

        public async Task<Group> GetGroup()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.Groups.FindAsync(Group_Id);
            }
        }

        public async Task<User> GetSenator(VooperContext context)
        {
            User user = await context.Users.FindAsync(Senator);

            return user;
        }

        public static List<SelectListItem> GetDistrictListForDropdown(VooperContext context)
        {
            List<SelectListItem> items = new List<SelectListItem>();

            items.Add(new SelectListItem() { Text = "None", Value = "" });

            foreach (District d in context.Districts)
            {
                items.Add(new SelectListItem() { Text = d.Name, Value = d.Name });
            }

            return items;
        }

        public static async Task<List<User>> GetAllSenatorsAsync(VooperContext context)
        {
            List<User> senators = new List<User>(15);

            foreach (District dis in context.Districts)
            {
                User user = await context.Users.FindAsync(dis.Senator);

                if (user != null)
                {
                    senators.Add(user);
                }
            }

            return senators;
        }
    }
}
