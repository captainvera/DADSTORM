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

namespace DADSTORM
{
    public class ReplicaProcess
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Wrong number of arguments provided, exiting.");
                Console.ReadLine();
                Environment.Exit(1);
            }
            string dtoXml = args[0];

            OperatorDTO dto = Deserialize<OperatorDTO>(dtoXml);

            Log.writeLine("Processed " + dto.next_op.Count + " output replicas", "ReplicaProcess");

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

        public static String getPath()
        {
            return Environment.CurrentDirectory;
        }

        public static OperatorDTO Deserialize<OperatorDTO>(string opXml)
        {
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
        private string op_id, port, replication, routing, address, logging, semantics;
        private string[] output, op_spec;
        private List<ReplicaRepresentation> input_ops;
        private int rep_number;
        private int rep_factor;

        /** ------------------- Multithreading ---------------------------- **/

        OperatorWorkerPool op_pool;
        BlockingCollection<Tuple> input_buffer;
        BlockingCollection<Tuple> output_buffer;
        List<TupleFileReaderWorker> tfr_workers;

        /** ------------------- Replica Abstraction ----------------------- **/

        private IOperator op;
        private IRoutingStrategy router;
        private ISemanticStrategy sem;
        private TcpChannel channel;
        private ReplicaCommunicator comm;

        /** ------------------ Some getters/setters ---------------------- **/

        public int getReplicationFactor()
        {
            return rep_factor;
        }

        public int getReplicaNumber()
        {
            return rep_number;
        }

        public ReplicaCommunicator getCommunicator()
        {
            return comm;
        }
        /** --------------------------------------------------------------- **/

        public Replica(OperatorDTO dto)
        {
            //Replica configuration
            primary = false;
            running = false;
            frozen = false;

            //Some parameteres are unnecessary, remove later
            op_id = dto.op_id;
            rep_number = dto.curr_rep;
            port = dto.ports[dto.curr_rep];

            //output = dto.next_op_addresses.ToArray();

            replication = dto.rep_fact;
            routing = dto.routing;
            address = dto.address[dto.curr_rep];
            logging = dto.logging;
            semantics = dto.semantics;
            op_spec = dto.op_spec.ToArray();
            input_ops = dto.before_op;
            rep_factor = Int32.Parse(dto.rep_fact);

            //Semantics factory plz
            comm = new ReplicaCommunicator();
            comm.parseDto(dto);

            Console.WriteLine("------------------------- I AM REPLICA " + rep_number + " of operator " + op_id);
            //Setting global logging level for this process
            Config.setLoggingLevel("debug");

            log = new RemoteLogger("Replica" + op_id + "-" + rep_number.ToString(), dto.pmAdress);

            //Routing Strategy for this replica
            router = RoutingStrategyFactory.create(routing, this);

            //op = new op(op_spec);
            op = OperatorFactory.create(dto.op_spec[0], dto.op_spec.GetRange(1, dto.op_spec.Count - 1).ToArray());

            Task.Factory.StartNew(() =>
            {
                Console.WriteLine("STARTING TASK");
                System.Threading.Thread.Sleep(5000);
                Console.WriteLine("PINGING");
                foreach (ReplicaRepresentation rr in dto.before_op)
                {
                    Console.WriteLine("--------- PINGING" + "Hello from " + dto.op_id + " and " + rep_number);
                    Replica r = comm.getPreviousReplica(rr.rep);
                    r.ping(">>>>>>>>>>>>>>>>> Hello from " + dto.op_id + " and " + rep_number + " to " + rr.op + " | " + rr.rep);
                }


                Console.WriteLine("YOOOOOO MY REP FACTOR IS " + rep_factor);
                for(int i = 0; i < rep_factor; i ++)
                {
                    if (i != rep_number)
                    {
                        Replica r = comm.getOwnReplica(i);
                        Console.WriteLine("--------->>>>>> PINGING from " + rep_number + " to " + i);
                        r.ping(">>>>>>>>>>>>>>>>>>>> Hello from " + dto.op_id + " and " + rep_number);
                    }
                }

                Console.WriteLine("&&&&&&&&&&&&&&&&&&&& PRINTING TABLES MY FRIENDS!!");
                sem.printTables();
            });

            sem = new ExactlyOnce(this);

            //Multithreading setup
            input_buffer = new BlockingCollection<Tuple>();
            output_buffer = new BlockingCollection<Tuple>();
            op_pool = new OperatorWorkerPool(1, op, input_buffer, output_buffer);

            //Check if we have input files
            tfr_workers = new List<TupleFileReaderWorker>();
            foreach (string s in dto.input_ops)
            {
                //It's a file
                if (s.Contains("."))
                {
                    TupleFileReaderWorker tfrw = new TupleFileReaderWorker(input_buffer, s, this);
                    tfr_workers.Add(tfrw);
                }
            }

            log.writeLine("Now online but not processing");
        }

        public void inputSafe(Tuple t)
        {
            log.debug("Received tuple " + t.toString());

            if (sem.accept(t))
            {
                input_buffer.Add(t);
            }
        }

        public bool input(Tuple t)
        {
            log.debug("Received tuple " + t.toString());
            if (sem.accept(t))
            {
                input_buffer.Add(t);
                return true;
            }
            return false;
        }

        public void process()
        {
            Tuple data;
            while (true)
            {
                Console.WriteLine("afssdgsagasgasfgfasgasfgfsgfdgdgd SIIIIIIIIIIIZE is " + output_buffer.Count);
                log.debug("SIIIIIIIIIIIZE is " + output_buffer.Count);

                data = output_buffer.Take();
                log.debug("Tuple is " + data.toString());
                log.debug("----> Removing tuple id:" + data.getId().id + " | " + data.getId().op + " | " + data.getId().rep);

                if(data.getSize() == 0)
                {
                    //Null tuple result, still have to signal the tuple
                    Console.WriteLine("======================== OK, we got a null result");
                    string prev_op = data.getId().op;
                    int prev_rep = data.getId().rep;

                    if(!prev_op.Equals(op_id))
                        sem.delivered(data, prev_op, prev_rep);
                    sem.tupleConfirmed(data.getId().getUID());
                    continue;
                }

                int dest = router.route(data);

                if (dest == -1)
                {
                    Log.debug("End of streaming chain detected!", "RandomRouting");

                    //In case of end of chain we consider the tuple received and signal it
                    string prev_op = data.getId().op;
                    int prev_rep = data.getId().rep;

                    if(!prev_op.Equals(op_id))
                        sem.delivered(data, prev_op, prev_rep);

                    sem.tupleConfirmed(data.getId().getUID());
                    continue;
                }
                send(data, dest);
            }
        }

        public void takeOver(int deadRepIndex)
        {
            //fix previous operator's replica's "routing tables"
            for (int repN = 0; repN < comm.getPreviousReplicaCount(); repN++)
            {
                Replica prevRep = comm.getPreviousReplica(repN);
                prevRep.comm.setNextCorrespondence(comm.getOwnReplicaHolder(rep_number), deadRepIndex);
            }

            //fix colleague replica's "routing tables"
            for (int repN = 0; repN< comm.getOwnReplicaCount(); repN++)
            {
                if (repN != rep_number)//skip it self
                {
                    Replica colleagueReplica = comm.getOwnReplica(repN);
                    colleagueReplica.comm.setOwnCorrespondence(comm.getOwnReplicaHolder(rep_number), deadRepIndex);
                }  
            }

            //fix downward replica's "routing tables"
            for(int repN = 0; repN < comm.getNextReplicaCount(); repN++)
            {
                Replica nextRep = comm.getNextReplica(repN);
                nextRep.comm.setPrevCorrespondence(comm.getOwnReplicaHolder(rep_number), deadRepIndex);
            }
        }

        //reinstates replica's place when coming back from the dead
        public void reinstate()
        {
            //fix previous operator's replica's "routing tables"
            for (int repN = 0; repN < comm.getPreviousReplicaCount(); repN++)
            {
                Replica prevRep = comm.getPreviousReplica(repN);
                prevRep.comm.setNextCorrespondence(comm.getOwnReplicaHolder(rep_number), rep_number);
            }
            //fix colleague replica's "routing tables"
            for (int repN = 0; repN < comm.getOwnReplicaCount(); repN++)
            {
                if (repN != rep_number)
                {
                    Replica colleagueRep = comm.getOwnReplica(repN);
                    colleagueRep.comm.setNextCorrespondence(comm.getOwnReplicaHolder(rep_number), rep_number);
                }
            }

            //fix downward replica's "routing tables"
            for(int repN = 0; repN < comm.getNextReplicaCount(); repN++)
            {
                Replica nextRep = comm.getNextReplica(repN);
                nextRep.comm.setPrevCorrespondence(comm.getOwnReplicaHolder(rep_number), rep_number);
            }
        }

        public int takeOverCandidateIndex(int previousIndex)
        {
            return (previousIndex + 1) % comm.getNextReplicaCount();
        }

        public void send(Tuple t, int dest)
        {
            log.info("tuple " + address + " " + t.toString());

            //Updating operator identifier with last replica processing
            string prev_op = t.getId().op;
            int prev_rep = t.getId().rep;

            t.update(op_id, rep_number);

            //Replica next = (Replica)Activator.GetObject(typeof(Replica), dest);
            Replica next = comm.getNextReplica(dest);

            if(next == null)
            {
                Console.WriteLine("F\nF\nF\nF\nF\nF\nF\nF\nF\nF\nFUUUUUUUUUUUUUUCK. I was trying to send to " + dest + " but failed in getting the object");
            }

            bool res = false;

            try
            {
                res = next.input(t);
            }
            catch (Exception e)//TODO failed to connect exception
            {
                log.info("Exception when sending tuple: " + e);
                int candidateIndex = takeOverCandidateIndex(dest);
                Replica takeoverCandidate = comm.getNextReplica(candidateIndex);
                takeoverCandidate.takeOver(dest);
                send(t, candidateIndex); //might be dangerous
                return;
            }

            if (res)
            {
                if (!prev_op.Equals(op_id))
                    sem.delivered(t, prev_op, prev_rep);
                else
                {
                    //Don't need to confirm delivery because there is no previous node
                    sem.firstDelivered(t);
                }
            }
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
            //Is this necessary?
            //How can we receive unfreeze if we disconnect the remote object?
            //RemotingServices.Disconnect(this);
            op_pool.freezeAll();
        }

        public void unfreeze()
        {
            frozen = false;
            log.info("Received unfreeze command");
            //Is this necessary?
            //RemotingServices.Connect(typeof(Replica), "op", this);
            op_pool.unfreezeAll();
            reinstate();
        }

        public void readFile()
        {
            string file = @"..\..\..\tweeters.dat";
            log.info("Processing file: " + file);
            TupleFileReaderWorker tfrw = new TupleFileReaderWorker(input_buffer, file, this);
            tfrw.start();
        }

        public void start()
        {
            log.info("Received start command");
            running = true;
            op_pool.start();
            //Read all input files 
            foreach (TupleFileReaderWorker tfrw in tfr_workers)
            {
                tfrw.start();
            }
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
            string res = address + " - ONLINE -";
            if (running)
                res += " PROCESSING";
            else
                res += " WAITING";
            if (frozen)
                res += " FROZEN";
            log.writeLine(res);
        }

        public int interval(int time)
        {
            log.info("Received interval command with time " + time);
            op_pool.intervalAll(time);
            return time;
        }

        public void addRecord(TupleRecord tr)
        {
            Console.WriteLine("------->Received tuple record: id:" + tr.getUID() + " | from " + tr.id.op + "->" + tr.id.rep);
            sem.addRecord(tr);               
        }

        public void purgeRecord(TupleRecord tr)
        {
            Console.WriteLine("------>Received purge record notice id:" + tr.getUID() + " | from " + tr.id.op + "->" + tr.id.rep);
            sem.purgeRecord(tr);
        }
        public void tupleConfirmed(string uid)
        {
            Console.WriteLine("------Got confirmation for delivery of " + uid);
            sem.tupleConfirmed(uid);
        }

        public ReplicaRepresentation getRepresentation()
        {
            return new ReplicaRepresentation(op_id, rep_number, address);
        }
    }
}
