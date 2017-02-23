using Pirates;
using MyBot.Engine;

namespace MyBot.Drones
{
    class AvoidingDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailMaximizeShipDistance);
        }
    }
}
