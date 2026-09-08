using System;

namespace KfcQueueSim.Models
{
    /// <summary>
    /// Records what the SimulationEngine computed for ONE customer during a run.
    /// </summary>
    public class CustomerSimStat
    {
        public int CustomerId { get; set; }
        public double ArrivalTime { get; set; }
        public double ServiceStartTime { get; set; }   // -1 if balked
        public double ServiceEndTime { get; set; }     // -1 if balked
        public bool WasBalked { get; set; }

        public CustomerSimStat()
        {
            ServiceStartTime = -1;
            ServiceEndTime = -1;
        }

        // ── Derived quantities ────────────────────────────────────────────────
        public double WaitTime
        {
            get { return WasBalked ? 0.0 : Math.Max(0.0, ServiceStartTime - ArrivalTime); }
        }

        public double TimeInSystem
        {
            get { return WasBalked ? 0.0 : (ServiceEndTime - ArrivalTime); }
        }

        public double ServiceTime
        {
            get { return WasBalked ? 0.0 : (ServiceEndTime - ServiceStartTime); }
        }
    }
}
