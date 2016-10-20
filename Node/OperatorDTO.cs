﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace DADSTORM
{
    public class OperatorDTO
    {
        public string op_id;
        public List<string> input_ops = new List<string>();
        public string rep_fact;
        public string routing;
        public List<string> address = new List<string>();
        public List<string> op_spec = new List<string>();

        public OperatorDTO(string id, List<string> inputs, string rep, string rout, List<string> addr, List<string> spec)
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
