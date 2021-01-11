using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using SpookVooper.Web.Entities;
using SpookVooper.Web.DB;

namespace SpookVooper.VoopAIService
{
    public static class ReactionHandler
    {
        public static async Task ReactionAdded(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {

            if (reaction.Emote.Name == "commend")
            {
                IUserMessage msg = (IUserMessage)channel.GetMessageAsync(message.Id).Result;
                
                IUser author = msg.Author;

                if (author.IsBot)
                {
                    return;
                }
                
                IUser sender = reaction.User.Value;

                using (VooperContext context = new VooperContext(VoopAI.DBOptions))
                {
                    User senderData = context.Users.FirstOrDefault(u => u.discord_id == sender.Id);

                    if (senderData == null || author.Id == sender.Id || senderData.discord_last_commend_hour == DateTime.Now.Hour)
                    {
                        await msg.RemoveReactionAsync(reaction.Emote, sender);
                    }
                    else
                    {
                        await AddCommend(author, sender, message.Id);
                    }
                }
            }
        }

        public static async Task ReactionRemoved(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Emote.Name == "commend")
            {
                IUser author = channel.GetMessageAsync(message.Id).Result.Author;

                if (author.IsBot)
                {
                    return;
                }

                IUser sender = reaction.User.Value;

                if (author.Id != sender.Id)
                {
                    await RemoveCommend(author, sender, message.Id);
                }
            }
        }

        public static async Task AddCommend(IUser target, IUser giver, ulong messageID)
        {
            Console.WriteLine(giver.Username + " commended " + target.Username);

            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User giverData = context.Users.FirstOrDefault(u => u.discord_id == giver.Id);
                User user = context.Users.FirstOrDefault(u => u.discord_id == target.Id);

                if (user != null)
                {
                    user.discord_commends++;
                }

                if (giverData != null)
                {
                    giverData.discord_commends_sent++;
                    giverData.discord_last_commend_hour = DateTime.Now.Hour;
                    giverData.discord_last_commend_message = messageID;
                }

                await context.SaveChangesAsync();
            }
        }

        public static async Task RemoveCommend(IUser target, IUser giver, ulong messageID)
        {


            using (VooperContext context = new VooperContext(VoopAI.DBOptions))
            {
                User giverData = context.Users.FirstOrDefault(u => u.discord_id == giver.Id);
                User user = context.Users.FirstOrDefault(u => u.discord_id == target.Id);

                if (giverData != null && giverData.discord_last_commend_message == messageID)
                {
                    Console.WriteLine(giver.Username + " un-commended " + target.Username);

                    if (user != null)
                    {
                        user.discord_commends--;
                    }

                    giverData.discord_commends_sent--;
                    giverData.discord_last_commend_hour = DateTime.Now.Hour;
                    giverData.discord_last_commend_message = 0;

                    await context.SaveChangesAsync();
                }
            }
            
        }
    }
}
