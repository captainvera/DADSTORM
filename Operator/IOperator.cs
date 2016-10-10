using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DADSTORM;

namespace DADSTORM
{
    interface IOperator<T>
    {
        T process(Tuple t);
    }
}
