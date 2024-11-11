namespace ProblemSourceModule.Services.ProblemGenerators
{
    public interface IRandomNumberGenerator
    {
        int Next(int maxExclusive);
        double NextDouble();
    }

    public class DefaultRNG : IRandomNumberGenerator
    {
        private Random rnd;
        public DefaultRNG()
        {
            rnd = new Random();
        }
        public int Next(int maxExclusive) => rnd.Next(maxExclusive);
        public double NextDouble() => rnd.NextDouble();
    }
}
