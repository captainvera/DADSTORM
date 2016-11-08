using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    public class Logger
    {
        /**
         * Logging level:
         * 0 - Regular
         * 1 - Verbose
         * 2 - Debug
         */

        private string id;

        static int level = 1;

        public Logger(string _id)
        {
            id = _id;
        }

        public void writeLine(string s, params object[] args)
        {
            Console.WriteLine("[" + id + "] " + String.Format(s, args)); 
        }

        public void write(string s, params object[] args)
        {
            Console.Write("[" + id + "] " + String.Format(s, args)); 
        }

        public static void writeLine(string s, string id, params object[] args)
        {
            Console.WriteLine("[" + id + "] " + String.Format(s, args));
        }

        public static void write(string s, string id, params object[] args)
        {
            Console.Write("[" + id + "] " + String.Format(s, args));
        }

        public static void debug(string s, params object[] args)
        {
            if(level > 1)
                Console.Write("[DEBUG] " + String.Format(s, args));
        }
    }
}
