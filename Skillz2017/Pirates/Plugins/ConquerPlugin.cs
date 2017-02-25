using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class ConquerPlugin : PiratePlugin
    {
        SmartIsland island;
        bool InRangeOnly;
        public ConquerPlugin(SmartIsland island, bool InRangeOnly = false)
        {
            this.island = island;
            this.InRangeOnly = InRangeOnly;
        }

        public bool DoTurn(PirateShip ship)
        {
            if (ship.Distance(island) == 0 || (!InRangeOnly && island.InControlRange(ship))) return false;
            else
                ship.Sail(island.Location, ship.SailMaximizeDrone);
            return true;
        }
    }
}
