using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Data.Services;
using SpookVooper.Web.Models.GovernmentViewModels;
using SpookVooper.Web.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SpookVooper.Web.Helpers;
using Microsoft.EntityFrameworkCore;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.Government;
using SpookVooper.Web.Government.Voting;

namespace SpookVooper.Web.Controllers
{

    [Route("[controller]/[action]")]
    public class GovernmentController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly VooperContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IConnectionHandler _connectionHandler;

        public GovernmentController(
            VooperContext context,
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IConnectionHandler connectionHandler)
        {
            _emailSender = emailSender;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _connectionHandler = connectionHandler;
            _context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> Index()
        {
            GovernmentIndexModel model = new GovernmentIndexModel();

            // TODO: Fix this shit
            await Task.Run(async () =>
            {
                model.president = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == "spikeviper");
                model.vicePresident = await _context.Users.FirstOrDefaultAsync(x => x.UserName.ToLower() == "popefrancis");
                model.justices = null;
            });

            return View(model);
        }

        [Authorize]
        public async Task<IActionResult> MoveDistrict(string districtname)
        {
            District district = _context.Districts.FirstOrDefault(d => d.Name.ToLower() == districtname.ToLower());

            if (district == null)
            {
                StatusMessage = $"Error: Could not find {districtname}!";
                return Redirect(Request.Headers["Referer"].ToString());
            }

            User user = await _userManager.GetUserAsync(User);

            if (user.district_move_date != null)
            {
                var daysWaited = Math.Round(DateTime.Now.Subtract(user.district_move_date.Value).TotalDays, 0);

                if (daysWaited < 60)
                {
                    StatusMessage = $"Error: You must wait another {60 - daysWaited} days to move again!";
                    return Redirect(Request.Headers["Referer"].ToString());
                }
            }

            user.district = district.Name;

            if (!string.IsNullOrWhiteSpace(user.district))
            {
                user.district_move_date = DateTime.Now;
            }

            await _userManager.UpdateAsync(user);

            StatusMessage = $"You have moved to {districtname}!";

            return Redirect(Request.Headers["Referer"].ToString());
        }

        public async Task<IActionResult> Map()
        {
            return View();
        }

        public async Task<IActionResult> PolMap()
        {
            return View();
        }

        public async Task<ActionResult> ViewDistrict(string id)
        {
            if (id == null)
            {
                id = "";
            }

            District district = _context.Districts.FirstOrDefault(d => d.Name.ToLower() == id.ToLower());

            return View(district);
        }

