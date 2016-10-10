using System;

namespace DADSTORM
{
    public class FILTER : IOperator<Tuple>
    {

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
            bool isNumber = false;
            int field = 0;
            int value = 0;

            try
            {
                field = Int32.Parse(t.get(_fieldNumber));
                value = Int32.Parse(_testValue);
                isNumber = true;
            }
            catch (FormatException e)
            {
                if (_condition != "=")
                    throw e;
            }

            if (isNumber == true)
            {
                if (_condition == "<" && field < value)
                    return t;
                if (_condition == "<" && field < value)
                    return t;
                if (_condition == "<" && field < value)
                    return t;
            }

            return null;
            //TODO check if null is correct thing to send
        }

    }
}
