using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Config
    {
        /**
         * Logging level:
         * 0 - light
         * 1 - full
         * 2 - Debug
         */
        public static int logLevel = 0;

        public static void setLoggingLevel(int level)
        {
            Config.logLevel = level;
        }
    }

}
