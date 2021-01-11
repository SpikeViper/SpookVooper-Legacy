using SpookVooper.Web.Entities;
using System.Collections.Generic;

namespace SpookVooper.Web.Models.LeaderboardViewModels
{
    public class LeaderboardIndexModel
    {
        public List<User> users;
        public int page;
        public int amount;
    }
}
