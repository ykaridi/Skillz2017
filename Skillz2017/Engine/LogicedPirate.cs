using MyBot.Pirates.Plugins;

namespace MyBot.Engine
{
    class LogicedPirate
    {
        public readonly PirateShip s;
        public readonly PirateLogic logic;
        public LogicedPirate(PirateShip s, PirateLogic logic)
        {
            this.s = s;
            this.logic = logic;
        }

        public void DoTurn()
        {
            logic.DoTurn(s);
        }
    }
    abstract class PirateLogic
    {
        public PiratePlugin[] Plugins
        {
            get; set;
        } = new PiratePlugin[0];
        public abstract void DoTurn(PirateShip pirate);
    }
}
