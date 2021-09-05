
using SpookVooper.Web.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpookVooper.Web.Models.UserViewModels
{
    public class UserPayModel
    {
        public User User { get; set; }
        public string Target { get; set; }
        public decimal Amount { get; set; }
    }
}