using Pirates;
using MyBot.Engine;

namespace MyBot.Drones
{
    class RandomDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailRandom);
        }
    }
}
