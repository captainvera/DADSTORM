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

        public void createProcess(OperatorDTO op)
        {
            
            log.writeLine("Creating new Replica Process | id = " + op.op_id + " | port = " + op.ports[op.curr_rep]);

            Process p = new Process();
            string t = ReplicaProcess.getPath() + "\\Replica.exe";
            p.StartInfo.FileName = t;

            //Argument order: id , port (...?)
            const string separator = " ";
            string outputs = string.Join(separator, op.next_op_addresses);

            p.StartInfo.Arguments = op.op_id + " " + op.ports[op.curr_rep] + " " + outputs;

            p.Start();

            log.writeLine("Replica " + op.op_id + " created");
        }
    }
}
