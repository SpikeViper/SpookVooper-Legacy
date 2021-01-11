using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpookVooper.Data.Services;
using SpookVooper.Web.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;

namespace SpookVooper.Web.Controllers
{
    public class NotificationController : Controller
    {
        public NotificationController()
        {
        }

        [TempData]
        public string StatusMessage { get; set; }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            return View();
        }
    }
}
