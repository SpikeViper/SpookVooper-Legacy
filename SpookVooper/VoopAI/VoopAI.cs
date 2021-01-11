#region using
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using SpookVooper.VoopAIService.Game;
using Discord.Rest;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SpookVooper.Web.Economy.Stocks;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;
using SpookVooper.Web.Government.Voting;
using SpookVooper.Web.Government;
using SpookVooper.Web.Entities.Groups;
using SpookVooper.Web.Managers;
using SpookVooper.Web;
using SpookVooper.Web.Oauth2;
using SpookVooper.Web.Forums;
using SpookVooper.Web.News;

#endregion

namespace SpookVooper.VoopAIService
{
    public class VoopAI
    {

        // IDs for server and interaction channel
        const ulong serverID = 317462818490286081;
        const ulong govserverID = 639209247699238922;
        const ulong botChannelID = 318383837052665856;
        const ulong twitchChannelID = 601520387565813762;
        const ulong gameChannelID = 620641429257060372;
        const ulong forumChannelID = 658185767537082368;
        const ulong ecoChannelID = 690697994185474128;
        const ulong stockChannelID = 691363418707591240;
        const ulong newsChannelID = 651476179798327316;

        public static DbContextOptions DBOptions;
        public static DbContextOptions NCDBOptions;

        /// <summary>
        /// Version number
        /// </summary>
        public string version = "V2.0";

        /// <summary>
        /// The randomness of this bot
        /// </summary>
        public static Random random = new Random();

        /// <summary>
        /// User database context
        /// </summary>
        //public static UserContext DB;

        /// <summary>
        /// The client instance for the discord bot
        /// </summary>
        public static DiscordSocketClient discordClient;

        /// <summary>
        /// Client instance for the twitch bot
        /// </summary>
        public static TwitchClient twitchClient;

        /// <summary>
        /// This will stop the bot if set to true
        /// </summary>
        public bool stop = false;

        /// <summary>
        /// Channel for bot interaction
        /// </summary>
        public static SocketTextChannel botChannel;

        /// <summary>
        /// Channel for rpg
        /// </summary>
        public static SocketTextChannel gameChannel;

        public static SocketTextChannel forumChannel;

        public static SocketTextChannel ecoChannel;

        public static SocketTextChannel stockChannel;

        public static SocketTextChannel newsChannel;

        public static SocketTextChannel logChannel;
        public static ulong logChannelID = 699705386370072676;

        public static SocketTextChannel migrateChannel;
        public static ulong migrateChannelID = 724405602796961804;

        /// <summary>
        /// The SpookVooper server
        /// </summary>
        public static SocketGuild server;
        public SocketGuild serverInstance;

        /// <summary>
        /// The Vooperian Government server
        /// </summary>
        public static SocketGuild govServer;

        /// <summary>
        /// Logger for task
        /// </summary>
        public static ILogger<VoopAIWorker> logger;

        /// <summary>
        /// Used for twitch channel confirmations
        /// </summary>
        public static Dictionary<int, string> confirmations = new Dictionary<int, string>();

        /// <summary>
        /// Used for caching exchange list
        /// </summary>
        public static List<StockObject> exchangeList = new List<StockObject>();
        public static List<StockObject> exchangeListCompanyValue = new List<StockObject>();
        public static List<StockObject> exchangeListPrice = new List<StockObject>();

        /// <summary>
        /// Used for easy leaderboard creation
        /// </summary>
        public static List<User> leaderboard = new List<User>();

        /// <summary>
        /// A cheat method to skip a ton of DB lookups
        /// </summary>
        public static HashSet<ulong> usersInDB = new HashSet<ulong>();

        /// <summary>
        /// Dictionary for district roles
        /// </summary>
        public static Dictionary<string, SocketRole> districtRoles = new Dictionary<string, SocketRole>();

        /// <summary>
        /// Last hour checked
        /// </summary>
        public static int lastHour;

        public static VoopAIWorker service;

        public static bool isDev = false;

        public static bool migrating = false;

