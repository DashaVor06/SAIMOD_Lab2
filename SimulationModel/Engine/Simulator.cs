using SimulationModel.Engine.Statistics.Tracer;

namespace Simulation.Engine;

/// <summary>
/// Универсальный квазипараллельный имитатор (Simulation Engine).
/// Не зависит от предметной области конкретной модели.
/// Управляет модельным временем, Списком событий (Event List) и циклом моделирования.
/// </summary>
public class Simulator
{
    public double CurrentTime { get; private set; }
    public EventList Events { get; } = new();
    public ISimulationModel Model { get; }
    public SimulatorConfig Config { get; }
    public ITracer Tracer { get; set; }

    public bool IsWarmupCompleted { get; private set; }
    public bool IsArrivalStopped { get; private set; }
    public bool IsStopped { get; private set; }

    /// <summary>
    /// Событие продвижения модельного времени: (dt, currentTime).
    /// Используется статистическими сборщиками для расчета интегралов.
    /// </summary>
    public event Action<double, double>? OnTimeAdvanced;

    /// <summary>
    /// Событие завершения прогрева (для сброса статистик).
    /// </summary>
    public event Action<double>? OnWarmupEnded;

    public Simulator(ISimulationModel model, SimulatorConfig config, ITracer? tracer = null)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
        Config = config ?? throw new ArgumentNullException(nameof(config));
        Tracer = tracer ?? (config.IsLogged ? new ConsoleTracer() : NullTracer.Instance);
    }

    /// <summary>
    /// Запланировать событие в Список событий (Event List).
    /// </summary>
    public void Schedule(SimulationEvent evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));
        if (evt.Time < CurrentTime)
        {
            throw new InvalidOperationException(
                $"Попытка запланировать событие '{evt.Name}' в прошлое: время события {evt.Time:F2}, текущее время {CurrentTime:F2}");
        }

        Events.Add(evt);
    }

    /// <summary>
    /// Запуск имитационного прогона.
    /// </summary>
    public void Run()
    {
        CurrentTime = 0.0;
        Events.Clear();
        IsWarmupCompleted = !Config.WithWarmup;
        IsArrivalStopped = false;
        IsStopped = false;

        // Планирование системных событий в Список событий
        if (Config.WithWarmup && Config.WarmupTime > 0 && Config.WarmupTime < Config.EndTime)
        {
            Schedule(new WarmupEndEvent(Config.WarmupTime));
        }

        Schedule(new EndTimeReachedEvent(Config.EndTime));

        // Инициализация начального состояния предметной модели
        Model.Initialize(this);

        Tracer.Trace(CurrentTime, "SIMULATION_START", Model.GetStateSummary(), "Старт моделирования");

        // Главный цикл квазипараллельного дискретно-событийного моделирования
        while (!IsStopped && !Events.IsEmpty && CurrentTime <= Config.MaxSimulationTime)
        {
            var nextEvent = Events.ExtractNext();
            if (nextEvent == null) break;

            // Продвижение модельного времени к моменту наступления события (метод особых состояний)
            AdvanceTime(nextEvent.Time);

            // Выполнение события
            nextEvent.Execute(this);

            // Трассировка
            Tracer.Trace(CurrentTime, nextEvent.Name, Model.GetStateSummary(), nextEvent.ToString());

            // Если основной период завершен и включен режим дообслуживания,
            // проверяем, осталась ли работа в системе
            if (IsArrivalStopped && Config.WithClearance)
            {
                if (!Model.HasWorkRemaining())
                {
                    StopSimulation("Все начатые работы успешно дообслужены (система пуста).");
                    break;
                }
            }
        }

        Tracer.Trace(CurrentTime, "SIMULATION_END", Model.GetStateSummary(), "Окончание моделирования");
        Tracer.Flush();
    }

    /// <summary>
    /// Продвижение модельного времени и оповещение подписчиков (сборщиков непрерывной статистики).
    /// </summary>
    public void AdvanceTime(double newTime)
    {
        if (newTime > CurrentTime)
        {
            double dt = newTime - CurrentTime;
            CurrentTime = newTime;
            OnTimeAdvanced?.Invoke(dt, CurrentTime);
        }
    }

    /// <summary>
    /// Обработка системного события завершения периода прогрева.
    /// </summary>
    internal void HandleWarmupEnd()
    {
        IsWarmupCompleted = true;
        OnWarmupEnded?.Invoke(CurrentTime);
        Model.OnWarmupEnded(this);
        Tracer.Trace(CurrentTime, "WARMUP_COMPLETED", Model.GetStateSummary(), "Период прогрева завершен, накопление чистой статистики.");
    }

    /// <summary>
    /// Обработка системного события достижения основного времени моделирования (EndTime).
    /// </summary>
    internal void HandleEndTimeReached()
    {
        IsArrivalStopped = true;

        if (!Config.WithClearance)
        {
            StopSimulation("Достигнуто предельное время прогона без дообслуживания.");
        }
        else
        {
            // При дообслуживании прекращается генерация новых входных заявок,
            // но работы в очередях и на станках продолжаются.
            Tracer.Trace(CurrentTime, "CLEARANCE_START", Model.GetStateSummary(), "Прекращение генерации новых заявок, режим дообслуживания начат.");
            if (!Model.HasWorkRemaining())
            {
                StopSimulation("В момент EndTime незавершенных работ нет.");
            }
        }
    }

    /// <summary>
    /// Принудительная остановка симулятора.
    /// </summary>
    public void StopSimulation(string reason)
    {
        IsStopped = true;
        Tracer.Trace(CurrentTime, "SIMULATION_STOPPED", Model.GetStateSummary(), reason);
    }
}
