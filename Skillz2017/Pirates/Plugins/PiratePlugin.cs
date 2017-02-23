using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    interface PiratePlugin
    {
        bool DoTurn(PirateShip ship);
    }
}
