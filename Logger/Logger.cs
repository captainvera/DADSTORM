using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{

    public interface ILogger
    {

        void writeLine(string s, params object[] args);

        void write(string s, params object[] args);

    }
    public class Logger : ILogger
    {
        /**
         * Logging level:
         * 0 - Regular
         * 1 - Verbose
         * 2 - Debug
         */

        private string id;

        static int level = 1;

        public Logger(string _id)
        {
            id = _id;
        }

        public void writeLine(string s, params object[] args)
        {
            Console.WriteLine("[" + id + "] " + String.Format(s, args)); 
        }

        public void write(string s, params object[] args)
        {
            Console.Write("[" + id + "] " + String.Format(s, args)); 
        }

        public static void writeLine(string s, string id, params object[] args)
        {
            Console.WriteLine("[" + id + "] " + String.Format(s, args));
        }

        public static void write(string s, string id, params object[] args)
        {
            Console.Write("[" + id + "] " + String.Format(s, args));
        }

        public static void debug(string s, params object[] args)
        {
            if(level > 1)
                Console.Write("[DEBUG] " + String.Format(s, args));
        }
    }

    public class RemoteLogger : ILogger
    {
        private string id;
        private bool connected;
        private ILogger remLogger;
        private string remoteAdress;
        public delegate void WriteAsyncDelegate(string str, params object[] args);

        /**
         * Logging level:
         * 0 - Regular
         * 1 - Verbose
         * 2 - Debug
         */

        public RemoteLogger(string _id)
        {
            id = _id;
        }

        public void connect(string address)
        {
            remLogger = (ILogger)Activator.GetObject(typeof(ILogger), address);
            remoteAdress = address;
            connected = true;
        }

        public void write(string s, params object[] args)
        {
            if (connected)
            {
                try
                {
                    WriteAsyncDelegate writeDel = new WriteAsyncDelegate(remLogger.writeLine);
                    IAsyncResult remAr = writeDel.BeginInvoke(s, args, null, null);
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

        }

        public void writeLine(string s, params object[] args)
        {
            if (connected)
            {
                try
                {
                    WriteAsyncDelegate writeDel = new WriteAsyncDelegate(remLogger.writeLine);
                    IAsyncResult remAr = writeDel.BeginInvoke(s, args, null, null);
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
        }
    }
}
