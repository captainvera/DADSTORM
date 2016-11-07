using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using DADSTORM;
using System.Xml.Serialization;
using System.IO;

namespace DADSTORM
{
    public class ProcessCreationService : MarshalByRefObject
    {
        private Logger log;

        public ProcessCreationService()
        {
            log = new Logger("Process Creation System"); 
        }

        public void createProcess(OperatorDTO op)
        {
            
            log.writeLine("Creating new Replica Process | id = " + op.op_id + " | port = " + op.ports[op.curr_rep]);

            Process p = new Process();
            string t = ReplicaProcess.getPath() + "\\Replica.exe";
            p.StartInfo.FileName = t;

            //Writing DTO to xml string
            string serializedDTO = Serialize(op);
            System.Console.WriteLine(serializedDTO);
            p.StartInfo.Arguments = serializedDTO;

            p.Start();

            log.writeLine("Replica " + op.op_id + " created");
        }

        //Converts DTO to XML and returns it as string
        public static string Serialize<OperatorDTO>(OperatorDTO op) {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(OperatorDTO));
            StringWriter textWriter = new StringWriter();
            xmlSerializer.Serialize(textWriter, op);
            return textWriter.ToString();
        }

        override public object InitializeLifetimeService()
        {
            return null;
        }
    }
}
