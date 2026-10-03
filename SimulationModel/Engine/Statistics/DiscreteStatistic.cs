namespace Simulation.Engine.Statistics;

/// <summary>
/// Статистический сборщик для откликов в дискретном времени (выборочные величины по заявкам/событиям).
/// </summary>
public class DiscreteStatistic
{
    public string Name { get; }
    public long Count { get; private set; }
    public double Sum { get; private set; }
    public double SumOfSquares { get; private set; }
    public double Min { get; private set; } = double.MaxValue;
    public double Max { get; private set; } = double.MinValue;

    public double Mean => Count > 0 ? Sum / Count : 0.0;

    public double Variance => Count > 1 ? Math.Max(0.0, (SumOfSquares - (Sum * Sum) / Count) / (Count - 1)) : 0.0;

    public double StandardDeviation => Math.Sqrt(Variance);

    public DiscreteStatistic(string name)
    {
        Name = name;
    }

    public void Add(double value)
    {
        Count++;
        Sum += value;
        SumOfSquares += value * value;
        if (value < Min) Min = value;
        if (value > Max) Max = value;
    }

    public void Reset()
    {
        Count = 0;
        Sum = 0;
        SumOfSquares = 0;
        Min = double.MaxValue;
        Max = double.MinValue;
    }
}
