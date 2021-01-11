using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Security.Claims;
using System.Linq;
using SpookVooper.VoopAIService;
using SpookVooper.Web.Controllers;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Helpers
{
    public class AuthorizeGovAttribute : TypeFilterAttribute
    {
        public AuthorizeGovAttribute(string claimValue) : base(typeof(GovFilter))
        {
            Arguments = new object[] { new Claim("gov", claimValue) };
        }
    }

    public class GovFilter : IAuthorizationFilter
    {
        readonly Claim _claim;

        public GovFilter(Claim claim)
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
                User user = c.Users.Find(userId);

                if (user == null) Fail(context);

                bool allowed = false;

                if (user.IsEmperor())
                {
                    allowed = true;
                }
                else
                {
                    if (_claim.Value.ToLower() == "senator")
                    {
                        if (user.IsSenator().Result)
                        {
                            allowed = true;
                        }
                    }
                    else if (_claim.Value.ToLower().StartsWith("ministry"))
                    {
                        if (c.Ministers.Any(x => x.UserId == user.Id && _claim.Value.ToLower() == x.Ministry.ToLower()))
                        {
                            allowed = true;
                        }
                    }
                    else if (_claim.Value.ToLower() == "justice")
                    {
                        if (user.IsJustice())
                        {
                            allowed = true;
                        }
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
