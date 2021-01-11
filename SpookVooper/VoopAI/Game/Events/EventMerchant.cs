using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using System.Linq;

namespace SpookVooper.VoopAIService.Game.Events
{
    public class EventMerchant : Event
    {
        public EventMerchant(Location l) : base(l)
        {
            this.entity = new GameEntity(RPG_Game.level.team.units.PickRandom());

            for (int i = 0; i < VoopAI.random.Next(1, 5); i++)
            {
                List<Item> buyables = new List<Item>() { new ItemHealthPotion(), new ItemSword(), new ItemSpoon(), new ItemDeagle() };

                entity.items.Add(buyables.PickRandom());
            }
        }

        public override async Task RunEvent(Player player, bool skipDesc = false)
        {
            if (!skipDesc)
            {
                await VoopAI.gameChannel.SendMessageAsync(GetDescription());
            }

            while (!this.endEvent)
            {
                await PlayerTurn(player);
            }

        }

        public virtual async Task PlayerTurn(Player player)
        {
            bool usedAction = false;

            while (!usedAction)
            {
                List<IVoteable> options = new List<IVoteable>();
                options.AddRange(GetActions());

                Action result = (Action)await VoopAI.DoVote(options, 15);

                if (result is AttackAction)
                {
                    Event battleEvent = new EventAmbush(location, entity);

                    await VoopAI.gameChannel.SendMessageAsync("You jump the merchant, hoping to loot him rather than pay!");

                    Thread.Sleep(5000);

                    await battleEvent.RunEvent(player, true);

                    usedAction = true;
                    this.endEvent = true;
                }
                else
                {
                    await result.DoAction(this, player);

                    if (!result.Passive())
                    {
                        usedAction = true;
                    }
                }
            }
        }

        public override string GetName()
        {
            return "A Merchant Appears!";
        }

        public override List<Action> GetActions()
        {
            return new List<Action>() { new BuyAction(entity), new SellAction(entity), new AttackAction(), new LeaveAction(), new UseAction(), new InventoryAction(), new GetEntityInfoAction() };
        }

        public override string GetDescription()
        {

            return $"You come across one of your people named {entity.name} who claims to have items you could use, for a price. It may be a good idea to gear up for the journey.";
        }
    }
}
