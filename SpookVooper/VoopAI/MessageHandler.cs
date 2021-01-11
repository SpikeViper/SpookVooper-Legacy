using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using SpookVooper.Common.VoopAIService;
using Discord.Commands;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;
using SpookVooper.Web.Managers;
using SpookVooper.Web;
using SpookVooper.VoopAIService.Game;
using SpookVooper.Web.Extensions;

namespace SpookVooper.VoopAIService
{
    static class MessageHandler
    {

        private const string CMD_PREFIX = "/";
        public static VoopAI voopAI;
        public static IEmote yeahok = Emote.Parse("<:yeah_okay:768998766274412585>");

        public static async Task MessageReceived(SocketMessage message)
        {
            SocketUser user = message.Author;

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User userData = context.Users.FirstOrDefault(u => u.discord_id == user.Id);

                if (userData != null && userData.Id == "u-be323eec-014a-420c-afd5-cddfe308b8c4")
                {
                    message.AddReactionAsync(yeahok);
                }

                if (!(message.Channel is SocketDMChannel))
                {
                    if (userData != null)
                    {
                        userData.discord_message_count += 1;

                        if (!message.Author.IsBot)
                        {
                            // In this case it's been a minute since the last message
                            if (DateTime.Now.Minute != userData.discord_last_message_minute)
                            {
                                // Give message XP
                                userData.discord_message_xp++;
                                userData.discord_last_message_minute = DateTime.Now.Minute;
                                userData.Discord_Last_Message_Time = DateTime.UtcNow;
                            }
                        }

                        await context.SaveChangesAsync();
                    }
                }
            }



            if (message.Channel.Id == VoopAI.botChannel.Id)
            {
                //VoopAI.logger.LogInformation("Recieved message in Bot channel.");
                await OnMessageBotChannel(message);
            }
            else if (message.Channel.GetType() == typeof(SocketDMChannel))
            {
                //VoopAI.logger.LogInformation("Recieved message in DM.");
                await OnMessageDM(message);
            }

            await OnMessageChannel(message);

            await ChatFilter.FilterMessage(message);

        }

        /// <summary>
        /// Called only in DMs
        /// </summary>
        public static async Task OnMessageDM(SocketMessage message)
        {
            if (message.Content.StartsWith(CMD_PREFIX))
            {
                string[] args = message.Content.Substring(CMD_PREFIX.Length).Split(' ');

                await CommandDM(message, args);
            }
        }

        public static async Task CommandDM(SocketMessage message, string[] args)
        {
            string rootCmd = args[0].ToLower();

            //VoopAI.logger.LogInformation(rootCmd);

            if (rootCmd == "connectsite")
            {
                if (args.Length < 2)
                {
                    await message.Channel.SendMessageAsync("Please include your key!");
                    return;
                }

                int key = -1;

                bool success = int.TryParse(args[1], out key);

                if (success)
                {
                    using (VooperContext context = new VooperContext(VoopAI.DBOptions))
                    {
                        var user = context.Users.FirstOrDefault(u => u.Id == VoopAI.service._connectionHandler.GetUserFromKey(key));
                        VoopAI.logger.LogInformation("0");

                        if (user != null)
                        {
                            user.discord_id = message.Author.Id;

                            await context.SaveChangesAsync();
                            VoopAI.service._connectionHandler.RemoveKey(key);

                            await message.Author.SendMessageAsync($"Successfully linked to {user.UserName}!");

                        }
                        else
                        {
                            await message.Author.SendMessageAsync("Failed to verify this key.");
                            return;
                        }
                    }
                }
                else
                {
                    await message.Author.SendMessageAsync($"Unable to read your key.");
                }
            }
        }

        /// <summary>
        /// Called only on bot channel
        /// </summary>
        public static async Task OnMessageBotChannel(SocketMessage message)
        {
            if (message.Content.StartsWith(CMD_PREFIX))
            {
                string[] args = message.Content.Substring(CMD_PREFIX.Length).Split(' ');

                await CommandBotChannel(message, args);
            }
        }

        /// <summary>
        /// Called for every channel
        /// </summary>
        public static async Task OnMessageChannel(SocketMessage message)
        {
            if (message.Content.StartsWith(CMD_PREFIX))
            {
                string[] args = message.Content.Substring(CMD_PREFIX.Length).Split(' ');

                await CommandChannel(message, args);
            }
        }

