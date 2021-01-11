using Microsoft.AspNetCore.Mvc;
using SpookVooper.Web.Models.ForumViewModels;
using System.Threading.Tasks;

namespace SpookVooper.Web.Views.Forum.Components
{
    public class Comment : ViewComponent
    {

        public async Task<IViewComponentResult> InvokeAsync(CommentViewModel comment)
        {
            return View(comment);
        }
    }
}
