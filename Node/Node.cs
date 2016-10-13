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
            log = new Logger("Physical Node");

            log.writeLine("Initializing PCS");
            ProcessCreationService pcs = new ProcessCreationService();

            //TODO::Publish PCS online

            log.writeLine("PCS created & online");

            pcs.createProcess("1", "10010");

            log.writeLine("Physical Node Initialized");

            Console.ReadLine();
        }
    }
}
