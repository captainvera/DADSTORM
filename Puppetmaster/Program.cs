using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

using Tuple = DADSTORM.Tuple;

namespace DADSTORM
{
    class Program
    {
        private static Logger log;

        static void Main(string[] args)
        {
            log = new Logger("PupperMaster");
            log.writeLine("Starting Puppetmaster");
            log.writeLine("Parsing configuration file");
            
            //Parser parser = new Parser();
            //Dictionary<string, OperatorDTO> operatorDTOs = parser.makeOperatorDTOs(parser.readConfig());

            log.writeLine("Done");

            //Puppetmaster pm = new Puppetmaster();

            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            
            //Get physical node pcs
            log.writeLine("Creating Replica");
            ProcessCreationService pcs = (ProcessCreationService)Activator.GetObject(
                typeof(ProcessCreationService), "tcp://localhost:10000/pcs");

            if (pcs == null)
                log.writeLine("ERROR: NO PCS SERVER");
            
            //Remotely create process in node
            pcs.createProcess("1", "10011", new string[]{"tcp://localhost:10012/Replica2"});
            pcs.createProcess("2", "10012", new string[]{"tcp://localhost:10013/Replica3"});
            pcs.createProcess("3", "10013", new string[]{"X"});

            System.Threading.Thread.Sleep(1000);

            //Connect to created replica;
            log.writeLine("Trying to connect to replica");
            Replica rb = (Replica)Activator.GetObject(typeof(Replica),
                "tcp://localhost:10011/Replica1");
            
            log.writeLine("Done");
            if (rb == null)
                log.writeLine("ERROR: NO SERVER");
            else
            {
                for (int i = 999; i > 0; i--)
                {
                    Tuple t = new Tuple(1);
                    t.set(0, i + " bottles of beer on the wall, " + i + " bottles of beer, take one down, pass it around you got " + (i-1) + " bottles of beer on the wall!");
                    rb.input(t);
                    System.Threading.Thread.Sleep(100);
                    if(i == 970)
                    {
                        rb.freeze();
                        System.Threading.Thread.Sleep(3000);
                        rb.unfreeze();
                    }
                }
            }

            Console.ReadLine();
        }
    }

    class Puppetmaster {
    };
}
