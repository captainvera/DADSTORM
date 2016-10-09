using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    public class Tuple
    {
        private int _size;
        private string[] _items;

        public Tuple(int size)
        {
            _items = new string[size];
        }

        public Tuple(Tuple tup){
            _items = tup.getArray();
            _size = _items.Length;
            
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
        
        public string[] getArray()
        {
            return _items;
        }
    }
}
