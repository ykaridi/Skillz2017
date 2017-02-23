using System.Linq;

namespace MyBot.Engine
{
    class LogicedDroneSquad
    {
        public delegate void Logic();

        public readonly DroneSquad s;
        public readonly LogicedDrone[] lds;
        public readonly Logic logic;
        public LogicedDroneSquad(DroneSquad s, DroneSquadLogic logic)
        {
            this.s = s;
            this.logic = () => logic.DoTurn(s);
        }
        public LogicedDroneSquad(LogicedDrone[] drones)
        {
            lds = drones;
            s = new DroneSquad(drones.Select(x => x.s));
            logic = () => lds.ToList().ForEach(x => x.DoTurn());
        }

        public void DoTurn()
        {
            logic();
        }
    }
    interface DroneSquadLogic
    {
        void DoTurn(DroneSquad ds);
    }
}
