using System;
using DADSTORM;
using Tuple = DADSTORM.Tuple;

namespace DADSTORM{
    public class COUNT : Operator<int>{

        private int _countNumber=0;

        public COUNT(){
        }

        public int process(Tuple t){
            return _countNumber++;
        }
           
        public int get(){
            return _countNumber; 
        }
    }
}