        /// <summary>
        /// Main method for the bot
        /// </summary>
        public async Task MainAsync()
        {
            //DbContextOptionsBuilder builder = new DbContextOptionsBuilder();

            //builder.UseMySql(Secrets.DBstring, options =>
            //{
            //    options.EnableRetryOnFailure().CharSet(CharSet.Utf8Mb4).ServerVersion(new Version(8, 0, 20), ServerType.MySql);
            //});

            // Link message handler
            MessageHandler.voopAI = this;

            Console.WriteLine("Initializing Discord Hook");
            service._logger.LogInformation("Initializing Discord Hook");

            // Initialize bot
            await InitializeDiscord();

            await Migrate();

            Console.WriteLine("Initializing Twitch Hook");
            service._logger.LogInformation("Initializing Twitch Hook");
            // Initialize twitch bot
            InitializeTwitch();

            // await SaveUserProperty(338248768346652672, "message_count", 33);

            // Updates ranks
            System.Timers.Timer hourTimer = new System.Timers.Timer();

            hourTimer.Interval = 1000 * 60 * 60;
            hourTimer.AutoReset = true;
            hourTimer.Elapsed += ((sender, e) => OnHourPass());

            hourTimer.Start();

            OnHourPass();

            System.Timers.Timer minTimer = new System.Timers.Timer();

            minTimer.Interval = 1000 * 60;
            minTimer.AutoReset = true;
            minTimer.Elapsed += ((sender, e) => OnMinutePass());

            minTimer.Start();

            OnMinutePass();

            Console.WriteLine("Rank Timer Begun");

            RPG_Game.gameChannel = (SocketTextChannel)server.GetTextChannel(gameChannelID);

            // Loop to look for input
            while (!stop)
            {
                string input = Console.ReadLine();

                if (!String.IsNullOrEmpty(input))
                {
                    // Send input as message
                    await botChannel.SendMessageAsync(input);
                }
            }
        }

        public void OnMinutePass()
        {
            new Task(() =>
            {
                PollElections();
            }).Start();
        }

        public void PollElections()
        {
            using (VooperContext context = new VooperContext(DBOptions))
            {
                foreach (Election election in context.Elections.AsQueryable().Where(x => x.Active))
                {
                    if (DateTime.UtcNow > election.End_Date)
                    {
                        // End the election
                        if (election.Type.ToLower() == "senate")
                        {
                            var results = election.GetResults().Result;

                            User winner = results[0].Candidate;

                            election.Active = false;
                            election.Winner_Id = winner.Id;

                            context.Elections.Update(election);
                            context.SaveChanges();

                            District district = context.Districts.Find(election.District);
                            district.Senator = winner.Id;

                            Group group = context.Groups.Find(district.Group_Id);
                            group.Owner_Id = winner.Id;

                            context.Groups.Update(group);
                            context.Districts.Update(district);
                            context.SaveChanges();

                            SocketGuildUser dUser = server.GetUser((ulong)winner.discord_id);
                            if (dUser != null)
                            {
                                dUser.AddRoleAsync(server.Roles.FirstOrDefault(x => x.Name == "Senator"));
                            }

                            EmbedBuilder embed = new EmbedBuilder()
                            {
                                Color = new Color(0, 100, 255),
                                Title = $"**{winner.UserName}** wins Senate Election!"
                            }
                            .WithAuthor(dUser)
                            .WithCurrentTimestamp();

                            embed.AddField("News Outlet", "VoopAI Auto News");
                            embed.AddField("Author", "VoopAI The Bot");
                            embed.AddField("Content", $"Congratulations to {winner.UserName} on winning the {election.District} elections! They won with {results[0].Votes} votes to become the new Senator. " +
                                $"Please check other news outlets for more details!");

                            VoopAI.newsChannel.SendMessageAsync(embed: embed.Build());
                        }
                    }
                }
            }
        }

        public void OnHourPass()
        {
            new Task(() =>
            {
                UpdateRanks();
            }).Start();
            new Task(() =>
            {
                DoSalary();
            }).Start();
            new Task(() =>
            {
                UpdateGovRanks();
            }).Start();
        }

