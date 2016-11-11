using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Remoting.Messaging;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tuple = DADSTORM.Tuple;

namespace DADSTORM
{
    class Program
    {

        static void Main(string[] args)
        {
            int port = 10001;
            Logger log = new Logger("PuppetMaster");
            log.writeLine("Starting Puppetmaster");
            log.writeLine("Parsing configuration file");

            //TODO put something on config file QUESTION, AQUI USAR LOGGER?
            Console.WriteLine("What is the current IP address of the puppetmaster?");
            string ip = Console.ReadLine();
            ip = string.Concat("tcp://", ip, ":", port, "/pml");
            log.writeLine("Located at: " + ip);

            Parser parser = new Parser(@"..\..\..\dadstorm.config");
            string[] commands =  parser.readCommands();
            Dictionary<string, OperatorDTO> operatorDTOs = parser.makeOperatorDTOs(ip);

            PuppetmasterListener pml = new PuppetmasterListener(log);
            
            TcpChannel channel = new TcpChannel(port);
            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(pml, "pml", typeof(PuppetmasterListener));

            Puppetmaster pm = new Puppetmaster(operatorDTOs, log);

            Shell sh = new Shell(pm);

            pm.setUpOperators();

            log.writeLine("Welcome to the PuppetMaster Shell.");
            log.writeLine("Do not be afraid to write \"help\" if needed.");
            //sh.start(commands);
            sh.start();
            log.writeLine("Goodbye.");

        }
    }

    class Puppetmaster
    {
        Logger logger;
        public Dictionary<string, OperatorDTO> operatorDTOs;
        public delegate string PingAsyncDelegate(string str);
        public delegate void IntervalAsyncDelegate(int time);
        public delegate void VoidAsyncDelegate();
        static string pingResult;

        public Puppetmaster(Dictionary<string, OperatorDTO> opDTOs, Logger log)
        {
            operatorDTOs = opDTOs;
            logger = log;
        }

        public void setUpOperators()
        {
            logger.writeLine("Setting up operators.");
            foreach (OperatorDTO op in operatorDTOs.Values)
            {
                createOperator(op);
            }
        }

        private void createOperator(OperatorDTO op)
        {
            logger.writeLine("Creating operator " + op.op_id);
            for(int i=0; i<op.address.Count; i++)
            {
                string PCSaddress = Parser.parseIPFromAddress(op.address[i]) + ":10000/pcs";
                op.curr_rep = i;
                createReplica(PCSaddress, op);
            }
        }

        private void createReplica(string PCSaddress, OperatorDTO op) {
            logger.writeLine("Creating replica " + op.op_id +"-" +op.curr_rep);

            ProcessCreationService pcs = getPCS(PCSaddress, op);
            if (pcs == null)
                logger.writeLine("Couldn't reach PCS.");
            try
            { 
                pcs.createProcess(op);
            }
            catch (System.Net.Sockets.SocketException e)
            {
                logger.writeLine("Couldn't reach PCS with address:" + PCSaddress + ".");
            }
        }

        private ProcessCreationService getPCS(string PCSaddress, OperatorDTO op) {
            ProcessCreationService pcs = (ProcessCreationService)Activator.GetObject(typeof(ProcessCreationService), PCSaddress);
            return pcs;
        }

        private Replica getReplica(string address)
        {
            Replica rep = (Replica)Activator.GetObject(typeof(Replica), address);
            return rep;
        }

        private List<Replica> getReplicas(OperatorDTO op)
        {
            List<Replica> result = new List<Replica>();
            if (op != null)
            {
                for (int i = 0; i < op.address.Count; i++)
                {
                    result.Add(getReplica(op.address[i]));
                }
            }
            else logger.writeLine("getReplicas FAILED because Operator was NULL");

            return result;
        }

        private OperatorDTO getOperator(string op)
        {
            OperatorDTO oper = null;
            try
            {
                oper = operatorDTOs[op];
            } catch (System.Collections.Generic.KeyNotFoundException e)
            {
                logger.writeLine("Operator: " + op + " does not exist.");
            }


            return oper;
        }

        public void start(string op)
        {
            logger.writeLine("Start " + op);
            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                foreach(Replica rep in getReplicas(oper))
                {
                    try {
                        VoidAsyncDelegate startDel = new VoidAsyncDelegate(rep.start);
                        IAsyncResult remAr = startDel.BeginInvoke(null, null);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        logger.writeLine(oper.op_id + " has an unreachable replica.");
                    }
                }

            }
        }

        public void wait(int time)
        {
            logger.writeLine("wait " + time);
            Thread.Sleep(time);
        }
    
