using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using SpookVooper.Common.Managers;

namespace SpookVooper.Web.Hubs
{
    public class TransactionHub : Hub
    {
        public static IHubContext<TransactionHub> Current;
    }
}
