using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Discord.WebSocket;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;

namespace SpookVooper.VoopAIService.Models
{
    [Table("DiscordUsers")]
    public class DiscordUser
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong id { get; set; }

        [Display(Name = "Name")]
        public string username { get; set; }

        [Display(Name = "Commends")]
        public int commends { get; set; }

        [Display(Name = "Commends Sent")]
        public int commends_sent { get; set; }

        [Display(Name = "Last Commend Hour")]
        public int last_commend_hour { get; set; }

        [Display(Name = "Last Commend Message (ID)")]
        public ulong last_commend_message { get; set; }

        [Display(Name = "Credits")]
        public float credits { get; set; }

        [Display(Name = "Discord Message XP")]
        public int message_xp { get; set; }

        [Display(Name = "Discord Messages")]
        public int message_count { get; set; }

        [Display(Name = "Last Message Minute")]
        public int last_message_minute { get; set; }

        [Display(Name = "Warnings")]
        public int warning_count { get; set; }

        [Display(Name = "Bans")]
        public int ban_count { get; set; }

        [Display(Name = "Kicks")]
        public int kick_count { get; set; }

        [Display(Name = "Game XP")]
        public int game_xp { get; set; }

        public User GetUser(VooperContext context)
        {
            return context.Users.FirstOrDefault(u => u.discord_id == id);
        }

        public int GetXP()
        {
            return (commends * 5) + (message_xp * 2) + (game_xp / 100);
        }

        public bool Senator()
        {
            return VoopAI.govServer.GetUser(id).Roles.Any(r => r.Name.ToLower().Contains("senator"));
        }

        public bool President()
        {
            return VoopAI.govServer.GetUser(id).Roles.Any(r => r.Id == 639212924724051988);
        }

        public bool VicePresident()
        {
            return VoopAI.govServer.GetUser(id).Roles.Any(r => r.Name.ToLower().Contains("vice president"));
        }

        public bool Justice()
        {
            return VoopAI.govServer.GetUser(id).Roles.Any(r => r.Name.ToLower().Contains("supreme court"));
        }

        public SocketRole Party()
        {
            return VoopAI.govServer.GetUser(id).Roles.FirstOrDefault(r => r.Name.ToLower().Contains("party"));
        }

        public SocketRole XPRole()
        {
            SocketUser user = VoopAI.server.GetUser(id);

            if (user == null)
            {
                return null;
            }

            return VoopAI.server.GetUser(id).Roles.FirstOrDefault(r => r.Name.ToLower().Contains("rank"));
        }

        public SocketUser Socket()
        {
            SocketUser user = VoopAI.server.GetUser(id);

            if (user == null)
            {
                user = VoopAI.govServer.GetUser(id);
            }

            return user;
        }

        public string AvatarUrl()
        {
            SocketUser user = Socket();

            if (user == null)
            {
                return "/media/unity-128.png";
            }
            else
            {
                return user.GetAvatarUrl();
            }
        }
    }
}
