using System.Collections.Generic;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Models.GovernmentViewModels
{
    public class GovernmentIndexModel
    {
        public User president;
        public User vicePresident;
        public IEnumerable<User> justices;
    }
}
