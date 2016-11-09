﻿using System;
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

        public TupleFileReader(string file)
        {
            _tuples = new Queue<string>();
            _file = file;

        }

        public void readFile()
        {
            System.IO.StreamReader reader = new System.IO.StreamReader(_file);
            string line = reader.ReadLine();
            while ((line = reader.ReadLine()) != null)
            {
                Logger.debug(line, "TupleFileReader");
                _tuples.Enqueue(line);
            }
            reader.Close();
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
            for(int i = 0; i < fields.Length; i++)
            {
                res.set(i, fields[i]);
                Logger.debug(fields[i] + " inserted at " + i, "TupleFileReader");
                
            }
            return res;
        }
    }

    class TupleFileReaderWorker
    {
        private TupleFileReader _reader;
        private BlockingCollection<Tuple> _outputBuffer;
        private Thread _wthread;

        public TupleFileReaderWorker(BlockingCollection<Tuple> output, string file)
        {
            _outputBuffer = output;
            _reader = new TupleFileReader(file);
            _wthread = new Thread(this.process);
        }

        public void start()
        {
            _wthread.Start(); 
        }

        public void process()
        {
            Thread.Sleep(2000);
            Logger.writeLine("Starting outputting tuples to buffer", "TFRWorker");

            _reader.readFile();
            Tuple tup;
            while((tup = _reader.getNextTuple()) != null)
            {
                _outputBuffer.Add(tup);
            }

            Logger.writeLine("Finished reading file. Exiting...", "TFRWorker");
        }
    } 
}