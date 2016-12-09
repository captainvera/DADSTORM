using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Tuple = DADSTORM.Tuple;
using System.Threading;

namespace DADSTORM
{
    class TupleFileReader
    {

        private Queue<string> _tuples;
        private string _file;
        private ReplicaRepresentation _rep;

        public TupleFileReader(string file, ReplicaRepresentation r)
        {
            _tuples = new Queue<string>();
            _file = file;
            _rep = r;

        }

        public void readFile()
        {
            try
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(_file);

                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    Log.debug(line, "TupleFileReader");
                    if (!line.StartsWith("%%"))
                        _tuples.Enqueue(line);
                }
                reader.Close();
            }
            catch(Exception e)
            {
                Log.writeLine("Failed to open provided file. Ignoring input file....", "TupleFileReaderWorker");
            }
        }

        public Tuple getNextTuple()
        {
            string tup;

            try
            {
                tup = _tuples.Dequeue();
            } catch(InvalidOperationException e)
            {
                //empty queue
                return null;
            }

            char[] splitChars = { ' ', ',' };

            string[] fields = tup.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            Tuple res = new Tuple(fields.Length);

            res.stamp(_rep);

            for(int i = 0; i < fields.Length; i++)
            {
                string str = fields[i];

                if (str.Contains("\""))
                {
                    Log.debug("String" + str + " contained \"", "TFRWorker");
                    str = str.Replace("\"", "");
                    Log.debug("Result : " + str, "TFRWorker");
                } 
                res.set(i, str);
               // Log.debug(str + " inserted at " + i, "TupleFileReader");
            }
            return res;
        }
    }

    class TupleFileReaderWorker
    {
        private TupleFileReader _reader;
        private BlockingCollection<Tuple> _outputBuffer;
        private Thread _wthread;
        private Replica _rep;

        public TupleFileReaderWorker(BlockingCollection<Tuple> output, string file, Replica rep)
        {
            _outputBuffer = output;
            _reader = new TupleFileReader(file, rep.getRepresentation());
            _wthread = new Thread(this.process);
            _rep = rep;
        }

        public void start()
        {
            _wthread.Start(); 
        }

        public void process()
        {
            Log.writeLine("Starting outputting tuples to buffer", "TFRWorker");

            _reader.readFile();
            Tuple tup;

            tup = null;
            while((tup = _reader.getNextTuple()) != null)
            {
                Log.writeLine("Tuple is " + tup.toString(), "TFRWorker");
                Log.writeLine("Adding tuple with id " + tup.getId().id + "->" +tup.getId().op + "->" + tup.getId().rep, "TFRWorker");

                //_outputBuffer.Add(tup);
                _rep.input(tup);
            }

            Log.writeLine("Finished reading file. Exiting...", "TFRWorker");
        }
    } 
}
