using System.Collections.Generic;
using System.Linq;
using Pirates;

namespace MyBot.Engine
{
    class GameEngine
    {
        public GameEngine()
        {
            random = new System.Random();
            store = new DataStore();
        }

        public readonly System.Random random;
        public const int CampRange = 5;
        public Dictionary<int, int> HitList;
        public Dictionary<int, bool> MoveList;
        internal DataStore store { get; private set; }
        protected PirateGame game { get; private set; }
        public void Update(PirateGame pg)
        {
            HitList = new Dictionary<int, int>();
            MoveList = new Dictionary<int, bool>();
            this.game = pg;

            this.MyLivingAircrafts.ForEach(aircraft =>
            {
                MarkOnList(aircraft.aircraft, true);
            });
            RefreshCampCheck();
        }

        public void DoTurn(IndividualPirateHandler ph, IndividualDroneHandler dh, bool RespectDataStoreAssignments = true)
        {
            DoTurn(ph, RespectDataStoreAssignments);
            DoTurn(dh, RespectDataStoreAssignments);
        }
        public void DoTurn(IndividualPirateHandler ph, SquadDroneHandler dh, bool RespectDataStoreAssignments = true)
        {
            DoTurn(ph, RespectDataStoreAssignments);
            DoTurn(dh, RespectDataStoreAssignments);
        }
        public void DoTurn(SquadPirateHandler ph, IndividualDroneHandler dh, bool RespectDataStoreAssignments = true)
        {
            DoTurn(ph, RespectDataStoreAssignments);
            DoTurn(dh, RespectDataStoreAssignments);
        }
        public void DoTurn(SquadPirateHandler ph, SquadDroneHandler dh, bool RespectDataStoreAssignments = true)
        {
            DoTurn(ph, RespectDataStoreAssignments);
            DoTurn(dh, RespectDataStoreAssignments);
        }

