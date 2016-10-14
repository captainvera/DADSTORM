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
            Console.WriteLine("Starting Puppetmaster");
            Parser parser = new Parser();
            Dictionary<string, OperatorDTO> operatorDTOs = parser.makeOperatorDTOs(parser.readConfig());
            Puppetmaster pm = new Puppetmaster();

            log = new Logger("Puppetmaster");

            //pm.makeOperatorDrafts(pm.readConfig());

            /*
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);

            log.writeLine("Trying to connect");
            Replica rb = (Replica)Activator.GetObject(typeof(Replica),
                "tcp://localhost:10010/Replica1");
            if (rb == null)
                log.writeLine("ERROR: NO SERVER");
            else
            {
                string s = rb.input("TEST");
                log.writeLine("Got input: " + s);
            }
            */

            Console.ReadLine();
        }
    }
    class Puppetmaster{
        
        
    }


}
