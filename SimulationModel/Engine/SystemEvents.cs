namespace Simulation.Engine;

/// <summary>
/// Системное событие окончания периода прогрева.
/// </summary>
public class WarmupEndEvent : SimulationEvent
{
    public override string Name => "SYSTEM_WARMUP_END";

    public WarmupEndEvent(double time) : base(time, priority: -100) { }

    public override void Execute(Simulator simulator)
    {
        simulator.HandleWarmupEnd();
    }
}

/// <summary>
/// Системное событие окончания основного интервала моделирования.
/// </summary>
public class EndTimeReachedEvent : SimulationEvent
{
    public override string Name => "SYSTEM_END_TIME";

    public EndTimeReachedEvent(double time) : base(time, priority: -90) { }

    public override void Execute(Simulator simulator)
    {
        simulator.HandleEndTimeReached();
    }
}
