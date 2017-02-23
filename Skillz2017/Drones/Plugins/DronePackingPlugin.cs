using Pirates;
using MyBot.Engine;
using System.Linq;

namespace MyBot.Drones.Plugins
{
    class DronePackingPlugin : DronePlugin
    {
        int size;
        int fleeDistance;
        public DronePackingPlugin(int size = 5, int fleeDistance = 5)
        {
            this.size = size;
            this.fleeDistance = fleeDistance;
        }

        public bool DoTurn(TradeShip ship, City city)
        {
            if (!ship.Location.Equals(ship.InitialLocation)) return false;
            if (ship.GetEnemyShipsInRange(fleeDistance).Count > 0) return false;
            if (Bot.Engine.GetAircraftsOn(ship).Where(x => x.IsOurs && x.Type == AircraftType.Drone).Count() >= size) return false;
            if (Bot.Engine.MyLivingDrones.Count >= Bot.Engine.MaxDronesCount) return false;
            else return true;
        }
    }
}
