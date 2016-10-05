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
        public ProcessCreationService()
        {

        }

        public void createProcess()
        {
            Replica r = new Replica();
            ThreadStart ts = new ThreadStart(r.Main);
            Thread t = new Thread(ts);
            t.Start();
        }

        public void listen()
        {
            while(true)
            {

            }
        }
    }
}
