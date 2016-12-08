using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    interface IRoutingStrategy
    {
        int route(Tuple data);
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
        private Replica _parent;
        private int _next;

        public PrimaryRoutingStrategy(Replica parent)
        {
            _parent = parent;

            _next = parent.getCommunicator().getNextReplicaCount();
        }

        public int route(Tuple data)
        {
            if (_next == 0)
            {
                return -1;
            }
            return 0;
        }
    }

    class RandomRoutingStrategy : IRoutingStrategy
    {
        private Replica _parent;
        private int _next;

        public RandomRoutingStrategy(Replica parent)
        {
            _parent = parent;
            _next = parent.getCommunicator().getNextReplicaCount();
        }

        public int route(Tuple data)
        {
            if (_next == 0)
            {
                return -1;
            }
            return RandomGenerator.nextInt(0, _next-1);
        }
    }

    class HashRoutingStrategy : IRoutingStrategy
    {
        private Replica _parent;
        private int _fieldID;
        private int _next;

        private int hash(Tuple data)
        {
            string field = data.get(_fieldID);

            int val = 0;
            if (Int32.TryParse(field, out val) == true)
                return val % _next;
            else return 0;
        }

        public HashRoutingStrategy(Replica parent, int fieldID)
        {
            _parent = parent;
            _next = parent.getCommunicator().getNextReplicaCount();
            _fieldID = fieldID;
        }

        public int route(Tuple data)
        {
            if (_next == 0)
            {
                return -1;
            }
            return hash(data);
        }
    }
}
