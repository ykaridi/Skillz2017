using Pirates;
using System.Linq;

namespace MyBot.Engine
{
    class SmartIsland : MapObject
    {
        Island island;
        public SmartIsland(Island island)
        {
            this.island = island;
        }

        #region Extends
        public int Id
        {
            get
            {
                return island.Id;
            }
        }
        public Location Location
        {
            get
            {
                return island.Location;
            }
        }
        public Player Owner
        {
            get
            {
                return island.Owner;
            }
        }
        public int TurnsToDroneCreation
        {
            get
            {
                return island.TurnsToDroneCreation;
            }
        }
        public int ControlRange
        {
            get
            {
                return island.ControlRange;
            }
        }
        public bool IsOurs
        {
            get
            {
                return Owner.Id == Bot.Engine.Self.Id;
            }
        }
        public bool IsTheirs
        {
            get
            {
                return Owner.Id == Bot.Engine.Enemy.Id;
            }
        }
        public bool IsNeutral
        {
            get
            {
                return Owner.Id == Bot.Engine.Neutral.Id;
            }
        }
        public bool InControlRange(MapObject other)
        {
            return island.InControlRange(other);
        }

        public override Location GetLocation()
        {
            return Location;
        }
        #endregion Extends
        #region Custom
        public City ClosestCity
        {
            get
            {
                return Bot.Engine.MyCities.OrderBy(c => c.Distance(this)).First();
            }
        }
        #endregion Custom
    }
}