        /// <summary>
        /// Called for twitch channel
        /// </summary>
        public static async Task OnMessageTwitchChannel(SocketMessage message)
        {
            if (message.Content.StartsWith(CMD_PREFIX))
            {
                string[] args = message.Content.Substring(CMD_PREFIX.Length).Split(' ');

                string rootCmd = args[0].ToLower();

                if (rootCmd == "confirm")
                {
                    await ConfirmTwitch(message, args);
                }
            }
        }

        public static async Task ConfirmTwitch(SocketMessage message, string[] args)
        {

            List<int> keys = VoopAI.confirmations.Keys.ToList();

            foreach (int key in keys)
            {
                int code = -1;

                if (args.Length < 2)
                {
                    await MessageHandler.SendMessage((SocketTextChannel)message.Channel, "Please include your code!");
                    return;
                }

                int.TryParse(args[1], out code);

                if (code == key)
                {
                    string name = VoopAI.confirmations[key];

                    await MessageHandler.SendMessage((SocketTextChannel)message.Channel, "Connected to Twitch account " + name);
                    //cnew UserData(message.Author.Id).twitch_username = name;
                    await MessageHandler.SendMessage((SocketTextChannel)message.Channel, "Tell spike to fix this, he was lazy");
                    return;
                }

            }

            await MessageHandler.SendMessage((SocketTextChannel)message.Channel, "Failed to connect.");
        }

        public static async Task CommandBotChannel(SocketMessage message, string[] args)
        {
            string rootCmd = args[0].ToLower();

            if (rootCmd == "xp")
            {
                await CmdXP(message);
            }

            else if (rootCmd == "leaderboard")
            {
                await CmdLeaderboard();
            }

            else if (rootCmd == "startgame")
            {
                await CmdGame(message);
            }

            else if (rootCmd == "joingame")
            {
                await CmdJoinGame(message);
            }

        }

        public static async Task CommandChannel(SocketMessage message, string[] args)
        {
            string rootCmd = args[0].ToLower();

            if (rootCmd == "roll")
            {
                await CmdRoll(message, args);
            }

            else if (rootCmd == "summon")
            {
                await SendMessage((SocketTextChannel)message.Channel, "Hello there");
            }

            else if (rootCmd == "save")
            {
                await CmdSave(message);
            }

            else if (rootCmd == "messages")
            {
                await CmdMessages(message, args);
            }

            else if (rootCmd == "balance" || rootCmd == "bal")
            {
                await CmdBalance(message);
            }

            else if (rootCmd == "ban")
            {
                await CmdBan(message);
            }

            else if (rootCmd == "migrate")
            {
                await Migrate(message);
            }

            else if (rootCmd == "eco")
            {
                await Eco(message);
            }

            else if (rootCmd == "pay")
            {
                await Pay(message);
            }

            else if (rootCmd == "stock")
            {
                await StockInfo(message, args);
            }

            else if (rootCmd == "fine")
            {
                await Fine(message, args);
            }

            else if (rootCmd == "clear")
            {
                await Clear(message, args);
            }

            else if (rootCmd == "roulette")
            {
                await CmdRoulette(message, args);
            }

            else if (rootCmd == "roulette")
            {
                await CmdRoulette(message, args);
            }

            else if (rootCmd == "svid")
            {
                await CmdSVID(message, args);
            }

            else if (rootCmd == "pfp")
            {
                await CmdPfp(message, args);
            }
        }

        public static async Task CmdPfp(SocketMessage msg, string[] args)
        {
            if (msg.MentionedUsers.Count == 0)
            {
                await msg.Channel.SendMessageAsync(msg.Author.GetAvatarUrl(size: 512));
            }
            else
            {
                string output = "";

                foreach(var user in msg.MentionedUsers)
                {
                    output += user.GetAvatarUrl(size: 512) + "\n";
                }

                await msg.Channel.SendMessageAsync(output);
            }
        }

