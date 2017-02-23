using Pirates;
using MyBot.Engine;

namespace MyBot.Drones
{
    class FarFromMiddleDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailMaximizeDistanceFromMiddle);
        }
    }
}
