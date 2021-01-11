using System;
using System.Collections.Generic;
using System.Text;

namespace SpookVooper.VoopAIService.Game
{
    public class Team
    {
        public string baseName;
        public List<Unit> units;
        public bool rebel = false;

        public void determine_rebel(Team enemy)
        {
            if (enemy.baseName == this.baseName)
            {
                rebel = true;
            }
            else
            {
                rebel = false;
            }

        }

        public string name()
        {
            if (rebel)
            {
                return "Rebel " + baseName;
            }
            else
            {
                return baseName;
            }
        }

    }

    class Teams
    {
        public static Team TeamYams = new Team()
        {
            baseName = "Yams",
            units = new List<Unit>() { Units.Yam }
        };

        public static Team TeamPotatos = new Team()
        {
            baseName = "Potatoes",
            units = new List<Unit>() { Units.Potato }
        };
    }
}
