using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class PrioritizedScheduling : RoundRobin
    {
        public PrioritizedScheduling(int iQuantum = 100) : base(iQuantum)
        {
        }

        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            int maxPrioriti = -1;
            int id_maxPrioriti = -1;
            foreach (int p_id in blockedQueue.ToArray())
            {
                if (!dProcessTable[p_id].Blocked)
                {
                    blockedQueue.Remove(p_id);
                    readyQueue.Add(p_id);
                }
            }
            foreach (int p_id in readyQueue.ToArray())
            {
                ProcessTableEntry process = dProcessTable[p_id];
                if (process.Done)
                {
                    readyQueue.Remove(p_id);
                    continue;
                }
                if (process.Blocked)
                {
                    readyQueue.Remove(p_id);
                    blockedQueue.Add(p_id);
                    continue;
                }
                if (process.Priority > maxPrioriti)
                {
                    maxPrioriti = process.Priority;
                    id_maxPrioriti = p_id;
                }
            }
            if (id_maxPrioriti != -1)
            {
                readyQueue.Remove(id_maxPrioriti);
                readyQueue.Add(id_maxPrioriti);
            }
            return id_maxPrioriti;
        }

        public override bool RescheduleAfterInterrupt()
        {
            return true;
        }
    }
}
