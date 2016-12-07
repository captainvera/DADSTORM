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
    public delegate void intervalEventHandler(object sender, IntervalEventArgs e);

    public class IntervalEventArgs : EventArgs
    {
        private readonly int _time;

        public IntervalEventArgs(int time)
        {
            _time = time;
        }

        public int time
        {
            get { return _time; }
        }
    }

    class OperatorWorkerPool
    {
        /** -----------------------Events-------------------------  **/
        public event freezeEventHandler freezeEventRaised;
        public event unfreezeEventHandler unfreezeEventRaised;
        public event crashEventHandler crashEventRaised;
        public event intervalEventHandler intervalEventRaised;

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

        protected virtual void onInterval(IntervalEventArgs e)
        {
            if (intervalEventRaised != null)
                intervalEventRaised(this, e);
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

        public void intervalAll(int time)
        {
            onInterval(new IntervalEventArgs(time));
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
            _source.Cancel();
            _wthread.Abort();
        }

        public void onInterval(object sender, IntervalEventArgs e)
        {
            _intervalTime = e.time;
            _interval = true;
            _source.Cancel();
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
        private bool _interval;
        private int _intervalTime;

        ManualResetEvent _unfreezeSignal;

        public OperatorWorker(OperatorWorkerPool parentPool, IOperator op, BlockingCollection<Tuple> input, BlockingCollection<Tuple> output)
        {
            _op = op;
            _in = input;
            _out = output;
            _wthread = new Thread(this.process);

            _source = new CancellationTokenSource();
            _unfreezeSignal = new ManualResetEvent(false);
            _freeze = false;
            _interval = false;

            parentPool.freezeEventRaised += new freezeEventHandler(onFreeze);
            parentPool.unfreezeEventRaised += new unfreezeEventHandler(onUnfreeze);
            parentPool.crashEventRaised += new crashEventHandler(onCrash);
            parentPool.intervalEventRaised += new intervalEventHandler(onInterval);
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
                        Log.debug("Null tuple result, ignoring", "Thread" + Thread.CurrentThread.ManagedThreadId);
                        Tuple t = new Tuple(0);
                        t.setId(data.getId());
                        _out.Add(t);
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                Log.info("Operation cancelled. Checking if there is data to restore.", "Thread" + Thread.CurrentThread.ManagedThreadId);
                if (res != null)
                {
                    Log.writeLine("Tuple restored to input buffer", "Thread" + Thread.CurrentThread.ManagedThreadId);
                    _out.Add(res);
                }
            }

            if (_freeze)
            {
                _freeze = false;
                Log.info("Received freeze event. Freezing...", "Thread" + Thread.CurrentThread.ManagedThreadId);
                waitFrozen();
            }

            else if(_interval)
            {
                _interval = false;
                Log.info("Received interval event. Freezing for " + _intervalTime + "...", "Thread" + Thread.CurrentThread.ManagedThreadId);
                Thread.Sleep(_intervalTime);
                Log.info("Interval time finished. Unfreezing...", "Thread" + Thread.CurrentThread.ManagedThreadId);
                process();
            }
        }        

        public void waitFrozen()
        {
            Log.info("Waiting for unfreeze signal...", "Thread" + Thread.CurrentThread.ManagedThreadId);
            _unfreezeSignal.WaitOne();
            _unfreezeSignal.Reset();
            Log.info("Unfreeze signal received! Restoring working state...", "Thread" + Thread.CurrentThread.ManagedThreadId);
            process();
        }
    }
}
