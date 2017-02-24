using Pirates;
using System.Collections.Generic;
using System.Linq;
using MyBot.Engine;

namespace MyBot.Drones
{
    class AvoidingDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailMaximizeShipDistance);
            return;

            List<Location> pl = Bot.Engine.GetAllSailOptions(ship, ship.NearestCity, false);

            IGrouping<int, Location> g = pl.Where(x => Bot.Engine.EnemyPirates.TrueForAll(y => !y.InRange(x, Bot.Engine.AttackRange + 1))).GroupBy(x => x.Distance(city)).OrderBy(x => x.Key).FirstOrDefault();
            ship.Sail(g.OrderByDescending(x => Bot.Engine.EnemyPirates.Select(y => x.Distance(y).Power(0.5)).Sum()).First());
        }
    }
}
