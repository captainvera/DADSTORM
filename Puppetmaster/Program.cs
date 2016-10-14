using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;
using System.Collections;


namespace DADSTORM
{
    class Program
    {
        private static Logger log;

        static void Main(string[] args)
        {
            log.writeLine("Starting Puppetmaster");
            log.writeLine("Parsing configuration file");
            
            Parser parser = new Parser();
            Dictionary<string, OperatorDTO> operatorDTOs = parser.makeOperatorDTOs(parser.readConfig());

            log.writeLine("Done");

            Puppetmaster pm = new Puppetmaster();

            log = new Logger("Puppetmaster");

            //Prepare TCP Channel for remote communication

            log = new Logger("Puppetmaster");

            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            
            //Get physical node pcs
            log.writeLine("Creating Replica");
            ProcessCreationService pcs = (ProcessCreationService)Activator.GetObject(
                typeof(ProcessCreationService), "tcp://localhost:10000/pcs");

            if (pcs == null)
                log.writeLine("ERROR: NO PCS SERVER");
            
            //Remotely create process in node
            pcs.createProcess("1", "10010");
            System.Threading.Thread.Sleep(1000);

            //Connect to created replica
            log.writeLine("Trying to connect to replica");
            Replica rb = (Replica)Activator.GetObject(typeof(Replica),
                "tcp://localhost:10010/Replica1");

            if (rb == null)
                log.writeLine("ERROR: NO SERVER");
            else
            {
                //Ping it
                string s = rb.input("TEST");
                log.writeLine("Got input: " + s);
            }

            Console.ReadLine();
        }
    }

    class Puppetmaster {
    };
}
