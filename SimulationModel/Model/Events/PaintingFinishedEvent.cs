using Simulation.Engine;
using Workshop.Model.Entities;

namespace Workshop.Model.Events;

/// <summary>
/// Событие окончания окраски одного изделия рабочим на краскопульте.
/// </summary>
public class PaintingFinishedEvent : SimulationEvent
{
    private readonly WorkshopModel _model;
    public Worker Worker { get; }
    public SprayGun SprayGun { get; }
    public Item Item { get; }

    public override string Name => $"PAINT_FINISHED_W{Worker.Id}_SG{SprayGun.Id}";

    public PaintingFinishedEvent(double time, Worker worker, SprayGun sprayGun, Item item, WorkshopModel model)
        : base(time, priority: 15)
    {
        Worker = worker;
        SprayGun = sprayGun;
        Item = item;
        _model = model;
    }

    public override void Execute(Simulator simulator)
    {
        _model.HandlePaintingFinished(Worker, SprayGun, Item, simulator);
    }

    public override string ToString() => $"[{Time:F2}] Завершение окраски изделия #{Item.Id} (рабочий #{Worker.Id}, краскопульт #{SprayGun.Id})";
}
