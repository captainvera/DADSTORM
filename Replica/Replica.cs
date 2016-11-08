using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Collections;
using System.Xml.Serialization;
using System.IO;

namespace DADSTORM {
    public class ReplicaProcess {
        public static void Main(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("Wrong number of arguments provided, exiting.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            string dtoXml = args[0];

            OperatorDTO op = Deserialize<OperatorDTO>(dtoXml);

            Logger.writeLine("Processed " + op.next_op_addresses.Count + " output replicas", "ReplicaProcess");

            //Might need proper implementation for naming: CHECK PROJ INSTR
            string id = op.op_id;
            string name = "Replica" + id;
            string replication = op.rep_fact;
            string routing = op.routing;
            string address = op.address[op.curr_rep];
            string[] op_spec = op.op_spec.ToArray();
            string port = op.ports[op.curr_rep];
            string[] nextOperators = op.next_op_addresses.ToArray();
            string logging = op.logging;
            string semantics = op.semantics;


            TcpChannel channel = new TcpChannel(Int32.Parse(port));
            ChannelServices.RegisterChannel(channel, false);

            Replica rep = new Replica(id, port.Replace("10", "11"), replication, routing, address, op_spec, logging, semantics, nextOperators);

            RemotingServices.Marshal(rep, "op", typeof(Replica));
            Logger.writeLine("Registered with name:" + name, "ReplicaProcess");

            Console.ReadLine();
        }

        public static String getPath() {
            return Environment.CurrentDirectory;
        }

        public static OperatorDTO Deserialize<OperatorDTO>(string opXml) {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(OperatorDTO));
            StringReader textReader = new StringReader(opXml);
            return (OperatorDTO)xmlSerializer.Deserialize(textReader);
        }
}
    //Should we have a broker between replica process and replica?
    public class Replica : MarshalByRefObject
    {
        private Logger log;

        private string id, port, replication, routing, address, logging, semantics;
        private string[] output, op_spec;
        private Boolean primary;

        private IOperator op;
        private IRoutingStrategy router;

        private TcpChannel channel;

        public Replica(string _id, string _port, string _replication, string _routing, string _address, string[] _op_spec, string _logging, string _semantics, string[] _next)
        {
            //Replica configuration
            primary = false;
            id = _id;
            port = _port;
            output = _next;
            replication = _replication;
            routing = _routing;
            address = _address;
            logging = _logging;
            semantics = _semantics;
            op_spec = _op_spec;

            //log = new Logger("Replica" + id, logging);
            log = new Logger("Replica" + id);

            //Routing Strategy for this replica
            //router = new routing(routing);
            router = new PrimaryRoutingStrategy(this);

            //op = new op(op_spec);
            op = new DUP();

            log.writeLine("Initialized Replica " + id + " with out port " + port);

            //Client TCP Channel to connect with other replicas
            //Fix Int32 parse -> more security plz
            IDictionary propBag = new Hashtable(); 
            propBag["port"] = Int32.Parse(port);
            propBag["name"] = "tcpOut";  // here enter unique channel name

            channel = new TcpChannel(propBag, new BinaryClientFormatterSinkProvider(), null);
            ChannelServices.RegisterChannel(channel, false);

            log.writeLine("Replica " + id + " is now online");
        }

        public void input(Tuple t)
        {
            log.writeLine("Received input: " + t.get(0));
            Tuple res = op.process(t);
            router.route(res);
        }

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

        public string ping(string value)
        {
            log.writeLine("ping: " + value);
            return value;
        }

        override public object InitializeLifetimeService()
        {
            return null;
        }
    }
}
