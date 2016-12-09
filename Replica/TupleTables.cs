using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace DADSTORM
{
    public class SharedTupleTable
    {
        private ConcurrentDictionary<string, TupleRecord> table;

        public SharedTupleTable()
        {
            table = new ConcurrentDictionary<string, TupleRecord>();
        }

        public bool contains(string uuid)
        {
            return table.ContainsKey(uuid);
        }

        public void add(Tuple t, TupleState ts, int rep)
        {
            TupleRecord tr = new TupleRecord(t.getId(), ts, rep);

            add(tr);
        }

        public void add(TupleRecord tr)
        {
            if (!contains(tr.getUID()))
            {
                table.TryAdd(tr.getUID(), tr);
            }
            else
            {
                Console.WriteLine("NO\nO\nO\nO\nO\nO\nO\nO\nO\nO\nOOOO WTF RECEIVED DUPLICATE RECORD????");
                Console.WriteLine("----->> DUP: " + tr.id.toString() + " & " + tr.state.ToString());
            }
        }

        public TupleRecord get(String id)
        {
            TupleRecord tr = null;
            table.TryGetValue(id, out tr);
            return tr;
        }
        
        public TupleRecord remove(TupleRecord tr)
        {
            return remove(tr.getUID());
        }

        public TupleRecord remove(string uuid)
        {
            if (table.ContainsKey(uuid))
            {
                TupleRecord removed;
                table.TryRemove(uuid, out removed);
                return removed;
            }
            return null;
        }

        public void purge(string uuid)
        {
            if (table.ContainsKey(uuid))
            {
                TupleRecord tr = get(uuid);
                tr.state = TupleState.purged;
            }
        }

        public void printTable()
        {
            Console.WriteLine("RECORD_ID | FROM_OP | FROM_REP | STATE");
            foreach(KeyValuePair<string, TupleRecord> entry in table)
            {
                Console.WriteLine(entry.Key + " | " + entry.Value.id.op + " | " + entry.Value.id.rep + " | " +entry.Value.state.ToString());
            }
        }

        public List<TupleRecord> toList()
        {
            return table.Values.ToList();
        }
    }

    public class DeliveryTable
    {
        private ConcurrentDictionary<string, Tuple> table;

        public DeliveryTable()
        {
            table = new ConcurrentDictionary<string, Tuple>();
        }

        public void add(Tuple t)
        {
            table.TryAdd(t.getId().getUID(), t);
        }

        public Tuple get(String id)
        {
            Tuple t = null;
            table.TryGetValue(id, out t);
            return t;
        }

        public bool contains(Tuple t)
        {
            return table.ContainsKey(t.getId().getUID());
        }

        public Tuple remove(string uuid)
        {
            if (table.ContainsKey(uuid))
            {
                Tuple t;
                table.TryRemove(uuid, out t);
                return t;
            }
            return null;
        }


        public Tuple remove(Tuple t)
        {
            return remove(t.getId().getUID());
        }

        public void printTable()
        {
            Console.WriteLine("ID | FROM_OP | FROM_REP");
            foreach(KeyValuePair<string, Tuple> entry in table)
            {
                Console.WriteLine(entry.Key + " | " + entry.Value.getId().op + " | " + entry.Value.getId().rep);
            }
        }
    }

    [Serializable]
    public class TupleRecord
    {
        public TupleId id;
        public TupleState state;
        public int rep;

        public TupleRecord(TupleId id, TupleState state, int rep)
        {
            this.id = id;
            this.state = state;
            this.rep = rep;
        }

        public string getUID()
        {
            return id.id; 
        }

        public int getRep()
        {
            return rep;
        }
    }

    [Serializable]
    public enum TupleState
    {
        pending,        //Not sure it's been delivered
        delivered,      //Got response confirming delivery
        purged          //Every replica knows it's delivery
    }
}
