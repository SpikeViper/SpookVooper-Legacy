using Microsoft.AspNetCore.Mvc;
using SpookVooper.Web.News;
using System.Threading.Tasks;

namespace SpookVooper.Web.Views.News.Components
{
    public class NewsBlurb : ViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync(NewsPost post)
        {
            return await Task.Run(() => View(post));
        }
    }
}
