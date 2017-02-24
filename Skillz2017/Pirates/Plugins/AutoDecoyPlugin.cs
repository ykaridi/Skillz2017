using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class AutoDecoyPlugin : PiratePlugin
    {
        Delegates.ShouldDecoyFunction Condition;

        public AutoDecoyPlugin()
        {
            Condition = (s) => true;
        }
        public AutoDecoyPlugin(Delegates.ShouldDecoyFunction Condition)
        {
            this.Condition = Condition;
        }

        public bool DoTurn(PirateShip ship)
        {
            if (Condition(ship) && ship.Decoy()) return true;
            return false;
        }
    }
}
