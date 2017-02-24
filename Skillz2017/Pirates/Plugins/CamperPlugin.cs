using Pirates;
using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class CamperPlugin : PiratePlugin
    {
        MapObject Camp;
        ShootingPlugin shooter;
        int range;
        public CamperPlugin(MapObject loc, ShootingPlugin shooter, int range = 13)
        {
            this.Camp = loc;
            this.shooter = shooter;
            this.range = range;
        }

        public bool DoTurn(PirateShip ship)
        {
            AircraftBase craft;
            if (ship.InRange(Camp, range))
            {
                if (shooter.Scan(Camp, range, out craft))
                {
                    if (ship.InAttackRange(craft)) ship.Attack(craft.aircraft);
                    else ship.Sail(craft, ship.SailMaximizeDrone);
                    return true;
                }
                else if (!ship.Location.Equals(Camp))
                {
                    ship.Sail(Camp, ship.SailDefault);
                    return true;
                }
                else return false;
            }
            else
            {
                ship.Sail(Camp, ship.SailMaximizeDrone);
                return true;
            }
        }
    }
}
