using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using System.Linq;
using SpookVooper.VoopAIService;
using Discord.WebSocket;
using SpookVooper.Web.Controllers;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Helpers
{
    public class AuthorizeDiscordAttribute : TypeFilterAttribute
    {
        public AuthorizeDiscordAttribute(string claimValue) : base(typeof(DiscordFilter))
        {
            Arguments = new object[] { new Claim("discord", claimValue) };
        }
    }

    public class DiscordFilter : IAuthorizationFilter
    {
        readonly Claim _claim;

        public DiscordFilter(Claim claim)
        {
            _claim = claim;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            if (context.HttpContext.User == null)
            {
                Fail(context);
                return;
            }

            var cl = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (cl == null) 
            { 
                Fail(context);
                return;
            }

            string userId = cl.Value;

            using (VooperContext c = new VooperContext(VoopAI.DBOptions)) 
            {
                Console.WriteLine("Test");

                User user = c.Users.Find(userId);

                if (user == null) Fail(context);

                SocketGuildUser discordUser = null;

                discordUser = VoopAI.server.Users.FirstOrDefault(u => u.Id == user.discord_id);

                if (discordUser == null) Fail(context);

                string[] claims = _claim.Value.Split(',');

                bool allowed = false;

                foreach (string claim in claims)
                {
                    if (discordUser.Roles.Any(r => r.Name.ToLower() == claim.ToLower()))
                    {
                        allowed = true;
                    }
                }

                if (!allowed)
                {
                    Fail(context);
                }
            }
        }

        public void Fail(AuthorizationFilterContext context)
        {
            AccountController.forbidden = $"This page is restricted to the role {_claim.Value}";
            context.Result = new ForbidResult();
        }
    }

}
