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

    class RoutingStrategyFactory
    {
        public static IRoutingStrategy create(string desc, Replica parent)
        {
            if (desc.StartsWith("hashing"))
            {
                int i = desc.IndexOf("(");
                int j = desc.IndexOf(")");

                Log.debug("Detected hashing routing... Argument: " + desc + " | parsing from " + i + " to " + j, "RoutingStrategyFactory");
                //From the character after the ( to the character before the )
                string fieldID = desc.Substring(i+1, j-i-1);

                int fID = 0;

                Log.debug("Trying to parse " + fieldID, "RoutingStrategyFactory");
                if(Int32.TryParse(fieldID, out fID))
                {
                    Log.debug("Creating Hashing Routing Strategy with field id : " + fID, "RoutingStrategyFactory");
                    return new HashRoutingStrategy(parent, fID);
                }
                else
                {
                    Log.debug("Creating Hashing Routing Strategy. Couldn't parse field id. Defaulting to 0", "RoutingStrategyFactory");
                    return new HashRoutingStrategy(parent, 0);
                }
            }
            else if (desc.StartsWith("primary"))
            {
                Log.debug("Creating Primary Routing Strategy", "RoutingStrategyFactory");
                return new PrimaryRoutingStrategy(parent);
            }
            else if (desc.StartsWith("random"))
            {
                Log.debug("Creating Random Routing Strategy", "RoutingStrategyFactory");
                return new RandomRoutingStrategy(parent);
            }
            else
            {
                Log.debug("Couldn't parse Routing Strategy... Defaulting to Random Routing Strategy", "RoutingStrategyFactory");
                return new PrimaryRoutingStrategy(parent);
            }
        }
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
                Log.debug("End of streaming chain detected!", "PrimaryRouting");
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
                Log.debug("End of streaming chain detected!", "RandomRouting");
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
                return val % _replicas.Length;
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
                Log.debug("End of streaming chain detected!", "RandomRouting");
            }
        }
    }
}
