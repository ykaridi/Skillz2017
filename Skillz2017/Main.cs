using System;using System.Collections.Generic;using System.Linq;using Pirates;namespace Skillz2017{    public class Bot : IPirateBot    {        DataStore store = new DataStore();        public void DoTurn(PirateGame game)        {            try            {
                store.NextTurn();
                GameEngine engine = game.Upgrade(store);

                store.FlushLogicAssignments();
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
            ship.SailToCity(ship.SailDefault);
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
            ship.SailToCity(ship.SailRandom);
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
            ship.SailToCity(ship.SailMinimizeShips);
        }
    }
    #endregion BasicDrones
    #region Pirates
    #endregion Pirates
    #region Pirate Plugins
    class ShootingPlugin : PiratePlugin
    {
        Func<AircraftBase, double> ScoringFunction;
        public ShootingPlugin(Func<AircraftBase, double> ScoringMehtod)
        {
            this.ScoringFunction = ScoringMehtod;
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
        public bool Scan(PirateShip ship, int range, out AircraftBase aircraft, bool OrderByDescending = true)
        {
            aircraft = null;

            List<AircraftBase> options = ship.GetAircraftsInRange(range);
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
    class ConquerPlugin : PiratePlugin
    {
        SmartIsland island;
        GameEngine engine;
        public ConquerPlugin(SmartIsland island, GameEngine engine)
        {
            this.island = island;
            this.engine = engine;
        }
        public bool DoTurn(PirateShip ship)
        {
            if (island.InControlRange(ship)) return false;
            else
                ship.Sail(island.Location, ship.SailMaximizeDrone);
            return true;
        }
    }
    class EscortPlugin : PiratePlugin
    {
        public bool DoTurn(PirateShip ship)
        {
            throw new NotImplementedException();
        }
    }
    class CamperPlugin : PiratePlugin
    {
        Location Camp;
        ShootingPlugin shooter;
        int range;
        public CamperPlugin(Location loc, ShootingPlugin shooter, int range = 7)
        {
            this.Camp = loc;
            this.shooter = shooter;
            this.range = range;
        }

        public bool DoTurn(PirateShip ship)
        {
            AircraftBase craft;
            if (shooter.Scan(ship, range, out craft))
            {
                if (ship.InAttackRange(craft)) ship.Attack(craft.aircraft);
                else ship.Sail(craft.Location, ship.SailMaximizeDrone);
                return true;
            }
            return false;
        }
    }
    class AntiCamperPlugin : PiratePlugin
    {
        GameEngine engine;
        int range;
        ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.5);
        public AntiCamperPlugin(GameEngine engine, int range = 7)
        {
            this.engine = engine;
            this.range = range;
        }
        public bool DoTurn(PirateShip ship)
        {
            List<City> cities = engine.MyCities.Where(c => engine.GetEnemyShipsInRange(ship.Location, range).Count > 0).OrderBy(x => x.Distance(ship)).ToList();
            if (cities.Count > 0)
            {
                City c = cities.First();
                AircraftBase craft;
                if (ship.InRange(c, range) && shooter.Scan(ship, range - ship.Distance(c), out craft))
                {
                    if (ship.InAttackRange(craft)) ship.Attack(craft.aircraft);
                    else ship.Sail(craft.Location, ship.SailMaximizeDrone);
                } else ship.Sail(c.Location, ship.SailMaximizeDrone);
                return true;
            }
            else
                return false;
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
        public PiratePlugin[] Plugins
        {
            get; set;
        } = new PiratePlugin[0];
        public abstract void DoTurn(PirateShip pirate);
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
    #region Game Classes    class GameEngine    {        public readonly Random random = new Random();        public Dictionary<int, int> HitList = new Dictionary<int, int>();        public Dictionary<int, bool> MoveList = new Dictionary<int, bool>();        public readonly DataStore store;        protected readonly PirateGame game;
        public GameEngine(PirateGame game, DataStore store)
        {
            this.game = game;
            this.store = store;

            this.MyLivingAircrafts.ForEach(aircraft =>
            {
                MarkOnList(aircraft.aircraft, true);
            });
        }
        public GameEngine(PirateGame game) : this(game, new DataStore())
        {
            this.game = game;
        }

        public void DoTurn(IndividualPirateGameLogic gl, bool RespectDataStoreAssignments = true)
        {
            foreach (PirateShip pirate in MyLivingPirates)
            {
                PirateLogic pl;
                if (RespectDataStoreAssignments && store.TryGetLogic(pirate, out pl)) { }
                else pl = gl.AssignPirateLogic(pirate);

                bool stop = false;
                foreach(PiratePlugin plugin in pl.Plugins)
                {
                    if ((stop = plugin.DoTurn(pirate)))
                        break;
                    stop = stop || (!pirate.CanPlay);
                }
                if (!stop)
                    pl.DoTurn(pirate);
            }

            foreach (TradeShip drone in MyLivingDrones)
            {
                DroneLogic dl;
                if (RespectDataStoreAssignments && store.TryGetLogic(drone, out dl)) { }
                else dl = gl.AssignDroneLogic(drone);
                dl.DoTurn(drone);
            }
        }
        public void DoTurn(PirateSquadGameLogic gl, bool RespectDataStoreAssignments = true)
        {
            List<PirateSquad> Squads = new List<PirateSquad>();
            PirateSquad APirates = new PirateSquad(this, MyLivingPirates);
            if (RespectDataStoreAssignments)
            {
                Squads.AddRange(store.GetSquads().Select(ids => {
                    PirateSquad ps = PirateSquad.SquadFromIds(this, APirates, ids);
                    APirates.FilterOutBySquad(ps);
                    return ps;
                    }));
            }
            Squads.AddRange(gl.AssignSquads(APirates.Select(x => (PirateShip)x).ToArray()));
            foreach (PirateSquad squad in Squads)
            {
                PirateSquadLogic sl;
                if (RespectDataStoreAssignments && store.TryGetLogic(squad, out sl)) { }
                else sl = gl.AssignSquadLogic(squad);
                int id = 0;
                foreach(PirateShip ps in squad)
                {
                    PirateLogic pl = sl.AssignPirateLogic(ps, id);
                    bool stop = false;
                    foreach (PiratePlugin plugin in pl.Plugins)
                    {
                        if ((stop = plugin.DoTurn(ps)))
                            break;
                        stop = stop || (!ps.CanPlay);
                    }
                    if (!stop)
                        pl.DoTurn(ps);
                    id += 1;
                }
            }

            foreach (TradeShip drone in MyLivingDrones)
            {
                DroneLogic dl;
                if (RespectDataStoreAssignments && store.TryGetLogic(drone, out dl)) { }
                else dl = gl.AssignDroneLogic(drone);
                dl.DoTurn(drone);
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

        private int MovelistId(Aircraft craft)
        {
            if (craft.DetermineType() == AircraftType.Drone)
                return craft.Id + MyPirates.Count;
            else
                return craft.Id;
        }
        private void MarkOnList(Aircraft craft, bool state = false)
        {
            MoveList[MovelistId(craft)] = state;
        }
        public bool CanPlay(Aircraft craft)
        {
            if (MoveList.ContainsKey(MovelistId(craft)))
                return MoveList[MovelistId(craft)];
            else
                return false;
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
        }        public Squad<TradeShip> GetEnemyDronesInRange(Location loc, int range)
        {
            return new Squad<TradeShip>(this, EnemyLivingDrones.Where(x => x.InRange(loc, range)));
        }        public Squad<TradeShip> GetEnemyDronesInAttackRange(Location loc)
        {
            return GetEnemyDronesInRange(loc, AttackRange);
        }        public Squad<PirateShip> GetEnemyShipsInRange(Location loc, int range)
        {
            return new Squad<PirateShip>(this, EnemyLivingPirates.Where(x => x.InRange(loc, range)));
        }        public Squad<PirateShip> GetEnemyShipsInAttackRange(Location loc)
        {
            return GetEnemyShipsInRange(loc, AttackRange);
        }
        public Squad<AircraftBase> GetEnemyAircraftsInRange(Location loc, int range)
        {
            return new Squad<AircraftBase>(this, EnemyLivingAircrafts.Where(x => x.InRange(loc, range)));
        }
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
        #region Sailing Functions        public Action<Aircraft, Location> SailMaximizeDrone
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    engine.Sail(ac, l, loc => engine.GetEnemyDronesInAttackRange(loc).Count);
                });
            }
        }
        public Action<Aircraft, Location> SailMinimizeShips
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    engine.Sail(ac, l, loc => engine.GetEnemyShipsInAttackRange(loc).Count, false);
                });
            }
        }
        public Action<Aircraft, Location> SailDefault
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    engine.Sail(ac, l);
                });
            }
        }
        public Action<Aircraft, Location> SailRandom
        {
            get
            {
                return new Action<Aircraft, Location>((ac, l) =>
                {
                    engine.Sail(ac, l);
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
        public static Func<AircraftBase, double> ShootByCurrentHealth
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
        public static Func<AircraftBase, double> ShootByNearnessToDeath
        {
            get
            {
                return new Func<AircraftBase, double>(x =>
                {
                    return ((double)(x.MaxHealth - x.CurrentHealth)) / x.MaxHealth;
                });
            }
        }
        #endregion Shooting Functions

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
        public bool CanPlay
        {
            get
            {
                return engine.CanPlay(aircraft);
            }
        }

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
        public void Sail(Location loc, Action<Aircraft, Location> SailFunction)
        {
            SailFunction.Invoke(aircraft, loc);
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

        public Squad<AircraftBase> Scan(int range)
        {
            return engine.GetEnemyAircraftsInRange(Location, range);
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
        public Squad<AircraftBase> GetAircraftsInAttackRange()
        {
            return GetAircraftsInRange(AttackRange);
        }
        public Squad<AircraftBase> GetAircraftsInRange(int range)
        {
            return engine.GetEnemyAircraftsInRange(Location, range);
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

        public Squad<T> FilterById(IEnumerable<int> ids)
        {
            return Filter(x => ids.Contains(x.Id));
        }
        public Squad<T> FilterOutById(IEnumerable<int> ids)
        {
            return Filter(x => !ids.Contains(x.Id));
        }
        public Squad<T> FilterOutBySquad(Squad<T> squad)
        {
            return FilterOutById(squad.Select(x => x.Id));
        }
    }
    class PirateSquad : Squad<PirateShip>
    {
        public PirateSquad(GameEngine engine, IEnumerable<PirateShip> pirates) : base(engine, pirates) { }
        public static PirateSquad SquadFromIds(GameEngine engine, IEnumerable<PirateShip> pirates, IEnumerable<int> ids)
        {
            return new PirateSquad(engine, pirates.Where(x => ids.Contains(x.Id)));
        }
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
        public static GameEngine Upgrade(this PirateGame game)
        {
            return new GameEngine(game);
        }
        public static GameEngine Upgrade(this PirateGame game, DataStore store)
        {
            return new GameEngine(game, store);
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
        }        public static void AttachPlugin(this PirateLogic logic, PiratePlugin plugin)
        {
            PiratePlugin[] old = logic.Plugins;
            PiratePlugin[] plugins = new PiratePlugin[old.Length + 1];
            for(int i = 0; i < old.Length; i++)
            {
                plugins[i] = old[i];
            }
            plugins[old.Length] = plugin;
            logic.Plugins = plugins;
        }    }    class PrioritizedActionList<T>    {        ConditioninalAction<T>[] actions;        public PrioritizedActionList(ConditioninalAction<T>[] actions)        {            this.actions = actions;        }        public bool invoke(T obj)        {            for (int i = 0; i < actions.Length; i++)            {                if (actions[i].invoke(obj))                    return true;            }            return false;        }    }    class ConditioninalAction<T>    {        private bool state;        private Func<T, bool> condition;        private Func<T, bool> action;        public ConditioninalAction(bool state, Func<T, bool> condition, Func<T, bool> action)        {            this.state = state;            this.condition = condition;            this.action = action;        }        public bool invoke(T obj)        {            if (condition.Invoke(obj) && state)            {                action(obj);                return true;            }            return false;        }        public bool invokeOnArray(T[] arr)        {            if (state)            {                foreach (T c in arr.Where(x => condition.Invoke(x)))                {                    if (action(c))                        return true;                }            }            return false;        }    }
    class DataStore : Dictionary<string, object>
    {
        public DataStore() : base()
        {

        }

        public void AssignLogic(PirateShip pirate, PirateLogic logic)
        {
            this.Add("[Wait]<Assignment>Pirate-" + pirate.Id, logic);
        }
        public bool TryGetLogic(PirateShip pirate, out PirateLogic logic)
        {
            logic = null;
            if (ContainsKey("<Assignment>Pirate-" + pirate.Id))
            {
                logic = (PirateLogic)this["<Assignment>Pirate-" + pirate.Id];
                return true;
            }
            else
                return false;
        }
        public void AssignLogic(PirateSquad squad, PirateSquadLogic logic)
        {
            string si = "[" + String.Join(",", squad.Select(x => x.Id.ToString()).OrderBy(x => x).ToArray()) + "]";
            this.Add("[Wait]<Assignment>Squad-" + si, logic);
        }
        public List<List<int>> GetSquads()
        {
            List<List<int>> squads = new List<List<int>>();
            foreach(string key in this.Keys)
            {
                if (key.StartsWith("<Assignment>Squad-"))
                {
                    string si = key.Substring("<Assignment>Squad-".Length);
                    squads.Add(si.Split(',').Select(x => int.Parse(x)).ToList());
                }
            }
            return squads;
        }
        public bool TryGetLogic(PirateSquad squad, out PirateSquadLogic logic)
        {
            string si = "[" + String.Join(",", squad.Select(x => x.Id.ToString()).OrderBy(x => x).ToArray()) + "]";
            logic = null;
            if (ContainsKey("<Assignment>Squad-" + si))
            {
                logic = (PirateSquadLogic)this["<Assignment>Squad-" + si];
                return true;
            }
            else
                return false;
        }
        public void AssignLogic(TradeShip drone, DroneLogic logic)
        {
            this.Add("[Wait]<Assignment>Drone-" + drone.Id, logic);
        }
        public bool TryGetLogic(TradeShip drone, out DroneLogic logic)
        {
            logic = null;
            if (ContainsKey("<Assignment>Drone-" + drone.Id))
            {
                logic = (DroneLogic)this["<Assignment>Drone-" + drone.Id];
                return true;
            }
            else
                return false;
        }
        public void FlushLogicAssignments()
        {
            foreach(string key in this.Keys)
            {
                if (key.StartsWith("<Assignment>")) this.Remove(key);
            }
        }
        public void NextTurn()
        {
            foreach(string key in this.Keys)
            {
                if (key.StartsWith("[Wait]"))
                {
                    object value = this[key];
                    this.Remove(key);
                    this.Add(key.Substring("[Wait]".Length), value);
                }
            }
        }
    }
    #endregion Utils}