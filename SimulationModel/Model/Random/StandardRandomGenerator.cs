namespace Workshop.Model.Random;

public class StandardRandomGenerator : IRandomGenerator
{
    private readonly System.Random _random;

    public StandardRandomGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
    }

    public double NextUniform()
    {
        double u = _random.NextDouble();
        // Избегаем 0.0 и 1.0 для корректности логарифмирования
        while (u <= 1e-12 || u >= 1.0 - 1e-12)
        {
            u = _random.NextDouble();
        }
        return u;
    }

    public double NextExponential(double rate)
    {
        if (rate <= 0) throw new ArgumentOutOfRangeException(nameof(rate), "Интенсивность должна быть положительной.");
        double u = NextUniform();
        return -Math.Log(1.0 - u) / rate;
    }

    public double NextNormal(double mean, double sigma, double min = 0.0)
    {
        if (sigma <= 0) return Math.Max(min, mean);

        // Усеченное нормальное распределение (метод отбора / Box-Muller)
        for (int i = 0; i < 20; i++)
        {
            double u1 = NextUniform();
            double u2 = NextUniform();

            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            double value = mean + sigma * z;

            if (value >= min)
            {
                return value;
            }
        }

        return min;
    }
}
