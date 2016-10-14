using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Puppetmaster
{
    public class Shell
    {
        private Dictionary<string, Command> _commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
        private string _prompt = ">>";

        public Shell()
        {
            new HelpCommand(this);
            new ExitCommand(this);
            new FreezeCommand(this);
            new UnfreezeCommand(this);
            new StartCommand(this);
            new CrashCommand(this);
            new IntervalCommand(this);
            new StatusCommand(this);

        }

        internal void add(Command command)
        {
            _commands.Add(command.getID(), command);
        }

        public void start()
        {
            string str;
            Console.Write(_prompt);
            while ((str = Console.ReadLine()) != null)
            {
                String[] args = str.Split(' ');
                Command c;
                _commands.TryGetValue(args[0], out c);
                if(c != null)
                {
                    c.execute(args);
                }
                Console.Write(_prompt);
            }
        }

        public void exit()
        {
            Environment.Exit(0);
        }

        public void print(string txt)
        {
            Console.WriteLine(txt);
        }

    }
}
