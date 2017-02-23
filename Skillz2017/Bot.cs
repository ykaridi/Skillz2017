using System.Collections.Generic;
using System.Linq;
using Pirates;
using MyBot.Engine;
using MyBot.Pirates;
using MyBot.Pirates.Plugins;
using MyBot.Drones;
using MyBot.Drones.Plugins;

namespace MyBot
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
        public DynamicAssignmentMarkII()
        {
            SUpper = new int[] { 0, 1, 2 }.ToList();
        }

        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            return new AvoidingDrone().Transform(Bot.Engine.GetEnemyShipsInRange(Bot.Engine.MyCities[0], 5).Count > 0 && Bot.Engine.MyScore < 18, x => x.AttachPlugin(new DronePackingPlugin(5, 3)).AttachPlugin(new DroneGatherPlugin(1, 7, 10, 6)));
        }

        public LogicedPirateSquad[] AssignSquads(PirateShip[] ps)
        {
            PirateSquad APirates = new PirateSquad(ps);
            PirateSquad Upper = new PirateSquad(APirates.FilterById(SUpper.ToArray()));
            APirates = new PirateSquad(APirates.FilterOutBySquad(Upper));
            while (Upper.Count() < SUpper.Count() && APirates.Count() > 1)
            {
                int dead = SUpper.First(i => !Bot.Engine.GetMyPirateById(i).IsAlive);
                SUpper.Remove(dead);
                PirateShip p = APirates.OrderBy(x => Upper.Sum(y => x.Distance(y))).First();
                APirates.Remove(p);
                SUpper.Add(p.Id);
                Upper.Add(p);
            }
            PirateSquad Lower = APirates;

            SmartIsland ei = Bot.Engine.Islands.OrderBy(x => x.Distance(Bot.Engine.MyCities[0])).First();
            SmartIsland li = Bot.Engine.Islands[3];

            return new LogicedPirateSquad[]
            {
                new LogicedPirateSquad(Lower, Bot.Engine.EnemyLivingDrones.Count > 7 ? (Bot.Engine.Self.Score >= 16 ? new EPSL() as PirateSquadLogic : new MPSL() as PirateSquadLogic) : new PSL(li) as PirateSquadLogic),
                new LogicedPirateSquad(Upper, Bot.Engine.Self.Score >= 16 ? new EPSL() : Bot.Engine.Islands.ToList().TrueForAll(x => x.Id == 3 || x.IsOurs) ? new MPSL() as PirateSquadLogic : new PSL(Bot.Engine.NotMyIslands.Where(x => x.Id != 3).OrderBy(x => Upper.Sum(y => y.Distance(x))).First()))
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
                ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(1.1);
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
        class Escort : PirateSquadLogic
        {
            int id;
            public Escort(int id)
            {
                this.id = id;
            }
            public void DoTurn(PirateSquad ps)
            {
                EscortPlugin ep = new EscortPlugin(() => Bot.Engine.GetMyDroneById(id));
                ps.ForEach(x => ep.DoTurn(x));
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
                ShootingPlugin dShooter = ShootingPlugin.PrioritizeByHealth().DronesOnly();
                ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PiratesOnly();
                CamperPlugin cp = new CamperPlugin(Bot.Engine.Islands[3].Location.Add(-5, 0), ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(1.1), range: 8);
                ps.ForEach(x =>
                {
                    if (x.CurrentHealth >= 2)
                        if (!shooter.DoTurn(x)) cp.DoTurn(x);
                        else
                            dShooter.DoTurn(x);
                });
            }
        }
        class EPSL : PirateSquadLogic
        {
            public void DoTurn(PirateSquad ps)
            {
                ShootingPlugin dShooter = ShootingPlugin.PrioritizeByHealth().DronesOnly();
                ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PiratesOnly();
                CamperPlugin cp = new CamperPlugin(Bot.Engine.Islands.OrderBy(x => x.Distance(Bot.Engine.MyCities[0])).First().Location.Add(1, System.Math.Sign(Bot.Engine.MyCities[0].Location.Col - Bot.Engine.EnemyCities[0].Location.Col) * 2), ShootingPlugin.PrioritizeByNearnessToDeath().PiratesOnly(), range: 9);
                ps.ForEach(x =>
                {
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
            List<SmartIsland> si = Bot.Engine.NotMyIslands.ToList();
            List<LogicedPirateSquad> pss = new List<LogicedPirateSquad>();
            while (!APirates.IsEmpty())
            {
                IEnumerable<Engine.Tuple<SmartIsland, PirateSquad, double>> ts = si.SelectMany(i =>
                {
                    int amt = Bot.Engine.GetEnemyShipsInRange(i, 5).Count;
                    PirateSquad s = new PirateSquad(APirates.OrderBy(x => x.Distance(i)).Take(amt + 1));
                    if (s.Count >= amt)
                        return new Engine.Tuple<SmartIsland, PirateSquad, double>[] { new Engine.Tuple<SmartIsland, PirateSquad, double>(i, s, /*System.Math.Pow(s.Sum(x => x.Distance(i)), 1.0 / s.Count)*/ s.Max(x => x.Distance(i))) };
                    else
                        return new Engine.Tuple<SmartIsland, PirateSquad, double>[] { };
                }).OrderBy(x => x.arg2);

                if (ts.IsEmpty())
                {
                    pss.Add(new LogicedPirateSquad(APirates, new MPSL()));
                    APirates = new PirateSquad(APirates.FilterOutBySquad(APirates));
                }
                else
                {
                    Engine.Tuple<SmartIsland, PirateSquad, double> t = ts.First();
                    pss.Add(new LogicedPirateSquad(t.arg1, new PSL(t.arg0)));
                    si.Remove(t.arg0);
                    APirates = new PirateSquad(APirates.FilterOutBySquad(t.arg1));
                }
            }
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
                ShootingPlugin shooter = ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(2);
                CamperPlugin cp = new CamperPlugin(new Location(Bot.Engine.Rows / 2, Bot.Engine.Columns / 2), ShootingPlugin.PrioritizeByNearnessToDeath().PrioritizeByValue(0.9), range: 8);
                ps.ForEach(x =>
                {
                    if (!shooter.DoTurn(x))
                        if (x.CurrentHealth >= 2)
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
}