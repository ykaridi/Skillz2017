namespace MyBot.Engine
{
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
}
