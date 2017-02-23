using Pirates;
using System.Collections.Generic;
using System.Linq;
using MyBot.Drones.Plugins;
using MyBot.Engine;
using MyBot.Pirates.Plugins;

namespace MyBot
{
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
}
