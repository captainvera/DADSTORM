using System;
using DADSTORM;
using Tuple = DADSTORM.Tuple;
using System.Reflection;

namespace DADSTORM{
    public class CUSTOM : IOperator<Tuple>{

        private MethodInfo _method;
        private Type _type;
        private object _instance;

        public CUSTOM(string dll, string classLoad, string method){

            Assembly assembly = Assembly.LoadFrom(dll);

            _type =  assembly.GetType(dll + "." + classLoad);

            if (_type == null)
                throw new WrongParameterException("CUSTOM operator, cannot find:" + classLoad + " in dll:" + dll);

            _method = _type.GetMethod(method, new Type[] { typeof(string[]) });

            if (_method == null)
                throw new WrongParameterException("CUSTOM operator, type:" + _type.ToString() + " has no method:" + method + " receiving a list of Strings");

            _instance = Activator.CreateInstance(_type);

        }

        public Tuple process(Tuple t){

            object result = _method.Invoke(_instance, t.toArray());
            
            Type returnType = _method.ReturnType;

            return (Tuple) result;
        }
           
    }
}
