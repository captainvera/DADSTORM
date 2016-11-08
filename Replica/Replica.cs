using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Collections;
using System.Collections.Concurrent;

namespace DADSTORM
{
    public class ReplicaProcess
    {
        public static void Main(string[] args)
        {
            if(args.Length < 3)
            {
                Console.WriteLine("Wrong number of arguments provided, exiting.");
                Console.ReadLine();
                Environment.Exit(1);
            }

            string id = args[0];
            string port = args[1];
            List<string> outputs = new List<string>();

            //This are our output replicas!
            for(int i = 2; i < args.Length; i++)
            {
                outputs.Add(args[i]);
            }

            Logger.writeLine("Processed " + outputs.Count + " output replicas", "ReplicaProcess");

            //Might need proper implementation for naming: CHECK PROJ INSTR
            string name = "Replica" + id;

            IDictionary propBag = new Hashtable();
            propBag["port"] = Int32.Parse(port);
            propBag["name"] = "tcpClientServer";  // here enter unique channel name

            TcpChannel channel = new TcpChannel(propBag, new BinaryClientFormatterSinkProvider(), new BinaryServerFormatterSinkProvider());
            ChannelServices.RegisterChannel(channel, false);

            Replica rep = new Replica(id, port.Replace("10", "11"), outputs.ToArray());
            RemotingServices.Marshal(rep, name, typeof(Replica));

            rep.process();

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
        private Logger log;

        OperatorWorkerPool op_pool;
        BlockingCollection<Tuple> input_buffer;
        BlockingCollection<Tuple> output_buffer;

        private string id, port;
        private string[] output;
        private Boolean primary;

        private IOperator op;
        private IRoutingStrategy router;

        public Replica(string _id, string _port, string[] _next)
        {
            //Replica configuration
            primary = false;
            id = _id;
            port = _port;
            output = _next;

            log = new Logger("Replica" + id);
            router = new PrimaryRoutingStrategy(this);
            op = new DUP();

            input_buffer = new BlockingCollection<Tuple>();
            output_buffer = new BlockingCollection<Tuple>();
            op_pool = new OperatorWorkerPool(4, op, input_buffer, output_buffer);

            op_pool.start();
            log.writeLine("Replica " + id + " is now online");
        }

        public void input(Tuple t)
        {
            //log.writeLine("Received input: " + t.get(0));
            //Tuple res = op.process(t);
            //router.route(res);
            log.writeLine("Got input");
            input_buffer.Add(t);
        }

        public void process()
        {
            Tuple data;
            while (true)
            {
                data = output_buffer.Take();
                log.writeLine("Sending tuple");
                router.route(data);
            }
        }

        //private??
        public void send(Tuple t, string dest)
        {
            Replica next = (Replica)Activator.GetObject(typeof(Replica), dest);
            next.input(t);
        }

        public Boolean isPrimary()
        {
            return primary;
        }

        public string[] getOutputReplicas()
        {
            return output; 
        }

        public void freeze()
        {
            op_pool.freezeAll(); 
        }

        public void unfreeze()
        {
            op_pool.unfreezeAll(); 
        }
    }
}
