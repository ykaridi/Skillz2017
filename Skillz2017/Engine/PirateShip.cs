using Pirates;

namespace MyBot.Engine
{
    class PirateShip : AircraftBase
    {
        public readonly Pirate pirate;
        public PirateShip(Pirate pirate) : base(pirate)
        {
            this.pirate = pirate;
        }

        #region Extends
        public int AttackRange
        {
            get
            {
                return pirate.AttackRange;
            }
        }
        public int TurnsToRevive
        {
            get
            {
                return pirate.TurnsToRevive;
            }
        }
        public bool InAttackRange(MapObject mapObject)
        {
            return pirate.InAttackRange(mapObject);
        }
        public bool Decoy()
        {
            return Bot.Engine.Decoy(this);
        }
        #endregion Custom
        #region Custom
        public bool Attack(Aircraft aircraft)
        {
            if (!InAttackRange(aircraft))
                return false;
            Bot.Engine.Attack(pirate, aircraft);
            return true;
        }
        public bool Attack(AircraftBase aircraft)
        {
            return Attack(aircraft.aircraft);
        }
        public Squad<AircraftBase> GetAircraftsInAttackRange()
        {
            return GetAircraftsInRange(AttackRange);
        }
        public Squad<AircraftBase> GetAircraftsInRange(int range)
        {
            return Bot.Engine.GetEnemyAircraftsInRange(Location, range);
        }

        public void ReserveLogic(PirateLogic logic)
        {
            Bot.Engine.store.AssignLogic(this, logic);
        }
        #endregion Custom
    }
}
