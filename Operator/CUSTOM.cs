using System;
using DADSTORM;
using Tuple = DADSTORM.Tuple;

namespace DADSTORM{
    public class CUSTOM : Operator<Tuple>{

        private MethodInfo _method;
        private Type _type;

        public CUSTOM(string dll, string classLoad, string method){

            Assembly assembly = Assembly.LoadFrom(dll);

            _type =  assembly.getType(dll + "." + classLoad);

            if (type == null)
                throw new WrongParameterException("CUSTOM operator, cannot find:" + classLoad + " in dll:" + dll);

            _method = type.getMethod(method, new Type[] { typeof(string[]) })

            if(_method == null)
                throw new WrongParameterException("CUSTOM operator, type:" + type.ToString() + " has no method:" + method " receiving a list of Strings");

        }

        public Tuple process(Tuple t){

            _instance = Activator.CreateInstance(_type);
            object result = Activator.CreateInstance(_type, t.getArray());

        }
           
    }
}
