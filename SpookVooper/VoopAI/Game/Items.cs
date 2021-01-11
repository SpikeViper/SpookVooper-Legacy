using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using System.Threading;
using System.Threading.Tasks;

namespace SpookVooper.VoopAIService.Game
{
    public interface IUsable
    {
        Task OnUse(GameEntity entity);
        string GetUseVerb();
    }

    public class ItemHealthPotion : Item
    {
        public override string GetName()
        {
            return "Health Potion";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("\u2764");
        }

        public override string GetUseVerb()
        {
            return "drink";
        }

        public override string GetInfo()
        {
            return $"Health: 25 | Buy: {GetValue()} | Sell: {GetValue() / 2}";
        }

        public override int GetValue()
        {
            return 10;
        }

        public override async Task OnUse(GameEntity entity)
        {
            entity.health += 25;

            if (entity.health > entity.baseUnit.maxhealth * 2)
            {
                entity.health = entity.baseUnit.maxhealth * 2;
            }

            if (entity is Player)
            {
                await VoopAI.gameChannel.SendMessageAsync($"You {GetUseVerb()} the {GetName()} and recovered 25 health!");
            }
            else
            {
                await VoopAI.gameChannel.SendMessageAsync($"{entity.name} {GetUseVerb()} the {GetName()} and recovered 25 health!");
            }

            entity.items.Remove(this);

            Thread.Sleep(4000);
        }
    }

    public class Item : IVoteable, IUsable
    {
        public virtual string GetName()
        {
            return "Null item";
        }

        public virtual Emoji GetEmote()
        {
            return new Emoji("");
        }

        public virtual string GetUseVerb()
        {
            return "use";
        }

        public virtual string GetInfo()
        {
            return $"Buy: {GetValue()} | Sell: {GetValue() / 2}";
        }

        public virtual int GetValue()
        {
            return 5;
        }

        public virtual async Task OnUse(GameEntity entity)
        {

        }
    }

    public class Weapon : Item
    {
        public virtual float GetDamage()
        {
            return 0;
        }

        public override string GetInfo()
        {
            return $"Damage: {GetDamage()} | Buy: {GetValue()} | Sell: {GetValue() / 2}";
        }
    }

    public class ItemSword : Weapon
    {
        public override string GetName()
        {
            return "Sword";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("\u2694");
        }

        public override string GetUseVerb()
        {
            return "slash";
        }

        public override float GetDamage()
        {
            return 20f;
        }

        public override int GetValue()
        {
            return 20;
        }
    }

    public class ItemDeagle : Weapon
    {
        public override string GetName()
        {
            return "Desert Eagle";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🔫");
        }

        public override string GetUseVerb()
        {
            return "fire at";
        }

        public override float GetDamage()
        {
            return 30f;
        }

        public override int GetValue()
        {
            return 45;
        }
    }

    public class ItemGrenade : Weapon
    {
        public override string GetName()
        {
            return "Grenade";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("💣");
        }

        public override string GetUseVerb()
        {
            return "throw it at";
        }

        public override float GetDamage()
        {
            return 85f;
        }

        public override int GetValue()
        {
            return 100;
        }

        public override async Task OnUse(GameEntity entity)
        {
            entity.items.Remove(this);
        }
    }

    public class ItemSpoon : Weapon
    {
        public override string GetName()
        {
            return "Spoon";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("🥄");
        }

        public override string GetUseVerb()
        {
            return "smack";
        }

        public override float GetDamage()
        {
            return 6f;
        }

        public override int GetValue()
        {
            return 5;
        }
    }

    public class ItemFist : Weapon
    {
        public override string GetName()
        {
            return "Fists";
        }

        public override Emoji GetEmote()
        {
            return new Emoji("✊");
        }

        public override string GetUseVerb()
        {
            return "punch";
        }

        public override float GetDamage()
        {
            return 5f;
        }
    }
}
