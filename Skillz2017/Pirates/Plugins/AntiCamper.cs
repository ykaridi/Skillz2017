using Pirates;
using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class AntiCamper : PiratePlugin
    {
        public bool DoTurn(PirateShip ship)
        {
            PirateShip c;
            if (Bot.Engine.CheckForCamper(ship.NearestCity, out c))
            {
                if (ship.InAttackRange(c)) ship.Attack(c);
                else ship.RandomSail(c);
                return true;
            }
            else return false;
        }
    }
}
