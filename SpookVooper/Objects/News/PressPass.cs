using SpookVooper.Web.DB;
using SpookVooper.Web.Entities.Groups;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace SpookVooper.Web.News
{
    public class PressPass
    {
        [Key]
        public string GroupID { get; set; }

        public static async Task<bool> HasPressPass(Group group, VooperContext context)
        {
            return (await context.PressPasses.FindAsync(group.Id) != null);
        }
    }
}
