using MyBot.Drones;
using MyBot.Engine;
using System.Collections.Generic;
using Pirates;
using System.Linq;

namespace MyBot.Pirates.Plugins
{
    class EscortPlugin : PiratePlugin
    {
        int id;
        int dis;
        ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PiratesOnly();
        public EscortPlugin(int id, int dis = -1)
        {
            this.id = id;
            this.dis = dis > 0 ? dis : Bot.Engine.PirateMaxSpeed;
        }
        public bool DoTurn(PirateShip ship)
        {
            TradeShip ts = Bot.Engine.GetMyDroneById(id);
            if (!ts.IsAlive)
            {
                IEnumerable<TradeShip> tss = Bot.Engine.MyLivingDrones.Where(x => x.Distance(ship) <= dis).OrderBy(x => x.Distance(ship));
                if (tss.IsEmpty()) return false;
                else
                {
                    ts = tss.First();
                    id = ts.Id;
                }
            }
            ship.ReserveLogic(new EmptyPirate().AttachPlugin(new EscortPlugin(id, dis)));
            if (ship.GetEnemyShipsInRange(ship.AttackRange).Count > 0 && ship.Decoy()) return true;
            if (!shooter.DoTurn(ship) && ship.Distance(ts) <= dis + ts.MaxSpeed)
            {
                List<Location> locs = Bot.Engine.GetAllSailOptions(ship, ts.NearestCity).Where(x => x.Distance(ts) <= dis + ts.MaxSpeed && x.Distance(ship) >= 1).ToList();
                ship.Sail(locs.OrderByDescending(x => x.Distance(ts)).FirstOr(ship.Location));
                return true;
            }
            return false;
        }
    }
}
