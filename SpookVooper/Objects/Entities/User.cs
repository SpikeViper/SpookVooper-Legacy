using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using SpookVooper.Web.DB;
using SpookVooper.Web.Oauth2;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Government.Voting;
using SpookVooper.Api.Entities;
using AutoMapper;

namespace SpookVooper.Web.Entities
{
    [JsonObject(MemberSerialization.OptIn)]
    public class User : IdentityUser, Entity
    {
        // Svid
        [JsonProperty]
        public override string Id { get => base.Id; set => base.Id = value; }

        [JsonProperty]
        public override string UserName { get => base.UserName; set => base.UserName = value; }

        // Other accounts
        [JsonProperty]
        public string twitch_id { get; set; }

        [JsonProperty]
        public ulong? discord_id { get; set; }

        // Forum stuff
        [JsonProperty]
        public int post_likes { get; set; }

        [JsonProperty]
        public int comment_likes { get; set; }

        // NationStates nation
        [JsonProperty]
        public string nationstate { get; set; }

        // Description
        [JsonProperty]
        public string description { get; set; }

        // Credits
        [JsonProperty]
        [Display(Name = "Credits")]
        public decimal Credits { get; set; }

        // API Key
        [JsonIgnore]
        public string Api_Key { get; set; }

        [JsonProperty]
        public int api_use_count { get; set; }

        [JsonProperty]
        public string minecraft_id { get; set; }

        [JsonProperty]
        public string Name { get { return UserName; } }

        // Twitch stuff
        [JsonProperty]
        public int twitch_last_message_minute { get; set; }

        [JsonProperty]
        public int twitch_message_xp { get; set; }

        [JsonProperty]
        public int twitch_messages { get; set; }

        // Discord stuff
        [JsonProperty]
        [Display(Name = "Commends")]
        public int discord_commends { get; set; }

        [JsonProperty]
        [Display(Name = "Commends Sent")]
        public int discord_commends_sent { get; set; }

        [JsonProperty]
        [Display(Name = "Last Commend Hour")]
        public int discord_last_commend_hour { get; set; }

        [JsonProperty]
        [Display(Name = "Last Commend Message (ID)")]
        public ulong discord_last_commend_message { get; set; }

        [JsonProperty]
        [Display(Name = "Discord Message XP")]
        public int discord_message_xp { get; set; }

        [JsonProperty]
        [Display(Name = "Discord Messages")]
        public int discord_message_count { get; set; }

        [JsonProperty]
        [Display(Name = "Last Message Minute")]
        public int discord_last_message_minute { get; set; }

        [JsonProperty]
        [Display(Name = "Last Message Time")]
        public DateTime Discord_Last_Message_Time { get; set; }

        [JsonProperty]
        [Display(Name = "Warnings")]
        public int discord_warning_count { get; set; }

        [JsonProperty]
        [Display(Name = "Bans")]
        public int discord_ban_count { get; set; }

        [JsonProperty]
        [Display(Name = "Kicks")]
        public int discord_kick_count { get; set; }

        [JsonProperty]
        [Display(Name = "Game XP")]
        public int discord_game_xp { get; set; }

        // Government Stuff
        [JsonProperty]
        [Display(Name = "District")]
        public string district { get; set; }

        [JsonProperty]
        [Display(Name = "District Move Date")]
        public DateTime? district_move_date { get; set; }

        public string Image_Url { get; set; }

        public decimal Credits_Invested { get; set; }

        public int GetTotalXP()
        {
            return post_likes + comment_likes + (twitch_message_xp * 4) + (discord_commends * 5) + (discord_message_xp * 2) + (discord_game_xp / 100);
        }

        // DISCORD HELPER METHODS

        public int GetDaysSinceLastMove()
        {
            if (district_move_date == null) return int.MaxValue;

            return (int)DateTime.Now.Subtract((DateTime)district_move_date).TotalDays;
        }

        public bool HasPermission(Entity entity, string perm)
        {
            return entity.Id == Id;
        }

        public async Task<bool> HasPermissionAsync(Entity entity, string perm)
        {
            return HasPermission(entity, perm);
        }

        public async Task<bool> HasPermissionWithKey(string key, string permission)
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                // Return true if matching master API key
                if (key == Api_Key)
                {
                    return true;
                }

                if (key.Contains('|'))
                {
                    string[] split = key.Split('|');
                    string token = split[0];
                    string app_secret = split[1];

                    OauthApp app = await context.OauthApps.AsQueryable().FirstOrDefaultAsync(x => x.Secret == app_secret);

                    if (app != null)
                    {
                        // Check if any Oauth tokens match
                        return await context.AuthTokens.AsQueryable().AnyAsync(x => x.AppId == app.Id && x.UserId == Id && x.Id == token && x.Scope.ToLower().Contains(permission.ToLower()));
                    }
                }

                return false;
            }
        }

        public async Task<bool> IsSenator()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                return await context.Districts.AsQueryable().AnyAsync(x => x.Senator == Id);
            }
        }

        public bool IsEmperor()
        {
            return (Id == "u-2a0057e6-356a-4a49-b825-c37796cb7bd9");
        }

        public bool IsPrimeMinister()
        {
            return (Id == "u-1419405d-9197-4383-a483-2eb93eab592e");
        }

        public bool IsJustice()
        {
            return false;
        }

        public async Task<IEnumerable<Group>> GetJoinedGroupsAsync()
        {
            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                var members = context.GroupMembers.AsQueryable().Where(x => x.User_Id == Id);

                List<Group> groups = new List<Group>();

                foreach (var member in members)
                {
                    Group group = await context.Groups.FindAsync(member.Group_Id);

                    if (group != null)
                    {
                        groups.Add(group);
                    }
                }

                return groups;
            }
        }

        public async Task<IEnumerable<Group>> GetOwnedGroupsAsync()
        {
            List<Group> groups = new List<Group>();

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                var topGroups = context.Groups.AsQueryable().Where(x => x.Owner_Id == Id);

                foreach (Group group in topGroups)
                {
                    groups.Add(group);
                    groups.AddRange(await group.GetOwnedGroupsAsync());
                }
            }

            return groups;
        }

        public async Task<bool> IsEligibleForElection(string type, string district)
        {
            if (district == null)
            {
                return false;
            }

            using (VooperContext context = new VooperContext(VooperContext.DBOptions))
            {
                CandidatePass pass = await context.CandidatePasses.AsQueryable().FirstOrDefaultAsync(x => x.UserId == Id && x.District == district && x.Type == type);

                if (pass != null)
                {
                    if (pass.Blacklist)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                // If there's no pass use normal criteria

                if (type == "Senate")
                {
                    if (IsEmperor())
                    {
                        return false;
                    }

                    return GetDaysSinceLastMove() > 30;
                }

                return true;
            }
        }

        public string GetPfpUrl()
        {
            return "/media/unity-128.png";
        }

        public UserSnapshot MapToSnapshot(IMapper mapper)
        {
            UserSnapshot snapshot = mapper.Map<UserSnapshot>(this);
            return snapshot;
        }
    }
}