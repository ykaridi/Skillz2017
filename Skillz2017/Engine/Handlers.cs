namespace MyBot.Engine
{
    interface IndividualPirateHandler
    {
        LogicedPirate[] AssignPirateLogic(PirateShip[] p);
        LogicedPirate DecoyHandler(PirateShip p);
    }
    interface SquadPirateHandler
    {
        LogicedPirateSquad[] AssignSquads(PirateShip[] ps);
        LogicedPirate DecoyHandler(PirateShip p);
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
