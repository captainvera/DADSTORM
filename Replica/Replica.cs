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
using System.Threading;

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
            propBag["timeout"] = 1 * 3000;
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
        private Boolean interval_active;
        private Boolean wait_takeover;
        private int interval_time;
        private string op_id, port, replication, routing, address, logging, semantics;
        private string[] output, op_spec;
        private List<ReplicaRepresentation> input_ops;

        private int rep_number;
        private int rep_factor;

        private List<int> indexes_owned = new List<int>();

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
            interval_active = false;
            wait_takeover = false;
            interval_time = 0;

            //Some parameteres are unnecessary, remove later
            op_id = dto.op_id;
            rep_number = dto.curr_rep;
            port = dto.ports[dto.curr_rep];
            indexes_owned.Add(rep_number);

            //output = dto.next_op_addresses.ToArray();

            replication = dto.rep_fact;
            routing = dto.routing;
            address = dto.address[dto.curr_rep];
            logging = dto.logging;
            semantics = dto.semantics;
            op_spec = dto.op_spec.ToArray();
            input_ops = dto.before_op;
            rep_factor = Int32.Parse(dto.rep_fact);

            //Setting global logging level for this process
            Config.setLoggingLevel(logging);
            log = new RemoteLogger("Replica" + op_id + "-" + rep_number.ToString(), dto.pmAdress);


            //Semantics factory plz
            log.info("Creating replica communicator...");
            comm = new ReplicaCommunicator();
            comm.parseDto(dto);

            log.writeLine("Creatting replica " + rep_number + " of operator " + op_id + " with adress " + address);

            log.info("Creating routing strategy");
            //Routing Strategy for this replica
            router = RoutingStrategyFactory.create(routing, this);

            log.info("Creating operator");
            //Operator to be used
            op = OperatorFactory.create(dto.op_spec[0], dto.op_spec.GetRange(1, dto.op_spec.Count - 1).ToArray(), getRepresentation());

            log.info("Creating semantic strategy");
            //Semantic Strategy to be used
            sem = SemanticStrategyFactory.create(semantics, this);

            log.info("Setting up buffers & operators");
            //Multithreading setup
            input_buffer = new BlockingCollection<Tuple>();
            output_buffer = new BlockingCollection<Tuple>();

            //Currently only using 1 thread for processing tuples
            op_pool = new OperatorWorkerPool(1, op, input_buffer, output_buffer);

            log.info("Starting timer thread for alive pings");

            Thread timer = new Thread(new ThreadStart(isAliveT));
            timer.IsBackground = true;
            timer.Start();

            //Proccess any input operators
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

        public void enforceState()
        {
            while(frozen)
            {
                Thread.Sleep(50);
            }
        }

        public bool input(Tuple t)
        {
            enforceState();

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
                log.debug("Waiting for next tuple...");

                data = output_buffer.Take();

                log.debug("- tuple is " + data.toString());
                log.debug("- tuple id is " + data.getId().toString());

                if(data.getSize() == 0)
                {
                    //Null tuple result, still have to signal the tuple
                    log.debug("Tuple processing resulted in null");

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
        
        public bool subNext(int deadRepIndex, int new_boss)
        {
            enforceState();

            log.debug("Subbing next rep " + deadRepIndex + "for rep " + new_boss);
            getCommunicator().setNextCorrespondence(getCommunicator().getNextReplicaHolder(new_boss), deadRepIndex);

            return true;
        }


        public bool subOwn(int deadRepIndex, int new_boss)
        {
            enforceState();

            log.debug("Subbing own rep " + deadRepIndex + "for rep " + new_boss);
            getCommunicator().setOwnCorrespondence(getCommunicator().getOwnReplicaHolder(new_boss), deadRepIndex);

            return true;
        }

        public bool subPrev(int deadRepIndex, int new_boss)
        {
            enforceState();

            log.debug("Subbing prev rep " + deadRepIndex + "for rep " + new_boss);
            getCommunicator().setPrevCorrespondence(getCommunicator().getPrevReplicaHolder(new_boss), deadRepIndex);

            return true;
        }

        public bool reinstatePrev(ReplicaRepresentation rep)
        {
            ReplicaHolder repH = new ReplicaHolder(rep);
            enforceState();
            log.debug("reinstating prev to index " + repH.representation.rep);

            getCommunicator().setPrevCorrespondence(repH, repH.representation.rep);

            return true;
        }

        public bool reinstateOwn(ReplicaRepresentation rep)
        {
            if (indexes_owned.Contains(rep.rep))
            {
                indexes_owned.Remove(rep.rep);
            }

            ReplicaHolder repH = new ReplicaHolder(rep);
            enforceState();
            log.debug("reinstating own to index " + repH.representation.rep);

            getCommunicator().setOwnCorrespondence(repH, repH.representation.rep);

            return true;
        }

        public bool reinstateNext(ReplicaRepresentation rep)
        {
            ReplicaHolder repH = new ReplicaHolder(rep);
            log.debug("reinstating next to index " + repH.representation.rep);
            enforceState();

            getCommunicator().setNextCorrespondence(repH, repH.representation.rep);

            return true;
        }

        public bool takeOver(int dead_rep_index)
        {

            log.debug("Adding " + dead_rep_index + " to indexes owned.");
            indexes_owned.Add(dead_rep_index);

            enforceState();

            log.debug("taking over for rep: " + dead_rep_index);

            //fix previous operator's replica's "routing tables"
            log.debug("Fixing previous op's tables");
            for (int repN = 0; repN < getCommunicator().getPreviousReplicaCount(); repN++)
            {
                log.debug("Accessing prev rep: " + repN);
                Replica prevRep = getCommunicator().getPreviousReplica(repN);
                prevRep.subNext(dead_rep_index, rep_number);
            }

            //fix colleague replica's "routing tables"
            for (int repN = 0; repN< comm.getOwnReplicaCount(); repN++)
            {
                if (repN != rep_number && repN != dead_rep_index)//skip it self and downed replica
                {
                    log.debug("Accessing colleague rep: " + repN + " and pointing to me: " + rep_number);
                    Replica colleagueReplica = comm.getOwnReplica(repN);
                    colleagueReplica.subOwn(dead_rep_index, rep_number);
                }  
            }

            //fix downward replica's "routing tables"
            for(int repN = 0; repN < comm.getNextReplicaCount(); repN++)
            {
                log.debug("Accessing next rep: " + repN);
                Replica nextRep = comm.getNextReplica(repN);
                nextRep.subPrev(dead_rep_index, rep_number);
            }

            //Now solve all missing tuples if necessary
            sem.fix(dead_rep_index);

            return true;
        }

        //reinstates replica's place when coming back from the dead
        public void reinstate()
        {
            log.debug("Starting reinstate process");
            enforceState();

            ReplicaRepresentation rep = new ReplicaRepresentation(op_id, rep_number, address);

            log.debug("Fixing previous op's replicas");
            //fix previous operator's replica's "routing tables"
            for (int repN = 0; repN < comm.getPreviousReplicaCount(); repN++)
            {
                log.debug("reinstating prev op of index :" + repN);

                try
                {
                    Replica prevRep = comm.getPreviousReplica(repN);
                    prevRep.reinstateNext(rep);
                }
                catch(Exception e)
                {
                    log.debug("failed to reinstate prev rep of index: " + repN + "  caught exception: " + e);
                }
            }
            //fix colleague replica's "routing tables"
            log.debug("Fixing colleague replica's");
            for (int repN = 0; repN < comm.getOwnReplicaCount(); repN++)
            {
                if (repN != rep_number)
                {
                    Replica colleagueRep = comm.getOwnReplica(repN);
                    colleagueRep.reinstateOwn(rep);
                }
            }

            //fix downward replica's "routing tables"
            log.debug("Fixing next op's replicas");
            for (int repN = 0; repN < comm.getNextReplicaCount(); repN++)
            {
                Replica nextRep = comm.getNextReplica(repN);
                nextRep.reinstatePrev(rep);
            }
        }

        public int takeOverNextCandidateIndex(int previousIndex)
        {
            enforceState();

            return (previousIndex + 1) % comm.getNextReplicaCount();
        }

        public int takeOverCandidateIndex(int previousIndex)
        {
            enforceState();

            return (previousIndex + 1) % comm.getOwnReplicaCount();
        }

        private void send(Tuple t, int dest)
        {
            log.info("tuple " + address + " " + t.toString());

            if (interval_active)
            {

                log.info("Applying interval delay " + interval_time);
                Thread.Sleep(interval_time);
            }

            //Updating operator identifier with last replica processing
            string prev_op = t.getId().op;
            int prev_rep = t.getId().rep;

            t.update(op_id, rep_number);

            bool res = false;

            res = comm.input(OperatorPosition.Next, dest, t);

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

        private Boolean isPrimary()
        {
            enforceState();

            return primary;
        }

        private string[] getOutputReplicas()
        {
            enforceState();

            return output;
        }

        public string ping(string value)
        {
            enforceState();

            //log.info("Received (echo) ping command -\n " + value);
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

            // Freezes all current processing
            op_pool.freezeAll();
        }

        public void unfreeze()
        {
            frozen = false;
            log.info("Received unfreeze command");

            // Unfreezes all current processing
            op_pool.unfreezeAll();

            reinstate();
        }

        public void readFile()
        {
            enforceState();

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
            //op_pool.intervalAll(time);

            interval_time = time;
            interval_active = true;
            
            return time;
        }

        public bool addRecord(TupleRecord tr)
        {
            enforceState();

            log.debug("------->Received tuple record: id:" + tr.getUID() + " | from " + tr.id.op + "->" + tr.id.rep);
            sem.addRecord(tr);

            return true;
        }

        public bool purgeRecord(TupleRecord tr)
        {
            enforceState();

            log.debug("------>Received purge record notice id:" + tr.getUID() + " | from " + tr.id.op + "->" + tr.id.rep);
            sem.purgeRecord(tr);

            return true;
        }
        public bool tupleConfirmed(string uid)
        {
            enforceState();

            log.debug("------Got confirmation for delivery of " + uid);
            sem.tupleConfirmed(uid);
            return true;
        }

        public ReplicaRepresentation getRepresentation()
        {
            enforceState();

            return new ReplicaRepresentation(op_id, rep_number, address);
        }

        
        public void pingColleague(int ping_target)
        {
            comm.getOwnReplica(ping_target).ping("i am number->" + rep_number + " and you, are you alive?");
        }

        private void isAliveT()
        {
            enforceState();

            Thread.Sleep(1500);
            if (wait_takeover)
            {
                Thread.Sleep(7500);
            }

            int toPing = takeOverCandidateIndex(rep_number);
            try
            {
                if (!indexes_owned.Contains(toPing))
                {
                    pingColleague(toPing);
                }
            }
            catch (Exception e)
            {
                log.writeLine("Replica->" + (rep_number + 1) % comm.getOwnReplicaCount() + " is dead!!! WARN THE OTHERS!");

                wait_takeover = true;
                startTakeover(toPing);

                log.writeLine("Done taking over");
            }
            isAliveT();
        }


        private void startTakeover(int rep)
        {
            int next = takeOverCandidateIndex(rep);

            log.writeLine("Trying to start takeOver of " + rep + " in replica " + next);
            if (!indexes_owned.Contains(next))
            {
                Replica r = comm.getOwnReplica(next);
                r.takeOver(rep);
            }else
            {
                takeOver(rep);
            }
        }

        public Tuple fetchTuple(TupleRecord tr)
        {
            return sem.get(tr.getUID());
        }

        public void injectInput(Tuple t)
        {
            input_buffer.Add(t);
        }

        public List<int> getIndexesOwned()
        {
            return indexes_owned;
        }
    }
}
