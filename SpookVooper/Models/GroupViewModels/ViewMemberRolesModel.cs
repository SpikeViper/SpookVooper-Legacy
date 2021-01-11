using SpookVooper.Web.Entities;
using SpookVooper.Web.Entities.Groups;

namespace SpookVooper.Web.Models.GroupViewModels
{
    public class ViewMemberRolesModel
    {
        public Group Group { get; set; }
        public User Target { get; set; }
    }
}
