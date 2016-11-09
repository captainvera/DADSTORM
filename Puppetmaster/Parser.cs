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

        string _pathToFile;

        public Parser(string targetFile)
        {
            _pathToFile = targetFile;

        }

        public Dictionary<string, OperatorDTO> makeOperatorDTOs()
        {
            Logger.debug("Building Operator drafts from previously split file.");

            Dictionary<string, OperatorDTO> operatorDTOs = new Dictionary<string, OperatorDTO>();

            string[] splitFile = readConfigOps();

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
                Logger.debug("\nWord being filtered: {0} - n = {1}", splitFile[n], n);
                switch (splitFile[n])
                {
                    case "input":
                        i = n + 2;
                        while (!splitFile[i].Equals("rep"))
                        {
                            Logger.debug("Operator's input: {0}", splitFile[i]);
                            inputs.Add(splitFile[i]);
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "rep":
                        rep = splitFile[n + 2];
                        Logger.debug("Operator rep factor: {0}", rep);
                        n += 2;
                        break;
                    case "routing":
                        rout = splitFile[n + 1];
                        Logger.debug("Operator routing policy: {0}", rout);
                        n++;
                        break;
                    case "address":
                        i = n + 1;
                        while (!splitFile[i].Equals("operator"))
                        {
                            Logger.debug("Adresses: {0}", splitFile[i]);
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
                            Logger.debug("Operator spec items: {0}. i = {1}", splitFile[i], i);
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
                            Logger.debug("spec array's current last item: {0}", spec[spec.Count - 1]);
                            n = i - 2;
                        }
                        else
                        {
                            n = i;
                        }

                        operatorDTOs.Add(id, new OperatorDTO(id, inputs, rep, rout, addr, spec, port));

                        Logger.debug("Added new operator draft to ArrayList. Continue?");

                        inputs = new List<string>();
                        addr = new List<string>();
                        spec = new List<string>();
                        port = new List<string>();

                        break;
                    default:
                        id = splitFile[n];
                        Logger.debug("New operator with id: {0}", id);
                        Logger.debug("Entered default statement.");
                        break;
                }
                n++;
            }
            Logger.debug("Drafts all done.");

            //setting DTO's next_op_addresses parameter
            setNextOperatorAddress(operatorDTOs);

            operatorDTOs.Last().Value.next_op_addresses.Add("X");

            //setting logging level and semantics
            string[] commands = readCommands();

            //TODO safe defaults not implemented
            foreach (string str in commands)
            {
                string[] splt = str.Split(' ');

                if (splt[0] == "LoggingLevel")
                {
                    if (splt[1] == "light" || splt[1] == "full")
                    {
                        foreach (KeyValuePair<string, OperatorDTO> op in operatorDTOs)
                            op.Value.logging = splt[1];

                        Logger.writeLine("Logging: " + splt[1], "Puppetmaster");
                    }
                }
                else if (splt[0] == "Semantics")
                {
                    if (splt[1] == "at-most-once" || splt[1] == "at-least-once" || splt[1] == "exactly-once")
                    {
                        foreach (KeyValuePair<string, OperatorDTO> op in operatorDTOs)
                            op.Value.semantics = splt[1];

                        Logger.writeLine("Semantics: " + splt[1], "Puppetmaster");

                    }
                }
            }

            return operatorDTOs;
        }

        public void setNextOperatorAddress(Dictionary<string, OperatorDTO> opDTOs)
        {
            foreach (OperatorDTO op in opDTOs.Values)
            {
                foreach (string input in op.input_ops)
                {
                    if (input.StartsWith("OP"))
                    {
                        opDTOs[input].next_op_addresses = opDTOs[input].next_op_addresses.Concat(op.address).ToList();
                    }
                }
            }
        }

        public string[] readConfigOps()
        {
            string opDef = "";
            System.IO.StreamReader reader = new System.IO.StreamReader(@"..\..\..\dadstorm.config");

            string line = reader.ReadLine();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith("OP"))
                    opDef = opDef + " " + line;
            }

            //splitting text
            char[] splitChars = { ' ', ',', '\t', '\n', '\r' };
            string[] splitFile = opDef.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            Logger.debug("Done splitting. Size = {0}. Continue?", splitFile.Count());

            return splitFile;
        }

        public string[] readCommands()
        {
            List<string> commands = new List<string>();
            System.IO.StreamReader reader = new System.IO.StreamReader(@"..\..\..\dadstorm.config");

            string line = reader.ReadLine();

            while ((line = reader.ReadLine()) != null)
            {
                if (!line.StartsWith("OP") && !line.StartsWith("%"))
                    commands.Add(line);
            }

            commands.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (string st in commands)
            {
                Logger.debug(st);
            }
            return commands.ToArray();
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
            string IPRegex = @"tcp://[0-9\.]+:";
            Match mc = Regex.Match(address, IPRegex);
            string IP = mc.Value;
            return IP.Substring(0, IP.Length - 1);
        }
    }

}
