namespace Simulation.Engine;

/// <summary>
/// Список событий (Event List) для квазипараллельного дискретно-событийного моделирования.
/// Хранит будущие события, упорядоченные по модельному времени наступления.
/// Исключает календарное планирование, реализуя линейно-динамический список событий.
/// </summary>
public class EventList
{
    private readonly List<SimulationEvent> _events = new();

    /// <summary>
    /// Количество запланированных событий в списке.
    /// </summary>
    public int Count => _events.Count;

    /// <summary>
    /// Признак пустоты списка событий.
    /// </summary>
    public bool IsEmpty => _events.Count == 0;

    /// <summary>
    /// Добавление события в список с сохранением упорядоченности по времени и приоритету.
    /// </summary>
    public void Add(SimulationEvent evt)
    {
        if (evt == null) throw new ArgumentNullException(nameof(evt));

        // Бинарный поиск позиции для вставки в упорядоченный список событий
        int index = _events.BinarySearch(evt);
        if (index < 0)
        {
            index = ~index;
        }

        _events.Insert(index, evt);
    }

    /// <summary>
    /// Извлечение первого (наиболее раннего по модельному времени) события из списка.
    /// </summary>
    public SimulationEvent? ExtractNext()
    {
        if (_events.Count == 0) return null;

        var next = _events[0];
        _events.RemoveAt(0);
        return next;
    }

    /// <summary>
    /// Очистка списка событий.
    /// </summary>
    public void Clear()
    {
        _events.Clear();
    }
}
