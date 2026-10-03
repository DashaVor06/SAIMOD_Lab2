using Simulation.Engine;
using SimulationModel.Engine.Statistics.Tracer;
using Workshop.Model;
using ScottPlot;

namespace Reporting;

/// <summary>
/// Проведение серий имитационных экспериментов, верификация и генерация отчетности.
/// </summary>
public class ExperimentRunner
{
    private readonly WorkshopConfig _workshopConfig;
    private readonly SimulatorConfig _baseSimulatorConfig;

    public ExperimentRunner(WorkshopConfig? workshopConfig = null, SimulatorConfig? simulatorConfig = null)
    {
        _workshopConfig = workshopConfig ?? new WorkshopConfig();
        _baseSimulatorConfig = simulatorConfig ?? new SimulatorConfig();
    }

    /// <summary>
    /// Запуск серии из N повторных прогонов (по умолчанию 1000).
    /// </summary>
    public ExperimentResult RunExperiment(int replicationsCount = 1000, int baseSeed = 100, Action<int, int>? progressCallback = null)
    {
        var result = new ExperimentResult(replicationsCount);

        for (int i = 0; i < replicationsCount; i++)
        {
            var simConfig = new SimulatorConfig
            {
                EndTime = _baseSimulatorConfig.EndTime,
                WarmupTime = _baseSimulatorConfig.WarmupTime,
                WithWarmup = _baseSimulatorConfig.WithWarmup,
                WithClearance = _baseSimulatorConfig.WithClearance,
                IsLogged = false,
                RngSeed = baseSeed + i
            };

            var model = new WorkshopModel(_workshopConfig);
            var simulator = new Simulator(model, simConfig, NullTracer.Instance);

            simulator.Run();

            // Сбор откликов после завершения прогона
            double effectiveTime = simulator.CurrentTime - (simConfig.WithWarmup ? simConfig.WarmupTime : 0);
            if (effectiveTime <= 0) effectiveTime = simulator.CurrentTime;

            result.ProcessedItems.Add(model.ProcessedItemsCount);
            result.MeanTimeInSystem.Add(model.TimeInSystemStat.Mean);
            result.MeanBusyWorkers.Add(model.BusyWorkersStat.GetMean(simulator.CurrentTime));
            result.MeanBusySprayGuns.Add(model.BusySprayGunsStat.GetMean(simulator.CurrentTime));

            progressCallback?.Invoke(i + 1, replicationsCount);
        }

        return result;
    }

    /// <summary>
    /// Запуск одного прогона с записью динамики во времени для каждого из 4 откликов по отдельности.
    /// </summary>
    public void RecordSingleRunDynamics(string outputDir, int seed = 42)
    {
        var simConfig = new SimulatorConfig
        {
            EndTime = _baseSimulatorConfig.EndTime,
            WarmupTime = _baseSimulatorConfig.WarmupTime,
            WithWarmup = _baseSimulatorConfig.WithWarmup,
            WithClearance = _baseSimulatorConfig.WithClearance,
            IsLogged = false,
            RngSeed = seed
        };

        var model = new WorkshopModel(_workshopConfig);
        var simulator = new Simulator(model, simConfig, NullTracer.Instance);

        var times = new List<double>();
        var busyWorkers = new List<double>();
        var busyGuns = new List<double>();

        var processedTimes = new List<double>();
        var processedCounts = new List<double>();

        var itemTimes = new List<double>();
        var itemSystemTimes = new List<double>();

        simulator.OnTimeAdvanced += (dt, t) =>
        {
            times.Add(t);
            busyWorkers.Add(model.CurrentBusyWorkers);
            busyGuns.Add(model.CurrentBusySprayGuns);

            processedTimes.Add(t);
            processedCounts.Add(model.CompletedItems.Count(item => item.CompletionTime.HasValue && item.CompletionTime.Value <= t));
        };

        simulator.Run();

        foreach (var item in model.CompletedItems)
        {
            if (item.CompletionTime.HasValue)
            {
                itemTimes.Add(item.CompletionTime.Value);
                itemSystemTimes.Add(item.TimeInSystem);
            }
        }

        Directory.CreateDirectory(outputDir);

        ChartGenerator.GenerateSingleResponseDynamicsPlot(
            times, busyWorkers, 
            "Динамика отклика: Среднее число занятых рабочих", 
            "чел", 
            Path.Combine(outputDir, "dynamics_3_busy_workers.png"), 
            Colors.ForestGreen);

        ChartGenerator.GenerateSingleResponseDynamicsPlot(
            times, busyGuns, 
            "Динамика отклика: Среднее число занятых краскопультов", 
            "шт", 
            Path.Combine(outputDir, "dynamics_4_busy_sprayguns.png"), 
            Colors.DarkOrange);

        ChartGenerator.GenerateSingleResponseDynamicsPlot(
            processedTimes, processedCounts, 
            "Динамика отклика: Количество обработанных изделий (кумулятивно)", 
            "шт", 
            Path.Combine(outputDir, "dynamics_1_processed_items.png"), 
            Colors.SteelBlue);

        ChartGenerator.GenerateScatterResponseDynamicsPlot(
            itemTimes, itemSystemTimes, 
            "Динамика отклика: Время нахождения изделия в мастерской", 
            "мин", 
            Path.Combine(outputDir, "dynamics_2_time_in_system.png"));
    }
}