        public void UpdateGovRanks()
        {
            using (VooperContext context = new VooperContext(DBOptions))
            {
                var senateRole = server.Roles.FirstOrDefault(x => x.Name == "Senator");

                // Remove old senators
                foreach (var duser in server.Users.Where(x => x.Roles.Any(x => x.Id == senateRole.Id)))
                {
                    User user = context.Users.FirstOrDefault(x => x.discord_id == duser.Id);

                    if (!context.Districts.AsQueryable().Any(x => x.Senator == user.Id))
                    {
                        duser.RemoveRoleAsync(senateRole);
                    }
                }

                // Add new senators
                foreach (User user in District.GetAllSenatorsAsync(context).Result)
                {
                    var duser = server.Users.FirstOrDefault(x => x.Id == user.discord_id);

                    if (duser != null && !duser.Roles.Contains(senateRole))
                    {
                        duser.AddRoleAsync(senateRole);
                    }
                }
            }

            Console.WriteLine("Updated gov ranks!");
        }

        public async Task Migrate()
        {
            using (NerdcraftContext nc = new NerdcraftContext(NCDBOptions))
            using (VooperContext c = new VooperContext(DBOptions))
            {
                foreach (District d in c.Districts)
                {
                    if (!(await c.Groups.AsQueryable().AnyAsync(x => x.Id == d.Group_Id)))
                    {
                        Group group = new Group()
                        {
                            Description = "The district of " + d.Name,
                            District_Id = d.Name,
                            Api_Key = Guid.NewGuid().ToString(),
                            Group_Category = "District",
                            Open = true,
                            Owner_Id = d.Senator,
                            Name = d.Name,
                            Id = d.Group_Id
                        };

                        c.Groups.Add(group);
                        await c.SaveChangesAsync();
                    }
                }
            }
        }

        public async void DoSalary()
        {

            using (VooperContext context = new VooperContext(DBOptions))
            {
                Console.WriteLine("Doing salary job");

                var groupRoles = context.GroupRoles;

                foreach (var rank in context.GroupRoles.AsQueryable().Where(x => x.Salary > 0m))
                {

                    Group group = await rank.GetGroup();

                    if (group == null)
                    {
                        context.GroupRoles.Remove(rank);
                        await context.SaveChangesAsync();
                    }
                    else
                    {
                        foreach (var user in await rank.GetUsers())
                        {
                            await new TransactionRequest(group.Id, user.Id, rank.Salary, $"{group.Name} salary for {rank.Name}", ApplicableTax.Payroll, false).Execute();
                        }
                    }
                }

                Console.WriteLine("Finished salary job");
            }
        }

        public static SocketRole spleenRole;
        public static SocketRole crabRole;
        public static SocketRole gatyRole;
        public static SocketRole corgiRole;
        public static SocketRole oofRole;

        public static SocketRole patreonCitizen;
        public static SocketRole patreonSoldier;
        public static SocketRole patreonLoyalist;
        public static SocketRole patreonHero;
        public static SocketRole patreonMadlad;

        public static SocketRole youtubeCitizen;

