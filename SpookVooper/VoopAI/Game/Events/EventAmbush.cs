using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using System.Linq;
using SpookVooper.VoopAIService;

namespace SpookVooper.VoopAIService.Game.Events
{
    public class EventAmbush : Event
    {
        public EventAmbush(Location l, GameEntity e = null) : base(l, e)
        {
            this.location = l;

            if (e != null)
            {
                this.entity = e;
            }
        }

        public override async Task RunEvent(Player player, bool skipDesc = false)
        {

            if (!skipDesc)
            {
                await VoopAI.gameChannel.SendMessageAsync(GetDescription());
            }

            while (!endEvent)
            {

                await PlayerTurn(player);

                if (entity.dead)
                {
                    Thread.Sleep(5000);

                    int xp = entity.GetXP();
                    int coins = xp * VoopAI.random.Next(1, 4);

                    await RPG_Game.gameChannel.SendMessageAsync($"{entity.name} has been killed! You earned {xp} XP and {coins} Coins!");

                    int drop = VoopAI.random.Next(0, 2);

                    if (drop == 1 && entity.items.Count > 1)
                    {
                        Item dropItem = entity.items.Where(x => !(x is ItemFist)).PickRandom();

                        await RPG_Game.gameChannel.SendMessageAsync($"{entity.name} dropped their {dropItem.GetName()}!");

                        player.items.Add(dropItem);
                    }

                    player.xp += xp;
                    player.coins += coins;

                    endEvent = true;
                }
                else
                {
                    await EntityTurn(player);

                    if (player.dead)
                    {
                        endEvent = true;
                    }
                }
            }
        }

        public virtual async Task EntityTurn(Player player)
        {
            Item chosen = (Item)entity.items.PickRandom();

            if (!(chosen is Weapon))
            {
                await chosen.OnUse(entity);
                return;
            }

            Weapon weap = (Weapon)chosen;

            int roll = VoopAI.random.Next(0, 21);
            float mult = (float)Math.Round(roll / 20.0, 1);
            float damage = (float)Math.Round(weap.GetDamage() * mult, 1);

            //await VoopAI.gameChannel.SendMessageAsync($"Roll: {roll} \n" +
            //                                          $"Damage: {chosen.GetDamage()} x {mult} = {damage}");

            EmbedBuilder embed = new EmbedBuilder()
            {
                Color = new Color(255, 0, 0),
                Title = "Enemy Attack:"
            };

            embed.AddField((efb) =>
            {
                efb.Name = "Roll 🎲";
                efb.Value = roll.ToString();
            });

            embed.AddField((efb) =>
            {
                efb.Name = $"Weapon Strength {chosen.GetEmote().Name}";
                efb.Value = weap.GetDamage();
            });

            embed.AddField((efb) =>
            {
                efb.Name = "Damage: 💥";
                efb.Value = $"{weap.GetDamage()} x {mult} = {damage}";
            });

            await VoopAI.gameChannel.SendMessageAsync(embed: embed.Build());

            Thread.Sleep(5000);

            player.health -= (int)damage;
            await weap.OnUse(entity);

            await VoopAI.gameChannel.SendMessageAsync($"{entity.name} uses their {chosen.GetName()} to {chosen.GetUseVerb()} you, " +
                                                $"dropping your health to {player.health}");
        }

        public virtual async Task PlayerTurn(Player player)
        {
            bool usedAction = false;

            while (!usedAction)
            {
                List<IVoteable> options = new List<IVoteable>();
                options.AddRange(GetActions());

                Action result = (Action)await VoopAI.DoVote(options, 10);

                await result.DoAction(this, player);

                if (!result.Passive())
                {
                    usedAction = true;
                }
            }
        }

        public override string GetName()
        {
            return "Ambush!";
        }

        public override List<Action> GetActions()
        {
            return new List<Action>() { new AttackAction(), new UseAction(), new InventoryAction(), new GetEntityInfoAction() };
        }

        public override string GetDescription()
        {
            if (entity.items.Count > 0 && entity.items != null)
            {
                return $"During your travel, you run across a {entity.baseUnit.name} who runs " +
                       $"towards you aggressively. You try to communicate, but before you know it, they draw out a " +
                       $"{entity.items.PickRandom().GetName()}. There's no choice but to fight!";
            }
            else
            {
                return $"During your travel, you run across a {entity.baseUnit.name} who runs " +
                       $"towards you aggressively. You try to communicate, but before you know it, they swing at you. " +
                       $"There's no choice but to fight!";
            }
        }
    }
}
