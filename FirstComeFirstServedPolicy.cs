using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class FirstComeFirstServedPolicy : SchedulingPolicy
    {
        protected List<int> readyQueue = new List<int>();
        protected List<int> blockedQueue = new List<int>();

        public override int NextProcess(Dictionary<int, ProcessTableEntry> dProcessTable)
        {
            foreach (int p_id in blockedQueue.ToArray())
            {
                if (!dProcessTable[p_id].Blocked) {
                    blockedQueue.Remove(p_id);
                    readyQueue.Add(p_id);
                }
            }
            foreach (int p_id in readyQueue.ToArray())
            {
                readyQueue.Remove(p_id);
                if (dProcessTable[p_id].Blocked) {
                    blockedQueue.Add(p_id);
                } 
                else if (!dProcessTable[p_id].Done)
                {
                    readyQueue.Add(p_id);
                    return p_id;
                }
            }
            return -1;
        }

        public override void AddProcess(int iProcessId)
        {
                readyQueue.Add(iProcessId);
        }

        public override bool RescheduleAfterInterrupt()
        {
            return false;
        }
    }
}
