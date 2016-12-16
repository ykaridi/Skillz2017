using System;using System.Collections.Generic;using System.Linq;using Pirates;namespace Skillz2017{    public class Bot : IPirateBot    {        public void DoTurn(PirateGame game)        {            try            {

            }            catch (Exception e)            {                game.Debug(e.StackTrace);            }        }    }
    #region Game Systems
    #region Basic Drones
    class StupidDrone : DroneLogic
    {
        GameEngine engine;
        public StupidDrone(GameEngine engine)
        {
            this.engine = engine;
        }
        public void DoTurn(TradeShip ship)
        {
            ship.SailToCity(engine.SailDefault);
        }
    }
    class RandomDrone : DroneLogic
    {
        GameEngine engine;
        public RandomDrone(GameEngine engine)
        {
            this.engine = engine;
        }
        public void DoTurn(TradeShip ship)
        {
            ship.SailToCity(engine.SailRandom);
        }
    }
    class AvoidingDrone : DroneLogic
    {
        GameEngine engine;
        public AvoidingDrone(GameEngine engine)
        {
            this.engine = engine;
        }
        public void DoTurn(TradeShip ship)
        {
            ship.SailToCity(engine.SailMinimizeShips);
        }
    }
    #endregion BasicDrones
    #region Pirates 
    #endregion Pirates
    #region Pirate Plugins
    class ShootingPlugin : PiratePlugin
    {
        Func<AircraftBase, double> ScoringMehtod;
        public ShootingPlugin(Func<AircraftBase, double> ScoringMehtod)
        {
            this.ScoringMehtod = ScoringMehtod;
        }
        public ShootingPlugin() : this(GameEngine.ShootRegular) { }

        public bool DoTurn(PirateShip ship)
        {
            if (ship.TryAttack(ScoringMehtod))
                return true;
            else
                return false;
        }

        public ShootingPlugin DronesOnly()
        {
            return new ShootingPlugin(ScoringMehtod.Times(GameEngine.ShootDronesOnly));
        }
        public ShootingPlugin PiratesOnly()
        {
            return new ShootingPlugin(ScoringMehtod.Times(GameEngine.ShootPiratesOnly));
        }
        public ShootingPlugin PrioritizeByValue(double ShipValue = 0.5)
        {
            return new ShootingPlugin(ScoringMehtod.Times(x =>
            {
                if (x.Type == AircraftType.Drone) return x.AsDrone().Value;
                else if (x.Type == AircraftType.Pirate) return ShipValue;
                else return 0;
            }));
        }

        public static ShootingPlugin PrioritizeByHealth()
        {
            return new ShootingPlugin(GameEngine.ShootByHealthRemaining);
        }
        public static ShootingPlugin PrioritizeByDamageTaken()
        {
            return new ShootingPlugin(GameEngine.ShootByDamageTaken);
        }
    }
    #endregion Pirate Plugins
    #endregion GameSystems
    #region Logic Interfaces
    interface IndividualPirateGameLogic
    {
        PirateLogic AssignPirateLogic(PirateShip p);
        DroneLogic AssignDroneLogic(TradeShip d);
    }
    abstract class PirateLogic
    {
        PiratePlugin[] Plugins
        {
            get;
        } = new PiratePlugin[0];
        abstract public void DoTurn(PirateShip pirate);
    }
    interface PiratePlugin
    {
        bool DoTurn(PirateShip ship);
    }
    interface PirateSquadGameLogic
    {
        PirateSquad[] AssignSquads(PirateShip[] ships);
        PirateSquadLogic AssignSquadLogic(PirateSquad psl);
        DroneLogic AssignDroneLogic(TradeShip d);
    }
    interface PirateSquadLogic
    {
        Location CalculateDestination();
        PirateLogic AssignPirateLogic(PirateShip pirate, int id);
    }
    interface DroneLogic
    {
        void DoTurn(TradeShip ship);
    }
    #endregion Logic Interfaces
    #region Game Classes    class GameEngine    {        public Dictionary<int, int> HitList = new Dictionary<int, int>();        public readonly Random random = new Random();        protected readonly PirateGame game;
        public GameEngine(PirateGame game)
        {
            this.game = game;
        }

        public void DoTurn(IndividualPirateGameLogic gl)
        {
            foreach (PirateShip pirate in MyLivingPirates)
            {
                gl.AssignPirateLogic(pirate).DoTurn(pirate);
            }
            foreach (TradeShip drone in MyLivingDrones)
            {
                gl.AssignDroneLogic(drone).DoTurn(drone);
            }
        }
        public void DoTurn(PirateSquadGameLogic gl)
        {
            foreach(PirateSquad squad in gl.AssignSquads(MyLivingPirates.Select(x => (PirateShip)x).ToArray()))
            {
                PirateSquadLogic sl = gl.AssignSquadLogic(squad);
                Location dest = sl.CalculateDestination();
                int id = 0;
                foreach(PirateShip ps in squad)
                {
                    sl.AssignPirateLogic(ps, id).DoTurn(ps);
                    id += 1;
                }
            }
            foreach (TradeShip drone in MyLivingDrones)
            {
                gl.AssignDroneLogic(drone).DoTurn(drone);
            }
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
        public Squad<AircraftBase> GetAircraftsOn(MapObject mapObject)
        {
            return game.GetAircraftsOn(mapObject).Squad(this);
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
        public Squad<PirateShip> EnemyPirates
        {
            get
            {
                return game.GetEnemy().AllPirates.Squad(this);
            }
        }
        public Squad<PirateShip> EnemyLivingPirates
        {
            get
            {
                return game.GetEnemyLivingPirates().Where(x => IsAlive(x)).Squad(this);
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
        public Squad<PirateShip> MyPirates
        {
            get
            {
                return game.GetAllMyPirates().Squad(this);
            }
        }
        public Squad<PirateShip> MyLivingPirates
        {
            get
            {
                return game.GetMyLivingPirates().Squad(this);
            }
        }
        public Squad<TradeShip> MyLivingDrones
        {
            get
            {
                return game.GetMyLivingDrones().Squad(this);
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
            return game.GetEnemyDroneById(id).Upgrade(this);
        }
        public Squad<AircraftBase> EnemyLivingAircrafts
        {
            get
            {
                return game.GetEnemyLivingAircrafts().Where(x => IsAlive(x)).Squad(this);
            }
        }
        public Squad<TradeShip> EnemyLivingDrones
        {
            get
            {
                return game.GetEnemyLivingDrones().Where(x => IsAlive(x)).Squad(this);
            }
        }
        public PirateShip GetEnemyPirateById(int id)
        {
            return game.GetEnemyPirateById(id).Upgrade(this);
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
            return game.GetMyDroneById(id).Upgrade(this);
        }
        public Squad<AircraftBase> MyLivingAircrafts
        {
            get
            {
                return game.GetMyLivingAircrafts().Squad(this);
            }
        }
        public PirateShip GetMyPirateById(int id)
        {
            return game.GetMyPirateById(id).Upgrade(this);
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
        public List<Location> GetSailOptions(AircraftBase aircraft, MapObject destination)
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
        public void SetSail(AircraftBase aircraft, MapObject destination)
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
        public void Sail(AircraftBase aircraft, MapObject destination, int idx = 0)
        {
            Sail(aircraft.aircraft, destination, idx);
        }
        public void RandomSail(Aircraft aircraft, MapObject destination)
        {
            List<Location> locs = GetSailOptions(aircraft, destination);
            SetSail(aircraft, locs[random.Next(locs.Count)]);
        }
        public void RandomSail(AircraftBase aircraft, MapObject destination)
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
        public void Sail(AircraftBase aircraft, MapObject destination, Func<Location, double> ScoreFunction, bool OrderByAscending = true)
        {
            Sail(aircraft.aircraft, destination, ScoreFunction, OrderByAscending);
        }




        #endregion Extends
        #region Custom         private int HitlistId(Aircraft aircraft)
        {
            if (aircraft.DetermineType() == AircraftType.Drone)
                return aircraft.Id + MyPirates.Count;
            else
                return aircraft.Id;
        }        private void AppendToHitlist(Aircraft aircraft)
        {
            HitList[HitlistId(aircraft)] = GetHits(aircraft) + 1;
        }
        private int GetHits(Aircraft aircraft)
        {
            int value = 0;
            if (HitList.TryGetValue(HitlistId(aircraft), out value))
                return value;
            else
            {
                HitList.Add(HitlistId(aircraft), 0);
                return 0;
            }
        }
        public int CheckHealth(Aircraft aircraft)
        {
            return aircraft.CurrentHealth - GetHits(aircraft);
        }
        public bool IsAlive(Aircraft aircraft)
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
        }        public Squad<TradeShip> GetDronesInAttackRange(Location loc)
        {
            return new Squad<TradeShip>(this, EnemyLivingDrones.Where(x => x.InRange(loc, AttackRange)));
        }        public Squad<PirateShip> GetEnemyShipsInAttackRange(Location loc)
        {
            return new Squad<PirateShip>(this, EnemyLivingPirates.Where(x => x.InRange(loc, AttackRange)));
        }
        #region Sailing Functions        public Action<Aircraft, Location> SailMaximizeDrone
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    Sail(ac, l, loc => GetDronesInAttackRange(loc).Count);
                });
            }
        }
        public Action<Aircraft, Location> SailMinimizeShips
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    Sail(ac, l, loc => GetEnemyShipsInAttackRange(loc).Count, false);
                });
            }
        }
        public Action<Aircraft, Location> SailDefault
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    Sail(ac, l);
                });
            }
        }
        public Action<Aircraft, Location> SailRandom
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    Sail(ac, l);
                });
            }
        }
        #endregion Sailing Functions
        #region Shooting Functions
        public static Func<AircraftBase, double> ShootDronesOnly
        {
            get
            {
                return new Func<AircraftBase, double>(x =>
                {
                    if (x.Type == AircraftType.Drone) return 1;
                    else return 0;
                });
            }
        }
        public static Func<AircraftBase, double> ShootPiratesOnly
        {
            get
            {
                return new Func<AircraftBase, double>(x =>
                {
                    if (x.Type == AircraftType.Pirate) return 1;
                    else return 0;
                });
            }
        }
        public static Func<AircraftBase, double> ShootByHealthRemaining
        {
            get
            {
                return new Func<AircraftBase, double>(x =>
                {
                    return x.CurrentHealth;
                });
            }
        }
        public static Func<AircraftBase, double> ShootByDamageTaken
        {
            get
            {
                return new Func<AircraftBase, double>(x =>
                {
                    return x.MaxHealth - x.CurrentHealth;
                });
            }
        }
        public static Func<AircraftBase, double> ShootRegular
        {
            get
            {
                return new Func<AircraftBase, double>(x =>
                {
                    return 1;
                });
            }
        }
        #endregion Shooting Functions
        #endregion Custom
    }
    #endregion Game Classes
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
    }    class AircraftBase : MapObject
    {
        protected GameEngine engine;
        public Aircraft aircraft;
        public AircraftBase(GameEngine engine, Aircraft aircraft)
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
                return aircraft.DetermineType();
            }
        }
        public int CurrentHealth
        {
            get
            {
                return engine.CheckHealth(aircraft);
            }
        }
        public int MaxHealth
        {
            get
            {
                if (Type == AircraftType.Drone)
                    return engine.DroneMaxHealth;
                else if (Type == AircraftType.Pirate)
                    return engine.PirateMaxHealth;
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

        public PirateShip AsPirate()
        {
            return new PirateShip(engine, (Pirate)aircraft);
        }
        public TradeShip AsDrone()
        {
            return new TradeShip(engine, (Drone)aircraft);
        }
        #endregion Custom
    }
    class PirateShip : AircraftBase
    {
        Pirate pirate;
        public PirateShip(GameEngine engine, Pirate pirate) : base(engine, pirate)
        {
            this.pirate = pirate;
        }

        #region Extends
        public int AttackRange
        {
            get
            {
                return pirate.AttackRange;
            }
        }
        public int TurnsToRevive
        {
            get
            {
                return pirate.TurnsToRevive;
            }
        }
        public bool Alive
        {
            get
            {
                return pirate.IsAlive();
            }
        }
        public bool InAttackRange(MapObject mapObject)
        {
            return pirate.InAttackRange(mapObject);
        }
        #endregion Custom
        #region Custom
        public bool Attack(Aircraft aircraft)
        {
            if (!InAttackRange(aircraft))
                return false;
            engine.Attack(pirate, aircraft);
            return true;
        }
        public bool Attack<T>(AircraftBase aircraft) where T : Aircraft
        {
            return Attack(aircraft.aircraft);
        }
        public bool TryAttack(Func<AircraftBase, double> ScoringFunction, bool OrderByDesecnding = true)
        {
            List<AircraftBase> options;
            if (OrderByDesecnding)
                options = GetAircraftsInAttackRange().OrderByDescending(x => ScoringFunction(x)).ToList();
            else
                options = GetAircraftsInAttackRange().OrderBy(x => ScoringFunction(x)).ToList();
            if (options.Count > 0)
            {
                AircraftBase f = options.First();
                if (ScoringFunction(f) <= 0)
                    return false;
                return Attack(options.First().aircraft);
            }
            else
                return false;
        }
        public List<AircraftBase> GetAircraftsInAttackRange()
        {
            return engine.EnemyLivingAircrafts.Where(x => this.InAttackRange(x)).ToList();
        }
        #endregion Custom
    }    class TradeShip : AircraftBase
    {
        Drone drone;
        public TradeShip(GameEngine engine, Drone drone) : base(engine, drone)
        {
            this.drone = drone;
        }

        #region Extends
        public int Value
        {
            get
            {
                return drone.Value;
            }
        }
        #endregion Extends
        #region Custom 
        public void SailToCity(Action<Aircraft, Location> SailFunction)
        {
            SailFunction(drone, engine.MyCities.OrderBy(x => x.Distance(this)).First().Location);
        }
        #endregion Custom
    }
    class Squad<T> : List<T> where T : AircraftBase
    {
        GameEngine engine;
        public Squad(GameEngine engine, IEnumerable<T> aircrafts) : base()
        {
            this.engine = engine;
            this.AddRange(aircrafts);
        }

        #region Extends
        public Squad<T> Select(Func<T, T> Selector)
        {
            return new Squad<T>(engine, this.AsEnumerable().Select(Selector));
        }
        public Squad<T> Filter(Func<T, bool> Predicate)
        {
            return new Squad<T>(engine, this.Where(Predicate));
        }
        #endregion Extends

        public Squad<T> FilterById(params int[] ids)
        {
            return Filter(x => ids.Contains(x.Id));
        }
    }
    class PirateSquad : Squad<PirateShip>
    {
        private PirateSquad(GameEngine engine, IEnumerable<PirateShip> pirates) : base(engine, pirates) { }
    }
    #endregion Aircrafts
    #region Utils    static class Extensions    {        public static bool isBetween(this Location loc, Location bound1, Location bound2)        {            return isBetween(loc.Row, bound1.Row, bound2.Row) && isBetween(loc.Col, bound1.Col, bound2.Col);        }        private static bool isBetween(int num, int bound1, int bound2)        {            if (bound1 > bound2)                return num >= bound2 && num <= bound1;            else                return num >= bound1 && num <= bound2;        }

        private static AircraftType DetermineType(string type)
        {
            if (type.Equals("Drone"))
                return AircraftType.Drone;
            else if (type.Equals("Pirate"))
                return AircraftType.Pirate;
            else
                return AircraftType.Generic;
        }        public static AircraftType DetermineType(this Aircraft aircraft)
        {
            return DetermineType(aircraft.Type);
        }        public static AircraftBase Upgrade(this Aircraft aircraft, GameEngine engine)
        {
            return new AircraftBase(engine, aircraft);
        }        public static PirateShip Upgrade(this Pirate pirate, GameEngine engine)
        {
            return new PirateShip(engine, pirate);
        }        public static TradeShip Upgrade(this Drone drone, GameEngine engine)
        {
            return new TradeShip(engine, drone);
        }

        public static Squad<PirateShip> Squad(this IEnumerable<Pirate> pirates, GameEngine engine)
        {
            return new Squad<PirateShip>(engine, pirates.Select(p => p.Upgrade(engine)));
        }
        public static Squad<TradeShip> Squad(this IEnumerable<Drone> drones, GameEngine engine)
        {
            return new Squad<TradeShip>(engine, drones.Select(d => d.Upgrade(engine)));
        }
        public static Squad<AircraftBase> Squad(this IEnumerable<Aircraft> aircrafts, GameEngine engine)
        {
            return new Squad<AircraftBase>(engine, aircrafts.Select(b => b.Upgrade(engine)));
        }        public static Func<T, double> Times<T>(this Func<T, double> f, Func<T, double> other)
        {
            return new Func<T, double>(x => f(x) * other(x));
        }    }    class PrioritizedActionList<T>    {        ConditioninalAction<T>[] actions;        public PrioritizedActionList(ConditioninalAction<T>[] actions)        {            this.actions = actions;        }        public bool invoke(T obj)        {            for (int i = 0; i < actions.Length; i++)            {                if (actions[i].invoke(obj))                    return true;            }            return false;        }    }    class ConditioninalAction<T>    {        private bool state;        private Func<T, bool> condition;        private Func<T, bool> action;        public ConditioninalAction(bool state, Func<T, bool> condition, Func<T, bool> action)        {            this.state = state;            this.condition = condition;            this.action = action;        }        public bool invoke(T obj)        {            if (condition.Invoke(obj) && state)            {                action(obj);                return true;            }            return false;        }        public bool invokeOnArray(T[] arr)        {            if (state)            {                foreach (T c in arr.Where(x => condition.Invoke(x)))                {                    if (action(c))                        return true;                }            }            return false;        }    }
    #endregion Utils}