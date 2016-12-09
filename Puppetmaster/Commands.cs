using System;

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
                print(getID() + " command does not take any arguments");
            } else print("Puppermaster shell help.\nAvailable commands:\nstart\ninterval\nstatus\nwait\ncrash\nfreeze\nunfreeze\nreadfile");
        }
    }

    class StartCommand : Command
    {
        public StartCommand(Shell sh) : base(sh, "start") { }

        public override void execute(string[] list)
        {
            if (list.Length != 2)
            {
                print("[Usage]" + getID() + " operator_id");
            }
            else Shell().startOP(list[1]);
        }
    }

    class IntervalCommand : Command
    {
        public IntervalCommand(Shell sh) : base(sh, "interval") { }

        public override void execute(string[] list)
        {
            if (list.Length != 3)
            {
                 print("[Usage]" + getID() + " operator_id x_ms");
            }
            else {
                int time = 0;
                if (Int32.TryParse(list[2], out time) == true)
                    Shell().interval(list[1], time);
                else print("[Usage] " + list[2] + " must be an integer");
            }
        }
    }

    class StatusCommand : Command
    {
        public StatusCommand(Shell sh) : base(sh, "status") { }

        public override void execute(string[] list)
        {
            if (list.Length != 1)
            {
                print("[Usage]" + getID() + " command has no arguments");
            }
            else Shell().status();
        }
    }

    class TablesCommand : Command
    {
        public TablesCommand(Shell sh) : base(sh, "tables") { }

        public override void execute(string[] list)
        {
            if (list.Length != 1)
            {
                print("[Usage]" + getID() + " command has no arguments");
            }
            else Shell().tables();
        }

    }
    class CrashCommand : Command
    {
        public CrashCommand(Shell sh) : base(sh, "crash") { }

        public override void execute(string[] list)
        {
            if (list.Length != 3)   
            {
                print("[Usage]" + getID() + " OperatorID ReplicaID");
            }
            else {
                int replica_id = 0;
                if (Int32.TryParse(list[2], out replica_id) == true)
                    Shell().crash(list[1], replica_id);
                else print("[Usage] " + list[2] + " must be an integer");
            }
        }
    }

    class FreezeCommand : Command
    {
        public FreezeCommand(Shell sh) : base(sh, "freeze") { }

        public override void execute(string[] list)
        {
            if (list.Length != 3)
            {
                print("[Usage]" + getID() + " OperatorID ReplicaID");
            }
            else {
                int replica_id = 0;
                if (Int32.TryParse(list[2], out replica_id) == true)
                    Shell().freeze(list[1], replica_id);
                else print("[Usage] " + list[2] + " must be an integer");
            }
        }
    }

    class UnfreezeCommand : Command
    {
        public UnfreezeCommand(Shell sh) : base(sh, "unfreeze") { }

        public override void execute(string[] list)
        {
            if (list.Length != 3)
            {
                print("[Usage]" + getID() + " OperatorID ReplicaID");
            }
            else
            {
                int replica_id = 0;
                if (Int32.TryParse(list[2], out replica_id) == true)
                    Shell().unfreeze(list[1], replica_id);
                else print("[Usage] " + list[2] + " must be an integer");
            }
        }
    }

    class ExitCommand : Command
    {
        public ExitCommand(Shell sh) : base(sh, "exit") { }

        public override void execute(string[] list)
        {
            if (list.Length != 1)
            {
                print("[Usage]" + getID() + " takes no arguments");
            }
            else Shell().exit();
        }
    }

    class WaitCommand : Command
    {
        public WaitCommand(Shell sh) : base(sh, "wait") { }

        public override void execute(string[] list)
        {
            if (list.Length != 2)
            {
                print("[Usage]" + getID() + " x_ms");
            }
            else {
                int time = 0;
                if (Int32.TryParse(list[1], out time) == true)
                    Shell().wait(time);
                else print("[Usage] " + list[1] +" must be an integer");
            }
        }
    }

    class ReadFileCommand : Command
    {
        public ReadFileCommand(Shell sh) : base(sh, "readfile") { }

        public override void execute(string[] list)
        {
            if (list.Length != 3)
            {
                print("[Usage]" + getID() + " file");
            }
            else {
                int replica_id = 0;
                if (Int32.TryParse(list[2], out replica_id) == true)
                    Shell().readFile(list[1], replica_id);
                else print("[Usage] " + list[2] + " must be an integer");
            }
        }
    }
}
