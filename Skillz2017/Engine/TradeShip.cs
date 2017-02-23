using Pirates;

namespace MyBot.Engine
{
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
}
