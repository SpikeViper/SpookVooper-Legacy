using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Discord.WebSocket;
using SpookVooper.VoopAIService.Game.Events;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;

namespace SpookVooper.VoopAIService.Game
{
    class RPG_Game
    {
        public static SocketTextChannel gameChannel;

        public static bool game_running = false;

        public static bool game_queuing = false;

        public static Level level;

        public static Player player;

        public static Location location;

        public static Location sublocation;

        public static List<SocketUser> currentPlayers;

        public static Goal goal;

        public static async void StartGame()
        {         
            currentPlayers = new List<SocketUser>();

            game_queuing = true;

            // Wait 5 minutes
            /*
            Thread.Sleep(60000);
            await gameChannel.SendMessageAsync("The game will begin in four minutes.");
            Thread.Sleep(60000);
            await gameChannel.SendMessageAsync("The game will begin in three minutes.");
            Thread.Sleep(60000);
            await gameChannel.SendMessageAsync("The game will begin in two minutes.");
            Thread.Sleep(60000);
            */
            await gameChannel.SendMessageAsync("The game will begin in one minute.");
            Thread.Sleep(30000);
            await gameChannel.SendMessageAsync("The game will begin in 30 seconds.");
            Thread.Sleep(20000);
            await gameChannel.SendMessageAsync("The game will begin in 10 seconds.");
            Thread.Sleep(10000);
            await gameChannel.SendMessageAsync("The game has begun.");

            game_queuing = false;

            game_running = true;

            int game_eventsleft = 10;

            string playing = $"There are {currentPlayers.Count} players in this game -";

            if (currentPlayers.Count > 0)
            {
                for (int i = 0; i < currentPlayers.Count; i++)
                {
                    if (i == 0)
                    {
                        playing += " " + currentPlayers[i].Username;
                    }

                    else if (i == currentPlayers.Count - 1)
                    {
                        playing += ", and " + currentPlayers[i].Username + ".";
                    }

                    else
                    {
                        playing += ", " + currentPlayers[i].Username;
                    }
                }

                await gameChannel.SendMessageAsync(playing);
            }
            else
            {
                await gameChannel.SendMessageAsync("There are no players! Ah! Cancelling the game...");
                game_running = false;
                return;
            }

            bool success = false;

            level = Levels.GenerateLevel();
            level.team.determine_rebel(level.enemyTeam);

            player = new Player(level.team.units.PickRandom());

            location = level.location;

            GameEntity mainBadGuy = new GameEntity(level.enemyTeam.units.PickRandom());
            mainBadGuy.health *= 3;

            goal = new GoalRescue(level.team, level.enemyTeam, player, mainBadGuy);

            await gameChannel.SendMessageAsync(goal.GetStory());

            Thread.Sleep(5000);

            await gameChannel.SendMessageAsync((string)($"It has begun. You are {player.name}, a {player.baseUnit.name} of the {level.team.name()}. " +
                                               $"You begin your journey in the {location.name}. " +
                                               $"You are equipped with {player.items.Count - 1} things to help you in your journey."));

            await ViewInventory();

            Thread.Sleep(10000);

            while (game_running)
            {
                if (game_eventsleft > 0)
                {
                    sublocation = location.GetSubLocation();

                    await gameChannel.SendMessageAsync($"You continue towards {goal.goalLocation.name} and run across the " +
                                                       $"{sublocation.name}.\n");

                    Thread.Sleep(5000);

                    Event e = sublocation.GetPossibleEvents().PickRandom();


                    try
                    {
                        await e.RunEvent(player);
                    }
                    catch(System.Exception ex)
                    {
                        gameChannel.SendMessageAsync(ex.ToString());
                    }

                    if (player.dead)
                    {
                        await EndGame(false);

                        game_running = false;
                    }

                    game_eventsleft -= 1;
                }
                else
                {
                    await gameChannel.SendMessageAsync($"You continue towards {goal.goalLocation.name} and... you make it. You see the {goal.GetPlaceName()} over the horizon, and your journey is almost complete. But suddenly, the leader of your enemies, {mainBadGuy.name} appears!");

                    Thread.Sleep(5000);

                    Event e = new EventAmbush(goal.goalLocation, mainBadGuy);

                    await e.RunEvent(player);

                    if (player.dead)
                    {
                        await EndGame(false);

                        game_running = false;
                    }
                    else
                    {
                        await EndGame(true);
                    }
                }
            }
        }

        public static async Task EndGame(bool success)
        {
            int coinxp = 0;

            if (success)
            {
                coinxp = 2 * player.coins;
            }

            int fullxp = coinxp + player.xp;

            await gameChannel.SendMessageAsync((string)($"Your journey is complete, {player.name}. You earned {player.xp} normal xp, " +
                                               $"during this game!"));

            if (success)
            {
                await gameChannel.SendMessageAsync($"Ultimately, you succeeded in your goal, and managed to {goal.GetObjective()} " +
                                                   $"! You managed to collect {player.coins} coins which " +
                                               $"remained unused and has been converted into {coinxp} xp! You will not be forgotten. Because of this, you have earned DOUBLE XP!");

                fullxp *= 2;
            }
            else
            {
                await gameChannel.SendMessageAsync($"Ultimately, you have failed to {goal.GetObjective()}. The people of {location.name} " +
                                                   $"are dead, or worse. You will be forgotten. Because of this, you have earned HALF XP!");

                fullxp /= 2;
            }

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                

                foreach (SocketUser data in currentPlayers)
                {
                    User user = context.Users.FirstOrDefault(u => u.discord_id == data.Id);

                    if (user != null)
                    {
                        user.discord_game_xp += fullxp;
                        await context.SaveChangesAsync();
                    }
                }
            }

            game_running = false;
        }



        public static async Task ViewInventory()
        {
            if (player.items.Count > 1)
            {
                string di = "You are equipped with ";

                for (int i = 1; i < player.items.Count; i++)
                {
                    if (i == 1)
                    {
                        di += "a " + player.items[i].GetName();
                    }

                    else if (i == player.items.Count - 1)
                    {
                        di += " and a " + player.items[i].GetName() + ".";
                    }

                    else
                    {
                        di += ", a " + player.items[i].GetName();
                    }
                }

                await gameChannel.SendMessageAsync(di);
            }
        }
    }
}
