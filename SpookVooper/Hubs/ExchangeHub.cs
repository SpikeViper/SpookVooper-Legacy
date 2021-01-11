using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SpookVooper.VoopAIService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;

namespace SpookVooper.Web.Hubs
{
    public class ExchangeHub : Hub
    {
        public static IHubContext<ExchangeHub> Current;

        public static Dictionary<string, string> lastMessages = new Dictionary<string, string>();
        public static Dictionary<string, DateTime> lastMessageTimes = new Dictionary<string, DateTime>();
        public static Dictionary<string, int> flagCount = new Dictionary<string, int>();

        public List<string> blocked = new List<string>();

        public static List<string> history = new List<string>();
        public static List<string> modehistory = new List<string>();

        public async Task RequestHistory()
        {
            await Clients.Caller.SendAsync("RecieveMessageHistory", history.TakeLast(15), modehistory.TakeLast(15));
        }

        public async Task SendMessage(string svid, string auth, string message, string ticker, string mode)
        {
            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                Entity entity = await Entity.FindAsync(svid);

                User authUser = await context.Users.AsQueryable().FirstOrDefaultAsync(u => u.Api_Key == auth);

                if (authUser == null || authUser.discord_id == null)
                {
                    return;
                }
                
                if (await entity.HasPermissionAsync(authUser, "eco"))
                {
                    message = message.Replace('<', '(');
                    message = message.Replace('>', ')');

                    if (blocked.Contains(svid))
                    {
                        return;
                    }

                    if (authUser.HasDiscordRole("Moderators"))
                    {
                        if (message.StartsWith('/'))
                        {
                            var split = message.Split(' ');

                            if (split[0] == "/mute")
                            {
                                if (split.Length > 2)
                                {
                                    string name = message.Substring(5, message.Length);

                                    Entity user = await context.Users.AsQueryable().FirstOrDefaultAsync(u => u.UserName.ToLower() == name);
                                    if (user == null) await context.Groups.AsQueryable().FirstOrDefaultAsync(u => u.Name.ToLower() == name);

                                    string blockid = user.Id;

                                    blocked.Add(blockid);
                                }
                            }
                        }
                    }

                    // Prevent large messages
                    if (message.Length > 200)
                    {
                        message = message.Substring(0, 199);
                    }

                    // Prevent same message multiple times
                    // And by speed
                    if (!lastMessages.ContainsKey(svid))
                    {
                        flagCount.Add(svid, 0);
                        lastMessageTimes.Add(svid, DateTime.UtcNow);
                        lastMessages.Add(svid, message);
                    }
                    else
                    {
                        if (DateTime.UtcNow.Subtract(lastMessageTimes[svid]).TotalSeconds < 1)
                        {
                            flagCount[svid] += 1;

                            if (flagCount[svid] > 10)
                            {
                                blocked.Add(svid);
                            }

                            return;
                        }
                        else
                        {
                            lastMessageTimes[svid] = DateTime.UtcNow;
                        }


                        if (lastMessages[svid] == message)
                        {
                            flagCount[svid] += 1;

                            if (flagCount[svid] > 10)
                            {
                                blocked.Add(svid);
                            }

                            return;
                        }
                        else
                        {
                            lastMessages[svid] = message;
                        }
                    }

                    string formatted = $"({ticker}) {entity.Name}: {message}";

                    history.Add(formatted);
                    modehistory.Add(mode);

                    await Clients.All.SendAsync("RecieveMessage", formatted, mode);
                }
            }
        }
    }
}
