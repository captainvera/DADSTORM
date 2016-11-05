    using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

using Tuple = DADSTORM.Tuple;

namespace DADSTORM
{
    class Program
    {
        private static Logger log;

        static void Main(string[] args)
        {
            log = new Logger("PupperMaster");
            log.writeLine("Starting Puppetmaster");
            log.writeLine("Parsing configuration file");
            
            Parser parser = new Parser();

            string[] commands =  parser.readCommands();
            Dictionary<string, OperatorDTO> operatorDTOs = parser.makeOperatorDTOs(parser.readConfigOps());

            log.writeLine("Done");

            Puppetmaster pm = new Puppetmaster(operatorDTOs, commands, log);

            pm.setUpOperators();

            //Reaching every PCS and sending it the DTOs
            //pm.setupOperators(operatorDTOs, parser, log);

            /*
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
            
            //Get physical node pcs
            log.writeLine("Creating Replica");
            ProcessCreationService pcs = (ProcessCreationService)Activator.GetObject(
                typeof(ProcessCreationService), "tcp://localhost:10000/pcs");

            if (pcs == null)
                log.writeLine("ERROR: NO PCS SERVER");
            
            //Remotely create process in node
            pcs.createProcess("1", "10011", new string[]{"tcp://localhost:10012/Replica2"});
            pcs.createProcess("2", "10012", new string[]{"tcp://localhost:10013/Replica3"});
            pcs.createProcess("3", "10013", new string[]{"tcp://localhost:10014/Replica4"});
            pcs.createProcess("4", "10014", new string[]{"tcp://localhost:10015/Replica5"});
            pcs.createProcess("5", "10015", new string[]{"tcp://localhost:10016/Replica6"});
            pcs.createProcess("6", "10016", new string[]{"tcp://localhost:10017/Replica7"});
            pcs.createProcess("7", "10017", new string[]{"tcp://localhost:10018/Replica8"});
            pcs.createProcess("8", "10018", new string[]{"tcp://localhost:10019/Replica9"});
            pcs.createProcess("9", "10019", new string[]{"X"});

            System.Threading.Thread.Sleep(1000);

            //Connect to created replica;
            log.writeLine("Trying to connect to replica");
            Replica rb = (Replica)Activator.GetObject(typeof(Replica),
                "tcp://localhost:10011/Replica1");
            log.writeLine("Done");
            if (rb == null)
                log.writeLine("ERROR: NO SERVER");
            else
            {
                for (int i = 999; i > 0; i--)
                {
                    Tuple t = new Tuple(1);
                    t.set(0, i + " bottles of beer on the wall, " + i + " bottles of beer, take one down, pass it around you got " + (i-1) + " bottles of beer on the wall!");
                    rb.input(t);
                    System.Threading.Thread.Sleep(100);
                }
            }
            */

            Shell sh = new Shell(pm);
            //Want the script to be executed? uncomment following line
            //sh.start(commands);

            sh.start();


            pm.test();

        }
    }

    class Puppetmaster {

        Logger logger;
        public Dictionary<string, OperatorDTO> operatorDTOs;
        public string[] commands;


        public Puppetmaster(Dictionary<string, OperatorDTO> opDTOs, string[] cmnds, Logger log) {
            operatorDTOs = opDTOs;
            commands = cmnds;
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
            pcs.createProcess(op);
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

        public void start(string op)
        {
            logger.writeLine("start " + op);
            OperatorDTO oper = operatorDTOs[op];
            if (oper != null)
            {
                foreach(Replica rep in getReplicas(oper))
                {
                    rep.ping("IS TIME TO GO");
                    //rep.start();
                }

            }
        }

        public void stop(string op)
        {
            logger.writeLine("stop " + op);
            OperatorDTO oper = operatorDTOs[op];
            if (oper != null)
            {
                foreach (Replica rep in getReplicas(oper))
                {
                    rep.ping("IS TIME TO STAHP");
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
            OperatorDTO oper = operatorDTOs[op];
            if (oper != null)
            {
                foreach (Replica rep in getReplicas(oper))
                {
                    rep.ping("Recess time is:" + time);
                    //rep.interval(time); 
                }

            }
        }

        public void crash(string op, int rep)
        {
            logger.writeLine("crash " + op + "." + rep);

            OperatorDTO oper = operatorDTOs[op];
            if(oper != null)
            {
                getReplica(oper.address[rep]).ping("Crash and burn");
                //getReplica(oper.address[rep]).crash();
            }

        }

        public void freeze(string op, int rep)
        {
            logger.writeLine("freeze " + op + "." + rep);

            OperatorDTO oper = operatorDTOs[op];
            if (oper != null)
            {
                getReplica(oper.address[rep]).ping("Frostnova");
                //getReplica(oper.address[rep]).freeze();
            }
        }

        public void unfreeze(string op, int rep)
        {
            logger.writeLine("unfreeze " + op + "." + rep);

            OperatorDTO oper = operatorDTOs[op];
            if (oper != null)
            {
                getReplica(oper.address[rep]).ping("Melt");
                //getReplica(oper.address[rep]).freeze();
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
}