        public async void UpdateRanks()
        {
            Console.WriteLine("Doing rank job");

            using (VooperContext context = new VooperContext(DBOptions))
            {
                Group government = context.Groups.AsQueryable().FirstOrDefault(x => x.Name == "Vooperia");

                if (government == null) Console.WriteLine("Holy fuck something is wrong.");

                leaderboard = context.Users.AsEnumerable().Where(u => u.Email != u.UserName).OrderByDescending(u => u.GetTotalXP()).ToList();

                List<SocketGuildUser> userList = new List<SocketGuildUser>();

                // Add connected users
                foreach (User userData in leaderboard)
                {
                    SocketGuildUser user = null;

                    if (userData.discord_id != null)
                    {
                        //user = server.Users.FirstOrDefault(x => x.Id == (ulong)userData.discord_id);
                        user = server.GetUser((ulong)userData.discord_id);
                    }

                    if (user != null)
                    {
                        // Clear roles if muted
                        if (userData.GetDiscordRoles().Any(r => r.Name == "Muted"))
                        {
                            if (user.Roles.Contains(spleenRole)) await user.RemoveRoleAsync(spleenRole);
                            if (user.Roles.Contains(crabRole)) await user.RemoveRoleAsync(crabRole);
                            if (user.Roles.Contains(gatyRole)) await user.RemoveRoleAsync(gatyRole);
                            if (user.Roles.Contains(corgiRole)) await user.RemoveRoleAsync(corgiRole);
                            if (user.Roles.Contains(oofRole)) await user.RemoveRoleAsync(oofRole);
                        }
                        else
                        {
                            userList.Add(user);
                        }
                    }
                }

                int counter = 0;

                int totalUsers = userList.Count;


                GovControls govControls = await GovControls.GetCurrentAsync(context);

                decimal UBITotal = govControls.UBIAccount;
                govControls.UBIAccount = 0;

                context.GovControls.Update(govControls);
                await context.SaveChangesAsync();


                int spleenUserCount = totalUsers / 100;
                int crabUserCount = (totalUsers / 20) - spleenUserCount;
                int gatyUserCount = (totalUsers / 10) - spleenUserCount - crabUserCount;
                int corgiUserCount = (totalUsers / 4) - spleenUserCount - crabUserCount - gatyUserCount;
                int oofUserCount = (totalUsers / 2) - spleenUserCount - crabUserCount - gatyUserCount - corgiUserCount;

                int unrankedCount = totalUsers - spleenUserCount - crabUserCount - gatyUserCount - corgiUserCount - oofUserCount;

                decimal spleenPay = 0.0m;
                decimal crabPay = 0.0m;
                decimal gatyPay = 0.0m;
                decimal corgiPay = 0.0m;
                decimal oofPay = 0.0m;
                decimal unrankedPay = 0.0m;

                if (spleenUserCount > 0)
                {
                    spleenPay = (UBITotal * (govControls.SpleenPayPercent / 100.0m)) / spleenUserCount;
                }
                if (crabUserCount > 0)
                {
                    crabPay = (UBITotal * (govControls.CrabPayPercent / 100.0m)) / crabUserCount;
                }
                if (gatyUserCount > 0)
                {
                    gatyPay = (UBITotal * (govControls.GatyPayPercent / 100.0m)) / gatyUserCount;
                }
                if (corgiUserCount > 0)
                {
                    corgiPay = (UBITotal * (govControls.CorgiPayPercent / 100.0m)) / corgiUserCount;
                }
                if (oofUserCount > 0)
                {
                    oofPay = (UBITotal * (govControls.OofPayPercent / 100.0m)) / oofUserCount;
                }
                if (unrankedCount > 0)
                {
                    unrankedPay = (UBITotal * (govControls.UnrankedPayPercent / 100.0m)) / unrankedCount;
                }

                foreach (SocketGuildUser discordUser in userList)
                {
                    User webUser = context.Users.FirstOrDefault(u => u.discord_id == discordUser.Id);

                    // Update pfp in storage
                    webUser.Image_Url = webUser.GetPfpUrl();
                    context.Update(webUser);
                    await context.SaveChangesAsync();

                    bool hasSpleen = discordUser.Roles.Contains(spleenRole);
                    bool hasCrab = discordUser.Roles.Contains(crabRole);
                    bool hasGaty = discordUser.Roles.Contains(gatyRole);
                    bool hasCorgi = discordUser.Roles.Contains(corgiRole);
                    bool hasOof = discordUser.Roles.Contains(oofRole);

                    bool hasCitizen = discordUser.Roles.Contains(patreonCitizen) || discordUser.Roles.Contains(youtubeCitizen);
                    bool hasSoldier = discordUser.Roles.Contains(patreonSoldier);
                    bool hasLoyalist = discordUser.Roles.Contains(patreonLoyalist);
                    bool hasHero = discordUser.Roles.Contains(patreonHero);
                    bool hasMadlad = discordUser.Roles.Contains(patreonMadlad);

                    bool patron = hasCitizen || hasSoldier || hasLoyalist || hasHero || hasMadlad;

                    // Inactivity tax
                    if (Math.Abs(webUser.Discord_Last_Message_Time.Subtract(DateTime.UtcNow).TotalDays) > 14 && !patron)
                    {
                        decimal tax = webUser.Credits * (govControls.InactivityTaxRate / 100.0M);

                        TransactionRequest req = new TransactionRequest(webUser.Id, EconomyManager.VooperiaID, tax, "Inactivity Tax", ApplicableTax.None, true);

                        TaskResult result = await req.Execute();

                        if (result.Succeeded)
                        {
                            govControls.InactivityTaxRevenue += tax;

                            // Add to UBI
                            govControls.UBIAccount += tax * (govControls.UBIBudgetPercent / 100.0M);

                            context.GovControls.Update(govControls);

                            await context.SaveChangesAsync();
                        }

                        // Remove last role
                        if (hasSpleen) await discordUser.RemoveRoleAsync(spleenRole);
                        if (hasCrab) await discordUser.RemoveRoleAsync(crabRole);
                        if (hasGaty) await discordUser.RemoveRoleAsync(gatyRole);
                        if (hasCorgi) await discordUser.RemoveRoleAsync(corgiRole);
                        if (hasOof) await discordUser.RemoveRoleAsync(oofRole);

                        continue;
                    }

                    // Set district
                    if (!String.IsNullOrWhiteSpace(webUser.district))
                    {
                        var oldDistrictRoles = discordUser.Roles.Where(x => x.Name.Contains("District") && !x.Name.Contains(webUser.district));

                        if (oldDistrictRoles.Count() > 0)
                        {
                            await discordUser.RemoveRolesAsync(oldDistrictRoles);
                        }

                        if (!discordUser.Roles.Any(x => x.Name == webUser.district + " District"))
                        {
                            await discordUser.AddRoleAsync(districtRoles[webUser.district + " District"]);
                        }
                    }

                    // Spleen rank
                    if (counter <= spleenUserCount)
                    {
                        // Add new role
                        if (!hasSpleen) await discordUser.AddRoleAsync(spleenRole);

                        // Remove last role
                        if (hasCrab) await discordUser.RemoveRoleAsync(crabRole);
                        if (hasGaty) await discordUser.RemoveRoleAsync(gatyRole);
                        if (hasCorgi) await discordUser.RemoveRoleAsync(corgiRole);
                        if (hasOof) await discordUser.RemoveRoleAsync(oofRole);

                        if (webUser != null)
                        {
                            //TransactionRequest transaction = new TransactionRequest(webUser.economy_id, government.economy_id, 238827, "UBI Mistake Fix", ApplicableTax.None, true);
                            TransactionRequest transaction = new TransactionRequest(government.Id, webUser.Id, spleenPay, "UBI Payment", ApplicableTax.None, true);
                            EconomyManager.RequestTransaction(transaction);
                        }
                    }
                    // Crab rank
                    else if (counter <= spleenUserCount + crabUserCount)
                    {
                        // Add new role
                        if (!hasCrab) await discordUser.AddRoleAsync(crabRole);

                        // Remove last role
                        if (hasSpleen) await discordUser.RemoveRoleAsync(spleenRole);
                        if (hasGaty) await discordUser.RemoveRoleAsync(gatyRole);
                        if (hasCorgi) await discordUser.RemoveRoleAsync(corgiRole);
                        if (hasOof) await discordUser.RemoveRoleAsync(oofRole);

                        if (webUser != null)
                        {
                            //TransactionRequest transaction = new TransactionRequest(webUser.economy_id, government.economy_id, 146267, "UBI Mistake Fix", ApplicableTax.None, true);
                            TransactionRequest transaction = new TransactionRequest(government.Id, webUser.Id, crabPay, "UBI Payment", ApplicableTax.None, true);
                            EconomyManager.RequestTransaction(transaction);
                        }
                    }
                    // Gaty rank
                    else if (counter <= spleenUserCount + crabUserCount + gatyUserCount)
                    {
                        // Add new role
                        if (!hasGaty) await discordUser.AddRoleAsync(gatyRole);

                        // Remove last role
                        if (hasSpleen) await discordUser.RemoveRoleAsync(spleenRole);
                        if (hasCrab) await discordUser.RemoveRoleAsync(crabRole);
                        if (hasCorgi) await discordUser.RemoveRoleAsync(corgiRole);
                        if (hasOof) await discordUser.RemoveRoleAsync(oofRole);

                        if (webUser != null)
                        {
                            //TransactionRequest transaction = new TransactionRequest(webUser.economy_id, government.economy_id, 125698, "UBI Mistake Fix", ApplicableTax.None, true);
                            TransactionRequest transaction = new TransactionRequest(government.Id, webUser.Id, gatyPay, "UBI Payment", ApplicableTax.None, true);
                            EconomyManager.RequestTransaction(transaction);
                        }
                    }
                    // Corgi rank
                    else if (counter <= spleenUserCount + crabUserCount + gatyUserCount + corgiUserCount)
                    {
                        // Add new role
                        if (!hasCorgi) await discordUser.AddRoleAsync(corgiRole);

                        // Remove last role
                        if (hasSpleen) await discordUser.RemoveRoleAsync(spleenRole);
                        if (hasCrab) await discordUser.RemoveRoleAsync(crabRole);
                        if (hasGaty) await discordUser.RemoveRoleAsync(gatyRole);
                        if (hasOof) await discordUser.RemoveRoleAsync(oofRole);

                        if (webUser != null)
                        {
                            //TransactionRequest transaction = new TransactionRequest(webUser.economy_id, government.economy_id, 110369, "UBI Mistake Fix", ApplicableTax.None, true);
                            TransactionRequest transaction = new TransactionRequest(government.Id, webUser.Id, corgiPay, "UBI Payment", ApplicableTax.None, true);
                            EconomyManager.RequestTransaction(transaction);
                        }
                    }
                    // Oof rank
                    else if (counter <= spleenUserCount + crabUserCount + gatyUserCount + corgiUserCount + oofUserCount)
                    {
                        // Add new role
                        if (!hasOof) await discordUser.AddRoleAsync(oofRole);

                        // Remove last role
                        if (hasSpleen) await discordUser.RemoveRoleAsync(spleenRole);
                        if (hasCrab) await discordUser.RemoveRoleAsync(crabRole);
                        if (hasGaty) await discordUser.RemoveRoleAsync(gatyRole);
                        if (hasCorgi) await discordUser.RemoveRoleAsync(corgiRole);

                        if (webUser != null)
                        {
                            //TransactionRequest transaction = new TransactionRequest(webUser.economy_id, government.economy_id, 91085, "UBI Mistake Fix", ApplicableTax.None, true);
                            TransactionRequest transaction = new TransactionRequest(government.Id, webUser.Id, oofPay, "UBI Payment", ApplicableTax.None, true);
                            EconomyManager.RequestTransaction(transaction);
                        }
                    }
                    // Unranked
                    else
                    {
                        // Remove last role
                        if (hasSpleen) await discordUser.RemoveRoleAsync(spleenRole);
                        if (hasCrab) await discordUser.RemoveRoleAsync(crabRole);
                        if (hasGaty) await discordUser.RemoveRoleAsync(gatyRole);
                        if (hasCorgi) await discordUser.RemoveRoleAsync(corgiRole);
                        if (hasOof) await discordUser.RemoveRoleAsync(oofRole);

                        if (webUser != null)
                        {
                            //TransactionRequest transaction = new TransactionRequest(webUser.economy_id, government.economy_id, 125698, "UBI Mistake Fix", ApplicableTax.None, true);
                            TransactionRequest transaction = new TransactionRequest(government.Id, webUser.Id, unrankedPay, "UBI Payment", ApplicableTax.None, true);
                            EconomyManager.RequestTransaction(transaction);
                        }
                    }

                    if (patron)
                    {

                        webUser = await context.Users.FindAsync(webUser.Id);

                        if (hasMadlad)
                        {
                            webUser.Credits += 500;
                        }
                        else if (hasHero)
                        {
                            webUser.Credits += 350;
                        }
                        else if (hasLoyalist)
                        {
                            webUser.Credits += 175;
                        }
                        else if (hasSoldier)
                        {
                            webUser.Credits += 60;
                        }
                        else if (hasCitizen)
                        {
                            webUser.Credits += 20;
                        }

                        context.Update(webUser);
                        await context.SaveChangesAsync();
                    }

                    counter++;
                }
            }


            Console.WriteLine("Finished rank system");

        }

