using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;

namespace DADSTORM
{
    public class Replica 
    {
        private Logger log;
        private IRoutingStrategy router;

        public Replica()
        {
            log = new Logger("ReplicaX");
            router = new PrimaryRoutingStrategy();
        } 

        public void Main()
        {
            log.writeLine("Starting operator replica");

            TcpChannel channel = new TcpChannel(10010);
            ChannelServices.RegisterChannel(channel, false);

            ReplicaBroker rb = new ReplicaBroker(this);
            RemotingServices.Marshal(rb, "Replica1", typeof(ReplicaBroker));

            log.writeLine("Replica online");

        }

        public string input(String t)
        {
            log.writeLine("Got input " + t);
            router.send(t);
            return t;
        }
    }

    public class ReplicaBroker : MarshalByRefObject
    {
        private Replica master;
        public ReplicaBroker(Replica r)
        {
            master = r;
        }
        public string input(String t)
        {
            return master.input(t);
        }
    }
}
