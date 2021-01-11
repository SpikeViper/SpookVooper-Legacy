using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;

namespace SpookVooper.VoopAIService.Game
{
    public class Event
    {
        public bool endEvent = false;

        public virtual string GetName()
        {
            return "Null event";
        }

        public virtual List<Action> GetActions()
        {
            return null;
        }

        public virtual string GetDescription()
        {
            return "Null event";
        }

        public virtual async Task RunEvent(Player player, bool skipDesc = false)
        {
            if (!skipDesc)
            {
                await VoopAI.gameChannel.SendMessageAsync(GetDescription());
            }
        }

        public Location location;
        public GameEntity entity;

        public Event(Location location, GameEntity entity = null)
        {
            this.location = location;

            if (entity == null)
            {
                this.entity = new GameEntity(RPG_Game.level.enemyTeam.units.PickRandom());
            }
        }
    }
}
