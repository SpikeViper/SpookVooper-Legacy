using SpookVooper.Web.Entities;
using SpookVooper.Web.Entities.Groups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpookVooper.Web.Models.GroupViewModels
{
    public class TransferGroupModel
    {
        public User User { get; set; }
        public Group Group { get; set; }
    }
}
