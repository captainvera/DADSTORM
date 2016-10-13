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
    class ProcessCreationService
    {
        private Logger log;

        public ProcessCreationService()
        {
            log = new Logger("Process Creation System"); 
        }

        public void createProcess(string id, string port)
        {
            log.writeLine("Creating new Replica Process | id =" + id + " | port = " + port);

            Process p = new Process();
            string t = ReplicaProcess.getPath() + "\\Replica.exe";
            p.StartInfo.FileName = t;

            //Argument order: id , port (...?)
            p.StartInfo.Arguments = id + " " + port;
            p.Start();

            log.writeLine("Replica " + id + " created");
        }
    }
}
