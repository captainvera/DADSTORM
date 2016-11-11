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

    class RandomRoutingStrategy : IRoutingStrategy
    {
        private string[] _replicas;
        private Replica _parent;

        public RandomRoutingStrategy(Replica parent)
        {
            _parent = parent;
            _replicas = parent.getOutputReplicas();
        }

        public void route(Tuple data)
        {
            if (_replicas[0] != "X")
            {
                _parent.send(data, _replicas[RandomGenerator.nextInt(0, _replicas.Length-1)]);
            }
            else
            {
                Logger.writeLine("End of streaming chain detected!", "RandomRouting");
            }
        }
    }

    class HashRoutingStrategy : IRoutingStrategy
    {
        private string[] _replicas;
        private Replica _parent;
        private int _fieldID;

        private int hash(Tuple data)
        {
            string field = data.get(_fieldID);

            int val = 0;
            if (Int32.TryParse(field, out val) == true)
                return val % data.getSize();
            else return 0;
        }

        public HashRoutingStrategy(Replica parent, int fieldID)
        {
            _parent = parent;
            _replicas = parent.getOutputReplicas();
            _fieldID = fieldID;
        }

        public void route(Tuple data)
        {
            if (_replicas[0] != "X")
            {
                Random rnd = new Random();
                _parent.send(data, _replicas[hash(data)]);
            }
            else
            {
                Logger.writeLine("End of streaming chain detected!", "RandomRouting");
            }
        }
    }
}
