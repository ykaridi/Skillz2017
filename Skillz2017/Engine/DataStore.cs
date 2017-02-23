using System.Collections.Generic;
using System.Linq;

namespace MyBot.Engine
{
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
}
