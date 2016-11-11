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
            return random.Value.Next(min, max+1);
        }
    }
}
