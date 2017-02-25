using Pirates;
using System.Collections.Generic;
using System.Linq;
using MyBot.Engine;

namespace MyBot.Drones.Plugins
{
    class DroneGatherPlugin : DronePlugin
    {
        int minSize;
        int maxDistance;
        int maxSize;
        int fleeRange;
        public DroneGatherPlugin(int minSize = 3, int maxDistance = 7, int maxSize = 10, int fleeRange = 4)
        {
            this.minSize = minSize;
            this.maxDistance = maxDistance;
            this.maxSize = maxSize;
            this.fleeRange = fleeRange;
        }

        public bool DoTurn(TradeShip ship, City city)
        {
            object tmp;
            if (Bot.Engine.store.TryGetValue("<Flush>" + ship.Location.ToString(), out tmp))
                return (bool)tmp;

            int rd = System.Math.Abs(ship.Location.Row - city.Location.Row) + 1;
            int cd = System.Math.Abs(ship.Location.Col - city.Location.Col) + 1;

            int mr = System.Math.Min(ship.Location.Row, city.Location.Row);
            int mc = System.Math.Min(ship.Location.Col, city.Location.Col);
            IEnumerable<Location> possibleLocs = Enumerable.Range(0, rd * cd).Select(x => new Location(mr + (x / rd), mc + (x % rd))).Where(x => {
                int c = Bot.Engine.GetAircraftsOn(x).Filter(y => y.IsOurs && y.Type == AircraftType.Drone).Count;
                return x.Distance(ship) > 0 && x.Distance(ship) < maxDistance && x.Distance(city) < ship.Distance(city) && c > 0 && c < maxSize && Bot.Engine.GetEnemyShipsInRange(x, fleeRange).IsEmpty();
            }).OrderBy(x =>
            {
                return ship.Distance(x) + x.Distance(city);
            });
            if (possibleLocs.IsEmpty())
                return false;
            Location target = possibleLocs.First();
            Squad<AircraftBase> drones = Bot.Engine.GetAircraftsOn(target).Filter(y => y.IsOurs && y.Type == AircraftType.Drone);
            if (drones.Count > 0)
            {
                Bot.Engine.GetAircraftsOn(ship).Where(x => x.IsOurs && x.Type == AircraftType.Drone).ToList().ForEach(x => x.Sail(target, ship.SailMinimizeShips));
                drones.ForEach(d =>
                {
                    d.AsDrone().ReserveLogic(new EmptyDrone());
                });
                Bot.Engine.store.SetValue("<Flush>" + ship.Location.ToString(), true);
                return true;
            }
            else
            {
                Bot.Engine.store.SetValue("<Flush>" + ship.Location.ToString(), false);
                return false;
            }
        }
    }
}
