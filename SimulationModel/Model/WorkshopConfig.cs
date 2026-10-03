namespace Workshop.Model;

/// <summary>
/// Параметры красильной мастерской (Вариант 10).
/// </summary>
public class WorkshopConfig
{
    /// <summary>
    /// Количество рабочих (чел). По умолчанию: 6.
    /// </summary>
    public int WorkersCount { get; set; } = 6;

    /// <summary>
    /// Количество краскопультов (шт). По умолчанию: 2.
    /// </summary>
    public int SprayGunsCount { get; set; } = 2;

    /// <summary>
    /// Интенсивность поступления изделий (шт/час). По умолчанию: 18.
    /// В минутах: 18 / 60 = 0.3 шт/мин.
    /// </summary>
    public double ArrivalIntensityPerHour { get; set; } = 12;

    /// <summary>
    /// Интенсивность поступления в минуту (шт/мин).
    /// </summary>
    public double ArrivalIntensityPerMinute => ArrivalIntensityPerHour / 60.0;

    /// <summary>
    /// Среднее время подготовки изделия (мин).
    /// </summary>
    public double PrepMean { get; set; } = 15.0;

    /// <summary>
    /// СКО времени подготовки изделия (мин).
    /// </summary>
    public double PrepSigma { get; set; } = 2.0;

    /// <summary>
    /// Минимальное время подготовки изделия (мин).
    /// </summary>
    public double PrepMin { get; set; } = 13.0;

    /// <summary>
    /// Среднее время окраски изделия (мин).
    /// </summary>
    public double PaintMean { get; set; } = 4.0;

    /// <summary>
    /// СКО времени окраски изделия (мин).
    /// </summary>
    public double PaintSigma { get; set; } = 0.8;

    /// <summary>
    /// Минимальное время окраски изделия (мин).
    /// </summary>
    public double PaintMin { get; set; } = 2.5;

    /// <summary>
    /// Количество изделий в очереди на момент открытия (шт). По умолчанию: 2.
    /// </summary>
    public int InitialItemsAtStart { get; set; } = 2;
}
