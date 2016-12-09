using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DADSTORM;
using System.Reflection;
using System.Collections;
using System.Globalization;

namespace DADSTORM
{
    public class OperatorFactory
    {
        static public IOperator create(string opname, string[] args, ReplicaRepresentation rr)
        {
            int field = 0;
            switch (opname)
            {
                //Argument number checking?
                //This is a OperatorFactory / OperatorSource, not an Operator
                //IOperator return OperatorFactory.create(OPNAME, ARGS);
                case "DUP":
                    Log.writeLine("DUP operator started.", "OperatorSelector");
                    return new DUP();

                case "UNIQ":
                    Log.writeLine("UNIQ operator started.", "OperatorSelector");
                    if (Int32.TryParse(args[0], out field) == true)
                        return new UNIQ(field);
                    else
                    {
                        Log.writeLine("ERROR: UNIQ operator could not be instanced, wrong arguments.", "OperatorSelector");
                        goto case "SAFE";
                    }

                case "CUSTOM":
                    Log.writeLine("CUSTOM operator started.", "OperatorSelector");
                    if (args.Length == 3)
                        return new CUSTOM(args[0], args[1], args[2], rr);
                    else
                    {
                        Log.writeLine("ERROR: CUSTOM operator could not be instanced, wrong arguments.", "OperatorSelector");
                        goto case "SAFE";
                    }

                case "FILTER":
                    Log.writeLine("FILTER operator started.", "OperatorSelector");
                    if (args.Length == 3 && Int32.TryParse(args[0], out field) == true)
                        return new FILTER(field, args[1], args[2]);
                    else
                    {
                        Log.writeLine("ERROR: FILTER operator could not be instanced, wrong arguments.", "OperatorSelector");
                        goto case "SAFE";
                    }

                case "COUNT":
                    Log.writeLine("COUNT operator started.", "OperatorSelector");
                    return new COUNT();

                case "SAFE":
                    Log.writeLine("ERROR: Instancing DUP as safe default.", "OperatorSelector");
                    return new DUP();

                default:
                    Log.writeLine("ERROR: Instancing DUP as safe default.", "OperatorSelector");
                    return new DUP();
            }
        }
    }

    public interface IOperator
    {
        List<Tuple> process(Tuple t);
        IList<IList<string>> CustomOperation(IList<string> l);
    }

    public class COUNT : IOperator
    {
        private int _countNumber = 0;

        public List<Tuple> process(Tuple t)
        {
            _countNumber++;

            Tuple res = new Tuple(1);
            res.set(0, _countNumber.ToString());

            res.setId(t.getId());

            List<Tuple> result = new List<Tuple>();
            result.Add(res);

            return  result;
        }

        public int get()
        {
            return _countNumber;
        }

        public IList<IList<string>> CustomOperation(IList<string> l)
        {
            return new List<IList<string>>();
        }
    }

    public class CUSTOM : IOperator
    {
        private MethodInfo _method;
        private Type _type;
        private object _instance;
        private ReplicaRepresentation _rr;

        public IList<IList<string>> CustomOperation(IList<string> l)
        {
            return new List<IList<string>>();

        }
        public CUSTOM(string dll, string classLoad, string method, ReplicaRepresentation rr)
        {
            Log.writeLine("Trying to load dll {0} , class {1} and method {2}", "CustomOPERATOR", dll, classLoad, method);
            _rr = rr;

            byte[] bytes = System.IO.File.ReadAllBytes(@".\" + dll);
            Assembly assembly = Assembly.Load(bytes);

            IEnumerable<Type> types = null;

            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Log.writeLine("Failed loading type... Trying alternative", "CustomOPERATOR");
                types = e.Types.Where(t => t != null);
            }

            foreach (Type type in types)
            {
                Log.writeLine("FOUND TYPE " + type.FullName, "CustomOPERATOR");
                if (type.IsClass == true)
                {
                    Log.writeLine("It's a class!", "CustomOPERATOR");
                    if (type.FullName.EndsWith("." + classLoad))
                    {
                        Log.writeLine("It's our class ;)!", "CustomOPERATOR");
                        _type = type;
                    }
                }
            }

            if (_type == null)
                throw new WrongParameterException("CUSTOM operator, cannot find:" + classLoad + " in dll:" + dll);

            _method = _type.GetMethod(method, new Type[] { typeof(string[]) });

            if (_method == null)
                throw new WrongParameterException("CUSTOM operator, type:" + _type.ToString() + " has no method:" + method + " receiving a list of Strings");

            _instance = Activator.CreateInstance(_type);
        }

