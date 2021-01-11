using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Oauth2;
using SpookVooper.Web.DB;
using SpookVooper.Web.Oauth2;
using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SpookVooper.Web.Controllers
{
    public class Oauth2Controller : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly VooperContext _context;

        public static List<AuthorizeModel> authModels = new List<AuthorizeModel>();

        [TempData]
        public string StatusMessage { get; set; }

        public Oauth2Controller(
            VooperContext context,
            UserManager<User> userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            User user = await _userManager.GetUserAsync(User);

            return View(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Authorize(string response_type, string client_id, string redirect_uri, string scope, string state)
        {
            User user = await _userManager.GetUserAsync(User);

            if (response_type == null || string.IsNullOrWhiteSpace(response_type))
            {
                return NotFound("Please define the response type.");
            }


            if (response_type.ToLower() == "code")
            {
                OauthApp app = await _context.OauthApps.FindAsync(client_id);

                if (app == null)
                {
                    return NotFound("Could not find that client ID!");
                }

                string fscope = "";

                foreach (string s in scope.Split(','))
                {
                    fscope += $"|{s}";
                }

                AuthorizeModel model = new AuthorizeModel()
                {
                    ClientID = client_id,
                    Redirect = redirect_uri,
                    UserID = user.Id,
                    ReponseType = response_type,
                    Scope = fscope,
                    State = state
                };

                return View(model);
            }
            else
            {
                return NotFound($"Response type {response_type} is not yet supported!");
            }

            return NotFound("Ahhhh");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Authorize (AuthorizeModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Create key
            model.Code = Guid.NewGuid().ToString();

            authModels.Add(model);

            return Redirect(model.Redirect + $"?code={model.Code}&state={model.State}");
        }

        [HttpGet]
        public async Task<IActionResult> RequestToken(string grant_type, string code, string redirect_uri, 
                                                      string client_id, string client_secret)
        {
            if (grant_type.ToLower() == "authorization_code")
            {
                AuthorizeModel auth = authModels.FirstOrDefault(x => x.Code == code);

                if (auth == null)
                {
                    return NotFound("Could not find specified code.");
                }

                if (auth.ClientID != client_id)
                {
                    return NotFound("Client ID does not match.");
                }

                if (auth.Redirect != redirect_uri)
                {
                    return NotFound("Redirect does not match.");
                }

                OauthApp app = await _context.OauthApps.FirstOrDefaultAsync(x => x.Id == client_id);

                if (app.Secret != client_secret)
                {
                    return Unauthorized("Failed authorization. This failure has been logged.");
                }

                app.Uses += 1;

                _context.OauthApps.Update(app);
                await _context.SaveChangesAsync();

                string hash = ToHex(SHA256.Create().ComputeHash(Guid.NewGuid().ToByteArray()), false);

                AuthToken token = new AuthToken()
                {
                    Id = hash,
                    AppId = client_id,
                    UserId = auth.UserID,
                    Scope = auth.Scope,
                    Time = DateTime.UtcNow
                };

                await _context.AuthTokens.AddAsync(token);
                await _context.SaveChangesAsync();

                TokenResponse response = new TokenResponse()
                {
                    access_token = token.Id,
                    expires_in = 3600,
                    svid = token.UserId
                };

                return Json(response);
            }
            else
            {
                return NotFound("Grant type not implemented!");
            }
        }

        private static string ToHex(byte[] bytes, bool upperCase)
        {
            StringBuilder result = new StringBuilder(bytes.Length * 2);

            for (int i = 0; i < bytes.Length; i++)
                result.Append(bytes[i].ToString(upperCase ? "X2" : "x2"));

            return result.ToString();
        }

        public class TokenResponse
        {
            [JsonProperty]
            public string access_token { get; set; }
            [JsonProperty]
            public int expires_in { get; set; }
            [JsonProperty]
            public string svid { get; set; }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RegisterApp()
        {
            RegisterAppModel model = new RegisterAppModel();
            return View(model);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterApp(RegisterAppModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                StatusMessage = "Please enter a valid name!";
                return View();
            }

            User user = await _userManager.GetUserAsync(User);

            OauthApp app = new OauthApp()
            {
                Id = Guid.NewGuid().ToString(),
                Secret = Guid.NewGuid().ToString(),
                Name = model.Name,
                Image_Url = model.Image_Url,
                Owner = user.Id,
                Uses = 0
            };

            await _context.OauthApps.AddAsync(app);
            await _context.SaveChangesAsync();

            StatusMessage = "Successfully created Application!";

            return RedirectToAction("ViewApp", new { appid  = app.Id });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ViewApp(string appid)
        {
            OauthApp app = await _context.OauthApps.FindAsync(appid);

            if (app == null)
            {
                return NotFound("Could not find that app!");
            }

            User user = await _userManager.GetUserAsync(User);

            if (app.Owner != user.Id)
            {
                return Unauthorized("You do not own this app!");
            }

            return View(app);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ViewSecret(string appid)
        {
            OauthApp app = await _context.OauthApps.FindAsync(appid);

            if (app == null)
            {
                return NotFound("Could not find that app!");
            }

            User user = await _userManager.GetUserAsync(User);

            if (app.Owner != user.Id)
            {
                return Unauthorized("You do not own this app!");
            }

            return View((object)app.Secret);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ResetSecret(string secret)
        {
            OauthApp app = await _context.OauthApps.FirstOrDefaultAsync(x => x.Secret == secret);

            if (app == null)
            {
                return NotFound("There was an error resetting the secret!");
            }

            User user = await _userManager.GetUserAsync(User);

            if (app.Owner != user.Id)
            {
                return Unauthorized("There was an error resetting the secret!");
            }

            app.Secret = Guid.NewGuid().ToString();

            _context.OauthApps.Update(app);
            await _context.SaveChangesAsync();

            return RedirectToAction("ViewApp", new { appid = app.Id });
        }

    }


}