        private void DoTurn(IndividualPirateHandler ph, bool RespectDataStoreAssignments)
        {
            LogicedPirate[] pirates = ph.AssignPirateLogic(MyLivingPirates.ToArray());
            for (int i = 0; i < pirates.Length; i++)
            {
                LogicedPirate pirate = pirates[i];

                PirateLogic pl;
                if (RespectDataStoreAssignments && store.TryGetLogic(pirate.s, out pl)) { }
                else pl = pirate.logic;

                pl.DoTurnWithPlugins(pirate.s);
            }
        }
        private void DoTurn(SquadPirateHandler ph, bool RespectDataStoreAssignments)
        {
            List<LogicedPirateSquad> Squads = new List<LogicedPirateSquad>();
            PirateSquad APirates = new PirateSquad(MyLivingPirates);
            if (RespectDataStoreAssignments)
            {
                Squads.AddRange(store.GetPirateSquads().Select(ids => {
                    PirateSquad ps = PirateSquad.SquadFromIds(APirates, ids);
                    APirates.FilterOutBySquad(ps);
                    PirateSquadLogic l;
                    store.TryGetLogic(ps, out l);
                    return new LogicedPirateSquad(ps, l);
                }));
            }
            Squads.AddRange(ph.AssignSquads(APirates.Select(x => (PirateShip)x).ToArray()));
            foreach (LogicedPirateSquad lps in Squads)
            {
                if (lps.s.IsEmpty()) continue;
                lps.DoTurn();
            }
        }
        private void DoTurn(IndividualDroneHandler dh, bool RespectDataStoreAssignments)
        {
            foreach (TradeShip drone in MyLivingDrones)
            {
                DroneLogic dl;
                if (RespectDataStoreAssignments && store.TryGetLogic(drone, out dl)) { }
                else dl = dh.AssignDroneLogic(drone);
                dl.DoTurnWithPlugins(drone);
            }
        }
        private void DoTurn(SquadDroneHandler dh, bool RespectDataStoreAssignments)
        {
            List<LogicedDroneSquad> Squads = new List<LogicedDroneSquad>();
            DroneSquad ADrones = new DroneSquad(MyLivingDrones);
            if (RespectDataStoreAssignments)
            {
                Squads.AddRange(store.GetDroneSquads().Select(ids => {
                    DroneSquad ds = DroneSquad.SquadFromIds(ADrones, ids);
                    ADrones.FilterOutBySquad(ds);
                    DroneSquadLogic l;
                    store.TryGetLogic(ds, out l);
                    return new LogicedDroneSquad(ds, l);
                }));
            }
            Squads.AddRange(dh.AssignSquads(ADrones.Select(x => (TradeShip)x).ToArray()));
            foreach (LogicedDroneSquad lds in Squads)
            {
                if (lds.s.IsEmpty()) continue;
                lds.DoTurn();
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
            return game.GetAircraftsOn(mapObject).Squad();
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
                return game.GetEnemy().AllPirates.Squad();
            }
        }
        public Squad<PirateShip> EnemyLivingPirates
        {
            get
            {
                return game.GetEnemyLivingPirates().Squad().Filter(x => x.IsAlive);
            }
        }
        public SmartIsland[] Islands
        {
            get
            {
                return game.GetAllIslands().Select(x => new SmartIsland(x)).ToArray();
            }
        }
        public SmartIsland[] EnemyIslands
        {
            get
            {
                return game.GetEnemyIslands().Select(x => new SmartIsland(x)).ToArray();
            }
        }
        public SmartIsland[] MyIslands
        {
            get
            {
                return game.GetMyIslands().Select(x => new SmartIsland(x)).ToArray();
            }
        }
        public SmartIsland[] NeutralIslands
        {
            get
            {
                return game.GetNeutralIslands().Select(x => new SmartIsland(x)).ToArray();
            }
        }
        public SmartIsland[] NotMyIslands
        {
            get
            {
                return game.GetNotMyIslands().Select(x => new SmartIsland(x)).ToArray();
            }
        }
        public Squad<PirateShip> MyPirates
        {
            get
            {
                return game.GetAllMyPirates().Squad();
            }
        }
        public Squad<PirateShip> MyLivingPirates
        {
            get
            {
                return game.GetMyLivingPirates().Squad();
            }
        }
        public Squad<TradeShip> MyLivingDrones
        {
            get
            {
                return game.GetMyLivingDrones().Squad();
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
            return game.GetEnemyDroneById(id).Upgrade();
        }
        public Squad<AircraftBase> EnemyLivingAircrafts
        {
            get
            {
                return game.GetEnemyLivingAircrafts().Squad().Filter(x => x.IsAlive);
            }
        }
        public Squad<TradeShip> EnemyLivingDrones
        {
            get
            {
                return game.GetEnemyLivingDrones().Squad().Filter(x => x.IsAlive);
            }
        }
        public PirateShip GetEnemyPirateById(int id)
        {
            return EnemyPirates.First(x => x.Id == id);
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
            return game.GetMyDroneById(id).Upgrade();
        }
        public Squad<AircraftBase> MyLivingAircrafts
        {
            get
            {
                return game.GetMyLivingAircrafts().Squad();
            }
        }
        public PirateShip GetMyPirateById(int id)
        {
            return game.GetMyPirateById(id).Upgrade();
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
            if (aircraft.Distance(destination) > 0 && CanPlay(aircraft))
            {
                MarkOnList(aircraft, false);
                game.SetSail(aircraft, destination);
            }
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
        public void Sail(Aircraft aircraft, MapObject destination, System.Func<Location, double> ScoreFunction, bool OrderByAscending = true)
        {
            List<Location> locs = GetSailOptions(aircraft, destination);
            Location l;
            if (OrderByAscending)
                l = locs.OrderBy(ScoreFunction).First();
            else
                l = locs.OrderByDescending(ScoreFunction).First();
            SetSail(aircraft, l);
        }
        public void Sail(AircraftBase aircraft, MapObject destination, System.Func<Location, double> ScoreFunction, bool OrderByAscending = true)
        {
            Sail(aircraft.aircraft, destination, ScoreFunction, OrderByAscending);
        }
        #endregion Extends
        #region Custom 
        public void RefreshCampCheck()
        {
            MyCities.ToList().ForEach(c =>
            {
                GetEnemyShipsInRange(c.Location, CampRange).ForEach(ps =>
                {
                    string key = "<Flush><Camper>" + c.Id + "-" + ps.Id;
                    store.SetValue("[Wait]" + key, store.GetValue(key, 0) + 1);
                });
            });
        }
        public bool CheckForCamper(City c, out PirateShip camper, int minTurns = 3)
        {
            camper = null;
            List<PirateShip> pirates = GetEnemyShipsInRange(c.Location, CampRange).OrderBy(ps =>
            {
                string key = "<Flush><Camper>" + c.Id + "-" + ps.Id;
                return store.GetValue(key, 0);
            }).ToList();
            if (pirates.Count > 0)
            {
                PirateShip p = pirates.First();
                string key = "<Flush><Camper>" + c.Id + "-" + p.Id;
                if (store.GetValue(key, 0) >= minTurns)
                {
                    camper = p;
                    return true;
                }
                else return false;
            }
            else
                return false;
        }

        private int HitlistId(Aircraft aircraft)
        {
            if (aircraft.DetermineType() == AircraftType.Drone)
                return aircraft.Id + MyPirates.Count;
            else
                return aircraft.Id;
        }
        private void AppendToHitlist(Aircraft aircraft)
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

        private int SerialID(Aircraft craft)
        {
            if (craft.DetermineType() == AircraftType.Drone)
                return craft.Id + MyPirates.Count;
            else
                return craft.Id;
        }
        private void MarkOnList(Aircraft craft, bool state = false)
        {
            MoveList[SerialID(craft)] = state;
        }
        public bool CanPlay(Aircraft craft)
        {
            if (MoveList.ContainsKey(SerialID(craft)))
                return MoveList[SerialID(craft)];
            else
                return false;
        }
        public bool Attack(Pirate pirate, Aircraft target)
        {
            if (IsAlive(target) && CanPlay(pirate))
            {
                MarkOnList(pirate, false);
                AppendToHitlist(target);
                game.Attack(pirate, target);
                return true;
            }
            else return false;
        }
        public Squad<TradeShip> GetEnemyDronesInRange(MapObject loc, int range)
        {
            return EnemyLivingDrones.Filter(x => x.InRange(loc, range));
        }
        public Squad<TradeShip> GetEnemyDronesInAttackRange(MapObject loc)
        {
            return GetEnemyDronesInRange(loc, AttackRange);
        }
        public Squad<PirateShip> GetEnemyShipsInRange(MapObject loc, int range)
        {
            return EnemyLivingPirates.Filter(x => x.InRange(loc, range));
        }
        public Squad<PirateShip> GetEnemyShipsInAttackRange(MapObject loc)
        {
            return GetEnemyShipsInRange(loc, AttackRange);
        }
        public Squad<AircraftBase> GetEnemyAircraftsInRange(MapObject loc, int range)
        {
            return EnemyLivingAircrafts.Filter(x => x.InRange(loc, range));
        }
        #endregion Custom
    }
}