        public void status()
        {
            logger.writeLine("Status:");
            foreach (KeyValuePair<string, OperatorDTO> entry in operatorDTOs)
            {
                for (int i = 0; i < entry.Value.address.Count; i++)
                {
                    try
                    {
                         Replica replica = getReplica(entry.Value.address[i]); ;
                         VoidAsyncDelegate statusDel = new VoidAsyncDelegate(replica.status);
                         IAsyncResult remAr = statusDel.BeginInvoke(null, null); 
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        //TODO apanhar excepçoes especificas
                        logger.writeLine("Replica " + i + " of " + entry.Value.op_id + " is unreachable.");
                    }

                }
            }
        }


        public void interval(string op, int time)
        {
            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                foreach (Replica rep in getReplicas(oper))
                {
                    try
                    {
                        logger.writeLine("Interval command");
                        //IntervalAsyncDelegate intervalDel = new IntervalAsyncDelegate(rep.interval);
                        //IAsyncResult remAr = intervalDel.BeginInvoke(time, Puppetmaster.IntervalAsyncCallback, null);
                        //rep.interval(time);
                        Replica replica = getReplica(oper.address[0]);
                        int i  = replica.interval(time);
                        logger.writeLine("Got response: " + i);
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        logger.writeLine(oper.op_id + " has an unreachable replica.");
                    }
                }
            }
        }

        public void callback()
        {
            logger.writeLine("Finished call");
        }

        public void crash(string op, int rep)
        {
            logger.writeLine("crash " + op + "." + rep);

            OperatorDTO oper = getOperator(op);
            if(oper != null)
            {
                try
                {
                    Replica replica = getReplica(oper.address[rep]);
                    VoidAsyncDelegate crashDel = new VoidAsyncDelegate(replica.crash);
                    IAsyncResult remAr = crashDel.BeginInvoke(null, null);
                    logger.writeLine("Implement me. (CRASH)");
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    logger.writeLine(oper.op_id + " replica " + rep + " is unreachable.");
                }
            }

        }

        public void freeze(string op, int rep)
        {
            logger.writeLine("freeze " + op + "." + rep);

            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                try
                {
                    Replica replica = getReplica(oper.address[rep]);
                    VoidAsyncDelegate freezeDel = new VoidAsyncDelegate(replica.freeze);
                    IAsyncResult remAr = freezeDel.BeginInvoke(null, null);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    logger.writeLine(oper.op_id + " replica " + rep + " is unreachable.");
                }
            }
        }

        public void unfreeze(string op, int rep)
        {
            logger.writeLine("unfreeze " + op + "." + rep);

            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                try
                {
                    Replica replica = getReplica(oper.address[rep]);
                    VoidAsyncDelegate unfreezeDel = new VoidAsyncDelegate(replica.unfreeze);
                    IAsyncResult remAr = unfreezeDel.BeginInvoke(null, null);
                }
                catch (System.Net.Sockets.SocketException e)
                {

                    logger.writeLine(oper.op_id + " replica " + rep + " is unreachable.");
                }
            }
        }

        public void readFile(string op, int rep)
        {
            logger.writeLine("Read file...");

            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                try
                {
                    Replica replica = getReplica(oper.address[rep]);
                    VoidAsyncDelegate readFileDel = new VoidAsyncDelegate(replica.readFile);
                    IAsyncResult remAr = readFileDel.BeginInvoke(null, null);
                }
                catch (System.Net.Sockets.SocketException e)
                {

                    logger.writeLine(oper.op_id + " replica " + rep + " is unreachable.");
                }
            }
        }

        public static void PingAsyncCallback(IAsyncResult res)
        {
            PingAsyncDelegate delegat = (PingAsyncDelegate)((AsyncResult)res).AsyncDelegate;
            pingResult = delegat.EndInvoke(res);
            return;
        }

        public static void IntervalAsyncCallback(IAsyncResult res)
        {
            VoidAsyncDelegate delegat = (VoidAsyncDelegate)((AsyncResult)res).AsyncDelegate;
            Log.writeLine("Finished call", "AsyncCallback");
            return;
        }

    };

    class PuppetmasterListener : MarshalByRefObject, ILoggerReceiver
    {
        private static Logger log;

        public PuppetmasterListener(Logger _log)
        {
            log = _log;
        }

        public void writeLine(string str, string id, params object[] args)
        {
            Log.writeLine(str, id, args);
        }

        public void write(string str, string id, params object[] args)
        {
            Log.write(str, id, args);
        }

        public void info(string str, string id, params object[] args)
        {
            Log.info(str, id, args);
        }

        public void debug(string str, string id, params object[] args)
        {
            Log.debug(str, id, args);
        }

        override public object InitializeLifetimeService()
        {
            return null;
        }
    }
}
