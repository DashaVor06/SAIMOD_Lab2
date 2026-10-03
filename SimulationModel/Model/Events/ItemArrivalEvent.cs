using Simulation.Engine;
using Workshop.Model.Entities;

namespace Workshop.Model.Events;

/// <summary>
/// Событие поступления нового изделия в красильную мастерскую.
/// </summary>
public class ItemArrivalEvent : SimulationEvent
{
    private readonly WorkshopModel _model;

    public override string Name => "ITEM_ARRIVAL";

    public ItemArrivalEvent(double time, WorkshopModel model) : base(time, priority: 10)
    {
        _model = model;
    }

    public override void Execute(Simulator simulator)
    {
        _model.HandleItemArrival(simulator);
    }
}
