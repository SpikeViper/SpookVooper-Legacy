using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpookVooper.Web.Models.ManageViewModels
{
    public class IndexViewModel
    {
        [Required]
        [RegularExpression("^[a-zA-Z0-9_]*$", ErrorMessage = "Please use only letters, numbers, and underscores.")]
        public string Username { get; set; }

        [Display(Name = "Discord ID")]
        public ulong? discordid { get; set; }

        [Display(Name = "Twitch ID")]
        public string twitchid { get; set; }

        [Display(Name = "NationStates Nation")]
        public string NationState { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Phone]
        [Display(Name = "Phone number")]
        public string PhoneNumber { get; set; }

        public string StatusMessage { get; set; }

        public string Id { get; set; }
    }
}
