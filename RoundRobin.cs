using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Scheduling
{
    class RoundRobin : FirstComeFirstServedPolicy

    {
        public int quantum { get; }

        public RoundRobin(int iQuantum = 100)
        {
            quantum = iQuantum;
        }
    }
}
