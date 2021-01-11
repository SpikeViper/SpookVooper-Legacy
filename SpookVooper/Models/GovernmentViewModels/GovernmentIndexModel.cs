using System.Collections.Generic;
using Discord.WebSocket;

namespace SpookVooper.Web.Models.GovernmentViewModels
{
    public class GovernmentIndexModel
    {
        public SocketGuild server;
        public SocketUser president;
        public SocketUser vicePresident;
        public IEnumerable<SocketUser> justices;
    }
}
