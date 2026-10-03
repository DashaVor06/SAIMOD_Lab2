using ScottPlot;

namespace Reporting;

/// <summary>
/// Генератор графиков и диаграмм результатов имитационного моделирования в формате PNG.
/// </summary>
public static class ChartGenerator
{
    /// <summary>
    /// Генерация гистограмм распределения значений отклика по серии репликаций.
    /// </summary>
    public static void GenerateDistributionPlot(
        MetricSummary metric,
        string outputPath,
        int binCount = 30,
        int width = 900,
        int height = 500)
    {
        if (metric.Values.Count == 0) return;

        var plot = new Plot();

        double min = metric.Min;
        double max = metric.Max;
        if (Math.Abs(max - min) < 1e-6)
        {
            max = min + 1.0;
        }

        // Расчет корзин вручную для полной независимости от версии ScottPlot
        double binWidth = (max - min) / binCount;
        double[] binCenters = new double[binCount];
        double[] binCounts = new double[binCount];

        for (int i = 0; i < binCount; i++)
        {
            binCenters[i] = min + (i + 0.5) * binWidth;
        }

        foreach (var val in metric.Values)
        {
            int idx = (int)((val - min) / binWidth);
            if (idx >= binCount) idx = binCount - 1;
            if (idx < 0) idx = 0;
            binCounts[idx]++;
        }

        // Построение столбчатой диаграммы распределения (осреднено на количество прогонов)
        var bars = new List<Bar>();
        for (int i = 0; i < binCount; i++)
        {
            bars.Add(new Bar
            {
                Position = binCenters[i],
                Value = (double)binCounts[i] / metric.N, // Вероятность (осреднение по прогонам N)
                Size = binWidth * 0.9,
                FillColor = Colors.SteelBlue.WithAlpha(0.8),
                LineColor = Colors.Navy,
                LineWidth = 1
            });
        }

        plot.Add.Bars(bars);

        // Вертикальная линия ожидаемого среднего значения (красная)
        var meanLine = plot.Add.VerticalLine(metric.Mean);
        meanLine.Color = Colors.Crimson;
        meanLine.LineWidth = 2.5f;
        meanLine.LinePattern = LinePattern.Dashed;
        meanLine.LabelText = $"Ожидаемое среднее: {metric.Mean:F2} {metric.Unit}";

        // Оформление графика
        plot.Title($"Гистограмма распределения отклика (осреднено по {metric.N} прогонам)");
        plot.Axes.Bottom.Label.Text = $"{metric.Name} [{metric.Unit}]";
        plot.Axes.Left.Label.Text = "Относительная частота (вероятность)";
        plot.ShowLegend();

        // Сохранение в PNG
        plot.SavePng(outputPath, width, height);
    }

    /// <summary>
    /// Генерация сводной диаграммы доверительных интервалов для откликов.
    /// </summary>
    public static void GenerateConfidenceIntervalsPlot(
        ExperimentResult result,
        string outputPath,
        int width = 900,
        int height = 550)
    {
        var plot = new Plot();

        var metrics = new List<MetricSummary>
        {
            result.ProcessedItems,
            result.MeanTimeInSystem,
            result.MeanBusyWorkers,
            result.MeanBusySprayGuns
        };

        var bars = new List<Bar>();
        for (int i = 0; i < metrics.Count; i++)
        {
            var m = metrics[i];
            bars.Add(new Bar
            {
                Position = i + 1,
                Value = m.Mean,
                Error = m.HalfInterval95,
                Size = 0.6,
                FillColor = Colors.Teal.WithAlpha(0.85),
                LineColor = Colors.DarkSlateGray,
                LineWidth = 1.5f
            });
        }

        plot.Add.Bars(bars);

        // Подписи делений оси X
        Tick[] ticks = new Tick[metrics.Count];
        for (int i = 0; i < metrics.Count; i++)
        {
            string shortLabel = (i + 1) switch
            {
                1 => "Обработано\n[шт]",
                2 => "Время в сист.\n[мин]",
                3 => "Занято раб.\n[чел]",
                4 => "Занято краск.\n[шт]",
                _ => metrics[i].Name
            };
            ticks[i] = new Tick(i + 1, shortLabel);
        }

        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
        plot.Title($"Оценки математических ожиданий и 95% ДИ откликов (N={result.ReplicationsCount})");
        plot.Axes.Left.Label.Text = "Значение отклика";

        plot.SavePng(outputPath, width, height);
    }



    private static (double[] x, double[] y) ToStepLine(List<double> times, List<double> values)
    {
        if (times.Count == 0) return (new double[0], new double[0]);
        var xList = new List<double>();
        var yList = new List<double>();

        xList.Add(times[0]);
        yList.Add(values[0]);

        for (int i = 0; i < times.Count - 1; i++)
        {
            xList.Add(times[i + 1]);
            yList.Add(values[i]);

            xList.Add(times[i + 1]);
            yList.Add(values[i + 1]);
        }

        return (xList.ToArray(), yList.ToArray());
    }

