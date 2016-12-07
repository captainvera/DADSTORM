using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Text.RegularExpressions;


namespace DADSTORM
{
    class Parser
    {
        //safe defaults
        string logging = "light";
        string semantics = "at-most-once";

        string _pathToFile, pmAddress;
        string[] cmds;

        public Parser(string targetFile)
        {
            _pathToFile = targetFile;

        }

        public Dictionary<string, OperatorDTO> makeOperatorDTOs(string pmAddr)
        {
            Log.debug("Building Operator drafts from previously split file.", "Parser");

            Dictionary<string, OperatorDTO> operatorDTOs = new Dictionary<string, OperatorDTO>();

            pmAddress = pmAddr;
            string[] splitFile = readConfigOps();

            //setting logging level and semantics
            parseCommands();

            string id = "placeholder";
            string rep = "placeholder";
            string rout = "placeholder";
            List<string> inputs = new List<string>();
            List<string> addr = new List<string>();
            List<string> spec = new List<string>();
            List<string> port = new List<string>();

            int n, i;
            n = i = 0;

            while (n < splitFile.Length)
            {
                Log.debug("\nWord being filtered: {0} - n = {1}", "Parser", splitFile[n], n);
                switch (splitFile[n])
                {
                    case "input":
                        i = n + 2;
                        while (!splitFile[i].Equals("rep"))
                        {
                            Log.debug("Operator's input: {0}", "Parser", splitFile[i]);
                            inputs.Add(splitFile[i]);
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "rep":
                        rep = splitFile[n + 2];
                        Log.debug("Operator rep factor: {0}", "Parser", rep);
                        n += 2;
                        break;
                    case "routing":
                        rout = splitFile[n + 1];
                        Log.debug("Operator routing policy: {0}", "Parser", rout);
                        n++;
                        break;
                    case "address":
                        i = n + 1;
                        while (!splitFile[i].Equals("operator"))
                        {
                            Log.debug("Adresses: {0}", "Parser", splitFile[i]);
                            addr.Add(splitFile[i]);
                            port.Add(Parser.parsePortFromAddress(splitFile[i]));
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "operator":
                        i = n + 2;
                        while (!splitFile[i].Equals("input"))
                        {
                            Log.debug("Operator spec items: {0}. i = {1}", "Parser", splitFile[i], i);
                            spec.Add(splitFile[i]);
                            i++;
                            if (i == splitFile.Count())
                            {
                                break;
                            }
                        }
                        if (i < splitFile.Count())
                        {
                            spec.RemoveAt(spec.Count - 1);
                            Log.debug("spec array's current last item: {0}", "Parser", spec[spec.Count - 1]);
                            n = i - 2;
                        }
                        else
                        {
                            n = i;
                        }

                        operatorDTOs.Add(id, new OperatorDTO(id, inputs, rep, rout, addr, spec, port));
                        operatorDTOs[id].pmAdress = pmAddress;
                        operatorDTOs[id].logging = logging;
                        operatorDTOs[id].semantics = semantics;
                        Log.debug("Added new operator draft to ArrayList. ", "Parser");

                        inputs = new List<string>();
                        addr = new List<string>();
                        spec = new List<string>();
                        port = new List<string>();

                        break;
                    default:
                        id = splitFile[n];
                        Log.debug("New operator with id: {0}", "Parser", id);
                        Log.debug("Entered default statement.", "Parser");
                        break;
                }
                n++;
            }
            Log.debug("Drafts all done.", "Parser");

            //setting DTO's before and after parameter
            setNextOperatorAddress(operatorDTOs);
            setPreviousOperatorAddress(operatorDTOs);

            //Technically not needed
            //setCurrentOperatorAdress(operatorDTOS);

            //Should not be needed anymore aswell
            //operatorDTOs.Last().Value.next_op.Add(new ReplicaRepresentation("X", 0, "X"));

            return operatorDTOs;
        }

        public void setNextOperatorAddress(Dictionary<string, OperatorDTO> opDTOs)
        {
            foreach (OperatorDTO op in opDTOs.Values)
            {
                foreach (string input in op.input_ops)
                {
                    int i = 0;
                    if (input.StartsWith("OP"))
                    {
                        foreach (string addr in op.address) {
                            opDTOs[input].next_op.Add(new ReplicaRepresentation(op.op_id, i, addr));
                            i++;
                        }
                        //opDTOs[input].next_op_addresses = opDTOs[input].next_op_addresses.Concat(op.address).ToList();
                    }
                }
            }
        }

        public void setPreviousOperatorAddress(Dictionary<string, OperatorDTO> opDTOs)
        {
            foreach (OperatorDTO op in opDTOs.Values)
            {
                foreach (string input in op.input_ops)
                {
                    int i = 0;
                    if (input.StartsWith("OP"))
                    {
                        foreach (string addr in opDTOs[input].address) {
                            op.before_op.Add(new ReplicaRepresentation(input, i, addr));
                            i++;
                        }
                    }
                }
            }
        }

        public string[] readConfigOps()
        {
            string opDef = "";
            System.IO.StreamReader reader = new System.IO.StreamReader(_pathToFile);

            string line = reader.ReadLine();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("OP"))
                    opDef = opDef + " " + line;
            }

            //splitting text
            char[] splitChars = { ' ', ',', '\t', '\n', '\r' };
            string[] splitFile = opDef.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            Log.debug("Done splitting. Size = {0}. ", "Parser", splitFile.Count());

            return splitFile;
        }


        public string[] readCommands()
        {
            parseCommands();
            return cmds;
        }

        public void parseCommands()
        {
            List<string> commands = new List<string>();
            System.IO.StreamReader reader = new System.IO.StreamReader(_pathToFile);

            string line = reader.ReadLine();

            while ((line = reader.ReadLine()) != null)
            {
                if (!line.StartsWith("OP") && !line.StartsWith("%"))
                    commands.Add(line);
            }

            commands.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (string st in commands)
            {
                Log.debug(st, "Parser");
            }
            cmds = commands.ToArray();

            foreach (string str in commands)
            {
                string[] splt = str.Split(' ');

                if (splt[0] == "LoggingLevel")
                {
                    if (splt[1] == "light" || splt[1] == "full")
                    {
                        logging = splt[1];
                        Log.writeLine("Logging: " + splt[1], "Puppetmaster");
                    }
                }
                else if (splt[0] == "Semantics")
                {
                    if (splt[1] == "at-most-once" || splt[1] == "at-least-once" || splt[1] == "exactly-once")
                    {
                        semantics = splt[1];
                        Log.writeLine("Semantics: " + splt[1], "Puppetmaster");
                    }
                }
            }
        }

        public static string parsePortFromAddress(string address)
        {
            string portRegex = @"\:[A-Za-z0-9\-]+\/";
            Match mc = Regex.Match(address, portRegex);
            string match = mc.Value;
            return match.Substring(1, match.Length - 2);
        }

        public static string parseIPFromAddress(string address)
        {
            string IPRegex = @"tcp://[0-9\.a-zA-Z]+:";
            Match mc = Regex.Match(address, IPRegex);
            string IP = mc.Value;
            return IP.Substring(0, IP.Length - 1);
        }
    }

}
