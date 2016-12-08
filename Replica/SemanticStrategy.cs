using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Tuple = DADSTORM.Tuple;

namespace DADSTORM
{
    interface ISemanticStrategy
    {
        bool accept(Tuple t);
        void delivered(Tuple t, String init_op, int init_rep);
        void firstDelivered(Tuple t);
        void addRecord(TupleRecord tr);
        void purgeRecord(TupleRecord tr);
        void tupleConfirmed(string uid);
        void printTables();
    }

    class SemanticStrategy : ISemanticStrategy
    {
        public bool accept(Tuple t)
        {
            return true;
        }

        public void delivered(Tuple t, String init_op, int init_rep)
        {
        }

        public void firstDelivered(Tuple t)
        {
        }

        public void addRecord(TupleRecord tr)
        {

        }

        public void purgeRecord(TupleRecord tr)
        {

        }

        public void tupleConfirmed(string uid)
        {

        }

        public void printTables()
        {
        }
    }

    class AtMostOnce : ISemanticStrategy
    {
        public bool accept(Tuple t)
        {
            return true;
        }

        public void delivered(Tuple t, String init_op, int init_rep)
        {
        }

        public void firstDelivered(Tuple t)
        {
        }

        public void addRecord(TupleRecord tr)
        {

        }

        public void purgeRecord(TupleRecord tr)
        {

        }

        public void tupleConfirmed(string uid)
        {

        }

        public void printTables()
        {
        }
    }

    class AtLeastOnce : ISemanticStrategy
    {
        public bool accept(Tuple t)
        {
            return true;
        }

        public void delivered(Tuple t, String init_op, int init_rep)
        {
        }

        public void firstDelivered(Tuple t)
        {
        }

        public void addRecord(TupleRecord tr)
        {

        }

        public void purgeRecord(TupleRecord tr)
        {

        }

        public void tupleConfirmed(string uid)
        {

        }

        public void printTables()
        {
        }
    }

    class ExactlyOnce : ISemanticStrategy
    {
        //Shared state of processed tuples between replicas
        SharedTupleTable shared_table;

        //Curent tuples in this replica
        DeliveryTable delivery_table;

        Replica rep;

        public ExactlyOnce(Replica r)
        {
            shared_table = new SharedTupleTable();
            delivery_table = new DeliveryTable();
            rep = r;
        }

        //Current node accepts a new tuple
        // -Add new TupleRecords on all shared tables
        // -Add tuple to the delivery table
        public bool accept(Tuple t)
        {
            if (!delivery_table.contains(t))
            {
                delivery_table.add(t);

                TupleRecord tr = new TupleRecord(t.getId(), TupleState.pending);
                shared_table.add(tr);

                //Adds a tuple record on every replica's table
                syncSharedTables(tr);

                return true;
            }
            return false;
        }

        //Current node's tuple is delivered
        // -Update every shared table (our's and other node's)
        // -Confirm node delivery to origin of Tuple (op n-1)
        // -Purge our local version
        public void delivered(Tuple t, String init_op, int init_rep )
        {
            //Warn node of origin
            if(t == null)
            {
                Console.WriteLine("!!!!!!ERROR!!!!! Delivered tuple doesn't exist?");
                return;
            }

            Console.WriteLine("Delivering confirmation to " + init_op + "->" + init_rep);

            //Replica r = rep.getCommunicator().getPreviousReplica(init_op, init_rep);
            Replica r = rep.getCommunicator().getPreviousReplica(init_rep);

            //Node that sent doesn't exist?!
            if(r == null)
            {
                Console.WriteLine("NODE THAT SENT DOESN'T EXIST!!");
            }

            r.tupleConfirmed(t.getId().getUID());

            //Then purge all shared tables
            purgeSharedTables(t.getId().getUID());

            //Finally purge our own shared table
            shared_table.purge(t.getId().getUID());
        }

        public void firstDelivered(Tuple t)
        {
            //Warn node of origin
            if(t == null)
            {
                Console.WriteLine("!!!!!!ERROR!!!!! Delivered tuple doesn't exist?");
                return;
            }

            //Then purge all shared tables
            purgeSharedTables(t.getId().getUID());

            //Finally purge our own shared table
            shared_table.purge(t.getId().getUID());
        }

        //Add a new tuple record to the shared table
        public void addRecord(TupleRecord tr)
        {
            shared_table.add(tr);
        }

        //Purge a TupleRecord by anothe replica of the same operator
        public void purgeRecord(TupleRecord tr)
        {
            shared_table.remove(tr); 
        }

        //A certain tuple on hold is received on the n+2 op
        //We can finnaly forget it
        public void tupleConfirmed(string uid)
        {
            delivery_table.remove(uid);
        }

        //add a node in a synchronized way
        public void syncSharedTables(TupleRecord tr)
        {
            for (int i = 0; i < rep.getCommunicator().getOwnReplicaCount(); i++) {
                if (i != rep.getReplicaNumber())
                {
                    Console.WriteLine("Sending tuple record " + tr.getUID() + " to " + i);
                    rep.getCommunicator().getOwnReplica(i).addRecord(tr);
                }
            }
        }

        public void purgeSharedTables(string uid)
        {
            TupleRecord tr = shared_table.get(uid);
            if(tr == null)
            {
                Console.WriteLine("!!!!!!ERROR!!!!! Purged tuple record doesn't exist on node");
                return;
            }
            for (int i = 0; i < rep.getReplicationFactor(); i++) {
                if (i != rep.getReplicaNumber())
                {
                    rep.getCommunicator().getOwnReplica(i).purgeRecord(tr);
                }
            }
        }

        public void printTables()
        {
            delivery_table.printTable();
            shared_table.printTable();
        }
    }
}
