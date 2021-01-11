using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using SpookVooper.VoopAIService.Game.Events;

namespace SpookVooper.VoopAIService.Game
{
    public class Location
    {
        public string name;

        public virtual List<Event> GetPossibleEvents()
        {
            return new List<Event>() { new EventAmbush(this), new EventMerchant(this) };
        }
        
        public virtual Location GetSubLocation()
        {
            return null;
        }
    }

    public class Locations
    {
        public class Country : Location
        {
            public Country()
            {
                name = new List<string>() { "Country of Vooperia", "City of New Yam", "Crater Map City", "City Of New Vooper", "Town of Briarwood Square" }.PickRandom();
            }

            public override Location GetSubLocation()
            {
                return new List<Location>() { new Mall(), new Store(), new Bathroom(), new Warehouse() }.PickRandom();
            }
        }

        public class Mall : Location
        {
            public Mall()
            {
                name = new List<string>() { "Mall Of Vooper", "New Yam Mall", "Central Shoppes" }.PickRandom();
            }

            public override Location GetSubLocation()
            {
                return new List<Location>() { new Store() }.PickRandom();
            }
        }

        public class Store : Location
        {
            public Store()
            {
                name = new List<string>() { "Headlight Fluid Outlet", "Silly's Lumber Shop", "Vooper Media Store", "Albright, Inc. Shoppe" }.PickRandom();
            }

            public override Location GetSubLocation()
            {
                return new List<Location>() { new Store(), new Bathroom() }.PickRandom();
            }
        }

        public class Bathroom : Location
        {
            public Bathroom()
            {
                name = "Bathroom";
            }
        }

        public class Base : Location
        {
            public Base()
            {
                name = new List<string>() { "Aircraft Carrier", "Army Base", "Air Base", "Space Station" }.PickRandom();
            }

            public override Location GetSubLocation()
            {
                return new List<Location>() { new Store(), new Bathroom(), new Warehouse() }.PickRandom();
            }
        }

        public class Warehouse : Location
        {
            public Warehouse()
            {
                name = new List<string>() { "Gun Storage Warehouse", "Supply Building", "Factory", "Stockpile" }.PickRandom();
            }

            public override Location GetSubLocation()
            {
                return new List<Location>() { new Bathroom(), new Warehouse() }.PickRandom();
            }
        }

        // Ending Locations //

        public class Headquarters : Location
        {
            public Headquarters(bool sub, Team enemy)
            {
                name = $"{enemy.name()} Headquarters";
            }
        }

        public class Prison : Location
        {
            public Prison(bool sub, Team enemy)
            {
                name = $"{enemy.name()} Prison";
            }
        }
    }
}