        /// <summary>
        /// Used to initialize the bot on twitch
        /// </summary>
        public void InitializeTwitch()
        {
            ConnectionCredentials credentials = new ConnectionCredentials("VoopAI", Secrets.twitchOAuth);
            twitchClient = new TwitchClient();
            twitchClient.Initialize(credentials, "spikeviper");

            twitchClient.OnLog += Twitch_OnLog;
            twitchClient.OnConnected += TwitchManager.Client_OnConnected;
            twitchClient.OnMessageReceived += TwitchManager.Client_OnMessageReceived;
            twitchClient.OnNewSubscriber += TwitchManager.Client_OnNewSubscriber;
            twitchClient.OnJoinedChannel += TwitchManager.Client_OnJoinedChannel;
            twitchClient.OnWhisperReceived += TwitchManager.Client_OnWhisperReceived;

            twitchClient.Connect();
        }

        /// <summary>
        /// Logging for twitch
        /// </summary>
        private void Twitch_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
        }

        /// <summary>
        /// Used to initialize the discord bot
        /// </summary>
        public async Task InitializeDiscord()
        {
            // Build client
            discordClient = new DiscordSocketClient();

            // Set up client logging
            discordClient.Log += Log;

            // Log bot in
            await discordClient.LoginAsync(TokenType.Bot, Secrets.token);
            await discordClient.StartAsync();

            server = discordClient.GetGuild(serverID);
            // Wait for it to load
            while (server == null)
            {
                await Task.Delay(2000);
                service._logger.LogInformation("Waiting for Discord server");
                server = discordClient.GetGuild(serverID);
            }

            serverInstance = server;
            govServer = server;

            /*
            govServer = discordClient.GetGuild(govserverID);

            // Wait for it to load
            while (govServer == null)
            {
                await Task.Delay(1000);
                service._logger.LogInformation("Waiting for Gov Discord server");
                govServer = discordClient.GetGuild(govserverID);
            }
            */

            botChannel = server.GetTextChannel(botChannelID);
            // Wait more
            while (botChannel == null)
            {
                await Task.Delay(1000);
                service._logger.LogInformation("Waiting for Discord Bot Channel");
                botChannel = server.GetTextChannel(botChannelID);
            }

            gameChannel = server.GetTextChannel(gameChannelID);
            // Wait more
            while (gameChannel == null)
            {
                await Task.Delay(1000);
                gameChannel = server.GetTextChannel(gameChannelID);
            }

            forumChannel = server.GetTextChannel(forumChannelID);
            while (forumChannel == null)
            {
                await Task.Delay(1000);
                forumChannel = server.GetTextChannel(forumChannelID);
            }

            ecoChannel = server.GetTextChannel(ecoChannelID);
            while (ecoChannel == null)
            {
                await Task.Delay(1000);
                ecoChannel = server.GetTextChannel(ecoChannelID);
            }

            stockChannel = server.GetTextChannel(stockChannelID);
            while (stockChannel == null)
            {
                await Task.Delay(1000);
                stockChannel = server.GetTextChannel(stockChannelID);
            }

            logChannel = server.GetTextChannel(logChannelID);
            while (logChannel == null)
            {
                await Task.Delay(1000);
                logChannel = server.GetTextChannel(logChannelID);
            }

            newsChannel = server.GetTextChannel(newsChannelID);
            while (newsChannel == null)
            {
                await Task.Delay(1000);
                newsChannel = server.GetTextChannel(newsChannelID);
            }

            migrateChannel = server.GetTextChannel(migrateChannelID);
            while (newsChannel == null)
            {
                await Task.Delay(1000);
                migrateChannel = server.GetTextChannel(migrateChannelID);
            }

            // await LoadUsers();

            await LoadRoles();

            // Set up chat parsing
            discordClient.MessageReceived += MessageHandler.MessageReceived;
            discordClient.ReactionAdded += ReactionHandler.ReactionAdded;
            discordClient.ReactionRemoved += ReactionHandler.ReactionRemoved;

            //await server.DownloadUsersAsync();
            //Console.WriteLine("Finished downloading Discord users.");
        }

