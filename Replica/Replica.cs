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
    public class ReplicaProcess
    {
        public static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("Wrong number of arguments provided, exiting.");
                Console.ReadLine();
                Environment.Exit(1);
            }

            string id = args[0];
            string port = args[1];

            //Might need proper implementation for naming: CHECK PROJ INSTR
            string name = "Replica" + id;

            Replica rep = new Replica(id, port);

            TcpChannel channel = new TcpChannel(10010);
            ChannelServices.RegisterChannel(channel, false);

            RemotingServices.Marshal(rep, name, typeof(Replica));

            Console.ReadLine();
        }

        public static String getPath()
        {
            return Environment.CurrentDirectory;
        }
    }

    //Should we have a broker between replica process and replica?
    public class Replica : MarshalByRefObject
    {

        private string id, port;
        private Boolean primary;

        public Replica(string _id, string _port)
        {
            primary = false;
            id = _id;
            port = _port;
            Logger log = new Logger("Replica" + id);

            IRoutingStrategy router = new PrimaryRoutingStrategy();
            log.writeLine("Replica " + id + " is now online");
        }

        public string input(String t)
        {
            return "Bounced " + t;
        }

        public Boolean isPrimary()
        {
            return primary;
        }
    }
}
