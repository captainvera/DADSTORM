﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DADSTORM
{
    class Shell
    {
        private Dictionary<string, Command> _commands = new Dictionary<string, Command>(StringComparer.OrdinalIgnoreCase);
        private string _prompt = ">> ";
        private Puppetmaster _pm;
        private bool _exit;

        public Shell(Puppetmaster pm)
        {
            new HelpCommand(this);
            new ExitCommand(this);
            new FreezeCommand(this);
            new UnfreezeCommand(this);
            new StartCommand(this);
            new CrashCommand(this);
            new IntervalCommand(this);
            new StatusCommand(this);
            new WaitCommand(this);
            new ReadFileCommand(this);
            new TablesCommand(this);
            _pm = pm;
        }

        internal void add(Command command)
        {
            _commands.Add(command.getID(), command);
        }

        public void start(string[] commands)
        {
            foreach(string cmd in commands)
            {
                string str = Console.ReadLine();
                Console.WriteLine("RECEIVED ------------>" + str);
                switch (str)
                {
                    case "next":
                        Console.WriteLine("PROCESSING------->" + cmd);
                        processCommand(cmd);
                        break;
                    case "all":
                        processCommandList(commands);
                        start();
                        break;
                    case "skip":
                        start();
                        break;
                }
            }

            start();
        }

        public void start()
        {
            string str;
            Console.Write(_prompt);
            while ((str = Console.ReadLine()) != null)
            {
                processCommand(str);
                if (_exit == true)
                    break;
                Console.Write(_prompt);
            }
        }

        public void processCommandList(string[] commands)
        {
            foreach(string command in commands)
            {
                processCommand(command);
            }
        }

        private void processCommand(string command)
        {
            String[] args = command.Split(' ');
            Command c;
            _commands.TryGetValue(args[0], out c);
            if (c != null)
            {
                c.execute(args);
            }
            else
            {
                print("ERROR: Command " + args[0] + " isn't recognized");
            }

        }

        public void exit()
        {
            _exit = true;
        }

        public void print(string txt)
        {
            Console.WriteLine("Console", txt);
        }

        public void startOP(string str)
        {
            _pm.start(str);
        }

        public void wait(int time)
        {
            _pm.wait(time);
        }

        public void status()
        {
            _pm.status();
        }

        public void tables()
        {
            _pm.printTables();
        }

        public void interval(string op, int time)
        {
            _pm.interval(op, time);
        }

        public void crash(string op, int replica)
        {
            _pm.crash(op, replica);
        }

        public void freeze(string op, int replica)
        {
            _pm.freeze(op, replica);
        }

        public void unfreeze(string op, int replica)
        {
            _pm.unfreeze(op, replica);
        }

        public void readFile(string op, int replica)
        {
            _pm.readFile(op, replica);
        }
    }
}
