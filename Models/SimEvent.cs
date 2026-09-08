using System;

namespace KfcQueueSim.Models
{
    public enum EventType
    {
        Arrival,
        Departure
    }

    /// <summary>
    /// Represents an event in the Future Event List (FEL).
    /// Ordered primarily by Time (lowest first).
    /// </summary>
    public class SimEvent : IComparable<SimEvent>
    {
        public EventType Type { get; set; }
        public double Time { get; set; }
        public int CustomerId { get; set; }
        public double ServiceDuration { get; set; }

        public int CompareTo(SimEvent other)
        {
            if (other == null) return 1;
            int timeCmp = Time.CompareTo(other.Time);
            if (timeCmp != 0) return timeCmp;
            // Departures processed before arrivals at identical timestamp
            return Type.CompareTo(other.Type);
        }
    }
}
