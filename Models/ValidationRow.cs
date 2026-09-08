using System;

namespace KfcQueueSim.Models
{
    /// <summary>
    /// Used in Part A (trace-driven validation) to compare recorded vs.
    /// simulated wait times side-by-side.
    /// </summary>
    public class ValidationRow
    {
        public int CustomerId { get; set; }
        public double ArrivalMin { get; set; }
        public double ServiceTimeMin { get; set; }
        public double RecordedWaitMin { get; set; }
        public double SimulatedWaitMin { get; set; }

        public double DeltaMin
        {
            get { return SimulatedWaitMin - RecordedWaitMin; }
        }
    }
}
