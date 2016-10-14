using System;
using System.Collections;

namespace DADSTORM
{
    public class OperatorDTO
    {
        public string op_id;
        public ArrayList input_ops = new ArrayList();
        public string rep_fact;
        public string routing;
        public ArrayList address = new ArrayList();
        public ArrayList op_spec = new ArrayList();

        public OperatorDTO(string id, ArrayList inputs, string rep, string rout, ArrayList addr, ArrayList spec)
        {
            op_id = id;
            input_ops = inputs;
            rep_fact = rep;
            routing = rout;
            address = addr;
            op_spec = spec;
        }
    }

}
