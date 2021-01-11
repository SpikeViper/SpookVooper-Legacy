using System.ComponentModel.DataAnnotations;

namespace SpookVooper.Web.Models.GroupViewModels
{
    public class GroupInviteModel
    {
        [Required]
        [Display(Name = "Invitee")]
        public string InviteUser { get; set; }

        public string Group { get; set; }
    }
}
