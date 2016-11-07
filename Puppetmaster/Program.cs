using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tuple = DADSTORM.Tuple;

namespace DADSTORM
{
    class Program
    {
        private static Logger log;

        static void Main(string[] args)
        {
            int port = 10001;

            

            log = new Logger("PuppetMaster");
            log.writeLine("Starting Puppetmaster");
            log.writeLine("Parsing configuration file");
            
            Parser parser = new Parser();

            string[] commands =  parser.readCommands();

            Console.WriteLine("commands:");
            foreach (string str in commands)
                Console.WriteLine(str);

            Dictionary<string, OperatorDTO> operatorDTOs = parser.makeOperatorDTOs(parser.readConfigOps());
            log.writeLine("Done");

            //TODO put something on config file
            Console.WriteLine("What is the current IP address of the puppetmaster?");
            string ip = Console.ReadLine();

            ip = string.Concat("tcp://", ip, ":", port, "/pml");

            log.writeLine("Current ip:" + ip);

            PuppetmasterListener pml = new PuppetmasterListener(log);


            TcpChannel channel = new TcpChannel(port);
            log.writeLine("PuppetMaster on port:" + port);

            ChannelServices.RegisterChannel(channel, false);
            RemotingServices.Marshal(pml, "pml", typeof(PuppetmasterListener));


            Puppetmaster pm = new Puppetmaster(operatorDTOs, log);

            pm.setUpOperators();

            Shell sh = new Shell(pm);
            //Want the script to be executed? uncomment following line
            //sh.start(commands);

            sh.start();

            log.writeLine("shell stopped");
            System.Threading.Thread.Sleep(10000);

            log.writeLine("shell restarted");
            sh.start(commands);

            pm.test();

        }
    }

    class Puppetmaster {

        Logger logger;
        public Dictionary<string, OperatorDTO> operatorDTOs;

        public Puppetmaster(Dictionary<string, OperatorDTO> opDTOs, Logger log) {
            operatorDTOs = opDTOs;
            logger = log;
        }

        public void setUpOperators() {
            logger.writeLine("Setting up operators.");
            foreach (OperatorDTO op in operatorDTOs.Values) {
                createOperator(op);
            }
        }

        private void createOperator(OperatorDTO op) {
            logger.writeLine("Creating operator " + op.op_id);
            for(int i=0; i<op.address.Count; i++) {
                //logger.writeLine("Reaching PCS at {0} to set up ", PCSaddress, op.op_id);
                string PCSaddress = Parser.parseIPFromAddress(op.address[i]) + ":10000/pcs";
                op.curr_rep = i;
                createReplica(PCSaddress, op);
            }
        }

        private void createReplica(string PCSaddress, OperatorDTO op) {
            if(op.next_op_addresses[0] != "X")
                logger.writeLine("Creating replica " + op.curr_rep + " for " + op.op_id + " with next address:" + op.next_op_addresses[0]);
            else logger.writeLine("Creating replica " + op.curr_rep + " for " + op.op_id + ". This operator is final.");

            ProcessCreationService pcs = getPCS(PCSaddress, op);
            if (pcs == null)
                logger.writeLine("Couldn't reach PCS.");
            try { 
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
            logger.writeLine("start " + op);
            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                foreach(Replica rep in getReplicas(oper))
                {
                    try { 
                        rep.ping("IS TIME TO GO");
                        //rep.start();
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        logger.writeLine(oper.op_id + " has an unreachable replica.");
                    }
                }

            }
        }

        public void stop(string op)
        {
            logger.writeLine("stop " + op);
            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                foreach (Replica rep in getReplicas(oper))
                {
                    try
                    {
                        rep.ping("IS TIME TO STAHP");
                    }
                    catch (System.Net.Sockets.SocketException e)
                    {
                        logger.writeLine("OP" + oper.op_id + " has an unreachable replica.");
                    }
                    //rep.stop();
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

            foreach (KeyValuePair<string, OperatorDTO> entry in operatorDTOs)
            {
                for (int i = 0; i < entry.Value.address.Count; i++)
                {
                    logger.writeLine("Getting replica with address:" + entry.Value.address[i]);
                    Replica rep = getReplica(entry.Value.address[i]);
                    if (rep == null) logger.writeLine("ABORT ABORT");
                    //TODO: should say actual status (stopped/started/etc)
                    try
                    {
                        if (rep.ping("ping") == "ping")
                            logger.writeLine("Replica " + i + " of " + entry.Value.op_id + " is alive.");
                    }
                    catch (Exception e)
                    {
                        //TODO apanhar excepçoes especificas
                        logger.writeLine("Replica " + i + " of " + entry.Value.op_id + " is dead.");
                    }

                }
            }
        }


        public void interval(string op, int time)
        {
            logger.writeLine("interval " + op);
            OperatorDTO oper = getOperator(op);
            if (oper != null)
            {
                foreach (Replica rep in getReplicas(oper))
                {
                try { 
                    rep.ping("Recess time is:" + time);
                    //rep.interval(time); 
                }
                    catch (System.Net.Sockets.SocketException e)
                {
                    logger.writeLine(oper.op_id + " has an unreachable replica.");
                }
            }

            }
        }

        public void crash(string op, int rep)
        {
            logger.writeLine("crash " + op + "." + rep);

            OperatorDTO oper = getOperator(op);
            if(oper != null)
            {
                try { 
                    getReplica(oper.address[rep]).ping("Crash and burn");
                    //getReplica(oper.address[rep]).crash();
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
                    getReplica(oper.address[rep]).ping("Frostnova");
                    //getReplica(oper.address[rep]).freeze();
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
                    getReplica(oper.address[rep]).ping("Melt");
                    //getReplica(oper.address[rep]).freeze();
                }
                catch (System.Net.Sockets.SocketException e)
                {

                    logger.writeLine(oper.op_id + " replica " + rep + " is unreachable.");
                }
            }
        }

        public void test()
        {
            for (int i = 999; i > 0; i--)
            {
                Tuple t = new Tuple(1);
                t.set(0, i + " bottles of beer on the wall, " + i + " bottles of beer, take one down, pass it around you got " + (i - 1) + " bottles of beer on the wall!");
                Replica rep = getReplica(operatorDTOs["OP1"].address[0]);
                rep.input(t);
                System.Threading.Thread.Sleep(100);
            }
        }

    };

    class PuppetmasterListener : MarshalByRefObject
    {
        private static Logger log;

        public PuppetmasterListener(Logger _log)
        {
            log = _log;
        }

        public void writeLine(string str)
        {
            log.writeLine(str);
        }

        public void write(string str)
        {
            log.write(str);
        }
    }
}
