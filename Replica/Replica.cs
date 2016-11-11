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

            Log.writeLine("Processed " + dto.next_op_addresses.Count + " output replicas", "ReplicaProcess");

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
            Log.writeLine("Registered with name:" + name, "ReplicaProcess");

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
        private ILogger log;

        /** ------------------ Replica Configuration ---------------------- **/

        private Boolean primary;
        private Boolean running;
        private Boolean frozen;
        private string id, port, replication, routing, address, logging, semantics;
        private string[] output, op_spec;
        private int repNmbr;

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
            frozen = false;

            //Some parameteres are unnecessary, remove later
            id = dto.op_id;
            repNmbr = dto.curr_rep;
            port = dto.ports[dto.curr_rep];
            output = dto.next_op_addresses.ToArray();
            replication = dto.rep_fact;
            routing = dto.routing;
            address = dto.address[dto.curr_rep];
            logging = dto.logging;
            semantics = dto.semantics;
            op_spec = dto.op_spec.ToArray();

            //Setting global logging level for this process
            Config.setLoggingLevel(logging);

            log = new RemoteLogger("Replica" + id + "-" + repNmbr.ToString(), dto.pmAdress);

            //Routing Strategy for this replica
            //TODO::XXX::Get routing strategy instance from routing parameter
            router = RoutingStrategyFactory.create(routing, this);

            //op = new op(op_spec);
            //TODO::XXX::Get Operator instance from op_spec parameter
            op = OperatorFactory.create(dto.op_spec[0], dto.op_spec.GetRange(1,dto.op_spec.Count-1).ToArray());

            //Multithreading setup
            input_buffer = new BlockingCollection<Tuple>();
            output_buffer = new BlockingCollection<Tuple>();
            op_pool = new OperatorWorkerPool(4, op, input_buffer, output_buffer);

            log.writeLine("Now online but not processing");
        }

        public void input(Tuple t)
        {
            input_buffer.Add(t);
        }

        public void process()
        {
            Tuple data;
            while (true)
            {
                data = output_buffer.Take();
                router.route(data);
            }
        }

        //private??
        public void send(Tuple t, string dest)
        {
            log.writeLine("tuple " + address + " " + t.toString());
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
            log.info("Received (echo) ping command -\n " + value);
            return value;
        }

        override public object InitializeLifetimeService()
        {
            return null;
        }

        public void freeze()
        {
            frozen = true;
            log.info("Received freeze command");
            //RemotingServices.Disconnect(this);
            op_pool.freezeAll(); 
        }

        public void unfreeze()
        {
            frozen = false;
            log.info("Received unfreeze command");
            //RemotingServices.Connect(typeof(Replica), "op", this);
            op_pool.unfreezeAll(); 
        }

        public void readFile()
        {
            string file = @"..\..\..\tweeters.dat";
            log.info("Processing file: " + file);
            TupleFileReaderWorker tfrw = new TupleFileReaderWorker(input_buffer, file);
            tfrw.start();
        }

        public void start()
        {
            log.info("Received start command");
            running = true;
            op_pool.start();
        }

        public void crash()
        {
            log.info("Received crash command... Sayonara!");
            System.Environment.Exit(1);
        }

        //TODO::XXX::FIXME -> check if frozen 
        public void status()
        {
            log.info("Received status command");
            string res = " - ONLINE -";
            if (running)
                res += " PROCESSING";
            //else if (op_pool.frozen)
            //    res += " FROZEN";
            else
                res += " WAITING";
            log.writeLine(res);
        }

        public int interval(int time)
        {
            log.info("Received interval command with time " + time);
            op_pool.intervalAll(time);
            return time;
        }
    }
}
