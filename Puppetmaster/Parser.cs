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

            int n, i;
            n = i = 0;

            while (n < splitFile.Length) {
                System.Console.WriteLine("\nWord being filtered: {0} - n = {1}", splitFile[n], n);
                switch (splitFile[n]) {
                    case "INPUT_OPS":
                        i = n + 1;
                        while (!splitFile[i].Equals("REP_FACT")) {
                            System.Console.WriteLine("Operator's input: {0}", splitFile[i]);
                            inputs.Add(splitFile[i]);
                            i++;
                        }
                        n = i - 1;
                        break;
                    case "REP_FACT":
                        rep = splitFile[n + 1];
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
                        operatorDTOs.Add(id, new OperatorDTO(id, inputs, rep, rout, addr, spec));
                        System.Console.WriteLine("Added new operator draft to ArrayList. Continue?");
                        inputs = new List<string>();
                        addr = new List<string>();
                        spec = new List<string>();
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
            return operatorDTOs;
        }

        public string[] readConfig() {
            //read file as one string
            string config_file = System.IO.File.ReadAllText(@"..\CONFIG_FILE");

            //TODO remove - print string read   --- ask for input
            System.Console.WriteLine("Contents of CONFIG_FILE: {0}", config_file);

            //splitting text
            char[] splitChars = { ' ', ',', '\t', '\n' };
            string[] splitFile = config_file.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            foreach (string st in splitFile) {
                System.Console.WriteLine(st);
            }

            System.Console.WriteLine("Done splitting. Size = {0}. Continue?", splitFile.Count());
            System.Console.ReadLine();

            return splitFile;
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
