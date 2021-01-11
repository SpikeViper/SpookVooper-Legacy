using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SpookVooper.Web.Models.ExchangeViewModels
{
    public class CreateStockModel
    {
        [Required]
        [MaxLength(4, ErrorMessage = "Ticker should be 4 or less characters.")]
        [MinLength(1, ErrorMessage = "Ticker should be at least 1 character.")]
        [RegularExpression("^[A-Z]*$", ErrorMessage = "Please use only capital letters.")]
        [Display(Name = "Ticker", Description = "A ticker is an identification for a stock. For example, $TSLA is Tesla stock.")]
        public string Ticker { get; set; }

        [Display(Name = "Issuance Amount", Description = "The amount of stock being issued, in total. This must be greater than " +
            "the company kept amount.")]
        [Required]
        [Range(1000, 1000000)]
        public int Amount { get; set; }

        [Display(Name = "Keep", Description = "The amount of stock the company will keep. This is how stock rising raises company value - it holds its own stock, " +
            "which it can hold or sell. This should be non-zero.")]
        [Required]
        public int Keep { get; set; }

        [Display(Name = "Group SVID", Description = "SVID for group to issue this stock to. Please double check this is correct.")]
        [Required]
        public string Group_Id { get; set; }

        [Display(Name = "Initial Valuation", Description = "The initial value of the stock. Please be realistic with this - " +
            "otherwise a stock may be stuck with zero activity or interest. Remember this relates to the amount, so if you " +
            "issue 10,000 stock at ¢100, you have valued the company at ¢1,000,000")]
        [Required]
        [Range(1.0, 1000)]
        public int Initial_Value { get; set; }
    }
}
