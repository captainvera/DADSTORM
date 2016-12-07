using System;
using System.Collections;
using System.Collections.Generic;

namespace DADSTORM
{
    [Serializable]
    public class OperatorDTO
    {
        public string op_id;
        public List<string> input_ops = new List<string>();
        public string rep_fact;
        public string routing;
        public List<string> address = new List<string>();
        public List<string> ports = new List<string>();
        public List<string> op_spec = new List<string>();

        public List<ReplicaRepresentation> next_op = new List<ReplicaRepresentation>();
        public List<ReplicaRepresentation> before_op = new List<ReplicaRepresentation>();
        public List<ReplicaRepresentation> current_op = new List<ReplicaRepresentation>();

        public string pmAdress;
        public int curr_rep;
        public string logging;
        public string semantics;

        public OperatorDTO(string id, List<string> inputs, string rep, string rout, List<string> addr, List<string> spec, List<string> port)
        {
            op_id = id;
            input_ops = inputs;
            rep_fact = rep;
            routing = rout;
            address = addr;
            op_spec = spec;
            ports = port;
        }
        private OperatorDTO() {
        }
    }

    [Serializable]
    public class ReplicaRepresentation
    {
        public string op;
        public int rep;
        public string addr;

        public ReplicaRepresentation()
        {
            this.op = "";
            this.addr = "";
            this.rep = -1;
        }

        public ReplicaRepresentation(string op, int rep, string addr)
        {
            this.op = op;
            this.addr = addr;
            this.rep = rep;
        }

        public ReplicaRepresentation(ReplicaRepresentation rr)
        {
            this.op = rr.op;
            this.addr = rr.addr;
            this.rep = rr.rep;
        }
    }
}
