using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SpookVooper.Web.Models.GroupViewModels
{
    public class IssueIPOModel
    {
        [MaxLength(4, ErrorMessage = "Ticker should be 4 or less characters.")]
        [MinLength(2, ErrorMessage = "Ticker should be over 2 characters.")]
        [RegularExpression("^[A-Z]*$", ErrorMessage = "Please use only capital letters.")]
        [Display(Name = "Ticker", Description = "A ticker is a identification for a stock. For example, $TSLA is Tesla stock.")]
        public string Ticker { get; set; }

        [Display(Name = "Amount", Description = "The amount of stock you will issue.")]
        public int Amount { get; set; }

        [Display(Name = "Keep", Description = "The amount of stock you will keep yourself, taken from the total issued. An amount under half will put you at risk for corporate takeovers!")]
        public int Keep { get; set; }

        public string Group { get; set; }
    }
}
