using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    public class Logger
    {
        private string id;
        public Logger(string _id)
        {
            id = _id;
        }

        public void writeLine(string s)
        {
            Console.WriteLine("[" + id + "] " + s); 
        }

        public void write(string s)
        {
            Console.Write("[" + id + "] " + s); 
        }

        public static void writeLine(string s, string id)
        {
            Console.WriteLine("[" + id + "] " + s);
        }

        public static void write(string s, string id)
        {
            Console.Write("[" + id + "] " + s);
        }
    }
}
