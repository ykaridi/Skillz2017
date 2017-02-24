using Pirates;
using System.Linq;
using System.Collections.Generic;
using MyBot.Engine;

namespace MyBot.Pirates.Plugins
{
    class ShootingPlugin : PiratePlugin
    {
        Delegates.AircraftScoringFunction ScoringFunction;
        Delegates.AircraftFilterFunction DFilter;
        public ShootingPlugin(Delegates.AircraftScoringFunction ScoringMehtod, bool AntiSuicide = false)
        {
            this.ScoringFunction = ScoringMehtod;
            if (AntiSuicide)
            {
                DFilter = (ab, l) =>
                {
                    if (ab.Type == AircraftType.Drone) return true;
                    List<PirateShip> e = Bot.Engine.GetEnemyShipsInAttackRange(l).ToList();
                    int EH = e.Sum(x => x.CurrentHealth);
                    int EHP = e.Count;
                    List<PirateShip> f = Bot.Engine.MyLivingPirates.Filter(x => x.InAttackRange(l)).ToList();
                    int HH = f.Sum(x => x.CurrentHealth);
                    int HHP = f.Count;

                    return EH / HHP < HH / HHP;
                };
            }
            else DFilter = (ab, l) => { return true; };
        }
        public ShootingPlugin() : this(PirateShip.ShootRegular) { }

        public bool DoTurn(PirateShip ship)
        {
            AircraftBase aircraft;
            if (Scan(ship, ship.AttackRange, out aircraft) && ship.Attack(aircraft.aircraft))
                return true;
            else
                return false;
        }
        public bool Scan(MapObject loc, int range, Delegates.AircraftFilterFunction Filter, out AircraftBase aircraft, bool OrderByDescending = true)
        {
            aircraft = null;

            List<AircraftBase> options = Bot.Engine.GetEnemyAircraftsInRange(loc, range).Filter(x => Filter(x, loc));
            if (OrderByDescending)
                options = options.OrderByDescending(x => ScoringFunction(x)).ToList();
            else
                options = options.OrderBy(x => ScoringFunction(x)).ToList();
            if (options.Count > 0)
            {
                aircraft = options.First();
                if (ScoringFunction(aircraft) <= 0)
                    return false;
                return true;
            }
            else
                return false;
        }
        public bool Scan(MapObject loc, int range, out AircraftBase aircraft, bool OrderByDescending = true)
        {
            return Scan(loc, range, DFilter, out aircraft, OrderByDescending);
        }

        public ShootingPlugin DronesOnly()
        {
            return new ShootingPlugin(ScoringFunction.Times(PirateShip.ShootDronesOnly));
        }
        public ShootingPlugin PiratesOnly()
        {
            return new ShootingPlugin(ScoringFunction.Times(PirateShip.ShootPiratesOnly));
        }
        public ShootingPlugin PrioritizeByValue(double ShipValue = 0.5)
        {
            return new ShootingPlugin(ScoringFunction.Times(x =>
            {
                if (x.Type == AircraftType.Drone) return x.AsDrone().Value;
                else if (x.Type == AircraftType.Pirate) return ShipValue;
                else return 0;
            }));
        }

        public static ShootingPlugin PrioritizeByHealth()
        {
            return new ShootingPlugin(PirateShip.ShootByCurrentHealth);
        }
        public static ShootingPlugin PrioritizeByDamageTaken()
        {
            return new ShootingPlugin(PirateShip.ShootByDamageTaken);
        }
        public static ShootingPlugin PrioritizeByNearnessToDeath()
        {
            return new ShootingPlugin(PirateShip.ShootByNearnessToDeath);
        }
    }
}
