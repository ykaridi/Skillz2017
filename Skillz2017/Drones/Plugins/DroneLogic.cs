using Pirates;
using MyBot.Engine;

namespace MyBot.Drones.Plugins
{
    interface DronePlugin
    {
        bool DoTurn(TradeShip ship, City city);
    }
}
