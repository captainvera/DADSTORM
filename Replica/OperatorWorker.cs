using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tuple = DADSTORM.Tuple;
using System.Threading;

namespace DADSTORM
{
    public delegate void freezeEventHandler(object sender, EventArgs e);
    public delegate void unfreezeEventHandler(object sender, EventArgs e);
    public delegate void crashEventHandler(object sender, EventArgs e);

    class OperatorWorkerPool
    {
        /** -----------------------Events-------------------------  **/
        public event freezeEventHandler freezeEventRaised;
        public event unfreezeEventHandler unfreezeEventRaised;
        public event crashEventHandler crashEventRaised;

        protected virtual void onFreeze(EventArgs e)
        {
            if (freezeEventRaised != null)
                freezeEventRaised(this, e);
        }

        protected virtual void onCrash(EventArgs e)
        {
            if (crashEventRaised != null)
                crashEventRaised(this, e);
        }

        protected virtual void onUnfreeze(EventArgs e)
        {
            if (unfreezeEventRaised != null)
                unfreezeEventRaised(this, e);
        }
        /** ------------------------------------------------------  **/

        private int _size;
        private List<OperatorWorker> _workers;
        private BlockingCollection<Tuple> _in;
        private BlockingCollection<Tuple> _out;

        public OperatorWorkerPool(int size, IOperator op, BlockingCollection<Tuple> input, BlockingCollection<Tuple> output)
        {
            _size = size;
            _workers = new List<OperatorWorker>();
            _in = input;
            _out = output;

            for(int i = 0; i < size; i++)
            {
                _workers.Add(new OperatorWorker(this, op, input, output));
            }
        }

        public void start()
        {
            foreach(OperatorWorker ow in _workers)
            {
                ow.start();
            }
        }

        public void haltAll(int time)
        {
            foreach(OperatorWorker ow in _workers)
            {
                ow.halt(time);
            }
        }

        public void freezeAll()
        {
            onFreeze(EventArgs.Empty);
        }

        public void unfreezeAll()
        {
            onUnfreeze(EventArgs.Empty);
        } 
    }
       
    class OperatorWorker
    {
        /** -----------------------Events-------------------------  **/
        public void onFreeze(object sender, EventArgs e)
        {
            _freeze = true;
            _source.Cancel();
        }

        public void onUnfreeze(object sender, EventArgs e)
        {
            _freeze = false;
            _unfreezeSignal.Set();
        }
        
        public void onCrash(object sender, EventArgs e)
        {
            //TODO::XXX::Maybe kill the threads in a safer way?
            _source.Cancel();
            _wthread.Abort();
        }
        /** ------------------------------------------------------  **/

        private IOperator _op;
        private BlockingCollection<Tuple> _in;
        private BlockingCollection<Tuple> _out;

        private Thread _wthread;
            
        private CancellationTokenSource _source;
        private CancellationToken _cancelToken;

        //Internal state variables
        //Boolean read and writes are atomic, no need for thread locking
        private bool _freeze;
        private bool _halt;
        private int _haltTime;

        ManualResetEvent _unfreezeSignal;

        public OperatorWorker(OperatorWorkerPool parentPool, IOperator op, BlockingCollection<Tuple> input, BlockingCollection<Tuple> output)
        {
            _op = op;
            _in = input;
            _out = output;
            _wthread = new Thread(this.process);

            _unfreezeSignal = new ManualResetEvent(false);
            _freeze = false;
            _halt = false;

            parentPool.freezeEventRaised += new freezeEventHandler(onFreeze);
            parentPool.unfreezeEventRaised += new unfreezeEventHandler(onUnfreeze);
            parentPool.crashEventRaised += new crashEventHandler(onCrash);
        }

        public void start()
        {
            _wthread.Start();
        }

        public void abort()
        {
            _wthread.Abort();
        }

        public void process()
        {
            Tuple res = null;

            //Generate a token to cancel blocking operation if necessary
            _source = new CancellationTokenSource();
            _cancelToken = _source.Token;

            Log.writeLine("Starting processing operation", "Thread" + Thread.CurrentThread.ManagedThreadId);
            try
            {
                foreach (var data in _in.GetConsumingEnumerable(_cancelToken))
                {
                    res = _op.process(data);
                    if (res != null)
                    {
                        Log.debug(res.toString(), "Thread" + Thread.CurrentThread.ManagedThreadId);

                        _out.Add(res);
                        res = null;
                    }
                    else
                    {
                        Log.writeLine("Null tuple result, ignoring", "Thread" + Thread.CurrentThread.ManagedThreadId);

                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Log.writeLine("Operation cancelled. Checking if there is data to restore.", "Thread" + Thread.CurrentThread.ManagedThreadId);
                if (res != null)
                {
                    Log.writeLine("Tuple restored to input buffer", "Thread" + Thread.CurrentThread.ManagedThreadId);
                    _out.Add(res);
                }
            }

            if (_freeze)
            {
                Log.writeLine("Received freeze event. Freezing...", "Thread" + Thread.CurrentThread.ManagedThreadId);
                waitFrozen();
            }
            else if(_halt)
            {
                _halt = false;
            }
        }        

        public void waitFrozen()
        {
            Log.writeLine("Waiting for unfreeze signal...", "Thread" + Thread.CurrentThread.ManagedThreadId);
            _unfreezeSignal.WaitOne();
            _unfreezeSignal.Reset();
            Log.writeLine("Unfreeze signal received! Restoring working state...", "Thread" + Thread.CurrentThread.ManagedThreadId);
            process();
        }

        public void halt(int time)
        {
            _halt = true;
            _haltTime = time;
        }
    }
}
