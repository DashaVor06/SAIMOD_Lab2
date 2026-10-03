namespace Simulation.Engine;

/// <summary>
/// Базовый класс события моделирования.
/// </summary>
public abstract class SimulationEvent : ISimulationEvent, IComparable<SimulationEvent>
{
    private static long _sequenceCounter = 0;
    private readonly long _sequenceId;

    public double Time { get; set; }
    public int Priority { get; set; }
    public abstract string Name { get; }

    protected SimulationEvent(double time, int priority = 0)
    {
        Time = time;
        Priority = priority;
        _sequenceId = Interlocked.Increment(ref _sequenceCounter);
    }

    public abstract void Execute(Simulator simulator);

    public int CompareTo(SimulationEvent? other)
    {
        if (other is null) return 1;

        int timeComparison = Time.CompareTo(other.Time);
        if (timeComparison != 0) return timeComparison;

        int priorityComparison = Priority.CompareTo(other.Priority);
        if (priorityComparison != 0) return priorityComparison;

        return _sequenceId.CompareTo(other._sequenceId);
    }

    public override string ToString() => $"[{Time:F2}] {Name} (Priority: {Priority})";
}
