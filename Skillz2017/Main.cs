using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading.Tasks;using Pirates;namespace Skillz2017{    public class Bot : IPirateBot    {        public void DoTurn(PirateGame game)        {            try            {

            }            catch (Exception e)            {                game.Debug(e.StackTrace);            }        }    }    class GameEngine    {        public Dictionary<int, int> HitList = new Dictionary<int, int>();        public readonly Random random;        protected readonly PirateGame game;
        public GameEngine(PirateGame game)
        {
            this.game = game;
        }

        #region Extends
        public Player Self
        {
            get
            {
                return game.GetMyself();
            }
        }
        public Player Enemy
        {
            get
            {
                return game.GetEnemy();
            }
        }
        public Player Neutral
        {
            get
            {
                return game.GetNeutral();
            }
        }
        public void Debug(params object[] messages)
        {
            game.Debug(messages);
        }
        public void Debug(string message)
        {
            game.Debug(message);
        }
        public AircraftList<Aircraft> GetAircraftsOn(MapObject mapObject)
        {
            return new AircraftList<Aircraft>(this, game.GetAircraftsOn(mapObject));
        }
        public City[] Cities
        {
            get
            {
                return game.GetAllCities().ToArray();
            }
        }
        public City[] MyCities
        {
            get
            {
                return game.GetMyCities().ToArray();
            }
        }
        public City[] EnemyCities
        {
            get
            {
                return game.GetEnemyCities().ToArray();
            }
        }
        public AircraftList<Pirate> EnemyPirates
        {
            get
            {
                return new AircraftList<Pirate>(this, game.GetEnemy().AllPirates);
            }
        }
        public AircraftList<Pirate> EnemyLivingPirates
        {
            get
            {
                return new AircraftList<Pirate>(this, game.GetEnemyLivingPirates().Where(x => IsAlive(x)));
            }
        }
        public SmartIsland[] Islands
        {
            get
            {
                return game.GetAllIslands().Select(x => new SmartIsland(x, this)).ToArray();
            }
        }
        public SmartIsland[] EnemyIslands
        {
            get
            {
                return game.GetEnemyIslands().Select(x => new SmartIsland(x, this)).ToArray();
            }
        }
        public SmartIsland[] MyIslands
        {
            get
            {
                return game.GetMyIslands().Select(x => new SmartIsland(x, this)).ToArray();
            }
        }
        public SmartIsland[] NeutralIslands
        {
            get
            {
                return game.GetNeutralIslands().Select(x => new SmartIsland(x, this)).ToArray();
            }
        }
        public SmartIsland[] NotMyIslands
        {
            get
            {
                return game.GetNotMyIslands().Select(x => new SmartIsland(x, this)).ToArray();
            }
        }
        public AircraftList<Pirate> MyPirates
        {
            get
            {
                return new AircraftList<Pirate>(this, game.GetAllMyPirates());
            }
        }
        public AircraftList<Pirate> MyLivingPirates
        {
            get
            {
                return new AircraftList<Pirate>(this, game.GetMyLivingPirates());
            }
        }
        public int AttackRange
        {
            get
            {
                return game.GetAttackRange();
            }
        }
        public int Columns
        {
            get
            {
                return game.GetColCount();
            }
        }
        public int ControlRange
        {
            get
            {
                return game.GetControlRange();
            }
        }
        public int DroneMaxHealth
        {
            get
            {
                return game.GetDroneMaxHealth();
            }
        }
        public int DroneMaxSpeed
        {
            get
            {
                return game.GetDroneMaxSpeed();
            }
        }

        public TradeShip GetEnemyDroneById(int id)
        {
            return new TradeShip(this, game.GetEnemyDroneById(id));
        }
        public AircraftList<Aircraft> EnemyLivingAircrafts
        {
            get
            {
                return new AircraftList<Aircraft>(this, game.GetEnemyLivingAircrafts().Where(x => IsAlive(x)));
            }
        }
        public AircraftList<Drone> EnemyLivingDrones
        {
            get
            {
                return new AircraftList<Drone>(this, game.GetEnemyLivingDrones().Where(x => IsAlive(x)));
            }
        }
        public PirateShip GetEnemyPirateById(int id)
        {
            return new PirateShip(this, game.GetEnemyPirateById(id));
        }
        public int EnemyScore
        {
            get
            {
                return game.GetEnemyScore();
            }
        }
        public int MyScore
        {
            get
            {
                return game.GetMyScore();
            }
        }
        public int MaxDroneCreationTurns
        {
            get
            {
                return game.GetMaxDroneCreationTurns();
            }
        }
        public int MaxDronesCount
        {
            get
            {
                return game.GetMaxDronesCount();
            }
        }
        public int MaxPoints
        {
            get
            {
                return game.GetMaxPoints();
            }
        }
        public int MaxTurns
        {
            get
            {
                return game.GetMaxTurns();
            }
        }
        public TradeShip GetMyDroneById(int id)
        {
            return new TradeShip(this, game.GetMyDroneById(id));
        }
        public AircraftList<Aircraft> MyLivingAircrafts
        {
            get
            {
                return new AircraftList<Aircraft>(this, game.GetMyLivingAircrafts());
            }
        }
        public PirateShip GetMyPirateById(int id)
        {
            return new PirateShip(this, game.GetMyPirateById(id));
        }
        public int PirateMaxHealth
        {
            get
            {
                return game.GetPirateMaxHealth();
            }
        }
        public int PirateMaxSpeed
        {
            get
            {
                return game.GetPirateMaxSpeed();
            }
        }
        public int Rows
        {
            get
            {
                return game.GetRowCount();
            }
        }
        public List<Location> GetSailOptions(Aircraft aircraft, MapObject destination)
        {
            return game.GetSailOptions(aircraft, destination);
        }
        public List<Location> GetSailOptions<T>(AircraftBase<T> aircraft, MapObject destination) where T : Aircraft
        {
            return game.GetSailOptions(aircraft.aircraft, destination);
        }
        public int SpawnTurns
        {
            get
            {
                return game.GetSpawnTurns();
            }
        }
        public int Turn
        {
            get
            {
                return game.GetTurn();
            }
        }
        public int UnloadRange
        {
            get
            {
                return game.GetUnloadRange();
            }
        }
        public void SetSail(Aircraft aircraft, MapObject destination)
        {
            game.SetSail(aircraft, destination);
        }
        public void SetSail<T>(AircraftBase<T> aircraft, MapObject destination) where T : Aircraft
        {
            SetSail(aircraft.aircraft, destination);
        }
        public void Sail(Aircraft aircraft, MapObject destination, int idx = 0)
        {
            List<Location> locs = GetSailOptions(aircraft, destination);
            if (idx >= locs.Count)
                idx = 0;
            SetSail(aircraft, locs[idx]);
        }
        public void Sail<T>(AircraftBase<T> aircraft, MapObject destination, int idx = 0) where T : Aircraft
        {
            Sail(aircraft.aircraft, destination, idx);
        }
        public void RandomSail(Aircraft aircraft, MapObject destination)
        {
            List<Location> locs = GetSailOptions(aircraft, destination);
            SetSail(aircraft, locs[random.Next(locs.Count)]);
        }
        public void RandomSail<T>(AircraftBase<T> aircraft, MapObject destination) where T : Aircraft
        {
            RandomSail(aircraft.aircraft, destination);
        }

        public void Sail(Aircraft aircraft, MapObject destination, Func<Location, double> ScoreFunction, bool OrderByAscending = true)
        {
            List<Location> locs = GetSailOptions(aircraft, destination);
            Location l;
            if (OrderByAscending)
                l = locs.OrderBy(ScoreFunction).First();
            else
                l = locs.OrderByDescending(ScoreFunction).First();
            SetSail(aircraft, l);
        }
        public void Sail<T>(AircraftBase<T> aircraft, MapObject destination, Func<Location, double> ScoreFunction, bool OrderByAscending = true) where T : Aircraft
        {
            Sail(aircraft.aircraft, destination, ScoreFunction, OrderByAscending);
        }




        #endregion Extends
        #region Custom         private void AppendToHitlist(Aircraft aircraft)
        {
            HitList[aircraft.Id] = GetHits(aircraft) + 1;
        }
        private int GetHits(Aircraft aircraft)
        {
            int value = 0;
            if (HitList.TryGetValue(aircraft.Id, out value))
                return value;
            else
            {
                HitList.Add(aircraft.Id, 0);
                return 0;
            }
        }
        private int CheckHealth(Aircraft aircraft)
        {
            return aircraft.CurrentHealth - GetHits(aircraft);
        }
        private bool IsAlive(Aircraft aircraft)
        {
            return CheckHealth(aircraft) > 0;
        }
        public bool Attack(Pirate pirate, Aircraft target)
        {
            if (IsAlive(target))
            {
                AppendToHitlist(target);
                game.Attack(pirate, target);
                return true;
            }
            else return false;
        }        public AircraftList<Drone> GetDronesInAttackRange(Location loc)
        {
            return new AircraftList<Drone>(this, EnemyLivingDrones.Where(x => x.InRange(loc, AttackRange)));
        }        public AircraftList<Pirate> GetEnemyShipsInAttackRange(Location loc)
        {
            return new AircraftList<Pirate>(this, EnemyLivingPirates.Where(x => x.InRange(loc, AttackRange)));
        }        public Action<Aircraft, Location> MaximizeDroneSail()
        {
            return new Action<Aircraft, Location>((ac, l) =>
             {
                 Sail(ac, l, loc => GetDronesInAttackRange(loc).Count);
             });
        }
        public Action<Aircraft, Location> CountEnemyShips()
        {
            return new Action<Aircraft, Location>((ac, l) =>
            {
                Sail(ac, l, loc => GetEnemyShipsInAttackRange(loc).Count, false);
            });
        }
        public Action<Aircraft, Location> SailDefault()
        {
            return new Action<Aircraft, Location>((ac, l) =>
            {
                Sail(ac, l);
            });
        }
        public Action<Aircraft, Location> SailRandom()
        {
            return new Action<Aircraft, Location>((ac, l) =>
            {
                Sail(ac, l);
            });
        }
        #endregion Custom    }

    #region Aircrafts    class SmartIsland    {        Island island;        GameEngine engine;        public SmartIsland(Island island, GameEngine engine)        {            this.island = island;            this.engine = engine;        }


        #region Extends        public int Id        {            get            {                return island.Id;            }        }        public Location Location        {            get            {                return island.Location;            }        }        public Player Owner        {            get            {                return island.Owner;            }        }        public int TurnsToDroneCreation        {            get            {                return island.TurnsToDroneCreation;            }        }        public int ControlRange        {            get            {                return island.ControlRange;            }        }        public bool InControlRange(MapObject other)        {            return island.InControlRange(other);        }        public int Distance(MapObject other)        {            return island.Distance(other);        }        public bool IsOurs        {            get            {                return Owner.Id == engine.Self.Id;            }        }        public bool IsTheirs        {            get            {                return Owner.Id == engine.Enemy.Id;            }        }
        public bool IsNeutral
        {
            get
            {
                return Owner.Id == engine.Neutral.Id;
            }
        }
        #endregion Extends    }
    public enum AircraftType
    {
        Drone, Pirate, Generic
    }    class AircraftBase<T> : MapObject where T : Aircraft
    {
        protected GameEngine engine;
        public readonly T aircraft;
        public AircraftBase(GameEngine engine, T aircraft)
        {
            this.engine = engine;
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
                return Utils.DetermineType(aircraft.Type);
            }
        }
        public int CurrentHealth
        {
            get
            {
                return aircraft.CurrentHealth;
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
        public void Sail(Location loc, int idx = 0)
        {
            engine.Sail(this, loc, idx);
        }
        public void RandomSail(Location loc)
        {
            engine.RandomSail(this, loc);
        }
        public void Sail(Location loc, Func<Location, Double> ScoreFunction, bool OrderByAscending = true)
        {
            engine.Sail(this, loc, ScoreFunction, OrderByAscending);
        }

        public override Location GetLocation()
        {
            return Location;
        }
        #endregion Custom
    }
    class GenericAircraft : AircraftBase<Aircraft>
    {
        public GenericAircraft(GameEngine engine, Aircraft aircraft) : base(engine, aircraft) { }
    }
    class PirateShip : AircraftBase<Pirate>
    {
        public PirateShip(GameEngine engine, Pirate pirate) : base(engine, pirate) { }

        #region Extends
        public int AttackRange
        {
            get
            {
                return aircraft.AttackRange;
            }
        }
        public int TurnsToRevive
        {
            get
            {
                return aircraft.TurnsToRevive;
            }
        }
        public bool Alive
        {
            get
            {
                return aircraft.IsAlive();
            }
        }
        public bool InAttackRange(MapObject mapObject)
        {
            return aircraft.InAttackRange(mapObject);
        }
        #endregion Custom
        #region Custom
        public bool Attack(Aircraft aircraft)
        {
            if (!InAttackRange(aircraft))
                return false;
            engine.Attack(this.aircraft, aircraft);
            return true;
        }
        public bool Attack<T>(AircraftBase<T> aircraft) where T : Aircraft
        {
            return Attack(aircraft.aircraft);
        }
        public bool TryAttack(Func<AircraftBase<Aircraft>, double> ScoringFunction)
        {
            List<AircraftBase<Aircraft>> options = GetAircraftsInAttackRange().OrderBy(x => ScoringFunction(x)).ToList();
            if (options.Count > 0)
            {
                AircraftBase<Aircraft> f = options.First();
                if (ScoringFunction(f) < 0)
                    return false;
                return Attack(options.First());
            }
            else
                return false;
        }
        public bool TryAttackAnything(bool DronesFirst = true)
        {
            if (DronesFirst) return TryAttack(x =>
            {
                if (x.Type == AircraftType.Drone) return (engine.DroneMaxHealth - x.CurrentHealth) * engine.GetMyDroneById(aircraft.Id).Value * 10;
                else return engine.PirateMaxHealth - x.CurrentHealth;
            }); else return TryAttack(x =>
            {
                if (x.Type == AircraftType.Drone) return (engine.PirateMaxHealth - x.CurrentHealth) * 10;
                else return engine.DroneMaxHealth - x.CurrentHealth;
            });
        }
        public List<AircraftBase<Aircraft>> GetAircraftsInAttackRange()
        {
            return engine.EnemyLivingAircrafts.Where(x => this.InAttackRange(x)).ToList();
        }
        #endregion Custom
    }    class TradeShip : AircraftBase<Drone>
    {
        public TradeShip(GameEngine engine, Drone drone) : base(engine, drone) { }

        #region Extends
        public int Value
        {
            get
            {
                return aircraft.Value;
            }
        }
        #endregion Extends
        #region Custom 
        public void SailToCity(Action<Aircraft, Location> SailFunction)
        {
            SailFunction(this.aircraft, engine.MyCities.OrderBy(x => x.Distance(this)).First().Location);
        }
        #endregion Custom
    }
    class AircraftList<T> : List<AircraftBase<T>> where T : Aircraft
    {
        GameEngine engine;
        public AircraftList(GameEngine engine, IEnumerable<T> aircrafts) : base()
        {
            this.engine = engine;
            this.AddRange(aircrafts.Select(x => new AircraftBase<T>(engine, x)));
        }
        public AircraftList(GameEngine engine, IEnumerable<AircraftBase<T>> aircrafts) : base()
        {
            this.AddRange(aircrafts);
        }
    }
    #endregion Aircrafts
    #region Utils    class Utils    {        public static bool locationsEqual(Location loc1, Location loc2)        {            return (loc1.Row == loc2.Row) && (loc1.Col == loc2.Col);        }        public static bool isBetween(Location loc, Location bound1, Location bound2)        {            return isBetween(loc.Row, bound1.Row, bound2.Row) && isBetween(loc.Col, bound1.Col, bound2.Col);        }        public static bool isBetween(int num, int bound1, int bound2)        {            if (bound1 > bound2)                return num >= bound2 && num <= bound1;            else                return num >= bound1 && num <= bound2;        }

        public static AircraftType DetermineType(string type)
        {
            if (type.Equals("Drone"))
                return AircraftType.Drone;
            else if (type.Equals("Pirate"))
                return AircraftType.Pirate;
            else
                return AircraftType.Generic;
        }    }    class PrioritizedActionList<T>    {        ConditioninalAction<T>[] actions;        public PrioritizedActionList(ConditioninalAction<T>[] actions)        {            this.actions = actions;        }        public bool invoke(T obj)        {            for (int i = 0; i < actions.Length; i++)            {                if (actions[i].invoke(obj))                    return true;            }            return false;        }    }    class ConditioninalAction<T>    {        private bool state;        private Func<T, bool> condition;        private Func<T, bool> action;        public ConditioninalAction(bool state, Func<T, bool> condition, Func<T, bool> action)        {            this.state = state;            this.condition = condition;            this.action = action;        }        public bool invoke(T obj)        {            if (condition.Invoke(obj) && state)            {                action(obj);                return true;            }            return false;        }        public bool invokeOnArray(T[] arr)        {            if (state)            {                foreach (T c in arr.Where(x => condition.Invoke(x)))                {                    if (action(c))                        return true;                }            }            return false;        }    }
    #endregion Utils}