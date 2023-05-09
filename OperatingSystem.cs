namespace Scheduling
{
    class OperatingSystem
    {
        public Disk Disk { get; private set; }
        public CPU CPU { get; private set; }
        private Dictionary<int, ProcessTableEntry> m_dProcessTable;
        private List<ReadTokenRequest> m_lReadRequests;
        private int m_cProcesses;
        private SchedulingPolicy m_spPolicy;
        private static int IDLE_PROCESS_ID = 0;

        public OperatingSystem(CPU cpu, Disk disk, SchedulingPolicy sp)
        {
            CPU = cpu;
            Disk = disk;
            m_dProcessTable = new Dictionary<int, ProcessTableEntry>();
            m_lReadRequests = new List<ReadTokenRequest>();
            cpu.OperatingSystem = this;
            disk.OperatingSystem = this;
            m_spPolicy = sp;

            //create an "idle" process here
            IdleCode idle = new IdleCode();
            m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, "idle", idle);
            m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
            m_spPolicy.AddProcess(m_cProcesses);
            m_cProcesses++;

        }
        
        public void CreateProcess(string sCodeFileName)
        {
            Code code = new Code(sCodeFileName);
            m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, sCodeFileName, code);
            m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
            m_spPolicy.AddProcess(m_cProcesses);
            m_cProcesses++;
        }

        public void CreateProcess(string sCodeFileName, int iPriority)
        {
            Code code = new Code(sCodeFileName);
            m_dProcessTable[m_cProcesses] = new ProcessTableEntry(m_cProcesses, sCodeFileName, code);
            m_dProcessTable[m_cProcesses].Priority = iPriority;
            m_dProcessTable[m_cProcesses].StartTime = CPU.TickCount;
            m_spPolicy.AddProcess(m_cProcesses);
            m_cProcesses++;
        }

        public void ProcessTerminated(Exception e)
        {
            if (e != null)
                Console.WriteLine("Process " + CPU.ActiveProcess + " terminated unexpectedly. " + e);
            m_dProcessTable[CPU.ActiveProcess].Done = true;
            m_dProcessTable[CPU.ActiveProcess].Console.Close();
            m_dProcessTable[CPU.ActiveProcess].EndTime = CPU.TickCount;
            ActivateScheduler();
        }

        public void TimeoutReached()
        {
            ActivateScheduler();
        }

        public void ReadToken(string sFileName, int iTokenNumber, int iProcessId, string sParameterName)
        {
            ReadTokenRequest request = new ReadTokenRequest();
            request.ProcessId = iProcessId;
            request.TokenNumber = iTokenNumber;
            request.TargetVariable = sParameterName;
            request.Token = null;
            request.FileName = sFileName;
            m_dProcessTable[iProcessId].Blocked = true;
            if (Disk.ActiveRequest == null)
                Disk.ActiveRequest = request;
            else
                m_lReadRequests.Add(request);
            CPU.ProgramCounter = CPU.ProgramCounter + 1;
            ActivateScheduler();
        }

        public void Interrupt(ReadTokenRequest rFinishedRequest)
        {
            //implement an "end read request" interrupt handler.
            //translate the returned token into a value (double). 
            //when the token is null, EOF has been reached.
            //write the value to the appropriate address space of the calling process.
            //activate the next request in queue on the disk.

            double doubleToken = double.NaN;
            if (rFinishedRequest.Token != null)
                doubleToken = Convert.ToDouble(rFinishedRequest.Token);
            ProcessTableEntry Process = m_dProcessTable[rFinishedRequest.ProcessId];
            Process.AddressSpace[rFinishedRequest.TargetVariable] = doubleToken;
            Process.Blocked = false;
            Process.LastCPUTime = CPU.TickCount;
            m_dProcessTable[rFinishedRequest.ProcessId] = Process;

            if (m_lReadRequests.Count != 0)
            {
                Disk.ActiveRequest = m_lReadRequests[0];
                m_lReadRequests.RemoveAt(0);
            }
 
            if (m_spPolicy.RescheduleAfterInterrupt())
                ActivateScheduler();
        }

        private ProcessTableEntry ContextSwitch(int iEnteringProcessId)
        {
            //your code here
            //implement a context switch, switching between the currently active process on the CPU to the process with pid iEnteringProcessId
            //You need to switch the following: ActiveProcess, ActiveAddressSpace, ActiveConsole, ProgramCounter.
            //All values are stored in the process table (m_dProcessTable)
            //Our CPU does not have registers, so we do not store or switch register values.
            //returns the process table information of the outgoing process
            //After this method terminates, the execution continues with the new process

            ProcessTableEntry enterProcess = m_dProcessTable[iEnteringProcessId];
            ProcessTableEntry exitProcess = ReleaseProcess();

            if (enterProcess.MaxStarvation < CPU.TickCount - enterProcess.LastCPUTime)
                m_dProcessTable[iEnteringProcessId].MaxStarvation = CPU.TickCount - enterProcess.LastCPUTime;

            CPU.ActiveProcess = iEnteringProcessId;
            CPU.ActiveAddressSpace = enterProcess.AddressSpace;
            CPU.ProgramCounter = enterProcess.ProgramCounter;
            CPU.ActiveConsole = enterProcess.Console;

            if (m_spPolicy is RoundRobin) 
                CPU.RemainingTime = ((RoundRobin)m_spPolicy).quantum;

            return exitProcess;
        }


        public ProcessTableEntry ReleaseProcess()
        {
            ProcessTableEntry exitProcess = null;
            if (CPU.ActiveProcess != -1)
            {
                exitProcess = m_dProcessTable[CPU.ActiveProcess];
                exitProcess.AddressSpace = CPU.ActiveAddressSpace;
                exitProcess.ProgramCounter = CPU.ProgramCounter;
                exitProcess.Console = CPU.ActiveConsole;
                exitProcess.LastCPUTime = CPU.TickCount;
                m_dProcessTable[CPU.ActiveProcess] = exitProcess;
            }
            return exitProcess;
        }

        public void ActivateScheduler()
        {
            int iNextProcessId = m_spPolicy.NextProcess(m_dProcessTable);
            if (iNextProcessId == -1)
            {
                Console.WriteLine("All processes terminated or blocked.");
                CPU.Done = true;
            }
            else
            {
                bool bOnlyIdleRemains = true;
                //add code here to check if only the Idle process remains
                foreach (ProcessTableEntry process in m_dProcessTable.Values)
                {
                    if (!process.Done && process.Name != "idle")
                        bOnlyIdleRemains = false;
                }

                if (bOnlyIdleRemains)
                {
                    Console.WriteLine("Only idle remains.");
                    CPU.Done = true;
                }
                else
                    ContextSwitch(iNextProcessId);
            }
        }

        public double AverageTurnaround()
        {
            //Compute the average time from the moment that a process enters the system until it terminates.
            double sumTurnaruond = 0;
            foreach (ProcessTableEntry p in m_dProcessTable.Values) 
            {
                if(p.Name != "idle") 
                    sumTurnaruond += p.EndTime - p.StartTime;
            }
            return sumTurnaruond/m_dProcessTable.Count-1;    
        }

        public int MaximalStarvation()
        {
            //Compute the maximal time that some project has waited in a ready stage without receiving CPU time.
            int maxStarvation = 0;
            foreach (ProcessTableEntry p in m_dProcessTable.Values)
            {
                if (p.Name != "idle" && p.MaxStarvation > maxStarvation) 
                    maxStarvation = p.MaxStarvation;
            }
            return maxStarvation;
        }
    }
}
