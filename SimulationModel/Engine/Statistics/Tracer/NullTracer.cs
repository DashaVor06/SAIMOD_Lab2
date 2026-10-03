namespace SimulationModel.Engine.Statistics.Tracer
{
    public class NullTracer : ITracer
    {
        public static readonly NullTracer Instance = new();
        public void Trace(double time, string eventName, string stateSummary, string details) { }
        public void Flush() { }
    }
}
