using Pirates;
using MyBot.Engine;

namespace MyBot.Drones
{
    class StupidDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailDefault);
        }
    }
}
