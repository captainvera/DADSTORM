using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tuple = DADSTORM.Tuple;

namespace DADSTORM
{
    class OperatorWorkerPool
    {
        private int _size;
        private List<OperatorWorker> _workers;

        public OperatorWorkerPool(int size, IOperator op)
        {
            _size = size;
            _workers = new List<OperatorWorker>();

            for(int i = 0; i < size; i++)
            {
                _workers.Add(new OperatorWorker(op));
            }
        }
    }
       
    class OperatorWorker
    {
        private IOperator _op;
        private bool _active;

        public OperatorWorker(IOperator op)
        {
            op = _op;            
        }

        public void process()
        {
            while(_active)
            {
                
            }
        }
        
        private Tuple fetch()
        {
            //Get tuple from concurrent buffer
            return new Tuple(0);
        } 
    }
}
