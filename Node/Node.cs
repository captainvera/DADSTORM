using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Node
    {
        private static Logger log;

        static void Main(string[] args)
        {
            log = new Logger("NodeX");

            log.writeLine("Initializing PCS");
            ProcessCreationService pcs = new ProcessCreationService();
            pcs.createProcess();
            log.writeLine("Node initialized");
            Console.ReadLine();
        }
    }
}
