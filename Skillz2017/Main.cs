using System.Collections.Generic;
using System.Linq;
using Pirates;

namespace Skillz2017
{
    public class Bot : IPirateBot
    {
        internal static GameEngine Engine;
        private DynamicAssignmentMarkI dym1;
        private DynamicAssignmentMarkII dym2;
        private IslandSpreadStatic iss;
        public Bot()
        {
            iss = new IslandSpreadStatic();
            dym1 = new DynamicAssignmentMarkI();
            dym2 = new DynamicAssignmentMarkII();
            Engine = new GameEngine();
        }

        public void DoTurn(PirateGame game)
        {
            try
            {
                Engine.Update(game);
                Engine.store.NextTurn();
                Engine.DoTurn(dym2, dym2);
                game.Debug("Store [");
                Engine.store.Keys.ToList().ForEach(k =>
                {
                    game.Debug("\t{" + k + "," + Engine.store[k] + "}");
                });
                game.Debug("];");
                Engine.store.Flush();
            }
            catch (System.Exception e)
            {
                game.Debug("ERROR");
                game.Debug(e.StackTrace);
            }
        }
    }
    class DynamicAssignmentMarkII : SquadPirateHandler, IndividualDroneHandler
    {
        List<int> SUpper;
        List<int> SLower;
        public DynamicAssignmentMarkII()
        {
            SUpper = new int[] { 0, 1, 2 }.ToList();
            SLower = new int[] { 3, 4 }.ToList();
        }

        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            return new AvoidingDrone().Transform(Bot.Engine.GetEnemyShipsInRange(Bot.Engine.MyCities[0], 5).Count > 0, x => x.AttachPlugin(new DroneGatherPlugin(1, 6, 10, 6)));
        }

