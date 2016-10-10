using System;
using System.Collections;
using DADSTORM;
using Tuple = DADSTORM.Tuple;

public class UNIQ : IOperator<Tuple>{

  private int _fieldNumber;
  private SortedList _sorted; 

  public UNIQ(int fieldNumber){

    _fieldNumber = fieldNumber; 
    _sorted = new SortedList();

  }

  public Tuple process(Tuple t){

      string val = t.get(_fieldNumber);

      if (_sorted.Contains(val) == true)
          return null;

      _sorted.Add(val, val);

      return t;
  
  }

}
