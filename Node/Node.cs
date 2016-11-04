using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Node
    {
        private static Logger log;

        static void Main(string[] args)
        {
            int port = 10000;

            log = new Logger("Physical Node");

            log.writeLine("Initializing PCS");

            ProcessCreationService pcs = new ProcessCreationService();

            TcpChannel channel = new TcpChannel(port);
            log.writeLine("PCS on port:" + port);

            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(pcs, "pcs", typeof(ProcessCreationService));

            log.writeLine("PCS created & online");
            log.writeLine("Physical Node Initialized");

            Console.ReadLine();
        }
    }
}
