﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
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
            parser.readCommands();
            Dictionary<string, OperatorDTO> operatorDTOs = parser.makeOperatorDTOs(parser.readConfigOps());

            System.Console.WriteLine(" OP2.ports[0] : {0}", operatorDTOs["OP2"].ports[0]);
            System.Console.WriteLine("OP2ports[1]: {0}", operatorDTOs["OP2"].ports[1]);
            System.Console.WriteLine("OP2.next_op_addresses[0]: {0}", operatorDTOs["OP2"].next_op_addresses[0]);
            System.Console.WriteLine("OP2.next_op_addresses[1]: {0}", operatorDTOs["OP2"].next_op_addresses[1]);

            log.writeLine("Done");

            Puppetmaster pm = new Puppetmaster();

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
            Console.ReadLine();
        }
    }

    class Puppetmaster {

        public void setupOperators(Dictionary<string, OperatorDTO> operatorDTOs, Parser parser, Logger log) {
            log.writeLine("Setting up operators.");
            //foreach (KeyValuePair<string, OperatorDTO> op in operatorDTOs) {
            for(int i = 0; i < operatorDTOs.Count; i++) {
                foreach(string address in operatorDTOs.ElementAt(i).Value.address) {
                    string pcsAddress = Parser.parseIPFromAddress(address);
                    pcsAddress = pcsAddress + ":10000/pcs";
                    System.Console.WriteLine("Contacting {0}", pcsAddress);
                    ProcessCreationService pcs = (ProcessCreationService)Activator.GetObject(typeof(ProcessCreationService), pcsAddress);
                    if (pcs == null)
                        log.writeLine("ERROR: NO PCS SERVER");
                    pcs.createProcess(operatorDTOs.ElementAt(i).Value);
                }
            }
        }
    };
}
