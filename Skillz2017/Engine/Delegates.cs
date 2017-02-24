using Pirates;
using MyBot.Engine;

namespace MyBot.Engine
{
    static class Delegates
    {
        public delegate void SailingFunction(Aircraft ac, Location l);
        public delegate double LocationScoringFunction(Location l);
        public delegate double AircraftScoringFunction(AircraftBase ac);
        public delegate bool AircraftFilterFunction(AircraftBase ac, MapObject l);
        public delegate bool ShouldDecoyFunction(PirateShip ship);
    }
}