        public List<Tuple> process(Tuple t)
        {
            List<string> listargs = t.toArray().ToList<string>();

            object[] args = new object[] { listargs };
            object result = null;

            int tries = 0;

            //Only here for multi threading support in certain operator operations
            //like file reading
            List<Tuple> res = new List<Tuple>();

            while (tries < 10)
            {
                try
                {
                    result = _method.Invoke(_instance, args);
                    IList<IList<string>> lists = (IList<IList<string>>)result;

                    foreach(List<string> list in lists)
                    {
                        Tuple tup = new Tuple(list.ToArray());

                        tup.stamp(_rr);

                        res.Add(tup);
                    }
                }
                catch (Exception e)
                {
                    Log.debug("CAUGHT EXCEPTION AT INVOCATION: " + e.Message + "\n" + e.StackTrace, "CustomOperator.process()");
                }
                tries++;
                Log.debug("Failed method invocation at try {0}, retrying...", "CustomOperator.process()", tries);
            }

            return res;
        }

    }

    public class DUP : IOperator
    {
        public IList<IList<string>> CustomOperation(IList<string> l)
        {
            return new List<IList<string>>();
        }

        public DUP()
        {
        }

        public List<Tuple> process(Tuple t)
        {
            Tuple ret = new Tuple(t);
            List<Tuple> res = new List<Tuple>();
            res.Add(ret);

            return res;
        }
    }

    public class FILTER : IOperator
    {
        public IList<IList<string>> CustomOperation(IList<string> l)
        {
            return new List<IList<string>>();
        }

        private int _fieldNumber;
        private string _condition;
        private string _stringValue;
        private bool _isnumber;
        private double _doubleValue;
        private string[] _possibleConditions = { "=", "<", ">" };

        public FILTER(int fieldNumber, string condition, string testValue)
        {

            _fieldNumber = fieldNumber - 1;

            if (_possibleConditions.Contains(condition))
            {
                _condition = condition;

                if (condition == "<" || condition == ">")
                    _isnumber = true;
                else _isnumber = false;
            }
            else throw new WrongParameterException("FILTER condition cannot be " + condition);

            if (_isnumber == true)
            {
                if (double.TryParse(testValue, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _doubleValue) == false)
                    throw new WrongParameterException("With condition: \"" + _condition + "\" value: \"" + testValue + " needs to be a number");

            }
            else _stringValue = testValue;

        }

        public List<Tuple> process(Tuple t)
        {
            if (_fieldNumber > t.getSize() - 1)
            {
                Log.debug("Field Number for UNIQ operator is out of range. fieldNumber = " + _fieldNumber + " | tuple.size = " + t.getSize(), "FILTEROperator");
                return null;
                //Maybe use exception??
            }

            List<Tuple> res = new List<Tuple>();

            Log.debug("TEST: " + t.get(_fieldNumber) + " VS " + _stringValue, "FILTER");
            if (_isnumber)
            {
                double tupleValue;
                if (double.TryParse(t.get(_fieldNumber), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out tupleValue) == false)
                    throw new WrongParameterException("With condition: \"" + _condition + "\" field: \"" + _fieldNumber + " needs to be a number");

                switch (_condition)
                {
                    case "=":
                        if (tupleValue == _doubleValue)
                            res.Add(t);
                            return res;
                        break;
                    case ">":
                        if (_doubleValue > tupleValue)
                            res.Add(t);
                            return res;
                        break;
                    case "<":
                        if (_doubleValue < tupleValue)
                            res.Add(t);
                            return res;
                        break;
                }
            }
            else if (t.get(_fieldNumber) == _stringValue)
                res.Add(t);
                return res;

            return null;
        }

    }

    public class UNIQ : IOperator
    {
        public IList<IList<string>> CustomOperation(IList<string> l)
        {
            return new List<IList<string>>();
        }

        private int _fieldNumber;
        private SortedList _sorted;

        public UNIQ(int fieldNumber)
        {

            _fieldNumber = fieldNumber - 1;
            _sorted = new SortedList();

        }

        public List<Tuple> process(Tuple t)
        {

            if (_fieldNumber > t.getSize() - 1)
            {
                Log.debug("Field Number for UNIQ operator is out of range. fieldNumber = " + _fieldNumber + " | tuple.size = " + t.getSize(), "UNIQOperator");
                return null;
                //Maybe use exception??
            }
            string val = t.get(_fieldNumber);

            if (_sorted.Contains(val) == true)
                return null;

            _sorted.Add(val, val);

            List<Tuple> res = new List<Tuple>();
            res.Add(t);

            return res;
        }
    }
}
