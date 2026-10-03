using Simulation.Engine;
using Workshop.Model.Entities;

namespace Workshop.Model.Events;

/// <summary>
/// Событие окончания подготовки изделия рабочим.
/// </summary>
public class PreparationFinishedEvent : SimulationEvent
{
    private readonly WorkshopModel _model;
    public Worker Worker { get; }

    public override string Name => $"PREP_FINISHED_W{Worker.Id}";

    public PreparationFinishedEvent(double time, Worker worker, WorkshopModel model)
        : base(time, priority: 20)
    {
        Worker = worker;
        _model = model;
    }

    public override void Execute(Simulator simulator)
    {
        _model.HandlePreparationFinished(Worker, simulator);
    }

    public override string ToString() => $"[{Time:F2}] Завершение подготовки рабочим #{Worker.Id}";
}
