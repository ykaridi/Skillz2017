using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class KillerPlugin : PiratePlugin
    {
        System.Func<AircraftBase> TargetFunc;
        public KillerPlugin(System.Func<AircraftBase> TargetFunc)
        {
            this.TargetFunc = TargetFunc;
        }

        public bool DoTurn(PirateShip ship)
        {
            AircraftBase aircraft = TargetFunc();
            if (aircraft == null || !aircraft.IsAlive) return false;
            if (ship.InAttackRange(aircraft))
            {
                ship.Attack(aircraft.aircraft);
                if (aircraft.CurrentHealth > 0) ship.ReserveLogic(new EmptyPirate().AttachPlugin(this));
            }
            else ship.RandomSail(aircraft.Location);
            return true;
        }
    }
}
