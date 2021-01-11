using System;
using System.Collections.Generic;
using System.Text;

namespace SpookVooper.VoopAIService.Game
{
    public class Level
    {
        public Team team;

        public Team enemyTeam;

        public Location location;
    }

    public class Levels
    {
        public static Level GenerateLevel()
        {
            return new Level()
            {
                location = new List<Location>() { new Locations.Country(), new Locations.Mall(), new Locations.Base() }.PickRandom(),
                team = new List<Team>() { Teams.TeamPotatos, Teams.TeamYams }.PickRandom(),
                enemyTeam = new List<Team>() { Teams.TeamPotatos, Teams.TeamYams }.PickRandom()
            };
        }
    }
}
