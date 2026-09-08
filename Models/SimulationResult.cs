using System;

namespace KfcQueueSim.Models
{
    /// <summary>
    /// Aggregate statistics returned by SimulationEngine.Run() after one complete run.
    /// Includes all standard queueing theory metrics: L, Lq, W, Wq, rho.
    /// </summary>
    public class SimulationResult
    {
        public string Label { get; set; }

        // Customer counts
        public int TotalArrivals { get; set; }
        public int CustomersServed { get; set; }
        public int CustomersBalked { get; set; }

        // Core Queueing Theory Metrics
        /// <summary>Wq: Mean waiting time of customers in the queue (minutes).</summary>
        public double Wq { get; set; }

        /// <summary>W: Mean waiting / response time of customers in the system (minutes) = Wq + 1/mu.</summary>
        public double W { get; set; }

        /// <summary>Mean service time (1/mu) in minutes.</summary>
        public double MeanServiceTime { get; set; }

        /// <summary>Mean inter-arrival time (1/lambda) in minutes.</summary>
        public double MeanInterArrivalTime { get; set; }

        /// <summary>Arrival rate lambda (customers/minute).</summary>
        public double Lambda { get; set; }

        /// <summary>Service rate mu (customers/minute).</summary>
        public double Mu { get; set; }

        /// <summary>rho: Server utilization (time-averaged fraction of busy server).</summary>
        public double ServerUtilization { get; set; }

        /// <summary>Lq: Mean (time-average) number of customers in the queue.</summary>
        public double Lq { get; set; }

        /// <summary>L: Mean (time-average) number of customers in the system (queue + in service).</summary>
        public double L { get; set; }

        /// <summary>Peak queue length observed during the run.</summary>
        public int MaxQueueLength { get; set; }

        /// <summary>Total simulated time span (minutes).</summary>
        public double TotalSimulationTime { get; set; }

        public SimulationResult()
        {
            Label = "";
        }
    }
}
