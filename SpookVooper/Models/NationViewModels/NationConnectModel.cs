using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpookVooper.Web.Models.NationViewModels
{
    public class NationConnectModel
    {
        [Required]
        [Display(Name = "Nation Name")]
        public string NationName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Passcode")]
        public string Password { get; set; }
    }
}
