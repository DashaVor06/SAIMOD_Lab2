namespace Reporting;

/// <summary>
/// Статистические характеристики отклика по серии репликаций.
/// </summary>
public class MetricSummary
{
    public string Name { get; }
    public string Unit { get; }
    public List<double> Values { get; } = new();

    public int N => Values.Count;
    public double Mean => N > 0 ? Values.Average() : 0.0;
    public double Min => N > 0 ? Values.Min() : 0.0;
    public double Max => N > 0 ? Values.Max() : 0.0;

    public double Variance => N > 1
        ? Values.Sum(v => Math.Pow(v - Mean, 2)) / (N - 1)
        : 0.0;

    public double StandardDeviation => Math.Sqrt(Variance);
    public double StandardError => N > 0 ? StandardDeviation / Math.Sqrt(N) : 0.0;

    // 95% доверительный интервал (квантиль нормального распределения ~ 1.96)
    public double HalfInterval95 => 1.96 * StandardError;
    public double ConfidenceIntervalLower => Mean - HalfInterval95;
    public double ConfidenceIntervalUpper => Mean + HalfInterval95;

    public MetricSummary(string name, string unit = "")
    {
        Name = name;
        Unit = unit;
    }

    public void Add(double value) => Values.Add(value);
}

/// <summary>
/// Итоговые результаты серии имитационных экспериментов.
/// </summary>
public class ExperimentResult
{
    public int ReplicationsCount { get; }

    // 4 основных отклика по заданию
    public MetricSummary ProcessedItems { get; } = new("1. Количество обработанных изделий (аддитивный)", "шт");
    public MetricSummary MeanTimeInSystem { get; } = new("2. Среднее время в мастерской (дискретный)", "мин");
    public MetricSummary MeanBusyWorkers { get; } = new("3. Среднее число занятых рабочих (непрерывный)", "чел");
    public MetricSummary MeanBusySprayGuns { get; } = new("4. Среднее число занятых краскопультов (непрерывный)", "шт");

    public ExperimentResult(int replicationsCount)
    {
        ReplicationsCount = replicationsCount;
    }
}
