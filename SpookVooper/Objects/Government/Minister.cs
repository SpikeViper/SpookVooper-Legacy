using Microsoft.EntityFrameworkCore;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SpookVooper.Web.Government
{
    public class Minister
    {
        [Key]
        public string Ministry { get; set; }
        public string UserId { get; set; }

        /// <summary>
        /// Returns the User object for this Minister
        /// </summary>
        public async Task<User> GetUser(VooperContext context)
        {
            return await context.Users.AsQueryable().FirstOrDefaultAsync(x => x.Id == UserId);
        }

        /// <summary>
        /// Returns the Ministry object this Minister belongs to
        /// </summary>
        public async Task<Ministry> GetMinistry(VooperContext context)
        {
            return await context.Ministries.AsQueryable().FirstOrDefaultAsync(x => x.Name == Ministry);
        }
    }
}
