using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Text.RegularExpressions;


namespace DADSTORM {
    class Parser {

        public Dictionary<string, OperatorDTO> makeOperatorDTOs(string[] splitFile) {
            System.Console.WriteLine("Building Operator drafts from previously split file.");

            //ArrayList operatorDTOs = new ArrayList();
            Dictionary<string, OperatorDTO> operatorDTOs = new Dictionary<string, OperatorDTO>();

            string id = "placeholder";
            string rep = "placeholder";
            string rout = "placeholder";
            List<string> inputs = new List<string>();
            List<string> addr = new List<string>();
            List<string> spec = new List<string>();
            List<string> port = new List<string>();

            int n, i;
            n = i = 0;

            while (n < splitFile.Length) {
                //System.Console.WriteLine("\nWord being filtered: {0} - n = {1}", splitFile[n], n);
                switch (splitFile[n]) {
                    case "input":
                        i = n + 2;
                        while (!splitFile[i].Equals("rep")) {
                            //System.Console.WriteLine("Operator's input: {0}", splitFile[i]);
                            inputs.Add(splitFile[i]);
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "rep":
                        rep = splitFile[n + 2];
                        //System.Console.WriteLine("Operator rep factor: {0}", rep);
                        n += 2;
                        break;
                    case "routing":
                        rout = splitFile[n + 1];
                        //System.Console.WriteLine("Operator routing policy: {0}", rout);
                        n++;
                        break;
                    case "address":
                        i = n + 1;
                        while (!splitFile[i].Equals("operator")) {
                            //System.Console.WriteLine("Adresses: {0}", splitFile[i]);
                            addr.Add(splitFile[i]);
                            port.Add(Parser.parsePortFromAddress(splitFile[i]));
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "operator":
                        i = n + 2;
                        while (!splitFile[i].Equals("input")) {
                            //System.Console.WriteLine("Operator spec items: {0}. i = {1}", splitFile[i], i);
                            spec.Add(splitFile[i]);
                            i++;
                            if (i == splitFile.Count()) {
                                break;
                            }
                        }
                        if (i < splitFile.Count()) {
                            spec.RemoveAt(spec.Count - 1);
                            //System.Console.WriteLine("spec array's current last item: {0}", spec[spec.Count - 1]);
                            n = i - 2;
                        }
                        else {
                            n = i;
                        }
                        operatorDTOs.Add(id, new OperatorDTO(id, inputs, rep, rout, addr, spec, port));
                        //System.Console.WriteLine("Added new operator draft to ArrayList. Continue?");
                        inputs = new List<string>();
                        addr = new List<string>();
                        spec = new List<string>();
                        port = new List<string>();
                        System.Console.ReadLine();
                        break;
                    default:
                        id = splitFile[n];
                        //System.Console.WriteLine("New operator with id: {0}", id);
                        //System.Console.WriteLine("Entered default statement.");
                        break;
                }
                n++;
            }
            System.Console.WriteLine("Drafts all done.");

            //setting DTO's next_op_addresses parameter
            for (int j = 0; j < operatorDTOs.Count - 2; j++) {
                operatorDTOs.ElementAt(j).Value.next_op_addresses = operatorDTOs.ElementAt(j + 1).Value.address;
            }
            operatorDTOs.ElementAt(operatorDTOs.Count - 1).Value.next_op_addresses = new List<string> { "X" };
            

            return operatorDTOs;
        }

        public string[] readConfigOps() {

            string opDef = "";
            System.IO.StreamReader reader = new System.IO.StreamReader(@"..\..\..\test.config");

            string line = reader.ReadLine();
            while ((line = reader.ReadLine()) != null) {
                if(line.StartsWith("OP"))
                    opDef = opDef + " " + line;
            }

            //splitting text
            char[] splitChars = { ' ', ',', '\t', '\n', '\r' };
            string[] splitFile = opDef.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);            
            
            System.Console.WriteLine("Done splitting. Size = {0}. Continue?", splitFile.Count());
            System.Console.ReadLine();
            
            return splitFile;
        }

        public string[] readCommands() {
            List<string> commands = new List<string>();
            System.IO.StreamReader reader = new System.IO.StreamReader(@"..\..\..\test.config");

            string line = reader.ReadLine();
            while ((line = reader.ReadLine()) != null) {
                if (!line.StartsWith("OP") & !line.StartsWith("%"))
                    commands.Add(line);
            }
            commands.RemoveAll(string.IsNullOrWhiteSpace);
            foreach (string st in commands) {
                //System.Console.WriteLine(st);
            }
            return commands.ToArray();
        }

        public static string parsePortFromAddress(string address) {
            string portRegex = @"\:[A-Za-z0-9\-]+\/";
            Match mc = Regex.Match(address, portRegex);
            string match = mc.Value;
            return match.Substring(1, match.Length - 2);
        }

        public static string parseIPFromAddress(string address) {
            string IPRegex = @"tcp://[0-9\.]+:";
            Match mc = Regex.Match(address, IPRegex);
            string IP = mc.Value;
            return IP.Substring(0, IP.Length - 1);
        }
    }

}
