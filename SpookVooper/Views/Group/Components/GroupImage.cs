using Microsoft.AspNetCore.Mvc;
using SpookVooper.Web.Entities.Groups;
using System.Threading.Tasks;

namespace SpookVooper.Web.Views.Groups.Components
{
    public class GroupImage : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(Group group)
        {
            return View(group);
        }
    }
}