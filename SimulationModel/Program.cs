using Reporting;
using Simulation.Engine;
using SimulationModel.Engine.Statistics.Tracer;
using Workshop.Model;

namespace SimulationModel;

internal class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("==========================================================================");
        Console.WriteLine(" САИММОД. Лабораторная работа №2. Имитационное моделирование");
        Console.WriteLine(" Вариант №10: Красильная мастерская (6 рабочих, 2 краскопульта)");
        Console.WriteLine(" Архитектура: Квазипараллельный DES-симулятор на базе Списка событий");
        Console.WriteLine("==========================================================================");
        Console.ResetColor();

        if (args.Length > 0 && args[0].Equals("--trace", StringComparison.OrdinalIgnoreCase))
        {
            RunTrace();
            return;
        }

        if (args.Length > 0 && args[0].Equals("--experiment", StringComparison.OrdinalIgnoreCase))
        {
            RunExperiment();
            return;
        }

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Выберите режим работы:");
            Console.WriteLine(" 1. Запустить подробную трассировку 1 прогона (is logged = true, лог в консоль и trace.log)");
            Console.WriteLine(" 2. Запустить имитационный эксперимент (1000 прогонов) с расчетом 4+ откликов и построением PNG-диаграмм");
            Console.WriteLine(" 3. Выполнить всё (трассировка + эксперимент + графики)");
            Console.WriteLine(" 0. Выход");
            Console.Write("\nВаш выбор: ");

            string? choice = Console.ReadLine()?.Trim();
            Console.WriteLine();

            switch (choice)
            {
                case "1":
                    RunTrace();
                    break;
                case "2":
                    RunExperiment();
                    break;
                case "3":
                    RunTrace();
                    RunExperiment();
                    break;
                case "0":
                    return;
                default:
                    Console.WriteLine("Неверный ввод, повторите попытку.");
                    break;
            }
        }
    }

    private static void RunTrace()
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("--- РЕЖИМ ТРАССИРОВКИ (1 рабочий день = 480 минут) ---");
        Console.ResetColor();

        string traceLogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "trace.log");
        Console.WriteLine($"Запись подробного лога также ведется в файл: {traceLogPath}\n");

        var workshopConfig = new WorkshopConfig();
        var simConfig = new SimulatorConfig
        {
            EndTime = 480.0,
            WarmupTime = 60.0,
            WithWarmup = true,
            WithClearance = true,
            IsLogged = true,
            RngSeed = 42
        };

        // Трассировщик, пишущий одновременно в консоль и в файл
        using var fileTracer = new FileTracer(traceLogPath);
        var compositeTracer = new CompositeTracer(new ConsoleTracer(), fileTracer);

        var model = new WorkshopModel(workshopConfig);
        var simulator = new Simulator(model, simConfig, compositeTracer);

        simulator.Run();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n[Трассировка успешно завершена]");
        Console.ResetColor();
        Console.WriteLine($"Итоговое модельное время: {simulator.CurrentTime:F2} мин");
        Console.WriteLine($"Обработано изделий после прогрева: {model.ProcessedItemsCount} шт");
        Console.WriteLine($"Всего завершено изделий: {model.CompletedItems.Count} шт");
        Console.WriteLine($"Среднее время нахождения в мастерской: {model.TimeInSystemStat.Mean:F2} мин");
        Console.WriteLine($"Среднее число занятых рабочих: {model.BusyWorkersStat.GetMean(simulator.CurrentTime):F2} чел");
        Console.WriteLine($"Среднее число занятых краскопультов: {model.BusySprayGunsStat.GetMean(simulator.CurrentTime):F2} шт");
    }

    private static void RunExperiment()
    {
        int replications = 1000;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"--- СЕРИЯ ИЗ {replications} РЕПЛИКАЦИЙ (ВЕРИФИКАЦИЯ МОДЕЛИ) ---");
        Console.ResetColor();

        var runner = new ExperimentRunner();

        Console.Write("Выполнение прогонов: ");
        int lastPercent = -1;
        var result = runner.RunExperiment(replications, baseSeed: 100, (current, total) =>
        {
            int percent = (current * 100) / total;
            if (percent % 10 == 0 && percent != lastPercent)
            {
                Console.Write($"{percent}% ");
                lastPercent = percent;
            }
        });
        Console.WriteLine("\nГотово!\n");

        PrintTable(result);

        // Генерация PNG диаграмм
        string outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Charts");
        Directory.CreateDirectory(outputDir);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nГенерация PNG-диаграмм в папку: {outputDir}...");
        Console.ResetColor();

        string hist1 = Path.Combine(outputDir, "1_dist_processed_items.png");
        string hist2 = Path.Combine(outputDir, "2_dist_time_in_system.png");
        string hist3 = Path.Combine(outputDir, "3_dist_busy_workers.png");
        string hist4 = Path.Combine(outputDir, "4_dist_busy_sprayguns.png");

        string dyn1 = Path.Combine(outputDir, "dynamics_1_processed_items.png");
        string dyn2 = Path.Combine(outputDir, "dynamics_2_time_in_system.png");
        string dyn3 = Path.Combine(outputDir, "dynamics_3_busy_workers.png");
        string dyn4 = Path.Combine(outputDir, "dynamics_4_busy_sprayguns.png");

        ChartGenerator.GenerateDistributionPlot(result.ProcessedItems, hist1);
        ChartGenerator.GenerateDistributionPlot(result.MeanTimeInSystem, hist2);
        ChartGenerator.GenerateDistributionPlot(result.MeanBusyWorkers, hist3);
        ChartGenerator.GenerateDistributionPlot(result.MeanBusySprayGuns, hist4);
        
        runner.RecordSingleRunDynamics(outputDir, seed: 123);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Диаграммы успешно построены и сохранены:");
        Console.ResetColor();
        Console.WriteLine($" - {hist1}");
        Console.WriteLine($" - {hist2}");
        Console.WriteLine($" - {hist3}");
        Console.WriteLine($" - {hist4}");
        Console.WriteLine($" - {dyn3}");
        Console.WriteLine($" - {dyn4}");
        Console.WriteLine($" - {dyn1}");
        Console.WriteLine($" - {dyn2}");

        try
        {
            string solutionRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", ".."));
            if (File.Exists(Path.Combine(solutionRoot, "SimulationModel.sln")))
            {
                string solutionChartsDir = Path.Combine(solutionRoot, "Charts");
                Directory.CreateDirectory(solutionChartsDir);
                foreach (var file in Directory.GetFiles(outputDir, "*.png"))
                {
                    File.Copy(file, Path.Combine(solutionChartsDir, Path.GetFileName(file)), overwrite: true);
                }
                Console.WriteLine($"Копия диаграмм также помещена в корень решения: {solutionChartsDir}");
            }
        }
        catch { }
    }

    private static void PrintTable(ExperimentResult result)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(new string('-', 115));
        Console.WriteLine($"{"Отклик модели",-50} | {"Тип",-12} | {"Среднее (X)",12} | {"95% ДИ",18} | {"СКО (S)",10}");
        Console.WriteLine(new string('-', 115));
        Console.ResetColor();

        PrintRow(result.ProcessedItems, "аддитивный");
        PrintRow(result.MeanTimeInSystem, "дискретный");
        PrintRow(result.MeanBusyWorkers, "непрерывный");
        PrintRow(result.MeanBusySprayGuns, "непрерывный");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(new string('-', 115));
        Console.ResetColor();
    }

    private static void PrintRow(MetricSummary m, string type)
    {
        string meanStr = $"{m.Mean:F2} {m.Unit}";
        string ciStr = $"[{m.ConfidenceIntervalLower:F2}; {m.ConfidenceIntervalUpper:F2}]";
        string sStr = $"{m.StandardDeviation:F2}";

        Console.WriteLine($"{m.Name,-50} | {type,-12} | {meanStr,12} | {ciStr,18} | {sStr,10}");
    }
}

/// <summary>
/// Композитный трассировщик для одновременного логирования в консоль и файл.
/// </summary>
public class CompositeTracer : ITracer
{
    private readonly ITracer[] _tracers;

    public CompositeTracer(params ITracer[] tracers)
    {
        _tracers = tracers;
    }

    public void Trace(double time, string eventName, string stateSummary, string details)
    {
        foreach (var t in _tracers) t.Trace(time, eventName, stateSummary, details);
    }

    public void Flush()
    {
        foreach (var t in _tracers) t.Flush();
    }
}