        private async Task LoadRoles()
        {
            spleenRole = server.Roles.FirstOrDefault(x => x.Name == "Spleen Rank");
            crabRole = server.Roles.FirstOrDefault(x => x.Name == "Crab Rank");
            gatyRole = server.Roles.FirstOrDefault(x => x.Name == "Gaty Rank");
            corgiRole = server.Roles.FirstOrDefault(x => x.Name == "Corgi Rank");
            oofRole = server.Roles.FirstOrDefault(x => x.Name == "Oof Rank");

            patreonCitizen = server.Roles.FirstOrDefault(x => x.Name == "Patreon Citizen");
            patreonSoldier = server.Roles.FirstOrDefault(x => x.Name == "Patreon Soldier");
            patreonLoyalist = server.Roles.FirstOrDefault(x => x.Name == "Patreon Loyalist");
            patreonHero = server.Roles.FirstOrDefault(x => x.Name == "Patreon Hero");
            patreonMadlad = server.Roles.FirstOrDefault(x => x.Name == "Patreon Madlad");

            youtubeCitizen = server.Roles.FirstOrDefault(x => x.Id == 754387925742911600);

            using (VooperContext context = new VooperContext(DBOptions))
            {
                /*
                foreach(District district in govContext.Districts)
                {
                    if (!server.Roles.Any(x => x.Name == district.Name + " District"))
                    {
                        await server.CreateRoleAsync(district.Name + " District", GuildPermissions.None, null, false, RequestOptions.Default);
                    }
                }
                */

                foreach (District d in context.Districts)
                {
                    districtRoles.Add(d.Name + " District", server.Roles.FirstOrDefault(x => x.Name == d.Name + " District"));
                }
            }

        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        public static async Task<IVoteable> DoVote(List<IVoteable> options, int seconds)
        {
            Dictionary<Emoji, IVoteable> tracker = new Dictionary<Emoji, IVoteable>();

            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = new Color(0, 100, 255),
                Title = "Vote:"
            };

            //string mes = "|";

            foreach (IVoteable option in options)
            {
                if (!tracker.ContainsKey(option.GetEmote()))
                {

                    tracker.Add(option.GetEmote(), option);
                    //mes += $" {option.GetName()} = {option.GetEmote()} |";

                    embed.AddField((efb) =>
                    {
                        efb.Name = option.GetName();
                        efb.Value = option.GetEmote();
                    });
                }
            }

            RestUserMessage msg = await VoopAI.gameChannel.SendMessageAsync(embed: embed.Build());

            List<Emoji> distinct = new List<Emoji>();

            Emoji c = null;

            try
            {
                foreach (Emoji e in tracker.Keys)
                {
                    c = e;
                    await msg.AddReactionAsync(e);
                }
            }
            catch (System.Exception e)
            {
                await (gameChannel.SendMessageAsync(e.ToString()));
                await (gameChannel.SendMessageAsync(c.ToString()));
            }


            Thread.Sleep(seconds * 1000);

            IEmote chosen = tracker.Keys.PickRandom();
            int top = 0;

            IUserMessage liveMessage = (IUserMessage)await VoopAI.gameChannel.GetMessageAsync(msg.Id);

            foreach (KeyValuePair<IEmote, ReactionMetadata> pair in liveMessage.Reactions)
            {
                //await VoopAI.gameChannel.SendMessageAsync($"DEBUG: {pair.Key.Name} = {pair.Value.ReactionCount}");

                if (pair.Value.ReactionCount > top)
                {
                    top = pair.Value.ReactionCount;
                    chosen = pair.Key;
                }
            }

            IVoteable obj = tracker[new Emoji(chosen.Name)];

            await VoopAI.gameChannel.SendMessageAsync($"You have chosen [ {obj.GetName()} ]");

            Thread.Sleep(5000);

            return obj;
        }
    }
}
