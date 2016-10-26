using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.threading.tasks;
using Tuple = DADSTORM.Tuple;

namespace Replica
{
    interface ISemanticStrategy
    {
        void accept(Tuple t);
    }

    class SemanticStrategy : ISemanticStrategy
    {
        public void accept(Tuple t)
        {
        }
    }
}
