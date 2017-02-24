using Pirates;
using System.Linq;
using MyBot.Engine;

namespace MyBot.Drones
{
    abstract class NearestCityDroneLogic : DroneLogic
    {
        public override City CalculateDepositCity(TradeShip ship)
        {
            return ship.NearestCity;
        }
    }
}
