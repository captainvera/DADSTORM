using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    public class Config
    {
        /**
         * Logging level:
         * 0 - light
         * 1 - full
         * 2 - Debug
         */
        public static int logLevel = 2;

        public static void setLoggingLevel(int level)
        {
            Config.logLevel = level;
        }

        public static void setLoggingLevel(string level)
        {
            switch (level)
            {
                case "debug":
                    Config.logLevel = 2;
                    break;
                case "full":
                    Config.logLevel = 1;
                    break;
                case "light":
                    Config.logLevel = 0;
                    break;
                default:
                    Config.logLevel = 0;
                    break;
            }
        }
    }

}
