namespace SimulationModel.Engine.Statistics.Tracer;

/// <summary>
/// Интерфейс трассировщика состояний моделирования.
/// </summary>
public interface ITracer
{
    void Trace(double time, string eventName, string stateSummary, string details);
    void Flush();
}
