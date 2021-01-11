using System.ComponentModel.DataAnnotations;

namespace SpookVooper.Web.Models.ManageViewModels
{
    public class CategoryViewModel
    {
        [Display(Name = "Category Name")]
        [Required]
        [MaxLength(50, ErrorMessage = "Name should be under 50 characters.")]
        [RegularExpression("^[a-zA-Z0-9_ ]*$", ErrorMessage = "Please use only letters, numbers, and underscores.")]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [Required]
        [MaxLength(50, ErrorMessage = "Description should be under 50 characters.")]
        public string Description { get; set; }

        [Display(Name = "Tags")]
        [Required]
        [MaxLength(50, ErrorMessage = "Tags should be under 50 characters.")]
        [RegularExpression("^[a-zA-Z0-9, ]*$", ErrorMessage = "Please use only letters, numbers, and commas.")]
        public string Tags { get; set; }

        [Display(Name = "Role Access")]
        public string RoleAccess { get; set; }

        [Display(Name = "Parent")]
        public string Parent { get; set; }
    }
}
