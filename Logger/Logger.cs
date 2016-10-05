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
    }
}
