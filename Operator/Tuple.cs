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

        public Tuple(int size)
        {
            _size = size; 
            _items = new string[size];
        }

        public Tuple(Tuple tup){
            _items = tup.toArray();
            _size = _items.Length;
            
        }

        public Tuple(string[] str)
        {
            _items = str;
            _size = str.Count();
        }

        public void setFromArrayCopy(string[] items)
        {
            if(items.Length == _items.Length)
            {
                items.CopyTo(_items, 0);
            }
            else
            {
                Console.WriteLine("[ERROR] Tried to initialize Tuple with incorrectly sized array");
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
                    res += _items[i] + ", ";
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
    }
}
