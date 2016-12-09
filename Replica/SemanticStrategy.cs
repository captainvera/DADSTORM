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
        void fix(int n);
        Tuple get(string uid);
    }

    class SemanticStrategyFactory
    {
        public static ISemanticStrategy create(string strat, Replica r)
        {
            if (strat.StartsWith("at-most-once"))
            {
                Log.debug("Detected at-most-once semantic strategy... Argument: " + strat, "RoutingStrategyFactory");
                return new AtMostOnce();
            }
            else if (strat.StartsWith("at-least-once"))
            {
                Log.debug("Detected at-least-once semantic strategy... Argument: " + strat, "RoutingStrategyFactory");
                return new AtLeastOnce(r);
            }
            else if (strat.StartsWith("exactly-once"))
            {
                Log.debug("Detected exactly-once semantic strategy... Argument: " + strat, "RoutingStrategyFactory");
                return new ExactlyOnce(r);
            }
            else
            {
                //Default?
                return new AtMostOnce();
            }
        }
    }

    class AtMostOnce : ISemanticStrategy
    {
        public bool accept(Tuple t)
        {
            //Accept all tuples
            return true;
        }

        public void delivered(Tuple t, String init_op, int init_rep)
        {
            //No action necessary
        }

        public void firstDelivered(Tuple t)
        {
            //No action necessary
        }

        public void addRecord(TupleRecord tr)
        {
            //No action necessary
        }

        public void purgeRecord(TupleRecord tr)
        {
            //No action necessary
        }

        public void tupleConfirmed(string uid)
        {
            //No action necessary
        }

        public void printTables()
        {
            //No tables available
        }

        public void fix(int n)
        {
            //No action on takeover necessary
        }

        public Tuple get(string uid)
        {
            //No tuples stored, can't return anything
            return null;
        }
    }


    class AtLeastOnce : ISemanticStrategy
    {
        //Shared state of processed tuples between replicas
        protected SharedTupleTable shared_table;

        //Curent tuples in this replica
        protected DeliveryTable delivery_table;

        protected Replica rep;

        public AtLeastOnce(Replica r)
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

                TupleRecord tr = new TupleRecord(t.getId(), TupleState.pending, rep.getReplicaNumber());
                shared_table.add(tr);

                //Adds a tuple record on every replica's table
                syncSharedTables(tr);

            }

            return true;
        }

        //Current node's tuple is delivered
        // -Update every shared table (our's and other node's)
        // -Confirm node delivery to origin of Tuple (op n-1)
        // -Purge our local version
        public void delivered(Tuple t, String init_op, int init_rep)
        {
            //Warn node of origin
            if (t == null)
            {
                Log.info("!!!!!!ERROR!!!!! Delivered tuple doesn't exist?", "SemanticStrategy");
                return;
            }

            Log.debug("Delivering confirmation to " + init_op + "->" + init_rep, "SemanticStrategy");

            rep.getCommunicator().tupleConfirmed(OperatorPosition.Previous, init_rep, t.getId().getUID());

            //Then purge all shared tables
            purgeSharedTables(t.getId().getUID());

            //Finally purge our own shared table
            shared_table.purge(t.getId().getUID());
        }

        public void firstDelivered(Tuple t)
        {
            //Warn node of origin
            if (t == null)
            {
                Log.info("!!!!!!ERROR!!!!! Delivered tuple doesn't exist?", "SemanticStrategy");
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
            for (int i = 0; i < rep.getCommunicator().getOwnReplicaCount(); i++)
            {
                if (!rep.getIndexesOwned().Contains(i))
                {
                    Log.debug("Sending tuple record " + tr.getUID() + " to " + i, "SemanticStrategy");
                    rep.getCommunicator().addRecord(OperatorPosition.Own, i, tr);
                }
            }
        }

        public void purgeSharedTables(string uid)
        {
            TupleRecord tr = shared_table.get(uid);
            if (tr == null)
            {
                Log.debug("!!!!!!ERROR!!!!! Trying to purge record that doesn't exist on node", "SemanticStrategy");
                return;
            }

            for (int i = 0; i < rep.getReplicationFactor(); i++)
            {
                if (!rep.getIndexesOwned().Contains(i))
                {
                    rep.getCommunicator().purgeRecord(OperatorPosition.Own, i, tr);
                }
            }
        }

        public void printTables()
        {
            delivery_table.printTable();
            shared_table.printTable();
        }

        public void fix(int n)
        {
            List<TupleRecord> records = shared_table.toList();

            Log.writeLine("Fixing all unprocessed tuples from dead replica", "SemanticStrategy");

            foreach (TupleRecord tr in records)
            {
                if (tr.rep == n)
                {
                    resend(tr);
                }
            }

            Log.writeLine("Job done", "SemanticStrategy");
        }
        private void resend(TupleRecord tr)
        {
            Tuple t = rep.getCommunicator().fetchTuple(OperatorPosition.Previous, tr.id.rep, tr);

            if (t == null)
            {
                Log.debug("Trying to recover the unprocessed Tuple failed! Possible data loss...", "SemanticStrategy");
                return;
            }

            Log.debug("Fetched Tuple from previous server successfuly!", "SemanticStrategy");
            rep.injectInput(t);
            Log.debug("Injected Tuple for reprocessing", "SemanticStrategy");
        }

        public Tuple get(string uid)
        {
            return delivery_table.get(uid);
        }
    }

    class ExactlyOnce : AtLeastOnce
    {
        public ExactlyOnce(Replica r) : base(r)
        {
        }

        public new bool accept(Tuple t)
        {
            if (!delivery_table.contains(t))
            {
                delivery_table.add(t);

                TupleRecord tr = new TupleRecord(t.getId(), TupleState.pending, rep.getReplicaNumber());
                shared_table.add(tr);

                //Adds a tuple record on every replica's table
                syncSharedTables(tr);

                return true;
            }
            return false;
        }
    }
}
