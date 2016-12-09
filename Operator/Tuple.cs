using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    [Serializable]
    public class Tuple
    {
        private int _size;
        private string[] _items;
        private TupleId _id;
        
        public Tuple()
        {
            _size = 0;
            _items = new string[0];
            _id = new TupleId(generateId());
        }

        public Tuple(int size, TupleId id)
        {
            _size = size; 
            _items = new string[size];
            _id = id;
            _id = new TupleId(generateId());
        }

        public Tuple(int size)
        {
            _size = size; 
            _items = new string[size];
            _id = new TupleId(generateId());
        }

        public Tuple(Tuple tup){
            _items = tup.toArray();
            _size = _items.Length;
            _id = new TupleId();
            setId(tup.getId());
        }

        public Tuple(string[] str)
        {
            _items = str;
            _size = str.Count();
            _id = new TupleId(generateId());
        }

        public void update(String op, int rep)
        {
            if(_id == null)
            {
                _id = new TupleId(generateId());
            }
            _id.op = op;
            _id.rep = rep;
        }

        public static string generateId()
        {
            double r = RandomGenerator.nextLong(100000000000, 899999999999);
            r += 100000000000;
            String id = r.ToString();
            return id;
        }
        
        public TupleId getId()
        {
            return _id;
        }

        public void setId(TupleId id)
        {
            _id.id = id.id;
            _id.op = id.op;
            _id.rep = id.rep;
        }

        public void setFromArrayCopy(string[] items)
        {
            if(items.Length == _items.Length)
            {
                items.CopyTo(_items, 0);
            }
            else
            {
                Log.writeLine("[ERROR] Tried to initialize Tuple with incorrectly sized array", "TUPLE");
            }
        }

        public void set(int index, string data)
        {
            _items[index] = data;
        }

        public string get(int index)
        {
            return _items[index];
        }
        
        public string[] toArray()
        {
            return _items;
        }

        public string toString()
        {
            string res = "<";
            if(_items.Length > 0){
                for (int i = 0; i < _items.Length - 1; i++)
                {
                    res += _items[i] + " - ";
                }
                res += _items[_items.Length - 1];
            }
            res += ">";

            return res;
        }

        public int getSize()
        {
            return _size;
        }

        public void stamp(ReplicaRepresentation r)
        {
            _id.op = r.op;
            _id.rep = r.rep;
        }
    }

    [Serializable]
    public class TupleId
    {
        public string id;
        public string op;
        public int rep;

        public TupleId()
        {
            this.id = "";
            this.op = "";
            this.rep = -1;
        }

        public TupleId(string id, string op, int rep)
        {
            this.id = id;
            this.op = op;
            this.rep = rep;
        }

        public TupleId(string id)
        {
            this.id = id;
            this.op = "";
            this.rep = -1;
        }

        public String getUID()
        {
            return id;
        }

        public string toString()
        {
            return id + "(" + op + "->" + rep + ")";
        }
    }
}
