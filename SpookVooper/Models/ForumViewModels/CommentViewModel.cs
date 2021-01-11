using SpookVooper.Web.Entities;
using SpookVooper.Web.Forums;

namespace SpookVooper.Web.Models.ForumViewModels
{
    public class CommentViewModel
    {
        public User webUser { get; set; }
        public ForumComment comment { get; set; }
    }
}
