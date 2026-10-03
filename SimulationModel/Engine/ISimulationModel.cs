namespace Simulation.Engine;

/// <summary>
/// Интерфейс предметной имитационной модели.
/// Обеспечивает полную независимость ядра симулятора от предметной области.
/// </summary>
public interface ISimulationModel
{
    /// <summary>
    /// Инициализация начального состояния модели и первичное заполнение списка событий.
    /// </summary>
    void Initialize(Simulator simulator);

    /// <summary>
    /// Уведомление об окончании переходного периода (прогрева модели).
    /// </summary>
    void OnWarmupEnded(Simulator simulator);

    /// <summary>
    /// Проверка наличия незавершенной работы в системе для режима дообслуживания (with clearance).
    /// </summary>
    bool HasWorkRemaining();

    /// <summary>
    /// Краткая строка текущего состояния модели для трассировки.
    /// </summary>
    string GetStateSummary();
}
