using Pirates;
using System.Collections.Generic;
using System.Linq;
using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class DroneHunter : PiratePlugin
    {
        ShootingPlugin shooter;
        int range;
        public DroneHunter(ShootingPlugin shooter, int range)
        {
            this.shooter = shooter;
            this.range = range;
        }
        public bool DoTurn(PirateShip ship)
        {
            List<TradeShip> ds = ship.GetEnemyDronesInRange(range).OrderBy(x => x.Distance(ship)).ToList();
            if (ds.IsEmpty()) return false;
            TradeShip ts = ds.First();
            if (ship.InAttackRange(ts))
            {
                ship.Attack(ts);
                return true;
            }
            else
            {
                ship.Sail(ts);
                return true;
            }
        }
    }
}
