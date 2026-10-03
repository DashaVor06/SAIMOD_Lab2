namespace Simulation.Engine.Statistics;

/// <summary>
/// Статистический сборщик для откликов в непрерывном времени (интегральные характеристики).
/// Вычисляет взвешенное по времени среднее: (1 / T) * integral(x(t) dt).
/// </summary>
public class ContinuousStatistic
{
    public string Name { get; }
    private double _startTime;
    private double _lastUpdateTime;
    private double _currentValue;
    private double _integral;

    public double CurrentValue => _currentValue;
    public double TotalIntegral => _integral;

    public ContinuousStatistic(string name, double initialValue = 0, double startTime = 0)
    {
        Name = name;
        _currentValue = initialValue;
        _startTime = startTime;
        _lastUpdateTime = startTime;
        _integral = 0;
    }

    /// <summary>
    /// Фиксация изменения значения переменной в текущий момент времени.
    /// </summary>
    public void Update(double currentTime, double newValue)
    {
        AdvanceTo(currentTime);
        _currentValue = newValue;
    }

    /// <summary>
    /// Продвижение модельного времени без изменения значения переменной.
    /// </summary>
    public void AdvanceTo(double currentTime)
    {
        if (currentTime > _lastUpdateTime)
        {
            double dt = currentTime - _lastUpdateTime;
            _integral += _currentValue * dt;
            _lastUpdateTime = currentTime;
        }
    }

    /// <summary>
    /// Получение среднего взвешенного по времени значения на текущий момент времени.
    /// </summary>
    public double GetMean(double currentTime)
    {
        AdvanceTo(currentTime);
        double totalTime = currentTime - _startTime;
        return totalTime > 1e-9 ? _integral / totalTime : _currentValue;
    }

    /// <summary>
    /// Сброс накопленной статистики (например, по окончании периода прогрева).
    /// </summary>
    public void Reset(double currentTime)
    {
        _startTime = currentTime;
        _lastUpdateTime = currentTime;
        _integral = 0;
    }
}
