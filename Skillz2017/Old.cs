﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pirates;

namespace MyBot
{
    public class Bot : IPirateBot
    {
        public void DoTurn(PirateGame game)
        {
            try
            {
                SimpleGameLogic logic = new SimpleGameLogic();
                GameEngine engine = new GameEngine(logic, game);
                engine.DoTurn();
            }
            catch (Exception e)
            {
                game.Debug(e.StackTrace);
            }
        }
    }

    class SimpleGameLogic : GameLogic
    {
        public DroneLogic assignDroneLogic(TradeShip drone, GameEngine engine)
        {
            return new StupidDrone(engine);
        }

        public LogicedSquad[] AssignSquads(PirateShip[] pirates, GameEngine engine)
        {
            PirateShip[] ps = pirates.OrderBy(x => x.Id).ToArray();
            BasicSquad all = new BasicSquad(engine, ps);
            return SpreadIsland(engine, ps);
        }

        public LogicedSquad[] AllForIsland3(GameEngine engine, PirateShip[] pirates)
        {
            BasicSquad s = new BasicSquad(engine, pirates);
            return new LogicedSquad[]
            {
                new LogicedSquad(s.Logic(p => new StupidPirate(engine)), new LocationedSquadLogic(engine.IslandById(3).Location))
            };
        }
        public LogicedSquad[] SpreadIslandDynamic(GameEngine engine, PirateShip[] pirates)
        {
            PirateLogic sp = new StupidPirate(engine);

            LogicedSquad[] squads = new LogicedSquad[5];

            PirateShip[] Remaining = pirates;
            BasicSquad bs = Utils.CreateSquad(engine, Remaining, p => p.Distance(engine.EnemyCity.Location), 1);
            squads[0] = bs.Logic(x => sp).Logic(new CamperSquad(engine, engine.EnemyCity.Location));
            Remaining = Utils.FilterOut(Remaining, bs);

            SmartIsland[] islands = engine.Islands.OrderBy(i =>
            {
                int score = 0;
                if (engine.ScanForPirates(i.Location, 7).Length == 0) score += 10;
                if (i.IsTheirs) score += 5;
                if (!i.IsTheirs && !i.IsOurs) score += 1;
                return score;
            }).ToArray();

            for (int i = 0; i < 4; i++)
            {
                BasicSquad t = Utils.CreateSquad(engine, Remaining, p => p.Distance(islands[i].Location), 1);
                squads[i + 1] = t.Logic(p => sp).Logic(new LocationedSquadLogic(islands[i].Location));
                Remaining = Utils.FilterOut(Remaining, t);
            }
            return squads;
        }
        public LogicedSquad[] SpreadIsland(GameEngine engine, PirateShip[] pirates)
        {
            BasicSquad Camp = new BasicSquad(engine, pirates.Where(x => x.Id == 0).ToArray());
            BasicSquad s1 = new BasicSquad(engine, pirates.Where(x => x.Id == 1).ToArray());
            BasicSquad s2 = new BasicSquad(engine, pirates.Where(x => x.Id == 2).ToArray());
            BasicSquad s3 = new BasicSquad(engine, pirates.Where(x => x.Id == 3).ToArray());
            BasicSquad s4 = new BasicSquad(engine, pirates.Where(x => x.Id == 4).ToArray());

            StupidPirate spns = new StupidPirate(engine, true);
            StupidPirate spys = new StupidPirate(engine, true);
            LogicedSquad sl3;
            if (engine.ScanForCampers().Length > 0)
            {

                sl3 = s3.Logic(x => spns).Logic(new AntiCampSquad(engine));
            }
            else
            {
                sl3 = s3.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(2).Location));
            }
            if (engine.GetAllDrones().Length > 2)
            {
                return new LogicedSquad[]
                {
                    Camp.Logic(x => spys).Logic(new CamperSquad(engine, engine.EnemyCity.Location)),
                    s1.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(0).Location)),
                    s2.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(1).Location)),
                    sl3,
                    s4.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(3).Location))
                };
            }
            else
            {
                if (engine.EnemyCity.Location.Col > 23)
                {
                    return new LogicedSquad[]
                    {
                        Camp.Logic(x => spys).Logic(new CamperSquad(engine, engine.IslandById(2).Location)),
                        s1.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(0).Location)),
                        s2.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(1).Location)),
                        sl3,
                        s4.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(3).Location))
                    };
                }
                else
                {
                    return new LogicedSquad[]
                       {
                            Camp.Logic(x => spys).Logic(new CamperSquad(engine, engine.IslandById(1).Location)),
                            s1.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(0).Location)),
                            s2.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(1).Location)),
                            sl3,
                            s4.Logic(x => spns).Logic(new LocationedSquadLogic(engine.IslandById(3).Location))
                       };
                }
            }
        }
        public LogicedSquad[] CampDuoTripletCapture(GameEngine engine, PirateShip[] pirates)
        {
            BasicSquad C1 = new BasicSquad(engine, pirates.Where(x => x.Id == 0).ToArray());
            BasicSquad C2 = new BasicSquad(engine, pirates.Where(x => x.Id == 1).ToArray());
            BasicSquad Cap = new BasicSquad(engine, pirates.Where(x => x.Id >= 2).ToArray());

            SmartIsland[] si = engine.Islands.Where(i => i.Id <= 2).OrderBy(i =>
            {
                double dis = i.Distance(engine.MyCity);
                if (i.IsOurs)
                    return 1000 + Math.Pow(dis, -1);
                else
                    return dis;
            }).ToArray();

            Location CapLoc = si[0].Location;
            Location Cap1 = new Location(24, 23);
            //Location Cap2 = new Location(21, engine.EnemyCity.Location.Col - 4 * Math.Sign(engine.EnemyCity.Location.Col - engine.MyCity.Location.Col));
            Location Cap2 = engine.EnemyCity.Location;
            if (engine.ScanForCampers().Length > 0)
            {
                CapLoc = engine.MyCity.Location;
            }
            if (engine.ScanForPirates(engine.IslandById(3).Location, 7).Length == 0)
                Cap1 = engine.IslandById(3).Location;


            StupidPirate sp = new StupidPirate(engine, true);
            return new LogicedSquad[]
            {
                C1.Logic(x => sp).Logic(new CamperSquad(engine, Cap1)),
                C2.Logic(x => sp).Logic(new CamperSquad(engine, Cap2)),
                Cap.Logic(x => new StupidPirate(engine, false)).Logic(new LocationedSquadLogic(CapLoc))
            };
        }
        public LogicedSquad[] Squad221Dynamic(GameEngine engine, PirateShip[] pirates)
        {
            PirateShip[] Remaining = pirates;
            BasicSquad AntiCamp = Utils.CreateSquad(engine, Remaining, (p) => p.Distance(engine.MyCity.Location), engine.ScanForCampers(7).Length);
            Remaining = Utils.FilterOut(Remaining, AntiCamp);
            BasicSquad Camp = Utils.CreateSquad(engine, Remaining, (p) => p.Distance(engine.EnemyCity.Location), 1);
            Remaining = Utils.FilterOut(Remaining, Camp);













            // Camp + AntiCamper

            /*SmartIsland[] islands = engine.Islands.Where(i => !i.IsOurs).OrderBy(i =>
            {
                int score = 0;
                if (engine.ScanForPirates(i.Location, 7).Length == 0) score += 1;
                if (score > 0 && i.IsTheirs) score += 10;
                return score;
            }).ToArray();*/


            Location SI = engine.IslandById(1).Location;
            if (engine.IslandById(1).IsOurs && !engine.IslandById(0).IsOurs)
                SI = engine.IslandById(0).Location;
            BasicSquad s1 = Utils.CreateSquad(engine, Remaining, (p) => p.Distance(engine.IslandById(3).Location), 2);
            Remaining = Utils.FilterOut(Remaining, s1);
            BasicSquad s2 = Utils.CreateSquad(engine, Remaining, (p) => p.Distance(SI), 2);
            Remaining = Utils.FilterOut(Remaining, s2);

            StupidPirate sp = new StupidPirate(engine);
            return new LogicedSquad[]
            {
                AntiCamp.Logic(x => sp).Logic(new AntiCampSquad(engine)),
                Camp.Logic(x => sp).Logic(new CamperSquad(engine, engine.EnemyCity.Location)),
                s1.Logic(x => sp).Logic(new LocationedSquadLogic(engine.IslandById(3).Location)),
                s2.Logic(x => sp).Logic(new LocationedSquadLogic(SI))
            };

        }

        class StupidDrone : DroneLogic
        {
            GameEngine engine;
            public StupidDrone(GameEngine engine)
            {
                this.engine = engine;
            }
            public void DoTurn(TradeShip ship)
            {
                ship.Deposit(true);
            }
        }
        class StupidPirate : PirateLogic
        {
            GameEngine engine;
            PrioritizedActionList<PirateShip> actions;
            bool shoot;
            public StupidPirate(GameEngine engine, bool shoot = false)
            {
                this.engine = engine;
                this.shoot = shoot;
            }
            public void DoTurn(Location loc, PirateShip pirate)
            {
                actions = new PrioritizedActionList<PirateShip>(new ConditioninalAction<PirateShip>[]
                {
                    new ConditioninalAction<PirateShip>(shoot, (x) => x.AttackScan().Length > 0, (x) => x.TryAttack(true)),
                    new ConditioninalAction<PirateShip>(true, (x) => true, (x) => {
                        x.Sail(loc);
                        return true;
                    })
                });

                actions.invoke(pirate);
            }
            public void KillCamper(Location loc, PirateShip pirate)
            {
                actions = new PrioritizedActionList<PirateShip>(new ConditioninalAction<PirateShip>[]
                {
                    new ConditioninalAction<PirateShip>(shoot, (x) => x.AttackScan().Length > 0, (x) => x.TryAttack(true)),
                    new ConditioninalAction<PirateShip>(true, (x) => true, (x) => {
                        Location loc1 = new Location(pirate.Location.Row, loc.Col);
                        x.Sail(loc1);
                        Location loc2 = new Location(loc.Row, pirate.Location.Col);
                        x.Sail(loc2);
                        return true;
                    })
                });

                actions.invoke(pirate);
            }
        }

        class AntiCampSquad : SquadLogic
        {
            GameEngine engine;
            public AntiCampSquad(GameEngine engine)
            {
                this.engine = engine;
            }
            public Location CalculateLocation()
            {
                Pirate[] pirates = engine.ScanForCampers(7);
                if (pirates.Length > 0)
                    return pirates[0].Location;
                else
                    return engine.MyCity.Location;
            }
        }
        class CamperSquad : SquadLogic
        {
            GameEngine engine;
            Location loc;
            public CamperSquad(GameEngine engine, Location loc)
            {
                this.engine = engine;
                this.loc = loc;
            }

            public Location CalculateLocation()
            {
                Aircraft[] craft = engine.ScanForDrones(13);
                if (craft.Length > 0)
                    return craft[0].Location;
                else
                    return loc;
            }
        }
    }

    class LocationedSquadLogic : SquadLogic
    {
        Location loc;
        public LocationedSquadLogic(Location loc)
        {
            this.loc = loc;
        }
        public Location CalculateLocation()
        {
            return loc;
        }
    }
    class GameEngine
    {
        Random r = new Random();

        GameLogic logic;
        public PirateGame game;
        public GameEngine(GameLogic logic, PirateGame game)
        {
            this.logic = logic;
            this.game = game;
        }
        public void DoTurn()
        {
            List<PirateShip> AlivePirates = game.GetMyLivingPirates().Select(x => new PirateShip(x, this)).ToList();
            LogicedSquad[] squads = logic.AssignSquads(AlivePirates.ToArray(), this);
            foreach (LogicedSquad squad in squads)
            {
                squad.DoTurn();
            }

            List<TradeShip> TradesShips = game.GetMyLivingDrones().Select(x => new TradeShip(x, this)).ToList();
            foreach (TradeShip s in TradesShips)
            {
                logic.assignDroneLogic(s, this).DoTurn(s);
            }
        }



        #region Props
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
        public City MyCity
        {
            get
            {
                return game.GetMyCities()[0];
            }
        }
        public City EnemyCity
        {
            get
            {
                return game.GetEnemyCities()[0];
            }
        }
        public SmartIsland[] Islands
        {
            get
            {
                return game.GetAllIslands().Select(i => new SmartIsland(i, this)).ToArray();
            }
        }
        public SmartIsland[] MyIslands
        {
            get
            {
                return game.GetMyIslands().Select(i => new SmartIsland(i, this)).ToArray();
            }
        }
        public SmartIsland[] EnemyIslands
        {
            get
            {
                return game.GetEnemyIslands().Select(i => new SmartIsland(i, this)).ToArray();
            }
        }
        public SmartIsland[] NeutralIslands
        {
            get
            {
                return game.GetNeutralIslands().Select(i => new SmartIsland(i, this)).ToArray();
            }
        }


        #endregion Props

        public void SetSail(Aircraft craft, Location loc, int id = 0)
        {
            List<Location> locs = game.GetSailOptions(craft, loc);
            if (id >= locs.Count)
                id = 0;
            game.SetSail(craft, locs[id]);
        }
        public void RandomSail(Aircraft craft, Location loc)
        {
            List<Location> options = game.GetSailOptions(craft, loc);
            game.SetSail(craft, options[r.Next(options.Count)]);
        }
        public Aircraft[] Scan(Location loc, int Radius)
        {
            return game.GetEnemyLivingAircrafts().Where(x => loc.Distance(x) <= Radius).ToArray();
        }

        public void Attack(PirateShip ship, Aircraft craft)
        {
            game.Attack(ship.pirate, craft);
        }

        public SmartIsland IslandById(int id)
        {
            return new SmartIsland((game.GetAllIslands().Where(x => x.Id == id).ToArray())[0], this);
        }
        public bool IslandIsOur(int id)
        {
            return IslandById(id).IsOurs;
        }
        public Pirate[] ScanForPirates(Location loc, int radius)
        {
            List<Pirate> pirates = game.GetEnemyLivingPirates();
            return pirates.Where(p => p.Distance(loc) <= radius).ToArray();
        }
        public Pirate[] ScanForCampers(int radius = 7)
        {
            List<Pirate> pirates = game.GetEnemyLivingPirates();
            if (pirates.Count > 0) game.Debug("Found Camper");
            return pirates.Where(p => p.Distance(MyCity.Location) <= radius).ToArray();
        }
        public Drone[] ScanForDrones(int radius = 7)
        {
            List<Drone> drones = game.GetEnemyLivingDrones();
            return drones.Where(p => p.Distance(EnemyCity.Location) <= radius).ToArray();
        }
        public Drone[] GetAllDrones()
        {
            List<Drone> drones = game.GetEnemyLivingDrones();
            return drones.ToArray();
        }
    }


    #region Interfaces
    interface GameLogic
    {
        LogicedSquad[] AssignSquads(PirateShip[] pirates, GameEngine engine);
        DroneLogic assignDroneLogic(TradeShip drone, GameEngine engine);
    }
    interface PirateLogic
    {
        void DoTurn(Location loc, PirateShip ship);
        void KillCamper(Location loc, PirateShip ship);
    }
    class LogicedPirate
    {
        PirateShip ship;
        PirateLogic logic;
        public LogicedPirate(PirateShip ship, PirateLogic logic)
        {
            this.ship = ship;
            this.logic = logic;
        }
        public void DoTurn(Location loc)
        {
            logic.DoTurn(loc, ship);
        }
        public void KillCamper(Location loc)
        {
            logic.KillCamper(loc, ship);
        }
    }
    interface SquadLogic
    {
        Location CalculateLocation();
    }
    class LogicedSquad
    {
        public Squad squad;
        public SquadLogic logic;
        public LogicedSquad(Squad squad, SquadLogic logic)
        {
            this.squad = squad;
            this.logic = logic;
        }

        public void DoTurn()
        {
            squad.DoTurn(logic.CalculateLocation());
        }
    }
    class CamperSqaud
    {
        public Squad squad;
        public SquadLogic logic;
        public CamperSqaud(Squad squad, SquadLogic logic)
        {
            this.squad = squad;
            this.logic = logic;
        }

        public void DoTurn()
        {
            squad.DoTurn(logic.CalculateLocation());
        }
    }
    interface DroneLogic
    {
        void DoTurn(TradeShip ship);
    }



    #endregion Interfaces

    #region Aircrafts
    class SmartIsland
    {
        Island island;
        GameEngine engine;
        public SmartIsland(Island island, GameEngine engine)
        {
            this.island = island;
            this.engine = engine;
        }



        #region Extends
        public int Id
        {
            get
            {
                return island.Id;
            }
        }
        public Location Location
        {
            get
            {
                return island.Location;
            }
        }
        public Player Owner
        {
            get
            {
                return island.Owner;
            }
        }
        public int TurnsToDroneCreation
        {
            get
            {
                return island.TurnsToDroneCreation;
            }
        }
        public int ControlRange
        {
            get
            {
                return island.ControlRange;
            }
        }
        public bool InControlRange(MapObject other)
        {
            return island.InControlRange(other);
        }
        public int Distance(MapObject other)
        {
            return island.Distance(other);
        }

        public bool IsOurs
        {
            get
            {
                return Owner.Id == engine.Self.Id;
            }
        }
        public bool IsTheirs
        {
            get
            {
                return Owner.Id == engine.Enemy.Id;
            }
        }


        #endregion Extends
    }
    class PirateShip
    {
        public Pirate pirate;
        GameEngine engine;
        public PirateShip(Pirate pirate, GameEngine engine)
        {
            this.pirate = pirate;
            this.engine = engine;
            engine.game.Debug(pirate.Type);
        }



        #region Extending
        public int Id
        {
            get
            {
                return pirate.Id;
            }
        }
        public Player Owner
        {
            get
            {
                return pirate.Owner;
            }
        }
        public int MaxSpeed
        {
            get
            {
                return pirate.MaxSpeed;
            }
        }
        public Location InitialLocation
        {
            get
            {
                return pirate.InitialLocation;
            }
        }
        public Location Location
        {
            get
            {
                return pirate.Location;
            }
        }
        public int Health
        {
            get
            {
                return pirate.CurrentHealth;
            }
        }
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
        public bool IsAlive
        {
            get
            {
                return pirate.IsAlive();
            }
        }

        public int Distance(MapObject other)
        {
            return pirate.Distance(other);
        }
        public bool InAttackRange(MapObject other)
        {
            return Distance(other) <= AttackRange;
        }




        #endregion Extending

        #region Actions
        public void Sail(Location loc)
        {
            engine.SetSail(pirate, loc);
        }
        public Aircraft[] AttackScan()
        {
            return engine.Scan(Location, pirate.AttackRange);
        }

        public bool Attack(Aircraft craft)
        {
            if (InAttackRange(craft))
            {
                engine.Attack(this, craft);
                return true;
            }
            return false;
        }

        public bool TryAttack(bool DroneHigherPriority)
        {
            Aircraft[] crafts = AttackScan().OrderByDescending(x =>
            {
                int score = 0;
                if (DroneHigherPriority)
                {
                    if (x.Type == "Drone") score = 1;
                    else score = 0;
                }
                else
                {
                    if (x.Type == "Drone") score = 0;
                    else score = 1;
                }
                return score;
            }).ToArray();
            if (crafts.Length > 0)
            {
                return Attack(crafts[0]);
            }
            return false;
        }

        public LogicedPirate Logic(PirateLogic logic)
        {
            return new LogicedPirate(this, logic);
        }


        #endregion Actions
    }
    class TradeShip
    {
        Drone drone;
        GameEngine engine;
        public TradeShip(Drone drone, GameEngine engine)
        {
            this.drone = drone;
            this.engine = engine;
        }



        #region Extending
        public int Id
        {
            get
            {
                return drone.Id;
            }
        }
        public Location Location
        {
            get
            {
                return drone.Location;
            }
        }
        public Player Owner
        {
            get
            {
                return drone.Owner;
            }
        }
        public int MaxSpeed
        {
            get
            {
                return drone.MaxSpeed;
            }
        }
        public Location InitialLocation
        {
            get
            {
                return drone.InitialLocation;
            }
        }
        public int CurrentHealth
        {
            get
            {
                return drone.CurrentHealth;
            }
        }
        public int Value
        {
            get
            {
                return drone.Value;
            }
        }

        public int Distance(MapObject other)
        {
            return drone.Distance(other);
        }



        #endregion Extending

        #region Actions
        public void Sail(Location loc)
        {
            engine.SetSail(drone, loc);
        }
        public void Deposit(bool random)
        {
            if (random)
                engine.RandomSail(drone, engine.MyCity.Location);
            else
                engine.SetSail(drone, engine.MyCity.Location, 1);
        }


        #endregion Actions
    }

    class Squad
    {
        public int Count
        {
            get
            {
                return pirates.Length;
            }
        }

        LogicedPirate[] pirates;
        GameEngine engine;
        public Squad(GameEngine engine, params LogicedPirate[] pirates)
        {
            this.pirates = pirates;
            this.engine = engine;
        }
        public void DoTurn(Location loc)
        {
            foreach (LogicedPirate p in pirates)
            {
                p.DoTurn(loc);
            }
        }
        public void KillCamper(Location loc)
        {
            foreach (LogicedPirate p in pirates)
            {
                p.KillCamper(loc);
            }
        }

        public LogicedSquad Logic(SquadLogic logic)
        {
            return new LogicedSquad(this, logic);
        }
    }
    class BasicSquad
    {
        public int Count
        {
            get
            {
                return pirates.Length;
            }
        }

        public PirateShip[] pirates;
        GameEngine engine;
        public BasicSquad(GameEngine engine, params PirateShip[] pirates)
        {
            this.engine = engine;
            this.pirates = pirates;
        }

        public int GetSpread()
        {
            Location Middle = GetMiddle();
            return pirates.Select(x => x.Distance(Middle)).Max();
        }
        public Location GetMiddle()
        {
            int cr = pirates.Select(x => x.Location.Row).Sum() / pirates.Length;
            int cc = pirates.Select(x => x.Location.Col).Sum() / pirates.Length;
            return new Location(cr, cc);
        }
        public Squad Logic(Func<PirateShip, PirateLogic> f)
        {
            return new Squad(engine, pirates.Select(p => p.Logic(f.Invoke(p))).ToArray());
        }
    }



    #endregion Aircrafts

    #region Utils
    class Utils
    {
        public static bool locationsEqual(Location loc1, Location loc2)
        {
            return (loc1.Row == loc2.Row) && (loc1.Col == loc2.Col);
        }

        public static bool isBetween(Location loc, Location bound1, Location bound2)
        {
            return isBetween(loc.Row, bound1.Row, bound2.Row) && isBetween(loc.Col, bound1.Col, bound2.Col);
        }
        public static bool isBetween(int num, int bound1, int bound2)
        {
            if (bound1 > bound2)
                return num >= bound2 && num <= bound1;
            else
                return num >= bound1 && num <= bound2;
        }

        public static PirateShip[] FilterOut(PirateShip[] ships, PirateShip[] filterout)
        {
            int[] ids = filterout.Select(x => x.Id).ToArray();
            return ships.Where(p => !ids.Contains(p.Id)).ToArray();
        }
        public static PirateShip[] FilterOut(PirateShip[] ships, BasicSquad filterout)
        {
            return FilterOut(ships, filterout.pirates);
        }
        public static PirateShip[] FilterOut(PirateShip[] ships, params BasicSquad[] filterout)
        {
            List<PirateShip> _ships = new List<PirateShip>();
            foreach (BasicSquad s in filterout)
            {
                foreach (PirateShip ps in s.pirates)
                {
                    _ships.Add(ps);
                }
            }
            return FilterOut(ships, _ships.ToArray());
        }

        public static BasicSquad CreateSquad(GameEngine engine, PirateShip[] pirates, Func<PirateShip, int> order, int amt)
        {
            return new BasicSquad(engine, pirates.OrderBy(x => order(x)).Take(amt).ToArray());
        }
    }
    class PrioritizedActionList<T>
    {
        ConditioninalAction<T>[] actions;
        public PrioritizedActionList(ConditioninalAction<T>[] actions)
        {
            this.actions = actions;
        }

        public bool invoke(T obj)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                if (actions[i].invoke(obj))
                    return true;
            }
            return false;
        }
    }
    class ConditioninalAction<T>
    {
        private bool state;
        private Func<T, bool> condition;
        private Func<T, bool> action;
        public ConditioninalAction(bool state, Func<T, bool> condition, Func<T, bool> action)
        {
            this.state = state;
            this.condition = condition;
            this.action = action;
        }

        public bool invoke(T obj)
        {
            if (condition.Invoke(obj) && state)
            {
                action(obj);
                return true;
            }
            return false;
        }

        public bool invokeOnArray(T[] arr)
        {
            if (state)
            {
                foreach (T c in arr.Where(x => condition.Invoke(x)))
                {
                    if (action(c))
                        return true;
                }
            }
            return false;
        }
    }


    #endregion Utils
}