using System.Linq;

namespace MyBot.Engine
{
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
}
