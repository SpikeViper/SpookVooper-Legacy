using System;
using System.Linq;
using TwitchLib.Client.Events;
using SpookVooper.Web.DB;
using SpookVooper.Web.Entities;

namespace SpookVooper.VoopAIService
{
    static class TwitchManager
    {
        public static bool streaming = false;


        public static void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            //MessageHandler.SendMessage(VoopAI.botChannel, "Hooked into Twitch");
        }

        public static void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            VoopAI.twitchClient.SendMessage(e.Channel, "Initializing VoopAI Twitch Hook");
        }

        public static void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {

        }

        public static void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            Console.WriteLine($"Caught Twitch message {e.ChatMessage.Message}");

            string[] args = e.ChatMessage.Message.ToLower().Split(' ');

            if (args[0] == "!golive" && e.ChatMessage.DisplayName == "SpikeViper")
            {
                VoopAI.twitchClient.SendMessage(e.ChatMessage.Channel, "Message tracking enabled!");
                streaming = true;
            }

            if (args[0] == "!stoplive" && e.ChatMessage.DisplayName == "SpikeViper")
            {
                VoopAI.twitchClient.SendMessage(e.ChatMessage.Channel, "Message tracking disabled!");
                streaming = false;
            }

            if (args[0] == "!connectsite")
            {
                Console.WriteLine($"Detected connect site command!");

                if (args.Length < 2)
                {
                    VoopAI.twitchClient.SendMessage(e.ChatMessage.Channel, "Please include your key!");
                    Console.WriteLine($"Key was not included!");
                    return;
                }

                int key = -1;

                bool success = int.TryParse(args[1], out key);

                if (success)
                {
                    Console.WriteLine($"Parsed key {key}");

                    using (VooperContext context = new VooperContext(VoopAI.DBOptions))
                    {
                        var webUser = context.Users.FirstOrDefault(u => u.Id == VoopAI.service._connectionHandler.GetUserFromKey(key));
                        Console.WriteLine($"Got user");

                        if (webUser != null)
                        {
                            Console.WriteLine($"Found webuser {webUser.UserName}");

                            webUser.twitch_id = e.ChatMessage.DisplayName;

                            context.Update(webUser);
                            Console.WriteLine($"Updating context");
                            try
                            {
                                context.SaveChanges();
                            }
                            catch (System.Exception ex)
                            {
                                Console.WriteLine(ex.StackTrace);
                            }
                            Console.WriteLine($"Saving changes");
                            VoopAI.service._connectionHandler.RemoveKey(key);
                            Console.WriteLine($"Removing key");

                            VoopAI.twitchClient.SendMessage(e.ChatMessage.Channel, $"Successfully linked {e.ChatMessage.DisplayName}!");
                            Console.WriteLine($"Successfully linked twitch {e.ChatMessage.DisplayName}!");
                        }
                        else
                        {
                            VoopAI.twitchClient.SendMessage(e.ChatMessage.Channel, "Unable to find user who generated this key.");
                            Console.WriteLine($"Unable to find web user trying to connect {e.ChatMessage.DisplayName}!");
                            return;
                        }
                    }
                }
                else
                {
                    VoopAI.twitchClient.SendMessage(e.ChatMessage.Channel, "Unable to read your key.");
                    Console.WriteLine("Failed to read key!");
                }
            }

            if (!streaming)
            {
                return;
            }

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User user = context.Users.FirstOrDefault(u => u.twitch_id == e.ChatMessage.DisplayName);

                if (user != null)
                {
                    user.twitch_messages++;

                    if (user.twitch_last_message_minute != DateTime.Now.Minute)
                    {
                        user.twitch_message_xp++;
                        user.twitch_last_message_minute = DateTime.Now.Minute;
                    }

                    context.Update(user);
                    context.SaveChanges();
                }
            }
        }

        public static void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {

        }
    }
}
