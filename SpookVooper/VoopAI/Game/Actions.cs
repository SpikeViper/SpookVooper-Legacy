using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.API;
using Discord.WebSocket;
using System.Threading.Tasks;
using Discord.Rest;
using System.Threading;
using System.Linq;

namespace SpookVooper.VoopAIService.Game
{

    public interface IVoteable : INamed
    {
        Emoji GetEmote();
    }

    public interface INamed
    {
        string GetName();
    }

    public class Action : IVoteable
    {

        public virtual string GetName()
        {
            return "Null action";
        }

        public virtual Emoji GetEmote()
        {
            return new Emoji("❓");
        }

        public virtual bool Passive()
        {
            return false;
        }

        public virtual async Task DoAction(Event e, Player player)
        {

        }
    }

    public class BuyAction : Action
    {
        private GameEntity merchant;

        public BuyAction(GameEntity merchant)
        {
            this.merchant = merchant;
        }

        public override string GetName()
        {
            return "Buy";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🇧");
        }

        public override bool Passive()
        {
            return true;
        }

        public override async Task DoAction(Event e, Player player)
        {

            await VoopAI.gameChannel.SendMessageAsync($"You have {player.coins} coins. What would you like to buy?");

            List<Item> buyables = merchant.items.Where(x => !(x is ItemFist)).ToList();

            List<IVoteable> options = new List<IVoteable>();
            options.AddRange(buyables);

            Item chosen = (Item)await VoopAI.DoVote(options, 10);

            if (player.coins < chosen.GetValue())
            {
                await VoopAI.gameChannel.SendMessageAsync($"You attempt to buy the {chosen.GetName()}, but you cannot afford it. " +
                                                         $"{e.entity.name} sends you off, annoyed you wasted their time.");

                e.endEvent = true;
            }
            else
            {
                await VoopAI.gameChannel.SendMessageAsync($"You give {chosen.GetValue()} coins to {e.entity.name} and " +
                                                         $"they give you the {chosen.GetName()}.");

                player.coins -= chosen.GetValue();
                player.items.Add(chosen);

                merchant.items.Remove(chosen);
            }
        }
    }

    public class SellAction : Action
    {
        private GameEntity merchant;

        public SellAction(GameEntity merchant)
        {
            this.merchant = merchant;
        }

        public override string GetName()
        {
            return "Sell";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🇸");
        }

        public override bool Passive()
        {
            return true;
        }

        public override async Task DoAction(Event e, Player player)
        {
            if (player.items.Count < 2)
            {
                await VoopAI.gameChannel.SendMessageAsync($"You have nothing to sell!");
            }

            await VoopAI.gameChannel.SendMessageAsync($"You have {player.coins} coins. What would you like to sell?");

            List<Item> sellables = player.items.Where(x => !(x is ItemFist)).ToList();

            List<IVoteable> options = new List<IVoteable>();
            options.AddRange(sellables);

            Item chosen = (Item)await VoopAI.DoVote(options, 10);

            await VoopAI.gameChannel.SendMessageAsync($"You give {chosen.GetName()} to {e.entity.name} and " +
                                                     $"they give you {chosen.GetValue() / 2} coins.");

            player.coins += chosen.GetValue() / 2;
            player.items.Remove(chosen);

            merchant.items.Add(chosen);

        }
    }

    public class InventoryAction : Action
    {

        public override string GetName()
        {
            return "View Inventory";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("💼");
        }

        public override bool Passive()
        {
            return true;
        }

        public override async Task DoAction(Event e, Player player)
        {

            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = new Color(0, 100, 255),
                Title = "Inventory:"
            };

            embed.AddField($"Coins: ", player.coins.ToString());

            foreach (Item i in player.items)
            {
                embed.AddField($"{i.GetName()} {i.GetEmote()}", i.GetInfo());
            }

            await VoopAI.gameChannel.SendMessageAsync($"You take a peek into your inventory: \n", embed: embed.Build());

