namespace SimulationModel.Engine.Statistics.Tracer
{
    public class ConsoleTracer : ITracer
    {
        public void Trace(double time, string eventName, string stateSummary, string details)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"[{time,8:F2} мин] ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{eventName,-24} ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"| {details,-35} ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($"| Состояние: {stateSummary}");
            Console.ResetColor();
        }

        public void Flush() { }
    }
}
