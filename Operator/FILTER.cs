using System;
using DADSTORM;
using Tuple = DADSTORM.Tuple;
using WrongParameterException;

namespace DADSTORM{
    public class FILTER : Operator<Tuple>{
        }

        private int _fieldNumber;
        private string _condition;
        private string _testValue;

        public FILTER(int fieldNumber, string condition, string testValue){
        
            _fieldNumber = fieldNumber;

            if(condition == "=" || condition == ">" || condition == "<")
                _condition = condition;
            else throw new WrongParameterException("FILTER condition cannot be " + condition);

            _testValue = testValue;
            
        }

        public Tuple process(Tuple t){
            if(_condition == "=" && t.get(_fieldNumber) == _testValue)
                return t;

            if(_condition == ">" && t.get(_fieldNumber) > _testValue)
                return t;

            if(_condition == "<" && t.get(_fieldNumber) < _testValue)
                return t;

            return null;
            //TODO check if null is correct thing to send
        }

    }
}
