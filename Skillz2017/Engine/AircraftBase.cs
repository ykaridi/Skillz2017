using Pirates;
using System.Linq;

namespace MyBot.Engine
{
    public enum AircraftType
    {
        Drone, Pirate, Generic
    }
    class AircraftBase : MapObject
    {
        #region Sailing Functions
        public delegate void SailingFunction(Aircraft ac, Location l);
        public SailingFunction SailMaximizeDrone
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.Sail(ac, l, loc => Bot.Engine.GetEnemyDronesInAttackRange(loc).Count, false);
                });
            }
        }
        public SailingFunction SailMinimizeDroneDistance
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.Sail(ac, l, loc => Bot.Engine.EnemyLivingDrones.Select(p => System.Math.Pow(p.Distance(loc), 0.5)).Sum(), true);
                });
            }
        }
        public SailingFunction SailMinimizeShips
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.Sail(ac, l, loc => Bot.Engine.GetEnemyShipsInRange(loc, 7).Count, true);
                });
            }
        }
        public SailingFunction SailMaximizeShipDistance
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.Sail(ac, l, loc => Bot.Engine.EnemyLivingPirates.Select(p => System.Math.Pow(p.Distance(loc), 2)).Sum(), false);
                });
            }
        }
        public SailingFunction SailMaximizeMinimalDistance
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.Sail(ac, l, loc => Bot.Engine.EnemyLivingPirates.Select(p => System.Math.Pow(p.Distance(loc), 2)).Min(), false);
                });
            }
        }
        public SailingFunction SailMaximizeDistanceFromMiddle
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.Sail(ac, l, loc => loc.Distance(new Location(Bot.Engine.Rows / 2, Bot.Engine.Columns / 2)), false);
                });
            }
        }
        public SailingFunction SailDefault
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.Sail(ac, l);
                });
            }
        }
        public SailingFunction SailRandom
        {
            get
            {
                return ((ac, l) =>
                {
                    Bot.Engine.RandomSail(ac, l);
                });
            }
        }
        #endregion Sailing Functions
        #region Shooting Functions
        public static System.Func<AircraftBase, double> ShootDronesOnly
        {
            get
            {
                return new System.Func<AircraftBase, double>(x =>
                {
                    if (x.Type == AircraftType.Drone) return 1;
                    else return 0;
                });
            }
        }
        public static System.Func<AircraftBase, double> ShootPiratesOnly
        {
            get
            {
                return new System.Func<AircraftBase, double>(x =>
                {
                    if (x.Type == AircraftType.Pirate) return 1;
                    else return 0;
                });
            }
        }
        public static System.Func<AircraftBase, double> ShootByCurrentHealth
        {
            get
            {
                return new System.Func<AircraftBase, double>(x =>
                {
                    return x.CurrentHealth;
                });
            }
        }
        public static System.Func<AircraftBase, double> ShootByDamageTaken
        {
            get
            {
                return new System.Func<AircraftBase, double>(x =>
                {
                    return x.MaxHealth - x.CurrentHealth;
                });
            }
        }
        public static System.Func<AircraftBase, double> ShootRegular
        {
            get
            {
                return new System.Func<AircraftBase, double>(x =>
                {
                    return 1;
                });
            }
        }
        public static System.Func<AircraftBase, double> ShootByNearnessToDeath
        {
            get
            {
                return new System.Func<AircraftBase, double>(x =>
                {
                    return ((double)(x.MaxHealth - x.CurrentHealth)) / x.MaxHealth + 1;
                });
            }
        }
        #endregion Shooting Functions
        #region Scanning Functions
        public Squad<TradeShip> GetEnemyDronesInRange(int range)
        {
            return Bot.Engine.GetEnemyDronesInRange(this, range);
        }
        public Squad<TradeShip> GetEnemyDronesInAttackRange()
        {
            return Bot.Engine.GetEnemyDronesInAttackRange(this);
        }
        public Squad<PirateShip> GetEnemyShipsInRange(int range)
        {
            return Bot.Engine.GetEnemyShipsInRange(this, range);
        }
        public Squad<PirateShip> GetEnemyShipsInAttackRange()
        {
            return Bot.Engine.GetEnemyShipsInAttackRange(this);
        }
        public Squad<AircraftBase> GetEnemyAircraftsInRange(int range)
        {
            return Bot.Engine.GetEnemyAircraftsInRange(this, range);
        }
        #endregion Scanning Functions

        public Aircraft aircraft;
        public AircraftBase(Aircraft aircraft)
        {
            this.aircraft = aircraft;
        }

        #region Extends
        public int Id
        {
            get
            {
                return aircraft.Id;
            }
        }
        public Location Location
        {
            get
            {
                return aircraft.Location;
            }
        }
        public bool IsOurs
        {
            get
            {
                return Owner.Id == Bot.Engine.Self.Id;
            }
        }
        public Player Owner
        {
            get
            {
                return aircraft.Owner;
            }
        }
        public AircraftType Type
        {
            get
            {
                return aircraft.DetermineType();
            }
        }
        public int CurrentHealth
        {
            get
            {
                return Bot.Engine.CheckHealth(aircraft);
            }
        }
        public bool IsAlive
        {
            get
            {
                return CurrentHealth > 0;
            }
        }
        public int MaxHealth
        {
            get
            {
                if (Type == AircraftType.Drone)
                    return Bot.Engine.DroneMaxHealth;
                else if (Type == AircraftType.Pirate)
                    return Bot.Engine.PirateMaxHealth;
                else
                    return 0;
            }
        }
        public Location InitialLocation
        {
            get
            {
                return aircraft.InitialLocation;
            }
        }
        public int MaxSpeed
        {
            get
            {
                return aircraft.MaxSpeed;
            }
        }
        #endregion Extends
        #region Custom
        public bool CanPlay
        {
            get
            {
                return Bot.Engine.CanPlay(aircraft);
            }
        }

        public void Sail(MapObject loc, int idx = 0)
        {
            Bot.Engine.Sail(this, loc, idx);
        }
        public void RandomSail(MapObject loc)
        {
            Bot.Engine.RandomSail(this, loc);
        }
        public void Sail(MapObject loc, System.Func<Location, double> ScoreFunction, bool OrderByAscending = true)
        {
            Bot.Engine.Sail(this, loc, ScoreFunction, OrderByAscending);
        }
        public void Sail(MapObject loc, SailingFunction SailFunction)
        {
            SailFunction(aircraft, loc.GetLocation());
        }
        public override Location GetLocation()
        {
            return Location;
        }
        public PirateShip AsPirate()
        {
            if (IsOurs)
                return Bot.Engine.GetMyPirateById(aircraft.Id);
            else
                return Bot.Engine.GetEnemyPirateById(aircraft.Id);
        }
        public TradeShip AsDrone()
        {
            if (IsOurs)
                return Bot.Engine.GetMyDroneById(aircraft.Id);
            else
                return Bot.Engine.GetEnemyDroneById(aircraft.Id);
        }
        #endregion Custom
    }
}
