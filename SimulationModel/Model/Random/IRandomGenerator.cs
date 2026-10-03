namespace Workshop.Model.Random
{
    public interface IRandomGenerator
    {
        double NextUniform();
        double NextExponential(double rate);
        double NextNormal(double mean, double sigma, double min = 0.0);
    }
}
