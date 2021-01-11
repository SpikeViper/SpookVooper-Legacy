using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SpookVooper.Web.Models.GroupViewModels
{
    public class IssueStockModel
    {
        [Display(Name = "Amount", Description = "The amount of stock you will issue.")]
        public int Amount { get; set; }
        [Display(Name = "Purchase", Description = "An amount of stock to immediately purchase. This will ONLY work if you can afford the amount, " +
            "so please factor for the change in price and taxes involved.")]
        public int Purchase { get; set; }
        public string GroupID { get; set; }
    }
}