            Thread.Sleep(10000);
        }
    }

    public class TravelAction : Action
    {

        public override string GetName()
        {
            return "Travel";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🇹");
        }

        public override async Task DoAction(Event e, Player player)
        {
            if (RPG_Game.sublocation != null)
            {
                await VoopAI.gameChannel.SendMessageAsync($"You leave the {RPG_Game.sublocation.name}, and continue " +
                    $"towards the {RPG_Game.goal.goalLocation}.");
            }
            else
            {
                await VoopAI.gameChannel.SendMessageAsync($"You continue " +
                    $"towards the {RPG_Game.goal.goalLocation}.");
            }
        }
    }

    public class LeaveAction : Action
    {

        public override string GetName()
        {
            return "Leave";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🇱");
        }

        public override async Task DoAction(Event e, Player player)
        {
                await VoopAI.gameChannel.SendMessageAsync($"You leave the {RPG_Game.sublocation.name}, and continue " +
                    $"towards the {RPG_Game.goal.goalLocation}.");

            e.endEvent = true;
        }
    }

    public class UseAction : Action
    {

        public override string GetName()
        {
            return "Use Item";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🇺");
        }

        public override async Task DoAction(Event e, Player player)
        {
            List<Item> usables = player.items.Where(x => !(x is Weapon)).ToList();

            if (usables.Count == 0)
            {
                await RPG_Game.gameChannel.SendMessageAsync($"You have nothing to use!");
                return;
            }

            List<IVoteable> options = new List<IVoteable>();
            options.AddRange(usables);

            Item result = (Item)await VoopAI.DoVote(options, 10);

            await result.OnUse(player);

            Thread.Sleep(5000);
        }
    }

    public class GetEntityInfoAction : Action
    {
        public override string GetName()
        {
            return "Get Info";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🔎");
        }

        public override bool Passive()
        {
            return true;
        }

        public override async Task DoAction(Event e, Player player)
        {
            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = new Color(255, 0, 0),
                Title = e.entity.name
            };

            embed.AddField("Health ❤️", $"{e.entity.health} / {e.entity.baseUnit.maxhealth}");
            embed.AddField("Race 🏳️", e.entity.baseUnit.name);
            embed.AddField("Items: ", $"_____");

            foreach (Item i in e.entity.items)
            {
                embed.AddField($"{i.GetName()} {i.GetEmote()}", $"{i.GetInfo()}");
            }

            await VoopAI.gameChannel.SendMessageAsync(embed: embed.Build());

            Thread.Sleep(10000);
        }
    }

    public class AttackAction : Action
    {
        public override string GetName()
        {
            return "Attack";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🇦");
        }

        public override async Task DoAction(Event e, Player player)
        {
            await VoopAI.gameChannel.SendMessageAsync($"Please select an item for attack: ");

            List<IVoteable> options = new List<IVoteable>();
            options.AddRange(player.items.Where(x => x is Weapon).ToList());

            Weapon chosen = (Weapon)(await VoopAI.DoVote(options, 10));

            int roll = VoopAI.random.Next(0, 21);
            float mult = (float)Math.Round(roll / 20.0, 1);
            float damage = (float)Math.Round(chosen.GetDamage() * mult, 1);

            //await VoopAI.gameChannel.SendMessageAsync($"Roll: {roll} \n" +
            //                                          $"Damage: {chosen.GetDamage()} x {mult} = {damage}");

            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = new Color(0, 100, 255),
                Title = "Attack:"
            };

            embed.AddField("Roll 🎲", roll.ToString());
            embed.AddField($"Weapon Strength {chosen.GetEmote().Name}", chosen.GetDamage());
            embed.AddField("Damage: 💥", $"{chosen.GetDamage()} x {mult} = {damage}");

            await VoopAI.gameChannel.SendMessageAsync(embed: embed.Build());

            Thread.Sleep(2000);

            await chosen.OnUse(player);
            e.entity.health -= (int)Math.Ceiling(damage);

            await VoopAI.gameChannel.SendMessageAsync($"You {chosen.GetUseVerb()} {e.entity.name} and their health drops to {e.entity.health}.");

            Thread.Sleep(2000);

        }
    }
}
