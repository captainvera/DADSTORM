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

        static public IOperator create(string opname, string[] args)
        {
            int field = 0;
            switch (opname)
            {
                //Argument number checking?
                //This is a OperatorFactory / OperatorSource, not an Operator
                //IOperator return OperatorFactory.create(OPNAME, ARGS);
                case "DUP":
                    Logger.writeLine("DUP operator started.", "OperatorSelector");
                    return new DUP();

                case "UNIQ":
                    Logger.writeLine("UNIQ operator started.", "OperatorSelector");
                    if (Int32.TryParse(args[0], out field) == true)
                        return new UNIQ(field);
                    else
                    {
                        Logger.writeLine("ERROR: UNIQ operator could not be instanced, wrong arguments.", "OperatorSelector");
                        goto case "SAFE";
                    }

                case "CUSTOM":
                    Logger.writeLine("CUSTOM operator started.", "OperatorSelector");
                    if (args.Length == 3)
                        return new CUSTOM(args[0], args[1], args[2]);
                    else
                    {
                        Logger.writeLine("ERROR: CUSTOM operator could not be instanced, wrong arguments.", "OperatorSelector");
                        goto case "SAFE";
                    }

                case "FILTER":
                    Logger.writeLine("FILTER operator started.", "OperatorSelector");
                    if (args.Length == 3 && Int32.TryParse(args[0], out field) == true)
                        return new FILTER(field, args[1], args[2]);
                    else
                    {
                        Logger.writeLine("ERROR: FILTER operator could not be instanced, wrong arguments.", "OperatorSelector");
                        goto case "SAFE";
                    }

                case "COUNT":
                    Logger.writeLine("COUNT operator started.", "OperatorSelector");
                    return new COUNT();

                case "SAFE":
                    Logger.writeLine("ERROR: Instancing DUP as safe default.", "OperatorSelector");
                    return new DUP();

                default:
                    Logger.writeLine("ERROR: Instancing DUP as safe default.", "OperatorSelector");
                    return new DUP();
            }

        }

    }

    public interface IOperator
    {
        Tuple process(Tuple t);
        IList<IList<string>> CustomOperation(IList<string> l);
    }

        public class COUNT : IOperator
    {

        private int _countNumber = 0;

        public Tuple process(Tuple t)
        {
            Tuple res = new Tuple(1);
            res.set(0, _countNumber.ToString());
            _countNumber++;
            return res;
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

        public IList<IList<string>> CustomOperation(IList<string> l)
        {
            return new List<IList<string>>();

        }
        public CUSTOM(string dll, string classLoad, string method)
        {
            Logger.writeLine("Trying to load dll {0} , class {1} and method {2}", "CustomOPERATOR", dll, classLoad, method);
                
            byte[] bytes = System.IO.File.ReadAllBytes(@".\" + dll);
            Assembly assembly = Assembly.Load(bytes);

            IEnumerable<Type> types = null;
             
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.writeLine("Failed loading type... Trying alternative", "CustomOPERATOR");
                types = e.Types.Where(t => t != null);
            }

            foreach (Type type in types)
            {
                Logger.writeLine("FOUND TYPE " + type.FullName, "CustomOPERATOR");
                if (type.IsClass == true)
                {
                    Logger.writeLine("It's a class!", "CustomOPERATOR");
                    if (type.FullName.EndsWith("." + classLoad))
                    {
                        Logger.writeLine("It's our class ;)!", "CustomOPERATOR");
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

        public Tuple process(Tuple t)
        {

            List<string> listargs = t.toArray().ToList<string>();

            object[] args = new object[] { listargs };
            object result = null;

            int tries = 0;

            while (tries < 8)
            {
                try 
                {
                    result = _method.Invoke(_instance, args);
                    IList<IList<string>> lists = (IList<IList<string>>)result;

                    Tuple ret = new Tuple(lists[0].ToArray<string>());

                    Logger.debug("Success with {0} tries", "CustomOperator.process()", tries +1);
                    return ret;
                }
                catch (Exception e)
                {
                    Logger.debug("CAUGHT EXCEPTION AT INVOCATION: " + e.Message, "CustomOperator.process()");
                }
                tries++;
                Logger.debug("Failed method invocation at try {0}, retrying...", "CustomOperator.process()", tries);
            }

            return null;
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

        public Tuple process(Tuple t)
        {
            return new Tuple(t);
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

            _fieldNumber = fieldNumber;

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

        public Tuple process(Tuple t)
        {
            if (_fieldNumber > t.getSize() - 1)
            {
                Logger.debug("Field Number for UNIQ operator is out of range. fieldNumber = " + _fieldNumber + " | tuple.size = " + t.getSize());
                return null;
                //Maybe use exception??
            }

            if (_isnumber)
            {
                double tupleValue;
                if (double.TryParse(t.get(_fieldNumber), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out tupleValue) == false)
                    throw new WrongParameterException("With condition: \"" + _condition + "\" field: \"" + _fieldNumber + " needs to be a number");

                switch (_condition)
                {
                    case "=":
                        if (tupleValue == _doubleValue)
                            return t;
                        break;
                    case ">":
                        if (_doubleValue > tupleValue)
                            return t;
                        break;
                    case "<":
                        if (_doubleValue < tupleValue)
                            return t;
                        break;
                }
            }
            else if (t.get(_fieldNumber) == _stringValue)
                return t;

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

            _fieldNumber = fieldNumber;
            _sorted = new SortedList();

        }

        public Tuple process(Tuple t)
        {

            if (_fieldNumber > t.getSize() - 1)
            {
                Logger.debug("Field Number for UNIQ operator is out of range. fieldNumber = " + _fieldNumber + " | tuple.size = " + t.getSize());
                return null;
                //Maybe use exception??
            }
            string val = t.get(_fieldNumber);

            if (_sorted.Contains(val) == true)
                return null;

            _sorted.Add(val, val);

            return t;

        }
    }
}
