using System;
using DADSTORM;
using Tuple = DADSTORM.Tuple;

namespace DADSTORM{
    public class DUP : IOperator<Tuple>{

        public DUP(){
        }

        public Tuple process(Tuple t){
            return new Tuple(t);
        }

    }
}
