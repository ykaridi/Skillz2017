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
            simple = new Simple();
        }

        public void DoTurn(PirateGame game)
        {
            try
            {
                Engine.store.NextTurn();
                Engine.Update(game);
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
        public DroneLogic AssignDroneLogic(TradeShip d)
        {
            return new AvoidingDrone().Transform(Bot.Engine.EnemyIslands.Length == 0, x => x.AttachPlugin(new DronePackingPlugin(Bot.Engine.MaxDronesCount, 5)).AttachPlugin(new DroneGatherPlugin(1, 7, 5, 4)));
        }

        public LogicedPirate[] AssignPirateLogic(PirateShip[] p)
        {
            SmartIsland[] Islands = Bot.Engine.Islands.OrderBy(x => x.ClosestCity.Distance(x)).ToArray();
            if (Bot.Engine.MyIslands.Length == Bot.Engine.Islands.Length && Bot.Engine.MyLivingDrones.Count == Bot.Engine.MaxDronesCount && Bot.Engine.MaxDronesCount <= Bot.Engine.CountCampers(Bot.Engine.MyCities[0]) * 3)
            {
                return p.Select(x => new LogicedPirate(x, new EmptyPirate().AttachPlugin(new EscortPlugin(Bot.Engine.MyLivingDrones.OrderBy(y => x.Distance(y)).First().Id, 4)))).ToArray();
            }
            else
                return p.Select(x =>
                {
                    return new LogicedPirate(x, new EmptyPirate().AttachPlugin(new ConquerPlugin(Islands[x.Id % Islands.Length])));
                }).ToArray();
        }

        public LogicedPirate DecoyHandler(PirateShip p)
        {
            return new LogicedPirate(p, new EmptyPirate().AttachPlugin(new AntiCamper()));
        }
    }
}