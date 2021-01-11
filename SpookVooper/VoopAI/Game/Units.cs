using System;
using System.Collections.Generic;
using System.Text;

namespace SpookVooper.VoopAIService.Game
{
    public class Unit
    {
        public string name;
        public int maxhealth;

        public List<Item> items;
    }

    public static class Units
    {
        public static Unit Potato = new Unit()
        {
            name = "Potato",
            maxhealth = 50,
            items = new List<Item>() { new ItemDeagle(), new ItemSword(), new ItemSpoon(), new ItemGrenade(), new ItemHealthPotion() }
        };

        public static Unit Yam = new Unit()
        {
            name = "Yam",
            maxhealth = 50,
            items = new List<Item>() { new ItemDeagle(), new ItemSword(), new ItemSpoon(), new ItemGrenade(), new ItemHealthPotion() }
        };
    }
}
