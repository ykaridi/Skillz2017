using Pirates;
using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class EscortPlugin : PiratePlugin
    {
        System.Func<TradeShip> TargetFunc;
        ShootingPlugin shooter;
        public EscortPlugin(System.Func<TradeShip> TargetFunc) : this(TargetFunc, ShootingPlugin.PrioritizeByNearnessToDeath().PiratesOnly()) { }
        public EscortPlugin(System.Func<TradeShip> TargetFunc, ShootingPlugin shooter)
        {
            this.TargetFunc = TargetFunc;
            this.shooter = shooter;
        }

        public bool DoTurn(PirateShip ship)
        {
            TradeShip ts = TargetFunc();
            if (ts == null) return false;
            if (ts.IsAlive && !ts.Location.Equals(ts.InitialLocation))
            {
                ship.Sail(TargetFunc());
                ship.ReserveLogic(new EmptyPirate().AttachPlugin(shooter).AttachPlugin(this));
            }
            return ship.IsAlive;
        }
    }
}
