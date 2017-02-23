using System.Collections.Generic;
using System.Linq;
using Pirates;

namespace MyBot.Engine
{
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
}