        public static async Task Clear(SocketMessage msg, string[] args)
        {
            if (!((IGuildUser)msg.Author).GetPermissions((IGuildChannel)msg.Channel).ManageRoles)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "You are not an administrator.");
                return;
            }

            if (args.Length < 2)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "Specify an amount!");
                return;
            }

            int amount = 0;

            bool success = int.TryParse(args[1], out amount);

            if (!success) await SendMessage((SocketTextChannel)msg.Channel, "Specify a valid amount!");

            SocketTextChannel chan = msg.Channel as SocketTextChannel;

            if (chan != null)
            {
                var messages = chan.GetMessagesAsync(Math.Min(100, amount));


                foreach (var mess in messages.ToEnumerable())
                {
                    await chan.DeleteMessagesAsync(mess);
                }
            }
        }

        public static async Task Fine(SocketMessage msg, string[] args)
        {
            var dcontext = new SocketCommandContext(VoopAI.discordClient, (SocketUserMessage)msg);

            if (dcontext.Guild.Id != VoopAI.server.Id)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "This is not SpookVooper!");
                return;
            }

            if (!((IGuildUser)msg.Author).GetPermissions(VoopAI.botChannel).ManageRoles)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "You are not an administrator.");
                return;
            }

            if (args.Length < 2)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "Specify an amount!");
                return;
            }

            bool max = false;
            decimal amount = 0;

            if (args[1] == "max")
            {
                max = true;
            }
            else
            {
                bool success = decimal.TryParse(args[1], out amount);

                if (!success)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Failed to understand amount. Use /fine (amount) <user(s)>");
                    return;
                }

                amount = Math.Round(amount, 2);
            }

            if (msg.MentionedUsers.Count > 0)
            {
                foreach (var user in msg.MentionedUsers)
                {
                    using (VooperContext context = new VooperContext(VoopAI.DBOptions))
                    {
                        User webUser = context.Users.FirstOrDefault(u => u.discord_id == user.Id);

                        decimal fine = amount;
                        if (max)
                        {
                            fine = webUser.Credits;
                        }

                        await new TransactionRequest(webUser.Id, EconomyManager.VooperiaID, fine, "Government Fine", ApplicableTax.None, true).Execute();
                        await SendMessage((SocketTextChannel)msg.Channel, $"Fined {user.Username} {fine.Round()}.");
                    }
                }
            }
            else
            {
                if (args.Length < 3)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Specify users!");
                    return;
                }
                else
                {
                    string name = args[2];

                    if (args.Length > 3)
                    {
                        for (int i = 3; i < args.Length; i++)
                        {
                            name += $" {args[i]}";
                        }
                    }

                    using (VooperContext context = new VooperContext(VoopAI.DBOptions))
                    {
                        User user = context.Users.FirstOrDefault(u => u.UserName.ToLower() == name.ToLower());

                        if (user == null)
                        {
                            await SendMessage((SocketTextChannel)msg.Channel, $"Could not find {name}!");
                            return;
                        }
                        else
                        {
                            decimal fine = amount;
                            if (max)
                            {
                                fine = user.Credits;
                            }

                            await new TransactionRequest(user.Id, "GROUPACCOUNT-Vooperia", fine, "Government Fine", ApplicableTax.None, true).Execute();
                            await SendMessage((SocketTextChannel)msg.Channel, $"Fined {user.UserName} {fine.Round()}.");
                        }
                    }
                }
            }
        }

        public static async Task StockInfo(SocketMessage msg, string[] args)
        {
            await msg.Channel.SendMessageAsync($"Not implemented yet!");

            /*
            using (StockContext stockContext = new StockContext(VoopAI.DBOptions))
            using (GroupContext groupContext = new GroupContext(VoopAI.DBOptions))
            {
                if (args.Length < 2)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Please specify a valid stock!");
                    return;
                }

                string ticker = args[1].ToUpper();

                Stock stock = stockContext.Stocks.Find(ticker);

                if (stock == null)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Could not find that ticker!");
                    return;
                }

                Group group = await stock.GetGroup(groupContext);

                EmbedBuilder embed = new EmbedBuilder()
                {
                    Color = new Color(0, 100, 255),
                    Title = $"${stock.Ticker} Info :chart_with_upwards_trend:"
                }
                .WithAuthor(VoopAI.discordClient.CurrentUser)
                .WithCurrentTimestamp()
                .WithUrl($"https://spookvooper.com/Exchange/Trade/{ticker}");

                embed.AddField("Price", Math.Round((await stock.GetValue(groupContext)), 2));
                embed.AddField("Company", group.Name);
                embed.AddField("SVID", group.Id);
                embed.AddField("Company Value", Math.Round(group.Credits, 2));

                await msg.Channel.SendMessageAsync($"Current data:", false, embed.Build());
            }
            */
        }

        public static async Task CmdSVID(SocketMessage msg, string[] args)
        {
            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User target = null;

                if (msg.MentionedUsers.Count == 0)
                {
                    target = context.Users.FirstOrDefault(u => u.discord_id == msg.Author.Id);

                    if (target == null)
                    {
                        await msg.Channel.SendMessageAsync("You do not have a web account!");
                        return;
                    }
                }
                else
                {
                    target = context.Users.FirstOrDefault(u => u.discord_id == msg.MentionedUsers.First().Id);

                    if (target == null)
                    {
                        await msg.Channel.SendMessageAsync("They do not have a web account!");
                        return;
                    }
                }

                await msg.Channel.SendMessageAsync($"Their SVID is {target.Id}");
            }
        }

        public static async Task Pay(SocketMessage msg)
        {
            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                Entity from = context.Users.FirstOrDefault(u => u.discord_id == msg.Author.Id);

                if (from == null)
                {
                    await msg.Channel.SendMessageAsync("Sender does not have a web account!");
                    return;
                }

                Entity to = null;

                if (msg.MentionedUsers.Count > 0)
                {
                    SocketUser starget = msg.MentionedUsers.First();
                    to = context.Users.FirstOrDefault(u => u.discord_id == starget.Id);
                }

                string[] split = msg.Content.Split(' ');

                if (split.Length < 2)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Please specify a valid amount!");
                    return;
                }

                if (split.Length > 2 && to == null)
                {
                    string group = split[2];

                    if (split.Length > 3)
                    {
                        for (int i = 3; i < split.Length; i++)
                        {
                            group += $" {split[i]}";
                        }
                    }

                    to = context.Groups.FirstOrDefault(g => g.Name.ToLower() == group.ToLower());

                    if (to == null)
                    {
                        to = await Entity.FindAsync(split[2]);
                    }
                }

                if (to == null)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Could not find the user or group!");
                    return;
                }

                decimal amount = 0m;

                decimal.TryParse(split[1], out amount);

                if (amount == 0m)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Please specify a valid amount!");
                    return;
                }

                TaskResult result = await new TransactionRequest(from.Id, to.Id, amount, "Discord Direct Payment", ApplicableTax.None, false).Execute();
                string response = result.Info;

                await msg.Channel.SendMessageAsync(response);
            }
        }

        public static async Task Eco(SocketMessage msg)
        {
            string[] split = msg.Content.Split(' ');

            if (split.Length < 2)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "Please specify a subcommand!");
                return;
            }

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                if (split[1].ToLower() == "cap")
                {

                    decimal users = await context.Users.AsQueryable().Select(x => x.Credits).Where(x => x > 0.0m).SumAsync();

                    decimal groups = await context.Groups.AsQueryable().Where(x => x.Id != EconomyManager.VooperiaID).Select(x => x.Credits).Where(x => x > 0.0m).SumAsync();

                    decimal gov = (await context.Groups.FindAsync(EconomyManager.VooperiaID)).Credits;

                    EmbedBuilder embed = new EmbedBuilder()
                    {
                        Color = new Color(0, 100, 255),
                        Title = $":moneybag: Eco Cap"
                    }
                    .WithAuthor(VoopAI.discordClient.CurrentUser)
                    .WithCurrentTimestamp();

                    var total = users + groups + gov;

                    var userP = Math.Round((users / total) * 100);
                    var groupP = Math.Round((groups / total) * 100);
                    var govP = Math.Round((gov / total) * 100);

                    embed.AddField("Users", $"¢{Math.Round(users)} ({userP}%)");
                    embed.AddField("Groups", $"¢{Math.Round(groups)} ({groupP}%)");
                    embed.AddField("Vooperia", $"¢{Math.Round(gov)} ({govP}%)");
                    embed.AddField("Total", $"¢{Math.Round(total)}");

                    await msg.Channel.SendMessageAsync(embed: embed.Build());
                    return;
                }


                if (split[1].ToLower() == "volume")
                {

                    decimal bal = await context.Transactions.AsQueryable().Select(x => x.Credits).SumAsync();

                    await SendMessage((SocketTextChannel)msg.Channel, $"The Vooperian trade volume is ¢{Math.Round(bal, 2)}");
                    return;

                }
            }
        }

        public class TaxInfo
        {
            public string Corporate { get; set; }
            public string Payroll { get; set; }
            public string Sales { get; set; }
            public string CapitalGains { get; set; }
        }

        public static async Task Migrate(SocketMessage message)
        {
            await message.Channel.SendMessageAsync("The migration period is over. Sorry!");
        }

        public static async Task CmdLeaderboard()
        {

            if (VoopAI.leaderboard.Count < 15)
            {
                await SendMessage(VoopAI.botChannel, "Please wait until users are cached.");
                return;
            }

            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = new Color(0, 100, 255),
                Title = "Leaderboard"
            };

            for (int i = 0; i < 15; i++)
            {
                User user = VoopAI.leaderboard[i];

                if (user.UserName != user.Email)
                {
                    embed.AddField(user.UserName, user.GetTotalXP() + " XP");
                }
            }

            await VoopAI.botChannel.SendMessageAsync("", false, embed.Build());
        }

        public static async Task CmdXP(SocketMessage msg)
        {
            SocketUser target;
            
            if (msg.MentionedUsers.Count < 1) target = msg.Author;
            else target = msg.MentionedUsers.First();

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User data = context.Users.FirstOrDefault(u => u.discord_id == target.Id);

                if (data == null)
                {
                    await msg.Channel.SendMessageAsync("This user does not have a web account!");
                    return;
                }

                if (data.UserName == data.Email)
                {
                    return;
                }

                EmbedBuilder embed = new EmbedBuilder()
                {
                    Color = new Color(0, 100, 255),
                    Title = ":commend:"
                };

                embed.AddField("Total XP", data.GetTotalXP());
                embed.AddField("Commends Awarded", data.discord_commends);
                embed.AddField("Messages Sent", data.discord_message_count);
                embed.AddField("Game XP", data.discord_game_xp);

                await msg.Channel.SendMessageAsync("", false, embed.Build());
            }
        }

        public static async Task CmdBalance(SocketMessage msg)
        {
            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User userData;

                if (msg.MentionedUsers.Count < 1)
                {
                    userData = context.Users.FirstOrDefault(u => u.discord_id == msg.Author.Id);

                    if (userData != null)
                    {
                        await SendMessage((SocketTextChannel)msg.Channel, msg.Author.Username + " Balance: ¢" + userData.Credits.Round());
                    }
                    else
                    {
                        await SendMessage((SocketTextChannel)msg.Channel, "Please make a web account to use credit functions.");
                    }

                    return;
                }

                SocketUser target = msg.MentionedUsers.First();

                userData = context.Users.FirstOrDefault(u => u.discord_id == target.Id);

                if (userData != null)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, target.Username + " Balance: ¢" + userData.Credits.Round());
                }
                else
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Please make a web account to use credit functions.");
                }
            }
        }

        public static async Task CmdGame(SocketMessage msg)
        {
            if (msg.Channel.Id == VoopAI.botChannel.Id)
            {
                if (!RPG_Game.game_running && !RPG_Game.game_queuing)
                {
                    await VoopAI.gameChannel.SendMessageAsync("A new game is beginning! You have five minutes to use /joingame to be allowed into this session!");
                    await VoopAI.botChannel.SendMessageAsync("A new game will begin in five minutes! Use /joingame to be a part of it!");

                    new Task(() => RPG_Game.StartGame()).Start();
                }
                else
                {
                    await VoopAI.botChannel.SendMessageAsync("A game has already started or is starting!");
                }
            }
        }

        public static async Task CmdJoinGame(SocketMessage msg)
        {
            if (msg.Channel.Id == VoopAI.botChannel.Id)
            {
                if (!RPG_Game.game_running)
                {
                    if (RPG_Game.game_queuing)
                    {
                        if (RPG_Game.currentPlayers.Contains(msg.Author))
                        {
                            await VoopAI.botChannel.SendMessageAsync($"You have already joined the game, {msg.Author.Username}!");
                            return;
                        }

                        await VoopAI.gameChannel.SendMessageAsync($"{msg.Author.Username} has joined the game!");
                        await VoopAI.botChannel.SendMessageAsync($"{msg.Author.Username} has joined!");

                        RPG_Game.currentPlayers.Add(msg.Author);

                    }
                    else
                    {
                        await VoopAI.botChannel.SendMessageAsync("There is no current game to join!");
                    }
                }
                else
                {
                    await VoopAI.botChannel.SendMessageAsync("A game has already started!");
                }
            }
        }

        public static async Task SendMessage(SocketTextChannel channel, string message)
        {
            await channel.SendMessageAsync(message);
        }

        public static async Task CmdSave(SocketMessage msg)
        {
            if (((IGuildUser)msg.Author).GetPermissions((IGuildChannel)msg.Channel).ManageRoles)
            {
                // await VoopAI.SaveUsers();
                // await SendMessage((SocketTextChannel)msg.Channel, "Saved user data for " + VoopAI.userDataStore.Keys.Count + " users.");
                await SendMessage((SocketTextChannel)msg.Channel, "No need to do that, we are on a database now!");
            }
        }

        public static async Task CmdMessages(SocketMessage msg, string[] args)
        {
            if (msg.MentionedUsers.Count < 1)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "Please specify a name.");
                return;
            }

            SocketUser target = msg.MentionedUsers.First();

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User userData = context.Users.FirstOrDefault(u => u.discord_id == target.Id);

                if (userData != null)
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "The user " + target.Username + " has sent " + userData.discord_message_count + " messages on record.");
                }
                else
                {
                    await SendMessage((SocketTextChannel)msg.Channel, "Please make a web account for message tracking.");
                }
            }
        }

        public static async Task CmdRoll(SocketMessage msg, string[] args)
        {
            int max = 20;

            if (args.Length > 1)
            {
                bool success = int.TryParse(args[1], out max);

                if (!success)
                {
                    await msg.Channel.SendMessageAsync("Please enter a number between 0 and 2,147,483,647.");
                    return;
                }
            }

            if (max > int.MaxValue || max < 0)
            {
                await msg.Channel.SendMessageAsync("Please fit your roll within the positive range of a 32-bit integer.");
                return;
            }

            string rollValue = String.Format(":game_die: {0}", VoopAI.random.Next(max + 1));

            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = msg.Author.Username,
                Color = new Color(0, 100, 255),
            };

            embed.AddField("Roll", rollValue);

            await msg.Channel.SendMessageAsync("", false, embed.Build());
        }

        public static string[] macQuotes = new string[] { "If this virus ain't done this week I'm gonna have to interverne",
                                                          "Obama penis",
                                                          "lmao i just took off my pants just now; before the fetus thing",
                                                          "what about licking?", "" +
                                                          "You are literally a warcrime",
                                                          "After several complaints I am stepping down as god",
                                                          "I have committed s*x",
                                                          "you are not epic",
                                                          "I literally hate women",
                                                          "*I lean down and kiss you while you are playing roblox*",
                                                          "Silence woman you are irrelevant",
                                                          "The beatings will continue until morale improves", };

        public static async Task CmdRoulette(SocketMessage msg, string[] args)
        {
            int rand = VoopAI.random.Next(0, 38) - 1;

            string numberText = "00";

            string color = "Red :red_square:";

            int macID = VoopAI.random.Next(0, macQuotes.Length);

            string mac = macQuotes[macID];

            if (rand >= 0)
            {
                numberText = rand.ToString();
            }

            if (rand % 2 == 0)
            {
                color = "Black :black_large_square:";
            }

            if (numberText == "00" || numberText == "0")
            {
                color = "Green :green_square:";
            }

            string rollValue = String.Format($":game_die: Roulette");

            EmbedBuilder embed = new EmbedBuilder()
            {
                Title = msg.Author.Username,
                Color = new Color(0, 100, 255),
            };

            embed.AddField("Color", color);
            embed.AddField("Number", numberText);
            embed.AddField("Mac Quote", $"[{macID}] {mac}");

            await msg.Channel.SendMessageAsync("", false, embed.Build());
        }

        public static async Task CmdBan(SocketMessage msg)
        {
            if (!((IGuildUser)msg.Author).GetPermissions((IGuildChannel)msg.Channel).ManageRoles)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "You are not an administrator.");
                return;
            }

            if (msg.MentionedUsers.Count < 1)
            {
                await SendMessage((SocketTextChannel)msg.Channel, "Please specify a user!");
                return;
            }

            SocketUser target = msg.MentionedUsers.First();

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User userData = context.Users.FirstOrDefault(u => u.discord_id == target.Id);

                if (userData != null)
                {
                    userData.discord_ban_count += 1;
                    await context.SaveChangesAsync();
                }
            }

            SocketGuildChannel channel = msg.Channel as SocketGuildChannel;

            if (channel != null)
            {
                await channel.Guild.AddBanAsync(target);
            }
        }


    }


}
