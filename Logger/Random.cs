using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DADSTORM
{
    public static class RandomGenerator
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static int nextInt()
        {
            return random.Value.Next();
        }

        public static int nextInt(int min, int max)
        {
            return random.Value.Next(min, max + 1);
        }

        public static double nextDouble(double min, double max)
        {
            return random.Value.NextDouble() * (max - min) + min;

        }

        public static long nextLong(long min, long max)
        {
            long result = random.Value.Next((Int32)(min >> 32), (Int32)(max >> 32));
            result = (result << 32);
            result = result | (long)random.Value.Next((Int32)(min >> 32), (Int32)(max >> 32));
            return result;
        }
    }
}
