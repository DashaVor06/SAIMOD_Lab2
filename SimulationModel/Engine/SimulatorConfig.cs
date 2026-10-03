namespace Simulation.Engine;

/// <summary>
/// Конфигурация прогона имитационной модели.
/// </summary>
public class SimulatorConfig
{
    /// <summary>
    /// Длительность основного периода моделирования (мин).
    /// По умолчанию: 480 минут (8 часов).
    /// </summary>
    public double EndTime { get; set; } = 480.0;

    /// <summary>
    /// Использовать ли период прогрева (Warm-up).
    /// </summary>
    public bool WithWarmup { get; set; } = true;

    /// <summary>
    /// Длительность периода прогрева (мин).
    /// По умолчанию: 60 минут.
    /// </summary>
    public double WarmupTime { get; set; } = 60.0;

    /// <summary>
    /// Завершение с дообслуживанием начатых работ (With clearance).
    /// Если true: после EndTime новые заявки не принимаются, но все находящиеся в системе заявки дорабатываются.
    /// </summary>
    public bool WithClearance { get; set; } = true;

    /// <summary>
    /// Максимальное модельное время прогона (предохранитель от зацикливания).
    /// </summary>
    public double MaxSimulationTime { get; set; } = 5000.0;

    /// <summary>
    /// Включен ли подробный лог (трассировка).
    /// </summary>
    public bool IsLogged { get; set; } = false;

    /// <summary>
    /// Зерно датчика псевдослучайных чисел (RNG Seed).
    /// </summary>
    public int? RngSeed { get; set; } = 0;
}
