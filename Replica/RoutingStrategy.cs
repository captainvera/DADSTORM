using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    interface IRoutingStrategy
    {
        void setReplicaList(List<string> replicas);
        void send(String data);
    }

    class PrimaryRoutingStrategy : IRoutingStrategy
    {
        
        public void setReplicaList(List<string> replicas)
        {

        }

        public void send(String data)
        {
        
        }
    }
}
