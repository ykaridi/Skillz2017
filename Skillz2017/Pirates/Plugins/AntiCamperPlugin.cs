using Pirates;
using System.Collections.Generic;
using System.Linq;
using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class AntiCamperPlugin : PiratePlugin
    {
        int minTurns;

        public AntiCamperPlugin(int minTurns = 3)
        {
            this.minTurns = minTurns;
        }

        public bool DoTurn(PirateShip ship)
        {
            PirateShip temp;
            List<City> cities = Bot.Engine.MyCities.Where(c => Bot.Engine.CheckForCamper(c, out temp, minTurns)).OrderBy(x => x.Distance(ship)).ToList();
            if (cities.Count > 0)
            {
                City c = cities.First();
                if (Bot.Engine.CheckForCamper(c, out temp, minTurns))
                {
                    int id = temp.Id;
                    KillerPlugin killer = new KillerPlugin(() => Bot.Engine.GetEnemyPirateById(id));
                    return killer.DoTurn(ship);
                }
                else ship.Sail(c, ship.SailMaximizeDrone);
                return true;
            }
            else
                return false;
        }
    }
}
