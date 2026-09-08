using System;

namespace KfcQueueSim.Models
{
    /// <summary>
    /// Holds everything the DataLoader extracts from one CSV row.
    /// Times are stored both as raw strings (for clear display) and as
    /// "minutes elapsed from session start" (for arithmetic in the DES engine).
    /// </summary>
    public class CustomerRecord
    {
        // ── Raw CSV strings ───────────────────────────────────────────────────
        public int CustomerId { get; set; }
        public string SessionLabel { get; set; }
        public int SessionNumber { get; set; }          // 1 or 2
        public string RawArrivalTime { get; set; }
        public string RawServiceTime { get; set; }
        public string RawServiceStart { get; set; }
        public string RawServiceEnd { get; set; }
        public string RawWaitTime { get; set; }
        public string Status { get; set; }

        // ── Parsed numeric values (minutes elapsed from session start) ────────
        public double ArrivalMin { get; set; }

        /// <summary>
        /// Duration of service in minutes.
        /// 0 for balkers; double.NaN if unparseable.
        /// </summary>
        public double ServiceTimeMin { get; set; }
        public double ServiceStartMin { get; set; }
        public double ServiceEndMin { get; set; }
        public double RecordedWaitMin { get; set; }

        public CustomerRecord()
        {
            SessionLabel = "";
            RawArrivalTime = "";
            RawServiceTime = "";
            RawServiceStart = "";
            RawServiceEnd = "";
            RawWaitTime = "";
            Status = "";
            ServiceStartMin = -1;
            ServiceEndMin = -1;
            RecordedWaitMin = -1;
        }

        // ── Status helpers ────────────────────────────────────────────────────
        public bool IsBalker
        {
            get
            {
                if (string.IsNullOrEmpty(Status)) return false;
                return Status.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        // ── Data-quality flags ────────────────────────────────────────────────
        public bool IsExcluded { get; set; }          // Fatal anomaly: skip in simulation
        public string ExclusionReason { get; set; }

        public bool HasWarning { get; set; }          // Non-fatal anomaly: flag but retain
        public string WarningReason { get; set; }

        // Convenience: usable in the simulation
        public bool IsUsableForSimulation
        {
            get { return !IsExcluded && !IsBalker; }
        }
    }
}
