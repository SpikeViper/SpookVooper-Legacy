using System;
using System.Collections.Generic;
using System.Text;

namespace SpookVooper.VoopAIService.Game
{
    public class GameEntity
    {
        public string _name;
        public string _adjective;
        public bool dead = false;

        public virtual int GetXP()
        {
            return (int)(baseUnit.maxhealth / 10f);
        }

        public string name
        {
            get
            {
                return $"{_adjective} {_name}";
            }
        }

        public List<Item> items;

        private int _health;

        public int health
        {
            get
            {
                return _health;
            }
            set
            {
                _health = value;

                if (_health <= 0)
                {
                    dead = true;
                    _health = 0;
                }
            }
        }

        public Unit baseUnit;

        public GameEntity(Unit baseUnit)
        {
            this._name = Randoms.names.PickRandom();

            this._adjective = Randoms.people_adjectives.PickRandom();

            items = new List<Item>();

            items.Add(new ItemFist());

            int itemCount = VoopAI.random.Next(3);

            // Add two random possible items
            for (int i = 0; i < itemCount; i++)
            {
                items.Add(baseUnit.items.PickRandom());
            }

            this.baseUnit = baseUnit;

            health = baseUnit.maxhealth;

        }
    }


    public class Player : GameEntity
    {
        public int coins;
        public int xp;
        
        public Player(Unit unit) : base(unit)
        {
            items = new List<Item>();

            int itemCount = VoopAI.random.Next(5);

            items.Add(new ItemFist());

            health *= 2;

            // Add two random possible items
            for (int i = 0; i < itemCount; i++)
            {
                items.Add(baseUnit.items.PickRandom());
            }
        }
    }
}
