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
    public interface IOperator
    {
        Tuple process(Tuple t);
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
    }

    public class CUSTOM : IOperator
    {

        private MethodInfo _method;
        private Type _type;
        private object _instance;

        public CUSTOM(string dll, string classLoad, string method)
        {

            Assembly assembly = Assembly.LoadFrom(dll);

            _type = assembly.GetType(dll + "." + classLoad);

            if (_type == null)
                throw new WrongParameterException("CUSTOM operator, cannot find:" + classLoad + " in dll:" + dll);

            _method = _type.GetMethod(method, new Type[] { typeof(string[]) });

            if (_method == null)
                throw new WrongParameterException("CUSTOM operator, type:" + _type.ToString() + " has no method:" + method + " receiving a list of Strings");

            _instance = Activator.CreateInstance(_type);

        }

        public Tuple process(Tuple t)
        {

            object result = _method.Invoke(_instance, t.toArray());

            Type returnType = _method.ReturnType;

            return (Tuple)result;
        }

    }

    public class DUP : IOperator
    {

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

        }

        public Tuple process(Tuple t)
        {

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

        private int _fieldNumber;
        private SortedList _sorted;

        public UNIQ(int fieldNumber)
        {

            _fieldNumber = fieldNumber;
            _sorted = new SortedList();

        }

        public Tuple process(Tuple t)
        {

            string val = t.get(_fieldNumber);

            if (_sorted.Contains(val) == true)
                return null;

            _sorted.Add(val, val);

            return t;

        }
    }
}
