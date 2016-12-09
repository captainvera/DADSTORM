using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    //Holds information about all previous_replicas of an operator
    public class ReplicaCommunicator
    {
        Dictionary<int, ReplicaHolder> previous_replicas;
        Dictionary<int, ReplicaHolder> own_replicas;
        Dictionary<int, ReplicaHolder> next_replicas;
        //List<ReplicaHolder> own;

        public ReplicaCommunicator()
        {
            previous_replicas = new Dictionary<int, ReplicaHolder>();
            own_replicas = new Dictionary<int, ReplicaHolder>();
            next_replicas = new Dictionary<int, ReplicaHolder>();
            //own = new List<ReplicaHolder>();
        }

        public void parseDto(OperatorDTO dto)
        {
            Console.WriteLine("-------------------- Node connectivity ----------------------");
            Console.WriteLine("...................... Before .......................");
            foreach(ReplicaRepresentation rr in dto.before_op)
            {
                Console.WriteLine("REPLICA " + rr.rep +  " of " + rr.op + " in address " + rr.addr);
                previous_replicas.Add(rr.rep, new ReplicaHolder(rr));
            }

            Console.WriteLine("...................... After .......................");
            foreach(ReplicaRepresentation rr in dto.next_op)
            {
                Console.WriteLine("REPLICA " + rr.rep +  " of " + rr.op + " in address " + rr.addr);
                next_replicas.Add(rr.rep, new ReplicaHolder(rr));
            }

            Console.WriteLine("...................... Current .......................");
            int i = 0;
            foreach(string s in dto.address)
            {
                ReplicaRepresentation rr = new ReplicaRepresentation(dto.op_id, i, s);
                Console.WriteLine("REPLICA " + rr.rep +  " of " + rr.op + " in address " + rr.addr);
                own_replicas.Add(i, new ReplicaHolder(rr));
                i++;
            }
            Console.WriteLine("-------------------- Node connectivity ----------------------");
        }

        public Replica getReplica(string addr)
        { 
            foreach(KeyValuePair<int, ReplicaHolder> entry in previous_replicas)
            {
                if(entry.Value.representation.addr == addr)
                {
                    return entry.Value.getReplica();
                }
            }

            foreach(KeyValuePair<int, ReplicaHolder> entry in own_replicas)
            {
                if(entry.Value.representation.addr == addr)
                {
                    return entry.Value.getReplica();
                }
            }

            foreach(KeyValuePair<int, ReplicaHolder> entry in next_replicas)
            {
                if(entry.Value.representation.addr == addr)
                {
                    return entry.Value.getReplica();
                }
            }
            return null;
        }

        public Replica getReplica(OperatorPosition pos, int rep) 
        {
            switch (pos)
            {
                case OperatorPosition.Previous:
                    return getPreviousReplica(rep);
                case OperatorPosition.Own:
                    return getOwnReplica(rep);
                case OperatorPosition.Next:
                    return getNextReplica(rep);
                default:
                    return null;
            }
        }

        public Replica getPreviousReplica(int rep)
        {
            ReplicaHolder r;
            previous_replicas.TryGetValue(rep, out r);
            return r.getReplica();
        }

        public int getPreviousReplicaCount()
        {
            return previous_replicas.Count;
        }

        public ReplicaHolder getOwnReplicaHolder(int rep)
        {
            ReplicaHolder r;
            own_replicas.TryGetValue(rep, out r);
            return r;
        }

        public ReplicaHolder getNextReplicaHolder(int rep)
        {
            ReplicaHolder r;
            next_replicas.TryGetValue(rep, out r);
            return r;
        }

        public ReplicaHolder getPrevReplicaHolder(int rep)
        {
            ReplicaHolder r;
            previous_replicas.TryGetValue(rep, out r);
            return r;
        }

        public Replica getOwnReplica(int rep)
        {
            ReplicaHolder r;
            own_replicas.TryGetValue(rep, out r);
            return r.getReplica();
        }

        public int getOwnReplicaCount()
        {
            return own_replicas.Count;
        }

        public Replica getNextReplica(int rep)
        {
            ReplicaHolder r;
            next_replicas.TryGetValue(rep, out r);
            return r.getReplica();
        }

        public int getNextReplicaCount()
        {
            return next_replicas.Count;
        }
        
        //Allows redifining of a node as another node
        public void setNextCorrespondence(Replica r, ReplicaRepresentation rr, int n)
        {
            if (next_replicas.ContainsKey(n))
                next_replicas.Remove(n);

            Log.debug("Setting next correspondence of " + n + " to " + rr.rep, "ReplicaCommunicator");
            next_replicas.Add(n, new ReplicaHolder(rr, r));
        }

        public void setPrevCorrespondence(ReplicaHolder rh, int n)
        {
            if (previous_replicas.ContainsKey(n))
                previous_replicas.Remove(n);

            Log.debug("Setting prev correspondence of " + n + " to " + rh.representation.rep, "ReplicaCommunicator");
            previous_replicas.Add(n, rh);
        }

        public void setNextCorrespondence(ReplicaHolder rh, int n)
        {
            if (next_replicas.ContainsKey(n))
                next_replicas.Remove(n);

            Log.debug("Setting  next correspondence of " + n + " to " + rh.representation.rep, "ReplicaCommunicator");
            next_replicas.Add(n, rh);
        }

        public void setOwnCorrespondence(ReplicaHolder rh, int n)
        {
            if (own_replicas.ContainsKey(n))
                own_replicas.Remove(n);

            Log.debug("Setting  own correspondence of " + n + " to " + rh.representation.rep, "ReplicaCommunicator");
            own_replicas.Add(n, rh);
        }
        
        public T TryCallPrev<T>(Func<T> f, int target)
        {
            int tries = 0;
            while (tries < 3)
            {
                try
                {
                    return f();
                }
                catch (Exception e)
                {
                    tries++;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return default(T);
        }

        public T TryCallOwn<T>(Func<T> f, int target)
        {
            try
            {
                return f();
            }
            catch(Exception e)
            {
                return default(T);
            }
        }

        public T TryCallNext<T>(Func<T> f, int target)
        {
            try
            {
                return f();
            }
            catch(Exception e)
            {
                return default(T);
            }
        }

        public bool input(OperatorPosition pos, int rep, Tuple t)
        {
            int tries = 0;
            while (tries < 3)
            {
                Replica r = getReplica(pos, rep);
                try
                {
                    return r.input(t);
                }
                catch (Exception e)
                {
                    tries++;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return false;
        }

        public bool tupleConfirmed(OperatorPosition pos, int rep, string uid)
        {
            int tries = 0;
            while (tries < 3)
            {
                Replica r = getReplica(pos, rep);
                try
                {
                    return r.tupleConfirmed(uid);           
                }
                catch (Exception e)
                {
                    tries++;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return false;
        }

        public bool addRecord(OperatorPosition pos, int rep, TupleRecord tr)
        {
            int tries = 0;
            while (tries < 3)
            {
                Replica r = getReplica(pos, rep);
                try
                {
                    return r.addRecord(tr);
                }
                catch (Exception e)
                {
                    tries++;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return false;
        }

        public bool purgeRecord(OperatorPosition pos, int rep, TupleRecord tr)
        {
            int tries = 0;
            while (tries < 3)
            {
                Replica r = getReplica(pos, rep);
                try
                {
                    return r.purgeRecord(tr);
                }
                catch (Exception e)
                {
                    tries++;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return false;
        }

        public Tuple fetchTuple(OperatorPosition pos, int rep, TupleRecord tr)
        {
            int tries = 0;
            while (tries < 3)
            {
                Replica r = getReplica(pos, rep);
                try
                {
                    return r.fetchTuple(tr);           
                }
                catch (Exception e)
                {
                    tries++;
                    System.Threading.Thread.Sleep(1000);
                }
            }
            return null;
        }
    }

    public enum OperatorPosition {
        Previous,
        Own,
        Next            
    }

    public class ReplicaHolder
    {
        //Remote object to a replica
        public ReplicaRepresentation representation;
        private Replica r;

        public ReplicaHolder(ReplicaRepresentation rr)
        {
            //Replica replica = connectToReplica(addr);
            this.representation = rr;
            this.r = null;
        }

        public ReplicaHolder(ReplicaHolder rh)
        {
            //Replica replica = connectToReplica(addr);
            this.representation = new ReplicaRepresentation(rh.representation);
            this.r = rh.getReplica();
        }

        public ReplicaHolder(ReplicaRepresentation rr, Replica r)
        {
            //Replica replica = connectToReplica(addr);
            this.representation = rr;
            this.r = r;
        }

        public Replica getReplica()
        {
            if(r != null)
            {
                return r;
            }

            r = (Replica)Activator.GetObject(typeof(Replica), representation.addr);
            return r;
        }
    }
}