    public static void GenerateSingleResponseDynamicsPlot(
        List<double> times,
        List<double> values,
        string title,
        string unit,
        string outputPath,
        Color lineColor,
        int width = 1000,
        int height = 550)
    {
        var plot = new Plot();

        if (times.Count > 0)
        {
            var (stepTimes, stepValues) = ToStepLine(times, values);
            var sig = plot.Add.ScatterLine(stepTimes, stepValues, lineColor);
            sig.LineWidth = 1.5f;
            sig.LegendText = title;

            // Расчет кривой тренда (скользящее среднее) на стационарном участке (t >= 60)
            var (avgX, avgY) = CalculateMovingAverage(times, values, 40.0, 60.0);
            if (avgX.Length > 0)
            {
                var trendSig = plot.Add.ScatterLine(avgX, avgY, Colors.Crimson);
                trendSig.LineWidth = 2.5f;
                trendSig.LegendText = "Линия тренда (скользящее среднее)";
            }

            var warmupLine = plot.Add.VerticalLine(60.0);
            warmupLine.Color = Colors.Gray;
            warmupLine.LinePattern = LinePattern.Dashed;
            warmupLine.LineWidth = 1.5f;
            warmupLine.LabelText = "Конец прогрева (t=60, стац. режим)";

            var endLine = plot.Add.VerticalLine(480.0);
            endLine.Color = Colors.DarkRed;
            endLine.LinePattern = LinePattern.Dashed;
            endLine.LineWidth = 1.5f;
            endLine.LabelText = "Конец смены (t=480)";
        }

        plot.Title(title);
        plot.Axes.Bottom.Label.Text = "Модельное время (минуты)";
        plot.Axes.Left.Label.Text = $"Значение отклика [{unit}]";
        plot.ShowLegend(Edge.Right);

        plot.SavePng(outputPath, width, height);
    }

    public static void GenerateScatterResponseDynamicsPlot(
        List<double> times,
        List<double> values,
        string title,
        string unit,
        string outputPath,
        int width = 1000,
        int height = 550)
    {
        var plot = new Plot();

        if (times.Count > 0)
        {
            var sig = plot.Add.ScatterPoints(times.ToArray(), values.ToArray(), Colors.DarkBlue);
            sig.MarkerSize = 5f;
            sig.LegendText = "Значения изделий";

            // Расчет кривой тренда (скользящее среднее) на стационарном участке (t >= 60)
            var (avgX, avgY) = CalculateMovingAverage(times, values, 40.0, 60.0);
            if (avgX.Length > 0)
            {
                var trendSig = plot.Add.ScatterLine(avgX, avgY, Colors.Crimson);
                trendSig.LineWidth = 2.5f;
                trendSig.LegendText = "Линия тренда (скользящее среднее)";
            }

            var warmupLine = plot.Add.VerticalLine(60.0);
            warmupLine.Color = Colors.Gray;
            warmupLine.LinePattern = LinePattern.Dashed;
            warmupLine.LineWidth = 1.5f;
            warmupLine.LabelText = "Конец прогрева (t=60, стац. режим)";

            var endLine = plot.Add.VerticalLine(480.0);
            endLine.Color = Colors.DarkRed;
            endLine.LinePattern = LinePattern.Dashed;
            endLine.LineWidth = 1.5f;
            endLine.LabelText = "Конец смены (t=480)";
        }

        plot.Title(title);
        plot.Axes.Bottom.Label.Text = "Модельное время выхода изделия (минуты)";
        plot.Axes.Left.Label.Text = $"Время в мастерской [{unit}]";
        plot.ShowLegend(Edge.Right);

        plot.SavePng(outputPath, width, height);
    }

    private static (double[] x, double[] y) CalculateMovingAverage(List<double> times, List<double> values, double windowSize = 40.0, double startFromTime = 60.0)
    {
        var xList = new List<double>();
        var yList = new List<double>();

        double currentWindowStart = startFromTime;
        double maxTime = 480.0;
        if (times.Count > 0) maxTime = Math.Min(480.0, times[times.Count - 1]);

        while (currentWindowStart < maxTime)
        {
            double windowEnd = currentWindowStart + windowSize;
            double sum = 0;
            int count = 0;

            for (int i = 0; i < times.Count; i++)
            {
                if (times[i] >= currentWindowStart && times[i] < windowEnd)
                {
                    sum += values[i];
                    count++;
                }
            }

            if (count > 0)
            {
                xList.Add(currentWindowStart + windowSize / 2.0);
                yList.Add(sum / count);
            }

            currentWindowStart += windowSize / 4.0; // Высокая детализация кривой тренда
        }

        return (xList.ToArray(), yList.ToArray());
    }
}
