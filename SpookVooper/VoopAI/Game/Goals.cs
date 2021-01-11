using System;
using System.Collections.Generic;
using System.Text;

namespace SpookVooper.VoopAIService.Game
{
    public class Goal
    {
        public Team with;
        public Team against;
        public Player player;
        public Location goalLocation;
        public GameEntity mainBad;

        public Goal(Team with, Team against, Player player, GameEntity mainBad)
        {
            this.with = with;
            this.against = against;
            this.player = player;
            this.mainBad = mainBad;
        }

        public virtual string GetStory()
        {
            return "Null story";
        }

        public virtual string GetProblem()
        {
            return "Null problem";
        }

        public virtual string GetPlaceName()
        {
            return "Null place";
        }

        public virtual string GetSubjectName()
        {
            return "Null thing";
        }

        public virtual string GetObjective()
        {
            return "Null objective";
        }
    }

    public class GoalRescue : Goal
    {
        private string _subjectname;

        public GoalRescue(Team with, Team against, Player player, GameEntity mainBad) : base(with, against, player, mainBad)
        {
            goalLocation = new List<Location>() { new Locations.Headquarters(true, against), 
                                                  new Locations.Prison(true, against)}.PickRandom();
        }

        public override string GetSubjectName()
        {
            if (String.IsNullOrEmpty(_subjectname))
            {
                _subjectname = Randoms.names.PickRandom();
            }

            return _subjectname;
        }

        public override string GetPlaceName()
        {
            return goalLocation.name;
        }

        public override string GetProblem()
        {
            return $"{_subjectname} got kidnapped";
        }

        public override string GetStory()
        {
            return $"It's an emergency! {GetSubjectName()} has been kidnapped and brought to the " +
                   $"{goalLocation.name}! They need to be rescued before it's too late. It would appear that " +
                   $"they were kidnapped by the {against.name()}'s leader {mainBad.name}!";
        }

        public override string GetObjective()
        {
            return $"rescue {_subjectname}";
        }
    }
}
