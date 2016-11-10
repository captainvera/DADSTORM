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
using System.Collections.Concurrent;

namespace DADSTORM {
    public class ReplicaProcess {
        public static void Main(string[] args) {
            if (args.Length < 1) {
                Console.WriteLine("Wrong number of arguments provided, exiting.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            string dtoXml = args[0];

            OperatorDTO dto = Deserialize<OperatorDTO>(dtoXml);

            Logger.writeLine("Processed " + dto.next_op_addresses.Count + " output replicas", "ReplicaProcess");

            //Might need proper implementation for naming: CHECK PROJ INSTR
            string name = "Replica" + dto.op_id;
            string port = dto.ports[dto.curr_rep];

            IDictionary propBag = new Hashtable();
            propBag["port"] = Int32.Parse(port);
            propBag["name"] = "tcpClientServer";  // here enter unique channel name

            TcpChannel channel = new TcpChannel(propBag, new BinaryClientFormatterSinkProvider(), new BinaryServerFormatterSinkProvider());
            ChannelServices.RegisterChannel(channel, false);

            Replica rep = new Replica(dto);

            RemotingServices.Marshal(rep, "op", typeof(Replica));
            Logger.writeLine("Registered with name:" + name, "ReplicaProcess");

            rep.process();

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

        /** ------------------ Replica Configuration ---------------------- **/

        private Boolean primary;
        private Boolean running;
        private string id, port, replication, routing, address, logging, semantics;
        private string[] output, op_spec;

        /** ------------------- Multithreading ---------------------------- **/

        OperatorWorkerPool op_pool;
        BlockingCollection<Tuple> input_buffer;
        BlockingCollection<Tuple> output_buffer;

        /** ------------------- Replica Abstraction ----------------------- **/

        private IOperator op;
        private IRoutingStrategy router;
        private TcpChannel channel;

        /** --------------------------------------------------------------- **/

        public Replica(OperatorDTO dto)
        {
            //Replica configuration
            primary = false;
            running = false;
            id = dto.op_id;
            port = dto.ports[dto.curr_rep];
            output = dto.next_op_addresses.ToArray();
            replication = dto.rep_fact;
            routing = dto.routing;
            address = dto.address[dto.curr_rep];
            logging = dto.logging;
            semantics = dto.semantics;
            op_spec = dto.op_spec.ToArray();

            log = new Logger("Replica" + id);

            //Routing Strategy for this replica
            //TODO::XXX::Get routing strategy instance from routing parameter
            router = new PrimaryRoutingStrategy(this);

            //op = new op(op_spec);
            //TODO::XXX::Get Operator instance from op_spec parameter
            op = new DUP();

            //Multithreading setup
            input_buffer = new BlockingCollection<Tuple>();
            output_buffer = new BlockingCollection<Tuple>();
            op_pool = new OperatorWorkerPool(4, op, input_buffer, output_buffer);
            
            log.writeLine("Replica " + id + " is now online but not processing");
        }

        public void input(Tuple t)
        {
            log.writeLine("Received tuple");
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

        public string ping(string value)
        {
            log.writeLine("ping: " + value);
            return value;
        }

        override public object InitializeLifetimeService()
        {
            return null;
        }

        public void freeze()
        {
            op_pool.freezeAll(); 
        }

        public void unfreeze()
        {
            op_pool.unfreezeAll(); 
        }

        public void readFile()
        {
            string file = @"..\..\..\tweeters.dat";
            log.writeLine("Processing file: " + file);
            TupleFileReaderWorker tfrw = new TupleFileReaderWorker(input_buffer, file);
            tfrw.start();
        }

        public void start()
        {
            running = true;
            op_pool.start();
        }

        public void crash()
        {
            log.writeLine("Received crash command... Sayonara!");
            System.Environment.Exit(1);
        }

        //TODO::XXX::FIXME -> check if frozen 
        public string status()
        {
            string res = "Replica " + id + " - ONLINE -";
            if (running)
                res += " PROCESSING";
            else
                res += " WAITING";
            return res;
        }

        public void interval(int time)
        {
            //TODO::XXX::Implement me
        }
    }
}
