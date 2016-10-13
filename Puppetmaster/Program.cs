﻿using System;
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
            log = new Logger("Puppetmaster");

            pm.makeOperatorDrafts(pm.readConfig());

            /*
            TcpChannel channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);

            log.writeLine("Trying to connect");
            Replica rb = (Replica)Activator.GetObject(typeof(Replica),
                "tcp://localhost:10010/Replica1");
            if (rb == null)
                log.writeLine("ERROR: NO SERVER");
            else
            {
                string s = rb.input("TEST");
                log.writeLine("Got input: " + s);
            }
            */

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

            System.Console.WriteLine("Done splitting. Size = {0}. Continue?", splitFile.Count());
            System.Console.ReadLine();

            return splitFile;
        }
        
        public ArrayList makeOperatorDrafts(string[] splitFile){

            System.Console.WriteLine("Building Operator drafts from previously split file.");

            ArrayList operatorDrafts = new ArrayList();

            string id = "placeholder";
            int rep = 20; //should never be 20
            string rout = "placeholder";
            ArrayList inputs = new ArrayList();
            ArrayList addr = new ArrayList();
            ArrayList spec = new ArrayList();

            int n, i;
            n = i = 0;

            while(n < splitFile.Length){
                System.Console.WriteLine("\nWord being filtered: {0} - n = {1}", splitFile[n], n);
                switch (splitFile[n])
                {
                    case "INPUT_OPS":
                        i = n+1;
                        while (!splitFile[i].Equals("REP_FACT")){
                            System.Console.WriteLine("Operator's input: {0}", splitFile[i]);
                            inputs.Add(splitFile[i]);
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "REP_FACT":
                        rep = Convert.ToInt32(splitFile[n + 1]);
                        System.Console.WriteLine("Operator rep factor: {0}", rep);
                        n++;
                        break;
                    case "ROUTING":
                        rout = splitFile[n + 1];
                        System.Console.WriteLine("Operator routing policy: {0}", rout);
                        n++;
                        break;
                    case "ADDRESS":
                        i = n + 1;
                        while (!splitFile[i].Equals("OPERATOR_SPEC")) {
                            System.Console.WriteLine("Adresses: {0}", splitFile[i]);
                            addr.Add(splitFile[i]);
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "OPERATOR_SPEC":
                        i = n + 1;
                        while (!splitFile[i].Equals("INPUT_OPS")) {
                            System.Console.WriteLine("Operator spec items: {0}. i = {1}", splitFile[i], i);
                            spec.Add(splitFile[i]);
                            i++;
                            if (i == splitFile.Count()) {
                                break;
                            }
                        }
                        if (i < splitFile.Count()) {
                            spec.RemoveAt(spec.Count - 1);
                            System.Console.WriteLine("spec array's current last item: {0}", spec[spec.Count - 1]);
                            n = i - 2;
                        }
                        else {
                            n = i;
                        }
                        operatorDrafts.Add(new OperatorDraft(id, inputs, rep, rout, addr, spec));
                        System.Console.WriteLine("Added new operator draft to ArrayList. Continue?");
                        inputs = new ArrayList();
                        addr = new ArrayList();
                        spec = new ArrayList();
                        System.Console.ReadLine();
                        break;
                    default:
                        id = splitFile[n];
                        System.Console.WriteLine("New operator with id: {0}", id);
                        System.Console.WriteLine("Entered default statement.");
                        break;
                }
                n++;
            }
            System.Console.WriteLine("Drafts all done.");
            return operatorDrafts;
        }
        
    }

    class OperatorDraft{
        public string op_id;
        public ArrayList input_ops = new ArrayList();
        public int rep_fact;
        public string routing;
        public ArrayList address = new ArrayList();
        public ArrayList op_spec = new ArrayList();

        public OperatorDraft(string id, ArrayList inputs, int rep, string rout, ArrayList addr,ArrayList spec){
            op_id = id;
            input_ops = inputs;
            rep_fact = rep;
            routing = rout;
            address = addr;
            op_spec = spec;
        }
    }
}
