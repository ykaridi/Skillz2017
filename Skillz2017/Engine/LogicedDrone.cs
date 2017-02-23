using Pirates;
using MyBot.Drones.Plugins;

namespace MyBot.Engine
{
    class LogicedDrone
    {
        public readonly TradeShip s;
        public readonly DroneLogic logic;
        public LogicedDrone(TradeShip s, DroneLogic logic)
        {
            this.s = s;
            this.logic = logic;
        }

        public void DoTurn()
        {
            logic.DoTurnWithPlugins(s);
        }
    }
    abstract class DroneLogic
    {
        public DronePlugin[] Plugins
        {
            get; set;
        } = new DronePlugin[0];

        public abstract City CalculateDepositCity(TradeShip ship);
        public abstract void Sail(TradeShip ship, City city);
    }
}
