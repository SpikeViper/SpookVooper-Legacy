using SpookVooper.Web.DB;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace SpookVooper.Web.Entities.Groups
{
    public class GroupBan
    {
        [Key]
        public string Id { get; set; }
        public string Group_Id { get; set; }
        public string User_Id { get; set; }

        public async Task<Group> GetGroup(VooperContext context)
        {
            return await context.Groups.FindAsync(Group_Id);
        }

        public async Task<User> GetUser(VooperContext context)
        {
            return await context.Users.FindAsync(User_Id);
        }
    }
}
