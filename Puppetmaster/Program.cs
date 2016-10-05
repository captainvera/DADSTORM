using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Program
    {
        private static Logger log;

        static void Main(string[] args)
        {
            log = new Logger("Puppetmaster");

            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);

            log.writeLine("Trying to connect");
            ReplicaBroker rb = (ReplicaBroker)Activator.GetObject(typeof(ReplicaBroker),
                "tcp://localhost:10010/Replica1");
            if (rb == null)
                log.writeLine("ERROR: NO SERVER");
            else
            {
                string s = rb.input("TEST");
                log.writeLine("Got input: " + s);
            }

            Console.ReadLine();
        }
    }
}
