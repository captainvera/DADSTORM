using System;
using DADSTORM;
using Tuple = DADSTORM.Tuple;

namespace DADSTORM{
    public class DUP : Operator<Tuple>{

        public DUP(){
        }

        public Tuple process(Tuple t){
            return Tuple(t);
        }

    }
}
