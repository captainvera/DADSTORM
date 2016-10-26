﻿using System;

namespace DADSTORM
{
    abstract class Command
    {
        private Shell _sh;
        private string _id;

        public Command(Shell sh, string id)
        {
            _id = id;
            _sh = sh;

            _sh.add(this);
        }   

        public string getID()
        {
            return _id;
        }

        public void print(string s)
        {
            _sh.print(s);
        }

        protected Shell Shell()
        {
            return _sh;
        }

        public abstract void execute(params string[] list);
    }

    class HelpCommand : Command
    {
        public HelpCommand(Shell sh) : base(sh, "help") { }

        public override void execute(string[] list)
        {
            if (list.Length > 1)
            {
                Console.WriteLine( getID() + " command does not take any arguments");
            } else print("Puppermaster shell help.");
        }
    }

    class StartCommand : Command
    {
        public StartCommand(Shell sh) : base(sh, "start") { }

        public override void execute(string[] list)
        {
            if (list.Length != 2)
            {
                Console.WriteLine("[Usage]" + getID() + " operator_id");
            }
            else Shell().start(list[1]);
        }
    }

    class StopCommand : Command
    {
        public StopCommand(Shell sh) : base(sh, "stop") { }

        public override void execute(string[] list)
        {
            if (list.Length != 2)
            {
                Console.WriteLine("[Usage]" + getID() + " operator_id");
            }
            else Shell().stop(list[1]);
        }
    }

    class IntervalCommand : Command
    {
        public IntervalCommand(Shell sh) : base(sh, "interval") { }

        public override void execute(string[] list)
        {
            if (list.Length != 3)
            {
                Console.WriteLine("[Usage]" + getID() + " operator_id x_ms");
            }
            else print("implement me");//DO something
        }
    }

    class StatusCommand : Command
    {
        public StatusCommand(Shell sh) : base(sh, "status") { }

        public override void execute(string[] list)
        {
            if (list.Length != 1)
            {
                Console.WriteLine("[Usage]" + getID() + " command has no arguments");
            }
            else print("implement me");//DO something
        }
    }

    class CrashCommand : Command
    {
        public CrashCommand(Shell sh) : base(sh, "crash") { }

        public override void execute(string[] list)
        {
            if (list.Length != 2)   
            {
                Console.WriteLine("[Usage]" + getID() + " process_name");
            }
            else print("implement me");//DO something
        }
    }

    class FreezeCommand : Command
    {
        public FreezeCommand(Shell sh) : base(sh, "freeze") { }

        public override void execute(string[] list)
        {
            if (list.Length != 2)
            {
                Console.WriteLine("[Usage]" + getID() + " process_name");
            }
            else print("implement me");//DO something
        }
    }

    class UnfreezeCommand : Command
    {
        public UnfreezeCommand(Shell sh) : base(sh, "unfreeze") { }

        public override void execute(string[] list)
        {
            if (list.Length != 2)
            {
                Console.WriteLine("[Usage]" + getID() + " process_name");
            }
            else print("implement me");//DO something
        }
    }

    class ExitCommand : Command
    {
        public ExitCommand(Shell sh) : base(sh, "exit") { }

        public override void execute(string[] list)
        {
            if (list.Length != 1)
            {
                Console.WriteLine("[Usage]" + getID() + " takes no arguments");
            }
            else Shell().exit();//DO something
        }
    }
}