        [HttpGet]
        [AuthorizeDiscord("Senator,Emperor")]
        public async Task<IActionResult> EditDistrict(string id)
        {
            if (id == null)
            {
                id = "";
            }

            User user = await _userManager.GetUserAsync(User);

            District district = _context.Districts.AsNoTracking().FirstOrDefault(d => d.Name.ToLower() == id.ToLower());

            if (district == null)
            {
                StatusMessage = $"Could not find the district {id}!";
                return View();
            }

            if (district.Senator != user.Id)
            {
                StatusMessage = $"Could not find the district {id}!";
                return View();
            }

            //if (district == null)
            //{
            //    district = new District();
            //}

            return View(district);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDistrict(District model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            District old = await _context.Districts.AsNoTracking().AsQueryable().FirstOrDefaultAsync(x => x.Name == model.Name);

            User user = await _userManager.GetUserAsync(User);

            if (user.Id != old.Senator)
            {
                StatusMessage = "Failed: You do not have permission to edit this district!";
                return View();
            }

            if (!(await _context.Districts.AsQueryable().AnyAsync(d => d.Name.ToLower() == model.Name.ToLower())))
            {
                _context.Districts.Add(model);
            }
            else
            {


                // Ensure senator is not changed
                model.Senator = old.Senator;

                _context.Districts.Update(model);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("ViewDistrict", new { id = model.Name });
        }

        [HttpGet]
        [Authorize]
        [AuthorizeDiscord("Senator,Emperor,Prime Minister,Board of MOF")]
        public async Task<IActionResult> Budget()
        {
            return View();
        }

        public async Task<IActionResult> Elections()
        {
            var current = _context.Elections.AsQueryable().Where(x => x.Active);

            return View(current);
        }

        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        public async Task<IActionResult> ManageElections()
        {
            var current = _context.Elections.AsQueryable().Where(x => x.Active);

            return View(current);
        }

        [HttpGet]
        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        public async Task<IActionResult> StartElection()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartElection(Election model)
        {
            model.Start_Date = DateTime.UtcNow;
            model.End_Date = DateTime.UtcNow.AddDays(1);
            model.Active = true;

            model.Id = Guid.NewGuid().ToString();

            await _context.Elections.AddAsync(model);
            await _context.SaveChangesAsync();

            StatusMessage = "Successfully started election.";

            return RedirectToAction("Elections");
        }

        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        public async Task<IActionResult> EndElection(string id)
        {
            Election election = await _context.Elections.FindAsync(id);

            if (election == null)
            {
                StatusMessage = $"Error: Could not find election with Id {id}";
                return RedirectToAction("ManageElections");
            }

            election.End_Date = DateTime.UtcNow;

            _context.Elections.Update(election);
            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully set election to end.";
            return RedirectToAction("ManageElections");
        }


        [HttpGet]
        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        public async Task<IActionResult> IssuePass()
        {
            CandidatePass pass = new CandidatePass();
            return View(pass);
        }

        [HttpGet]
        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        public async Task<IActionResult> ViewVotes(string id)
        {
            Election election = await _context.Elections.FindAsync(id);

            if (election == null)
            {
                StatusMessage = $"Error: Could not find election with Id {id}";
                return RedirectToAction("ManageElections");
            }

            return View(election);
        }

        [HttpGet]
        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        public async Task<IActionResult> InvalidateVote(string id)
        {
            ElectionVote vote = await _context.ElectionVotes.FindAsync(id);

            if (vote == null)
            {
                StatusMessage = $"Error: Could not find vote with Id {id}";
                return RedirectToAction("ManageElections");
            }

            vote.Invalid = !vote.Invalid;

            _context.ElectionVotes.Update(vote);
            await _context.SaveChangesAsync();

            if (vote.Invalid)
            {
                StatusMessage = "Invalidated vote successfully.";
            }
            else
            {
                StatusMessage = "Revalidated vote successfully";
            }

            return RedirectToAction("ManageElections");
        }


        [HttpPost]
        [Authorize]
        [AuthorizeGov("Ministry of Elections")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssuePass(CandidatePass pass)
        {
            User user = await _context.Users.FindAsync(pass.UserId);

            if (user == null)
            {
                StatusMessage = $"Error: Failed to find user with ID {pass.UserId}";
                return View();
            }

            pass.Id = Guid.NewGuid().ToString();

            await _context.CandidatePasses.AddAsync(pass);
            await _context.SaveChangesAsync();

            if (!pass.Blacklist)
            {
                StatusMessage = $"Successfully issued candidate pass.";
            }
            else
            {
                StatusMessage = $"Successfully issued candidate blacklist.";
            }

            return RedirectToAction("ManageElections");
        }
        

        public async Task<IActionResult> ViewElection(string id)
        {
            Election election = await _context.Elections.FindAsync(id);

            return View(election);
        }

        [Authorize]
        public async Task<IActionResult> Vote(string id)
        {
            Election election = await _context.Elections.FindAsync(id);

            return View(election);
        }

        [Authorize]
        public async Task<IActionResult> ProcessVote(string id, string choice)
        {
            User user = await _userManager.GetUserAsync(User);
            Election election = await _context.Elections.FindAsync(id);

            if (election == null)
            {
                StatusMessage = $"Error: Could not find election with Id {id}";
                return RedirectToAction("Elections");
            }

            User chosen = await _context.Users.FindAsync(choice);

            if (chosen == null)
            {
                StatusMessage = $"Error: Could not find user with Id {choice}";
                return RedirectToAction("Elections");
            }

            if (!(await chosen.IsEligibleForElection(election.Type, election.District)))
            {
                StatusMessage = $"Error: The user {chosen.Name} was not valid for this election.";
                return RedirectToAction("Elections");
            }

            if (!user.IsEmperor() && user.district != election.District)
            {
                StatusMessage = $"Error: You are not eligible to vote in {election.District}";
                return RedirectToAction("Elections");
            }

            if (await _context.ElectionVotes.AsQueryable().AnyAsync(x => x.Election_Id == election.Id && x.User_Id == user.Id))
            {
                StatusMessage = $"Error: You have already voted in this election!";
                return RedirectToAction("Elections");
            }

            ElectionVote vote = new ElectionVote()
            {
                Date = DateTime.UtcNow,
                Choice_Id = choice,
                Election_Id = election.Id,
                Id = Guid.NewGuid().ToString(),
                User_Id = user.Id
            };

            await _context.ElectionVotes.AddAsync(vote);
            await _context.SaveChangesAsync();

            StatusMessage = $"Successfully voted for {chosen.Name}.";
            return RedirectToAction("Elections");
        }
    }

}