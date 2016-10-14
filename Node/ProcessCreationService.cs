using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;
using DADSTORM;

namespace DADSTORM
{
    public class ProcessCreationService : MarshalByRefObject
    {
        private Logger log;

        public ProcessCreationService()
        {
            log = new Logger("Process Creation System"); 
        }

        public void createProcess(string id, string port, string[] next)
        {
            log.writeLine("Creating new Replica Process | id = " + id + " | port = " + port);

            Process p = new Process();
            string t = ReplicaProcess.getPath() + "\\Replica.exe";
            p.StartInfo.FileName = t;

            //Argument order: id , port (...?)
            const string separator = " ";
            string outputs = string.Join(separator, next);

            p.StartInfo.Arguments = id + " " + port + " " + outputs;

            p.Start();

            log.writeLine("Replica " + id + " created");
        }
    }
}