        public LogicedPirateSquad[] AssignSquads(PirateShip[] ps)
        {
            PirateSquad APirates = new PirateSquad(ps);
            PirateSquad Upper = new PirateSquad(APirates.FilterById(SUpper.ToArray()));
            APirates = new PirateSquad(APirates.FilterOutBySquad(Upper));
            while (Upper.Count() < SUpper.Count() && APirates.Count() > 0)
            {
                int dead = SUpper.First(i => !Bot.Engine.GetMyPirateById(i).IsAlive);
                SUpper.Remove(dead);
                PirateShip p = APirates.OrderBy(x => Upper.Sum(y => x.Distance(y))).First();
                APirates.Remove(p);
                SUpper.Add(p.Id);
                Upper.Add(p);
            }
            PirateSquad Lower = APirates;

            /*
            PirateSquad APirates = new PirateSquad(ps);
            PirateSquad Lower = new PirateSquad(APirates.FilterById(SLower.ToArray()));
            APirates = new PirateSquad(APirates.FilterOutBySquad(Lower));
            while (Lower.Count() < SLower.Count)
            {
                int dead = SLower.First(i => !Bot.Engine.GetMyPirateById(i).IsAlive);
                SLower.Remove(dead);
                PirateShip p = APirates.OrderBy(x => Lower.Sum(y => x.Distance(y))).First();
                SLower.Add(p.Id);
                Lower.Add(p);
            }
            PirateSquad Upper = APirates;
            */

            SmartIsland ei = Bot.Engine.Islands.OrderBy(x => x.Distance(Bot.Engine.EnemyCities[0])).First();
            SmartIsland li = Bot.Engine.Islands[3];

            return new LogicedPirateSquad[]
            {
                new LogicedPirateSquad(Lower, Bot.Engine.EnemyLivingDrones.Count > 7 ? (false ? new PSL(ei) as PirateSquadLogic : new MPSL() as PirateSquadLogic) : new PSL(li) as PirateSquadLogic),
                new LogicedPirateSquad(Upper, Bot.Engine.Islands.ToList().TrueForAll(x => x.Id == 3 || x.IsOurs) ? new MPSL() as PirateSquadLogic : new PSL(Bot.Engine.NotMyIslands.Where(x => x.Id != 3).OrderBy(x => Upper.Sum(y => y.Distance(x))).First()))
            };
        }
        class PSL : PirateSquadLogic
        {
            SmartIsland si;
            public PSL(SmartIsland si)
            {
                this.si = si;
            }
            public void DoTurn(PirateSquad ps)
            {
                ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.9);
                ConquerPlugin conquer = new ConquerPlugin(si);
                EmptyPirate pl = new EmptyPirate().AttachPlugin(shooter).AttachPlugin(conquer);
                Location m = new Location(ps.Sum(x => x.Location.Row) / ps.Count, ps.Sum(x => x.Location.Col) / ps.Count);
                if (true || ps.TrueForAll(x => x.Location.Equals(m)))
                {
                    ps.ForEach(p =>
                    {
                        pl.DoTurnWithPlugins(p);
                    });
                }
                else
                    ps.ForEach(p =>
                    {
                        if (!shooter.DoTurn(p)) p.RandomSail(m);
                    });
            }
        }
        class CPSL : PirateSquadLogic
        {
            public void DoTurn(PirateSquad ps)
            {
                Location loc = Bot.Engine.EnemyCities[0].Location;
                CamperPlugin cp1 = new CamperPlugin(loc.Add(0, System.Math.Sign(Bot.Engine.MyCities[0].Location.Col - loc.Col) * 2), ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(2), range: 7);
                CamperPlugin cp2 = new CamperPlugin(loc.Add(-2, 0), ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(2), range: 7);
                CamperPlugin[] pls = new CamperPlugin[] { cp1, cp2 };
                int i = -1;
                ps.ForEach(x => pls[i += 1].DoTurn(x));
            }
        }
        class MPSL : PirateSquadLogic
        {
            public void DoTurn(PirateSquad ps)
            {
                CamperPlugin cp = new CamperPlugin(Bot.Engine.Islands[3].Location.Add(-5, 0), ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.9), range: 8);
                ps.ForEach(x =>
                {
                    if (x.CurrentHealth >= 2)
                        cp.DoTurn(x);
                });
            }
        }
    }
    class DynamicAssignmentMarkI : SquadPirateHandler, SquadDroneHandler
    {
        private const int MaxDistance = 3;
        private const int MaxSquadSize = 3;

        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            return new AvoidingDrone().AttachPlugin(new DronePackingPlugin(5, 6)).AttachPlugin(new DroneGatherPlugin(1, 4)).AttachPlugin(new DroneFreeze(4));
        }
        public LogicedDroneSquad[] AssignSquads(TradeShip[] ds)
        {
            DSL dsl = new DSL();
            return ds.GroupBy(x => x.Location).Select(x => new DroneSquad(x)).Select(x =>
            {
                return new LogicedDroneSquad(x, dsl);
            }).ToArray();
        }
        public LogicedPirateSquad[] AssignSquads(PirateShip[] ps)
        {
            PirateSquad APirates = new PirateSquad(ps);
            City ec = Bot.Engine.EnemyCities[0];
            LogicedPirateSquad CS = new LogicedPirateSquad(new PirateSquad(APirates.OrderBy(x => x.Distance(ec)).Take(Bot.Engine.EnemyLivingDrones.Count > 3 ? 1 : 0)), new CPSL());
            APirates = new PirateSquad(APirates.FilterOutBySquad(CS.s));
            /*LogicedPirateSquad[] pss = Bot.Engine.MyIslands.OrderByDescending(x => Bot.Engine.GetAircraftsOn(x).Count(a => a.Type == AircraftType.Drone)).Select(i =>
            {
                PirateSquad c = new PirateSquad(APirates.OrderBy(x => x.Distance(i)).Take(1));
                APirates = new PirateSquad(APirates.FilterOutBySquad(c));
                return new LogicedPirateSquad(c, new PSL(i));
            }).ToArray();
            pss = pss.Concat(APirates.GroupBy(x => x.Location, new LocationComparer()).Select(x =>
            {
                PirateSquad c = new PirateSquad(x);
                APirates = new PirateSquad(APirates.FilterOutBySquad(c));
                Location m = c.Middle;
                SmartIsland si = Bot.Engine.NotMyIslands.OrderBy(i => i.Distance(m)).First();
                return new LogicedPirateSquad(c, new PSL(si));
            })).ToArray();*/
            List<SmartIsland> si = Bot.Engine.NotMyIslands.ToList();
            List<LogicedPirateSquad> pss = new List<LogicedPirateSquad>();
            while (!APirates.IsEmpty())
            {
                /*
                PirateShip cp = APirates.First();
                PirateSquad c = new PirateSquad(APirates.OrderBy(x => x.Distance(cp)).TakeWhile(x => x.Distance(cp) <= MaxDistance).Take(MaxSquadSize));
                APirates = new PirateSquad(APirates.FilterOutBySquad(c));
                List<SmartIsland> pi = si.Where(i => Bot.Engine.GetEnemyShipsInRange(i, 5).Count <= c.Count).OrderBy(x => System.Math.Pow(x.Distance(Bot.Engine.MyCities[0]), 2) + System.Math.Pow(x.Distance(cp), 2)).ToList();
                if (pi.Count == 0)
                    pss.Add(new LogicedPirateSquad(c, new MPSL()));
                else
                {
                    SmartIsland mi = pi.First();
                    si.Remove(mi);
                    pss.Add(new LogicedPirateSquad(c, new PSL(mi)));
                }*/
                IEnumerable<Tuple<SmartIsland, PirateSquad, double>> ts = si.SelectMany(i =>
                {
                    int amt = Bot.Engine.GetEnemyShipsInRange(i, 5).Count;
                    PirateSquad s = new PirateSquad(APirates.OrderBy(x => x.Distance(i)).Take(amt + 1));
                    if (s.Count >= amt)
                        return new Tuple<SmartIsland, PirateSquad, double>[] { new Tuple<SmartIsland, PirateSquad, double>(i, s, /*System.Math.Pow(s.Sum(x => x.Distance(i)), 1.0 / s.Count)*/ s.Max(x => x.Distance(i))) };
                    else
                        return new Tuple<SmartIsland, PirateSquad, double>[] { };
                }).OrderBy(x => x.arg2);

                if (ts.IsEmpty())
                {
                    pss.Add(new LogicedPirateSquad(APirates, new MPSL()));
                    APirates = new PirateSquad(APirates.FilterOutBySquad(APirates));
                }
                else
                {
                    Tuple<SmartIsland, PirateSquad, double> t = ts.First();
                    pss.Add(new LogicedPirateSquad(t.arg1, new PSL(t.arg0)));
                    si.Remove(t.arg0);
                    APirates = new PirateSquad(APirates.FilterOutBySquad(t.arg1));
                }
            }
            /*LogicedPirateSquad[] pss = APirates.GroupBy(x => x.Location, new LocationComparer(1)).Select(x =>
            {
                PirateSquad c = new PirateSquad(x);
                APirates = new PirateSquad(APirates.FilterOutBySquad(c));
                Location m = c.Middle;
                SmartIsland si = Bot.Engine.NotMyIslands.OrderBy(i => i.Distance(m)).First();
                return new LogicedPirateSquad(c, new PSL(si));
            }).ToArray();*/
            //if (APirates.Count > 0) pss = pss.Concat(new LogicedPirateSquad[] { new LogicedPirateSquad(APirates, new RPSL()) }).ToArray();
            return pss.Concat(new LogicedPirateSquad[] { CS }).ToArray();
        }

        class LocationComparer : IEqualityComparer<Location>
        {
            int dis;
            public LocationComparer(int dis)
            {
                this.dis = dis;
            }

            public bool Equals(Location x, Location y)
            {
                return x.Distance(y) < dis;
            }

            public int GetHashCode(Location obj)
            {
                return obj.GetHashCode();
            }
        }

        class DroneFreeze : DronePlugin
        {
            int size;
            public DroneFreeze(int size)
            {
                this.size = size;
            }
            public bool DoTurn(TradeShip ship, City city)
            {
                if (Bot.Engine.GetAircraftsOn(ship).Count(x => x.IsOurs && x.Type == AircraftType.Drone) < size) return true;
                else
                    return false;
            }
        }

        class DSL : DroneSquadLogic
        {
            public void DoTurn(DroneSquad ds)
            {
                int mId = ds.First().Id;
                IEnumerable<PirateShip> ps = Bot.Engine.MyPirates.OrderBy(x => x.Distance(ds.Middle));
                if (!ps.IsEmpty() && ds.Count > 5 && ds.First().Distance(Bot.Engine.MyCities[0]) <= 15) ps.OrderBy(x => x.Distance(ds.First())).First().ReserveLogic(new EmptyPirate().AttachPlugin(new EscortPlugin(() => Bot.Engine.GetMyDroneById(mId))));
                ds.ForEach(d =>
                {
                    new AvoidingDrone().AttachPlugin(new DronePackingPlugin(10, 4)).AttachPlugin(new DroneFreeze(4)).DoTurnWithPlugins(d);
                });
            }
        }
        class PSL : PirateSquadLogic
        {
            SmartIsland si;
            public PSL(SmartIsland si)
            {
                this.si = si;
            }
            public void DoTurn(PirateSquad ps)
            {
                ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.9);
                ConquerPlugin conquer = new ConquerPlugin(si);
                EmptyPirate pl = new EmptyPirate().AttachPlugin(shooter).AttachPlugin(conquer);
                Location m = new Location(ps.Sum(x => x.Location.Row) / ps.Count, ps.Sum(x => x.Location.Col) / ps.Count);
                if (ps.TrueForAll(x => x.Location.Equals(m)))
                {
                    ps.ForEach(p =>
                    {
                        pl.DoTurnWithPlugins(p);
                    });
                }
                else
                    ps.ForEach(p =>
                    {
                        if (!shooter.DoTurn(p)) p.RandomSail(m);
                    });
            }
        }
        class CPSL : PirateSquadLogic
        {
            public void DoTurn(PirateSquad ps)
            {
                CamperPlugin cp = new CamperPlugin(Bot.Engine.EnemyCities[0].Location, ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(2), range: 7);
                ps.ForEach(x => cp.DoTurn(x));
            }
        }
        class MPSL : PirateSquadLogic
        {
            public void DoTurn(PirateSquad ps)
            {
                CamperPlugin cp = new CamperPlugin(new Location(Bot.Engine.Rows / 2, Bot.Engine.Columns / 2), ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.9), range: 8);
                ps.ForEach(x =>
                {
                    if (x.CurrentHealth >= 3)
                        cp.DoTurn(x);
                });
            }
        }
    }
    class IslandSpreadStatic : IndividualPirateHandler, IndividualDroneHandler
    {
        private DroneLogic dl;

        public IslandSpreadStatic()
        {
            this.dl = new AvoidingDrone().AttachPlugin(new DronePackingPlugin(10, 4));
        }
        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            return dl;
        }
        public LogicedPirate[] AssignPirateLogic(PirateShip[] ps)
        {
            ShootingPlugin NTD_DOP = ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.9);
            ShootingPlugin NTP = ShootingPlugin.PrioritizeByNearnessToDeath();
            return ps.Select(p =>
            {
                PirateLogic l;
                switch (p.Id)
                {
                    case 0:
                        {
                            if (Bot.Engine.EnemyLivingDrones.Count > 2)
                                l = new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new CamperPlugin(Bot.Engine.EnemyCities[0].Location, NTD_DOP, 9));
                            else
                                l = new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new CamperPlugin(Bot.Engine.Islands.OrderBy(i => i.Distance(Bot.Engine.EnemyCities[0].Location)).First().Location, NTD_DOP));
                            break;
                        }
                    case 1:
                        {
                            l = new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new ConquerPlugin(Bot.Engine.Islands[(p.Id - 1) % 4]));
                            break;
                        }
                    case 2:
                        {
                            l = new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new ConquerPlugin(Bot.Engine.Islands[(p.Id - 1) % 4]));
                            break;
                        }
                    case 3:
                        {
                            l = new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new ConquerPlugin(Bot.Engine.Islands[(p.Id - 1) % 4]));
                            break;
                        }
                    case 4:
                        {
                            l = new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new ConquerPlugin(Bot.Engine.Islands[(p.Id - 1) % 4]));
                            break;
                        }
                    default:
                        {
                            l = new EmptyPirate();
                            break;
                        }
                }
                return new LogicedPirate(p, l);
            }).ToArray();
        }
    }
    class IslandSpread : IndividualPirateHandler, IndividualDroneHandler
    {
        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            return new AvoidingDrone().AttachPlugin(new DronePackingPlugin(10, 4));
        }
        public LogicedPirate[] AssignPirateLogic(PirateShip[] pss)
        {
            ShootingPlugin NTD_DOP = ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.9);
            ShootingPlugin NTP = ShootingPlugin.PrioritizeByNearnessToDeath();

            SmartIsland[] islands = Bot.Engine.Islands.OrderBy(i =>
            {
                int score = 0;
                if (Bot.Engine.GetEnemyShipsInRange(i, 7).Count == 0) score += 10;
                if (i.IsTheirs) score += 5;
                if (!i.IsTheirs && !i.IsOurs) score += 1;
                return score;
            }).ToArray();

            LogicedPirate[] pirates = new LogicedPirate[pss.Length];
            Squad<PirateShip> APirates = new Squad<PirateShip>(pss);
            PirateShip camper = APirates.OrderBy(p => p.Distance(Bot.Engine.EnemyCities[0])).First();
            pirates[0] = new LogicedPirate(camper, new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new CamperPlugin(Bot.Engine.EnemyCities[0].Location, NTD_DOP, 9)));
            APirates = APirates.FilterOutById(camper.Id);
            int baseIdx = 1;
            if (Bot.Engine.GetEnemyShipsInRange(Bot.Engine.MyCities[0], 7).Count > 0 && pss.Length > 1)
            {
                PirateShip anticamper = APirates.OrderBy(p => p.Distance(Bot.Engine.MyCities[0])).First();
                pirates[1] = new LogicedPirate(anticamper, new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new AntiCamperPlugin()));
                APirates = APirates.FilterOutById(anticamper.Id);
                baseIdx += 1;
            }
            for (int i = 0; i < pss.Length - baseIdx; i++)
            {
                PirateShip s = APirates.OrderBy(p => p.Distance(islands[i])).First();
                APirates = APirates.FilterOutById(s.Id);
                pirates[i + baseIdx] = new LogicedPirate(s, new EmptyPirate().AttachPlugin(NTP).AttachPlugin(new ConquerPlugin(islands[i])));
            }
            return pirates;
        }
    }
    #region Game Systems
    #region Basic Drones
    class StupidDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailDefault);
        }
    }
    class RandomDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailRandom);
        }
    }
    class AvoidingDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailMaximizeShipDistance);
        }
    }
    class FarFromMiddleDrone : NearestCityDroneLogic
    {
        public override void Sail(TradeShip ship, City city)
        {
            ship.Sail(city, ship.SailMaximizeDistanceFromMiddle);
        }
    }

    abstract class NearestCityDroneLogic : DroneLogic
    {
        public override City CalculateDepositCity(TradeShip ship)
        {
            return Bot.Engine.MyCities.OrderBy(x => x.Distance(ship)).First();
        }
    }
    #endregion BasicDrones
    #region Pirates
    class EmptyPirate : PirateLogic
    {
        public override void DoTurn(PirateShip pirate)
        {

        }
    }
    #endregion Pirates
    #region Pirate Plugins
    class ShootingPlugin : PiratePlugin
    {
        System.Func<AircraftBase, double> ScoringFunction;
        System.Func<AircraftBase, MapObject, bool> DFilter;
        public ShootingPlugin(System.Func<AircraftBase, double> ScoringMehtod, bool AntiSuicide = false)
        {
            this.ScoringFunction = ScoringMehtod;
            if (AntiSuicide)
            {
                DFilter = new System.Func<AircraftBase, MapObject, bool>((ab, l) =>
                {
                    if (ab.Type == AircraftType.Drone) return true;
                    List<PirateShip> e = Bot.Engine.GetEnemyShipsInAttackRange(l).ToList();
                    int EH = e.Sum(x => x.CurrentHealth);
                    int EHP = e.Count;
                    List<PirateShip> f = Bot.Engine.MyLivingPirates.Filter(x => x.InAttackRange(l)).ToList();
                    int HH = f.Sum(x => x.CurrentHealth);
                    int HHP = f.Count;

                    return EH / HHP < HH / HHP;
                });
            }
            else DFilter = new System.Func<AircraftBase, MapObject, bool>((ab, l) => { return true; });
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
        public bool Scan(MapObject loc, int range, System.Func<AircraftBase, MapObject, bool> Filter, out AircraftBase aircraft, bool OrderByDescending = true)
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
    class ConquerPlugin : PiratePlugin
    {
        SmartIsland island;
        bool InRangeOnly;
        public ConquerPlugin(SmartIsland island, bool InRangeOnly = false)
        {
            this.island = island;
            this.InRangeOnly = InRangeOnly;
        }

        public bool DoTurn(PirateShip ship)
        {
            if (ship.Distance(island) == 0 || (InRangeOnly && island.InControlRange(ship))) return false;
            else
                ship.Sail(island.Location, ship.SailMaximizeDrone);
            return true;
        }
    }
    class EscortPlugin : PiratePlugin
    {
        System.Func<TradeShip> TargetFunc;
        ShootingPlugin shooter;
        public EscortPlugin(System.Func<TradeShip> TargetFunc) : this(TargetFunc, ShootingPlugin.PrioritizeByNearnessToDeath().PiratesOnly()) { }
        public EscortPlugin(System.Func<TradeShip> TargetFunc, ShootingPlugin shooter)
        {
            this.TargetFunc = TargetFunc;
            this.shooter = shooter;
        }

        public bool DoTurn(PirateShip ship)
        {
            TradeShip ts = TargetFunc();
            if (ts == null) return false;
            if (ts.IsAlive && !ts.Location.Equals(ts.InitialLocation))
            {
                ship.Sail(TargetFunc());
                ship.ReserveLogic(new EmptyPirate().AttachPlugin(shooter).AttachPlugin(this));
            }
            return ship.IsAlive;
        }
    }
    class CamperPlugin : PiratePlugin
    {
        Location Camp;
        ShootingPlugin shooter;
        int range;
        public CamperPlugin(Location loc, ShootingPlugin shooter, int range = 13)
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
    class KillerPlugin : PiratePlugin
    {
        System.Func<AircraftBase> TargetFunc;
        public KillerPlugin(System.Func<AircraftBase> TargetFunc)
        {
            this.TargetFunc = TargetFunc;
        }

        public bool DoTurn(PirateShip ship)
        {
            AircraftBase aircraft = TargetFunc();
            if (aircraft == null || !aircraft.IsAlive) return false;
            if (ship.InAttackRange(aircraft))
            {
                ship.Attack(aircraft.aircraft);
                if (aircraft.CurrentHealth > 0) ship.ReserveLogic(new EmptyPirate().AttachPlugin(this));
            }
            else ship.RandomSail(aircraft.Location);
            return true;
        }
    }
    #endregion Pirate Plugins
    #region Drone Plugins
    class DronePackingPlugin : DronePlugin
    {
        int size;
        int fleeDistance;
        public DronePackingPlugin(int size = 5, int fleeDistance = 5)
        {
            this.size = size;
            this.fleeDistance = fleeDistance;
        }

        public bool DoTurn(TradeShip ship, City city)
        {
            if (!ship.Location.Equals(ship.InitialLocation)) return false;
            if (ship.GetEnemyShipsInRange(fleeDistance).Count > 0) return false;
            if (Bot.Engine.GetAircraftsOn(ship).Where(x => x.IsOurs && x.Type == AircraftType.Drone).Count() >= size) return false;
            if (Bot.Engine.MyLivingDrones.Count >= Bot.Engine.MaxDronesCount) return false;
            else return true;
        }
    }
    class DroneGatherPlugin : DronePlugin
    {
        int minSize;
        int maxDistance;
        int maxSize;
        int fleeRange = 4;
        public DroneGatherPlugin(int minSize = 3, int maxDistance = 7, int maxSize = 10, int fleeRange = 4)
        {
            this.minSize = minSize;
            this.maxDistance = maxDistance;
            this.maxSize = maxSize;
            this.fleeRange = fleeRange;
        }

        public bool DoTurn(TradeShip ship, City city)
        {
            object tmp;
            if (Bot.Engine.store.TryGetValue("<Flush>" + ship.Location.ToString(), out tmp))
                return (bool)tmp;

            int rd = System.Math.Abs(ship.Location.Row - city.Location.Row) + 1;
            int cd = System.Math.Abs(ship.Location.Col - city.Location.Col) + 1;

            int mr = System.Math.Min(ship.Location.Row, city.Location.Row);
            int mc = System.Math.Min(ship.Location.Col, city.Location.Col);
            IEnumerable<Location> possibleLocs = Enumerable.Range(0, rd * cd).Select(x => new Location(mr + (x / rd), mc + (x % rd))).Where(x => {
                int c = Bot.Engine.GetAircraftsOn(x).Filter(y => y.IsOurs && y.Type == AircraftType.Drone).Count;
                return x.Distance(ship) > 0 && x.Distance(ship) < maxDistance && x.Distance(city) < ship.Distance(city) && c > 0 && c < maxSize && Bot.Engine.GetEnemyShipsInRange(x, fleeRange).IsEmpty();
            }).OrderBy/*OrderByDescending*/(x =>
            {
                return ship.Distance(x) + x.Distance(city);//Bot.Engine.GetAircraftsOn(x).Filter(y => y.IsOurs && y.Type == AircraftType.Drone).Count;
            });
            if (possibleLocs.IsEmpty())
                return false;
            Location target = possibleLocs.First();

            Squad<AircraftBase> drones = Bot.Engine.GetAircraftsOn(target).Filter(y => y.IsOurs && y.Type == AircraftType.Drone);
            if (drones.Count > 0)
            {
                Bot.Engine.GetAircraftsOn(ship).Where(x => x.IsOurs && x.Type == AircraftType.Drone).ToList().ForEach(x => x.Sail(target, ship.SailMinimizeShips));
                drones.ForEach(d =>
                {
                    d.AsDrone().ReserveLogic(new EmptyDrone());
                });
                Bot.Engine.store.SetValue("<Flush>" + ship.Location.ToString(), true);
                return true;
            }
            else
            {
                Bot.Engine.store.SetValue("<Flush>" + ship.Location.ToString(), false);
                return false;
            }
        }

        class EmptyDrone : NearestCityDroneLogic
        {
            public override void Sail(TradeShip ship, City city)
            {
            }
        }
    }
    #endregion DronePlugins
    #endregion GameSystems
    #region Logic Interfaces
    interface IndividualPirateHandler
    {
        LogicedPirate[] AssignPirateLogic(PirateShip[] p);
    }
    interface SquadPirateHandler
    {
        LogicedPirateSquad[] AssignSquads(PirateShip[] ps);
    }
    interface IndividualDroneHandler
    {
        DroneLogic AssignDroneLogic(TradeShip d);
    }
    interface SquadDroneHandler
    {
        LogicedDroneSquad[] AssignSquads(TradeShip[] ds);
    }
    class LogicedPirate
    {
        public readonly PirateShip s;
        public readonly PirateLogic logic;
        public LogicedPirate(PirateShip s, PirateLogic logic)
        {
            this.s = s;
            this.logic = logic;
        }

        public void DoTurn()
        {
            logic.DoTurn(s);
        }
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
    class LogicedPirateSquad
    {
        public delegate void Logic();

        public readonly LogicedPirate[] lps;
        public readonly PirateSquad s;
        public readonly Logic logic;
        public LogicedPirateSquad(PirateSquad s, PirateSquadLogic logic)
        {
            this.s = s;
            this.logic = () => logic.DoTurn(s);
        }
        public LogicedPirateSquad(LogicedPirate[] pirates)
        {
            lps = pirates;
            s = new PirateSquad(pirates.Select(x => x.s));
            this.logic = () => lps.ToList().ForEach(x => x.DoTurn());
        }

        public void DoTurn()
        {
            logic.Invoke();
        }
    }
    interface PirateSquadLogic
    {
        void DoTurn(PirateSquad ps);
    }
    class LogicedDroneSquad
    {
        public delegate void Logic();

        public readonly DroneSquad s;
        public readonly LogicedDrone[] lds;
        public readonly Logic logic;
        public LogicedDroneSquad(DroneSquad s, DroneSquadLogic logic)
        {
            this.s = s;
            this.logic = () => logic.DoTurn(s);
        }
        public LogicedDroneSquad(LogicedDrone[] drones)
        {
            lds = drones;
            s = new DroneSquad(drones.Select(x => x.s));
            logic = () => lds.ToList().ForEach(x => x.DoTurn());
        }

        public void DoTurn()
        {
            logic();
        }
    }
    interface DroneSquadLogic
    {
        void DoTurn(DroneSquad ds);
    }
    class LogicedDrone
    {
        public readonly TradeShip s;
        public readonly DroneLogic logic;
        public LogicedDrone(TradeShip s, DroneLogic logic)
        {
            this.s = s;
            this.logic = logic;
        }

        public void DoTurn()
        {
            logic.DoTurnWithPlugins(s);
        }
    }
    abstract class DroneLogic
    {
        public DronePlugin[] Plugins
        {
            get; set;
        } = new DronePlugin[0];

        public abstract City CalculateDepositCity(TradeShip ship);
        public abstract void Sail(TradeShip ship, City city);
    }
    interface DronePlugin
    {
        bool DoTurn(TradeShip ship, City city);
    }
    #endregion Logic Interfaces
    #region Game Classes
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
            return game.GetEnemyPirateById(id).Upgrade();
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
            if (aircraft.Distance(destination) > 0)
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
    #endregion Game Classes
    #region Aircrafts
    class SmartIsland : MapObject
    {
        Island island;
        public SmartIsland(Island island)
        {
            this.island = island;
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
        public bool IsOurs
        {
            get
            {
                return Owner.Id == Bot.Engine.Self.Id;
            }
        }
        public bool IsTheirs
        {
            get
            {
                return Owner.Id == Bot.Engine.Enemy.Id;
            }
        }
        public bool IsNeutral
        {
            get
            {
                return Owner.Id == Bot.Engine.Neutral.Id;
            }
        }
        public bool InControlRange(MapObject other)
        {
            return island.InControlRange(other);
        }

        public override Location GetLocation()
        {
            return Location;
        }
        #endregion Extends
        #region Custom
        public City ClosestCity
        {
            get
            {
                return Bot.Engine.MyCities.OrderBy(c => c.Distance(this)).First();
            }
        }
        #endregion Custom
    }
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
    class PirateShip : AircraftBase
    {
        Pirate pirate;
        public PirateShip(Pirate pirate) : base(pirate)
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
            Bot.Engine.Attack(pirate, aircraft);
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
            return Bot.Engine.GetEnemyAircraftsInRange(Location, range);
        }

        public void ReserveLogic(PirateLogic logic)
        {
            Bot.Engine.store.AssignLogic(this, logic);
        }
        #endregion Custom
    }
    class TradeShip : AircraftBase
    {
        Drone drone;
        public TradeShip(Drone drone) : base(drone)
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
        public void ReserveLogic(DroneLogic logic)
        {
            Bot.Engine.store.AssignLogic(this, logic);
        }
        #endregion Custom
    }
    class Squad<T> : List<T> where T : AircraftBase
    {
        public Squad(IEnumerable<T> aircrafts) : base()
        {
            this.AddRange(aircrafts);
        }

        #region Extends
        public Squad<T> Select(System.Func<T, T> Selector)
        {
            return new Squad<T>(this.AsEnumerable().Select(Selector));
        }
        public Squad<T> Filter(System.Func<T, bool> Predicate)
        {
            return new Squad<T>(this.Where(Predicate));
        }
        #endregion Extends
        #region Custom
        public Squad<T> FilterById(params int[] id)
        {
            return FilterById(id.AsEnumerable());
        }
        public Squad<T> FilterById(IEnumerable<int> ids)
        {
            return Filter(x => ids.Contains(x.Id));
        }
        public Squad<T> FilterOutById(params int[] id)
        {
            return FilterOutById(id.AsEnumerable());
        }
        public Squad<T> FilterOutById(IEnumerable<int> ids)
        {
            return Filter(x => !ids.Contains(x.Id));
        }
        public Squad<T> FilterOutBySquad(Squad<T> squad)
        {
            return FilterOutById(squad.Select(x => x.Id));
        }

        public Location Middle
        {
            get
            {
                return new Location(this.Select(x => x.Location.Row).Sum() / Count, this.Select(x => x.Location.Col).Sum() / Count);
            }
        }
        #endregion Custom
    }
    class PirateSquad : Squad<PirateShip>
    {
        public PirateSquad(IEnumerable<PirateShip> pirates) : base(pirates) { }
        public static PirateSquad SquadFromIds(IEnumerable<PirateShip> pirates, params int[] ids)
        {
            return SquadFromIds(pirates, ids);
        }
        public static PirateSquad SquadFromIds(IEnumerable<PirateShip> pirates, IEnumerable<int> ids)
        {
            return new PirateSquad(pirates.Where(x => ids.Contains(x.Id)));
        }
    }
    class DroneSquad : Squad<TradeShip>
    {
        public DroneSquad(IEnumerable<TradeShip> drones) : base(drones) { }
        public static DroneSquad SquadFromIds(IEnumerable<TradeShip> drones, params int[] ids)
        {
            return SquadFromIds(drones, ids);
        }
        public static DroneSquad SquadFromIds(IEnumerable<TradeShip> drones, IEnumerable<int> ids)
        {
            return new DroneSquad(drones.Where(x => ids.Contains(x.Id)));
        }
    }
    #endregion Aircrafts

    #region Utils
    static class PirateGameExtensions
    {
        private static AircraftType DetermineType(string type)
        {
            if (type.Equals("Drone"))
                return AircraftType.Drone;
            else if (type.Equals("Pirate"))
                return AircraftType.Pirate;
            else
                return AircraftType.Generic;
        }
        public static AircraftType DetermineType(this Aircraft aircraft)
        {
            return DetermineType(aircraft.Type);
        }

        public static AircraftBase Upgrade(this Aircraft aircraft)
        {
            return new AircraftBase(aircraft);
        }
        public static PirateShip Upgrade(this Pirate pirate)
        {
            return new PirateShip(pirate);
        }
        public static TradeShip Upgrade(this Drone drone)
        {
            return new TradeShip(drone);
        }
        public static Squad<PirateShip> Squad(this IEnumerable<Pirate> pirates)
        {
            return new Squad<PirateShip>(pirates.Select(p => p.Upgrade()));
        }
        public static Squad<TradeShip> Squad(this IEnumerable<Drone> drones)
        {
            return new Squad<TradeShip>(drones.Select(d => d.Upgrade()));
        }
        public static Squad<AircraftBase> Squad(this IEnumerable<Aircraft> aircrafts)
        {
            return new Squad<AircraftBase>(aircrafts.Select(b => b.Upgrade()));
        }

        public static T AttachPlugin<T>(this T logic, PiratePlugin plugin) where T : PirateLogic
        {
            PiratePlugin[] old = logic.Plugins;
            PiratePlugin[] plugins = new PiratePlugin[old.Length + 1];
            for (int i = 0; i < old.Length; i++)
            {
                plugins[i] = old[i];
            }
            plugins[old.Length] = plugin;
            logic.Plugins = plugins;
            return logic;
        }
        public static void DoTurnWithPlugins(this PirateLogic logic, PirateShip ship)
        {
            bool stop = false;
            foreach (PiratePlugin plugin in logic.Plugins)
            {
                if ((stop = plugin.DoTurn(ship)))
                    break;
                stop = stop || (!ship.CanPlay);
            }
            if (!stop)
                logic.DoTurn(ship);
        }
        public static T AttachPlugin<T>(this T logic, DronePlugin plugin) where T : DroneLogic
        {
            DronePlugin[] old = logic.Plugins;
            DronePlugin[] plugins = new DronePlugin[old.Length + 1];
            for (int i = 0; i < old.Length; i++)
            {
                plugins[i] = old[i];
            }
            plugins[old.Length] = plugin;
            logic.Plugins = plugins;
            return logic;
        }
        public static void DoTurnWithPlugins(this DroneLogic logic, TradeShip ship)
        {
            City city = logic.CalculateDepositCity(ship);
            bool stop = false;
            foreach (DronePlugin plugin in logic.Plugins)
            {
                if ((stop = plugin.DoTurn(ship, city)))
                    break;
                stop = stop || (!ship.CanPlay);
            }
            if (!stop)
                logic.Sail(ship, city);
        }
    }
    static class Extensions
    {
        public static bool isBetween(this MapObject loc, MapObject bound1, MapObject bound2)
        {
            return isBetween(loc.GetLocation().Row, bound1.GetLocation().Row, bound2.GetLocation().Row) && isBetween(loc.GetLocation().Col, bound1.GetLocation().Col, bound2.GetLocation().Col);
        }
        public static Location Add(this Location loc, int X, int Y)
        {
            return new Location(loc.Row + X, loc.Col + Y);
        }
        private static bool isBetween(int num, int bound1, int bound2)
        {
            if (bound1 > bound2)
                return num >= bound2 && num <= bound1;
            else
                return num >= bound1 && num <= bound2;
        }

        public static System.Func<T, double> Times<T>(this System.Func<T, double> f, System.Func<T, double> other)
        {
            return new System.Func<T, double>(x => f(x) * other(x));
        }
        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }
        public static T Transform<T>(this T arg, bool Condition, System.Func<T, T> Transformation)
        {
            if (Condition)
                return Transformation(arg);
            else return arg;
        }

        public static string Join(this string[] arr, string delimiter)
        {
            string str = "";
            foreach (string s in arr)
            {
                str += delimiter;
                str += s;
            }
            return str.Substring(1);
        }
    }
    class Tuple<T>
    {
        public T arg0;
        public Tuple(T arg0)
        {
            this.arg0 = arg0;
        }
    }
    class Tuple<T, U> : Tuple<T>
    {
        public U arg1;
        public Tuple(T arg0, U arg1) : base(arg0)
        {
            this.arg1 = arg1;
        }
    }
    class Tuple<T, U, W> : Tuple<T, U>
    {
        public W arg2;
        public Tuple(T arg0, U arg1, W arg2) : base(arg0, arg1)
        {
            this.arg2 = arg2;
        }
    }
    class DataStore : Dictionary<string, object>
    {
        public DataStore() : base()
        {

        }

        public void AssignLogic(PirateShip pirate, PirateLogic logic)
        {
            this["[Wait]<Assignment>Pirate-" + pirate.Id] = logic;
        }
        public bool TryGetLogic(PirateShip pirate, out PirateLogic logic)
        {
            logic = null;
            object obj;
            bool res = TryGetValue("<Assignment>Pirate-" + pirate.Id, out obj);
            if (res)
                logic = (PirateLogic)obj;
            return res;
        }
        public void AssignLogic(PirateSquad squad, PirateSquadLogic logic)
        {
            string si = "[" + squad.Select(x => x.Id.ToString()).OrderBy(x => x).ToArray().Join(",") + "]";
            this["[Wait]<Assignment>PSquad-" + si] = logic;
        }
        public List<List<int>> GetPirateSquads()
        {
            List<List<int>> squads = new List<List<int>>();
            foreach (string key in this.Keys)
            {
                if (key.StartsWith("<Assignment>PSquad-"))
                {
                    string si = key.Substring("<Assignment>PSquad-".Length);
                    squads.Add(si.Split(',').Select(x => int.Parse(x)).ToList());
                }
            }
            return squads;
        }
        public bool TryGetLogic(PirateSquad squad, out PirateSquadLogic logic)
        {
            logic = null;
            string si = "[" + squad.Select(x => x.Id.ToString()).OrderBy(x => x).ToArray().Join(",") + "]";
            object obj;
            bool res = TryGetValue("<Assignment>PSquad-" + si, out obj);
            if (res)
                logic = (PirateSquadLogic)obj;
            return res;
        }
        public void AssignLogic(DroneSquad squad, DroneSquadLogic logic)
        {
            string si = "[" + squad.Select(x => x.Id.ToString()).OrderBy(x => x).ToArray().Join(",") + "]";
            this["[Wait]<Assignment>DSquad-" + si] = logic;
        }
        public List<List<int>> GetDroneSquads()
        {
            List<List<int>> squads = new List<List<int>>();
            foreach (string key in this.Keys)
            {
                if (key.StartsWith("<Assignment>DSquad-"))
                {
                    string si = key.Substring("<Assignment>DSquad-".Length);
                    squads.Add(si.Split(',').Select(x => int.Parse(x)).ToList());
                }
            }
            return squads;
        }
        public bool TryGetLogic(DroneSquad squad, out DroneSquadLogic logic)
        {
            logic = null;
            string si = "[" + squad.Select(x => x.Id.ToString()).OrderBy(x => x).ToArray().Join(",") + "]";
            object obj;
            bool res = TryGetValue("<Assignment>DSquad-" + si, out obj);
            if (res)
                logic = (DroneSquadLogic)obj;
            return res;
        }
        public void AssignLogic(TradeShip drone, DroneLogic logic)
        {
            this["[Wait]<Assignment>Drone-" + drone.Id] = logic;
        }
        public bool TryGetLogic(TradeShip drone, out DroneLogic logic)
        {
            logic = null;
            object obj;
            bool res = TryGetValue("<Assignment>Drone-" + drone.Id, out obj);
            if (res)
                logic = (DroneLogic)obj;
            return res;
        }
        public void Flush()
        {
            string[] keys = Keys.ToArray();
            foreach (string key in keys)
            {
                if (key.StartsWith("<Flush>") || key.StartsWith("<Assignment>")) this.Remove(key);
            }
        }
        public void NextTurn()
        {
            string[] keys = Keys.ToArray();
            foreach (string key in keys)
            {
                if (key.StartsWith("[Wait]"))
                {
                    object value = this[key];
                    this.Remove(key);
                    this[key.Substring("[Wait]".Length)] = value;
                }
            }
        }

        public T GetValue<T>(string key, T def)
        {
            if (ContainsKey(key)) return (T)this[key];
            else return def;
        }
        public void SetValue<T>(string key, T value)
        {
            this[key] = value;
        }
    }
    #endregion Utils
}