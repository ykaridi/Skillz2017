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

        NoCity noCity;
        Simple simple;
        public Bot()
        {
            Engine = new GameEngine();
            noCity = new NoCity();
            simple = new Simple().WithSideEffect(z =>
            {
                z.SetIslandSelector(y =>
                {
                    if (Bot.Engine.NotMyIslands.Length == 0) return Bot.Engine.MyIslands.OrderBy(x => x.Distance(y)).First();
                    else return Bot.Engine.NotMyIslands.OrderBy(x => x.Distance(y)).First();
                }); return 0;
            }).WithSideEffect(z => { z.SetDecoyHandler(x => new EmptyPirate().AttachPlugin(new AntiCamper())); return 0; });
        }

        public void DoTurn(PirateGame game)
        {
            try
            {
                /* Update Game */
                Engine.store.NextTurn();
                Engine.Update(game);

                /* Strategy Change Check */
                if (Engine.Turn > Engine.MaxTurns / 3 && Engine.MyScore == Engine.EnemyScore)
                {
                    simple.SetIslandSelector(Simple.DefaultIslandSelector);
                }

                /* Play Strategy Selection */
                if (Engine.MyCities.Length == 0)
                {
                    if (Engine.EnemyCities.Length > 0)
                    {
                        Engine.DoTurn(noCity, noCity);
                    }
                } else
                {
                    if (Engine.Islands.Length > 0)
                    {
                        Engine.DoTurn(simple, simple);
                    }
                }

                /* Run */
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
                /* Error Handling */
                game.Debug("ERROR");
                game.Debug(e.StackTrace);
            }
        }
    }

    class NoCity : IndividualPirateHandler, IndividualDroneHandler
    {
        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            return new AvoidingDrone();
        }

        public LogicedPirate[] AssignPirateLogic(PirateShip[] p)
        {
            int c = Bot.Engine.EnemyCities.Length;
            ShootingPlugin shooter = new ShootingPlugin().PrioritizeByValue(1.1);
            return p.Select(x =>
            {
                return new LogicedPirate(x, new EmptyPirate().AttachPlugin(new CamperPlugin(Bot.Engine.EnemyCities[x.Id % c], shooter, range: 7)));
            }).ToArray();
        }

        public LogicedPirate DecoyHandler(PirateShip p)
        {
            return new LogicedPirate(p, new EmptyPirate());
        }
    }

    class Simple : IndividualPirateHandler, IndividualDroneHandler
    {
        public delegate SmartIsland IslandSelector(PirateShip s);
        public delegate PirateLogic DecoyHandlerFunction(PirateShip s);
        IslandSelector selector;
        DecoyHandlerFunction dHandler;

        public static IslandSelector DefaultIslandSelector
        {
            get
            {
                return x => Bot.Engine.Islands.OrderBy(y => y.ClosestCity.Distance(y)).ToArray()[x.Id % Bot.Engine.Islands.Length];
            }
        }
        public static DecoyHandlerFunction DefaultDecoyHandlerFunction
        {
            get
            {
                return x => new EmptyPirate().AttachPlugin(new AntiCamper());
            }
        }

        public Simple() : this(DefaultIslandSelector, DefaultDecoyHandlerFunction)
        {
            
        }
        private Simple(IslandSelector selector, DecoyHandlerFunction dHandler)
        {
            SetIslandSelector(selector);
            SetDecoyHandler(dHandler);
        }

        public void SetIslandSelector(IslandSelector selector)
        {
            this.selector = selector;
        }
        public void SetDecoyHandler(DecoyHandlerFunction dHandler)
        {
            this.dHandler = dHandler;
        }

        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            /* Achivement specific code
            if (false && Bot.Engine.Enemy.Score < 10 && Bot.Engine.Enemy.BotName == "12003")
                return new EmptyDrone(); */
            return new AvoidingDrone().Transform(true || Bot.Engine.EnemyIslands.Length == 0 || Bot.Engine.GetEnemyShipsInRange(d.NearestCity, 5).Count > 0, x => x.AttachPlugin(new DronePackingPlugin(Bot.Engine.MaxDronesCount, 5)).AttachPlugin(new DroneGatherPlugin(1, 7, 5, 4)));
        }

        public LogicedPirate[] AssignPirateLogic(PirateShip[] p)
        {
            ShootingPlugin POD = new ShootingPlugin().PrioritizeByValue(0.9);
            /* Achivement specific code
            if ((Bot.Engine.Enemy.Score < 10 && Bot.Engine.Enemy.BotName == "12003"))
                POD = POD.PiratesOnly();*/
            SmartIsland[] Islands = Bot.Engine.Islands.OrderBy(x => x.ClosestCity.Distance(x)).ToArray();
            if (Bot.Engine.MyCities.Length == 1 && Bot.Engine.MyIslands.Length == Bot.Engine.Islands.Length && Bot.Engine.MyLivingDrones.Count == Bot.Engine.MaxDronesCount && Bot.Engine.MaxDronesCount <= Bot.Engine.CountCampers(Bot.Engine.MyCities[0]) * 3)
            {
                return p.Select(x => new LogicedPirate(x, new EmptyPirate().AttachPlugin(new EscortPlugin(Bot.Engine.MyLivingDrones.OrderBy(y => x.Distance(y)).First().Id, 4)))).ToArray();
            }
            else
                return p.Select(x =>
                {
                    return new LogicedPirate(x, new EmptyPirate().Transform(Bot.Engine.MaxDronesCount > 3, y => y.AttachPlugin(POD)).AttachPlugin(new ConquerPlugin(selector(x), true)).Transform(Bot.Engine.MaxDronesCount > 3, y => y.AttachPlugin(new AutoDecoyPlugin())));
                }).ToArray();
        }

        public LogicedPirate DecoyHandler(PirateShip p)
        {
            return new LogicedPirate(p, dHandler(p));
        }
    }
}