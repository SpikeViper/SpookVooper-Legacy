using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SpookVooper.VoopAIService;
using System.Threading.Tasks;

namespace SpookVooper.Common.VoopAIService
{
    public static class ChatFilter
    {
        public static async Task FilterMessage(SocketMessage message)
        {
            await BlockRaids(message);
            await BlockProfanity(message);
        }

        public static async Task BlockRaids(SocketMessage message)
        {
            int total = message.MentionedUsers.Count + message.MentionedRoles.Count;

            if (total > 5 || message.MentionedRoles.Count > 1)
            {
                var channel = message.Channel as SocketGuildChannel;

                if (channel != null)
                {
                    SocketGuildUser user = channel.Guild.GetUser(message.Author.Id);

                    if (!message.Author.IsBot && !user.Roles.Any(x => x.Permissions.Administrator))
                    {
                        // If user is ALREADY MUTED then we go big ban
                        if (user.Roles.Any(x => x.Name == "Muted"))
                        {
                            await user.Guild.AddBanAsync(user);
                            await VoopAI.logChannel.SendMessageAsync($"[{channel.Guild.Name}] Banned {message.Author.Username} for mass-ping while muted! ({total})");
                            return;
                        }

                        var mutedRole = channel.Guild.Roles.FirstOrDefault(x => x.Name == "Muted");

                        if (mutedRole != null) 
                        {
                            await user.AddRoleAsync(mutedRole);
                            await VoopAI.logChannel.SendMessageAsync($"[{channel.Guild.Name}] Muted {message.Author.Username} for mass-ping! ({total})");
                            await message.DeleteAsync();
                        }
                        else await message.Channel.SendMessageAsync("Tried to mute user for security, but could not find 'Muted' role!");
                    }
                }
            }
        }

        public static List<string> bannedWords = new List<string>() { "nigger", "faggot" };

        public static async Task BlockProfanity(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            bool blocked = false;
            string word = null;

            foreach (string s in bannedWords)
            {
                if (message.Content.ToLower().Contains(s)) 
                { 
                    blocked = true; 
                    word = s; 
                    break; 
                }
            }

            if (blocked)
            {
                var channel = message.Channel as SocketGuildChannel;
                SocketGuildUser user = channel.Guild.GetUser(message.Author.Id);

                var mutedRole = channel.Guild.Roles.FirstOrDefault(x => x.Name == "Muted");

                if (mutedRole != null)
                {
                    await user.AddRoleAsync(mutedRole);
                    await VoopAI.logChannel.SendMessageAsync($"[{channel.Guild.Name}] Muted {message.Author.Username} for extreme profanity! ({word})");
                    await message.DeleteAsync();
                }
                else await message.Channel.SendMessageAsync("Tried to mute user for security, but could not find 'Muted' role!");
            }
        }
    }
}
