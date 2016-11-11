using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    /**
     * Static Logging Methods 
     */
    public interface ILogger
    {
        void writeLine(string s, params object[] args);
        void write(string s, params object[] args);
        void info(string s, params object[] args);
        void debug(string s, params object[] args);
    }

    public interface ILoggerReceiver
    {
        void writeLine(string s, string id, params object[] args);
        void write(string s, string id, params object[] args);
        void info(string s, string id, params object[] args);
        void debug(string s, string id, params object[] args);
    } 

    public class Log
    {
        public static void writeLine(string s, string id, params object[] args)
        {
            Console.WriteLine("[" + id + "] " + String.Format(s, args));
        }

        public static void write(string s, string id, params object[] args)
        {
            Console.Write("[" + id + "] " + String.Format(s, args));
        }

        public static void info(string s, string id, params object[] args)
        {
            if (Config.logLevel > 0)
                Console.WriteLine("[INFO-" + id + "]" + String.Format(s, args));
        }

        public static void debug(string s, string id, params object[] args)
        {
            if (Config.logLevel > 1)
                Console.WriteLine("[DEBUG-" + id + "]" + String.Format(s, args));
        }
    }

    /**
     * Logger base class (local logger) 
     */
    public class Logger : ILogger
    {

        protected string id;
        protected int level;

        public Logger(string _id)
        {
            id = _id;
            //FIXME::XXX -> get log level from constructor or somewhere else
            level = Config.logLevel;
        }

        public void writeLine(string s, params object[] args)
        {
            _writeLine(s, args);
        }

        public void write(string s, params object[] args)
        {
            _write(s, args);
        }

        public void info(string s, params object[] args)
        {
            if (level > 0)
                _info(s, args);
        }

        public void debug(string s, params object[] args)
        {
            if (level > 1)
                _debug(s, args);
        }

        protected virtual void _write(string s, params object[] args)
        {
            Console.Write("[" + id + "] " + String.Format(s, args));
        }

        protected virtual void _writeLine(string s, params object[] args)
        {
            Console.WriteLine("[" + id + "] " + String.Format(s, args));
        }

        protected virtual void _info(string s, params object[] args)
        {
            Console.WriteLine("[INFO-" + id + "] " + String.Format(s, args));
        }

        protected virtual void _debug(string s, params object[] args)
        {
            Console.WriteLine("[DEBUG-" + id + "]" + String.Format(s, args));
        }
    }

    public class RemoteLogger : Logger
    {
        private bool connected;
        private ILoggerReceiver remLogger;
        private string remoteAdress;
        public delegate void WriteAsyncDelegate(string str, string id, params object[] args);

        /**
         * Logging level:
         * 0 - light
         * 1 - full
         * 2 - Debug
         */

        public RemoteLogger(string _id, string pmAdress) : base(_id)
        {
            remoteAdress = pmAdress;
            connect(remoteAdress);
        }

        public void connect(string address)
        {
            remLogger = (ILoggerReceiver)Activator.GetObject(typeof(ILoggerReceiver), address);
            remoteAdress = address;
            connected = true;
        }

        protected override void _write(string s, params object[] args)
        {
            if (connected)
            {
                try
                {
                    WriteAsyncDelegate writeDel = new WriteAsyncDelegate(remLogger.writeLine);
                    IAsyncResult remAr = writeDel.BeginInvoke(s, id, args, null, null);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    connected = false;
                    writeLine("Remote logging failed", null);

                }
            }
            else if (remoteAdress != null)
            {
                connect(remoteAdress);
                write(s, args);
            }

            base._write(s, args);
        }

        protected override void _writeLine(string s, params object[] args)
        {
            if (connected)
            {
                try
                {
                    WriteAsyncDelegate writeDel = new WriteAsyncDelegate(remLogger.writeLine);
                    IAsyncResult remAr = writeDel.BeginInvoke(s, id, args, null, null);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    connected = false;
                    writeLine("Remote logging failed", null);
                }
            }
            else if (remoteAdress != null)
            {
                connect(remoteAdress);
                write(s, args);
            }

            base._writeLine(s, args);
        }

        protected override void _info(string s, params object[] args)
        {
            if (connected)
            {
                try
                {
                    WriteAsyncDelegate writeDel = new WriteAsyncDelegate(remLogger.info);
                    IAsyncResult remAr = writeDel.BeginInvoke(s, id, args, null, null);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    connected = false;
                    writeLine("Remote logging failed", null);

                }
            }
            else if (remoteAdress != null)
            {
                connect(remoteAdress);
                write(s, args);
            }

            base._info(s, args);
        }

        protected override void _debug(string s, params object[] args)
        {
            if (connected)
            {
                try
                {
                    WriteAsyncDelegate writeDel = new WriteAsyncDelegate(remLogger.debug);
                    IAsyncResult remAr = writeDel.BeginInvoke(s, id, args, null, null);
                }
                catch (System.Net.Sockets.SocketException e)
                {
                    connected = false;
                    writeLine("Remote logging failed", null);

                }
            }
            else if (remoteAdress != null)
            {
                connect(remoteAdress);
                write(s, args);
            }

            base._debug(s, args);
        }
    }
}