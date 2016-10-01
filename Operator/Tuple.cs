using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Operator
{
    public class Tuple
    {
        private int _size;
        private string[] _items;

        public Tuple(int size)
        {
            _items = new string[5];
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
