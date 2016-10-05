using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Program
    {
        private static Logger log;

        static void Main(string[] args)
        {
            Console.WriteLine("Hello");
            Puppetmaster pm = new Puppetmaster();
            pm.readConfig();
            log = new Logger("Puppetmaster");

            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);

            log.writeLine("Trying to connect");
            ReplicaBroker rb = (ReplicaBroker)Activator.GetObject(typeof(ReplicaBroker),
                "tcp://localhost:10010/Replica1");
            if (rb == null)
                log.writeLine("ERROR: NO SERVER");
            else
            {
                string s = rb.input("TEST");
                log.writeLine("Got input: " + s);
            }

            Console.ReadLine();
        }
    }
    class Puppetmaster{

        public string[] readConfig()
        {
            //read file as one string
            string config_file = System.IO.File.ReadAllText(@"C:\Users\José Semedo\Desktop\DAD\DADSTORM\CONFIG_FILE");

            //TODO remove - print string read   --- ask for input
            System.Console.WriteLine("Contents of CONFIG_FILE: {0}", config_file);

            //splitting text
            char[] splitChars = {' ', ',', '\t', '\n'};
            string[] splitFile= config_file.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            foreach (string st in splitFile){
                System.Console.WriteLine(st);
            }

            System.Console.ReadLine();

            return splitFile;
        }
        
        public ArrayList makeNodeDrafts(string[] splitFile){
            ArrayList nodeDrafts = new ArrayList();

            string id;
            ArrayList inputs = new ArrayList();
            int rep;
            string rout;
            ArrayList addr = new ArrayList();
            ArrayList spec = new ArrayList();

            int n, i;
            n = i = 0;

            while(n < splitFile.Length){
                switch (splitFile[n])
                {
                    case "INPUT_OPS":
                        i = n+1;
                        while (!splitFile[i].Equals("REP_FACT")){
                            inputs.Add(splitFile[i]);
                        }
                        n = i;
                        break;
                    case "REP_FACT":
                        rep = Convert.ToInt32(splitFile[n + 1]);
                        n++;
                        break;
                    case "ROUTING":
                        rout = splitFile[n + 1];
                        n++;
                        break;
                    case "ADDRESS":
                        i = n + 1;
                        while (!splitFile[i].Equals("OPERATOR_SPEC"))
                        {
                            addr.Add(splitFile[i]);
                        }
                        n = i;
                        break;
                    case "OPERATOR_SPEC":
                        break;
                    default:
                        break;
                }
                nodeDrafts.Add(new NodeDraft(id, inputs, rep, rout, addr, spec));
            }

            return nodeDrafts;
        }
        
    }

    class NodeDraft{
        string op_id;
        ArrayList input_ops = new ArrayList();
        int rep_fact;
        string routing;
        ArrayList address = new ArrayList();
        ArrayList op_spec = new ArrayList();

        public NodeDraft(string id, ArrayList inputs, int rep, string rout, ArrayList addr,ArrayList spec){
            op_id = id;
            input_ops = inputs;
            rep_fact = rep;
            routing = rout;
            address = addr;
            op_spec = spec;
        }
    }
}
