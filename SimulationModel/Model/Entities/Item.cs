namespace Workshop.Model.Entities;

/// <summary>
/// Изделие, поступающее на обработку в красильную мастерскую.
/// </summary>
public class Item
{
    public int Id { get; }
    public double CreationTime { get; }
    public double? PrepStartTime { get; set; }
    public double? PrepEndTime { get; set; }
    public double? PaintStartTime { get; set; }
    public double? CompletionTime { get; set; }

    /// <summary>
    /// Флаг: было ли изделие завершено после окончания периода прогрева.
    /// </summary>
    public bool CountInStatistics { get; set; } = true;

    /// <summary>
    /// Общее время нахождения изделия в мастерской (мин).
    /// </summary>
    public double TimeInSystem => (CompletionTime ?? 0) - CreationTime;

    public Item(int id, double creationTime)
    {
        Id = id;
        CreationTime = creationTime;
    }
}
