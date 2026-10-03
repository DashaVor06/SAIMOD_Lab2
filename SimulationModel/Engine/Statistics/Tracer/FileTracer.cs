namespace SimulationModel.Engine.Statistics.Tracer
{
    public class FileTracer : ITracer, IDisposable
    {
        private readonly StreamWriter _writer;

        public FileTracer(string filePath)
        {
            _writer = new StreamWriter(filePath, append: false, encoding: System.Text.Encoding.UTF8);
            _writer.WriteLine($"{"Время (мин)",12} | {"Событие",-25} | {"Детали",-40} | {"Состояние системы",-30}");
            _writer.WriteLine(new string('-', 120));
        }

        public void Trace(double time, string eventName, string stateSummary, string details)
        {
            _writer.WriteLine($"{time,12:F2} | {eventName,-25} | {details,-40} | {stateSummary,-30}");
        }

        public void Flush() => _writer.Flush();

        public void Dispose()
        {
            _writer.Flush();
            _writer.Dispose();
        }
    }
}
