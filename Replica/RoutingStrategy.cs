using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    interface IRoutingStrategy
    {
        void route(Tuple data);
    }

    class PrimaryRoutingStrategy : IRoutingStrategy
    {
        private string[] _replicas;
        private Replica _parent;

        public PrimaryRoutingStrategy(Replica parent)
        {
            _parent = parent;
            _replicas = parent.getOutputReplicas();
        }

        public void route(Tuple data)
        {
            if (_replicas[0] != "X")
            {
                _parent.send(data, _replicas[0]);
            }
            else
            {
                Logger.writeLine("End of streaming chain detected!", "PrimaryRouting");
            }
        }
    }
}
