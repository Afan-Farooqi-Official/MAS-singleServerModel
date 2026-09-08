using System;
using System.Collections.Generic;
using KfcQueueSim.Models;

namespace KfcQueueSim
{
    /// <summary>
    /// Discrete-Event Simulation (DES) Engine for a Single-Server First-Come-First-Served (FCFS) queue.
    /// Uses the Future Event List (FEL) Next-Event Time Advance algorithm.
    /// Tracks all standard Queueing Theory metrics:
    ///   - Mean inter-arrival time (1 / lambda)
    ///   - Mean service time (1 / mu)
    ///   - Arrival rate (lambda) & Service rate (mu)
    ///   - Server utilization (rho)
    ///   - Mean number of customers in the system (L)
    ///   - Mean number of customers in the queue (Lq)
    ///   - Mean wait of customers in the system (W)
    ///   - Mean wait of customers in the queue (Wq)
    /// </summary>
    public class SimulationEngine
    {
        private readonly int? _maxQueueLength;

        public SimulationEngine(int? maxQueueLength)
        {
            _maxQueueLength = maxQueueLength;
        }

        public SimulationEngine()
        {
            _maxQueueLength = null;
        }

        public class SimArrival
        {
            public int CustomerId { get; set; }
            public double ArrivalTime { get; set; }
            public double ServiceDuration { get; set; }

            public SimArrival(int cid, double arrTime, double servDur)
            {
                CustomerId = cid;
                ArrivalTime = arrTime;
                ServiceDuration = servDur;
            }
        }

        public class SimulationOutput
        {
            public SimulationResult Summary { get; set; }
            public List<CustomerSimStat> CustomerStats { get; set; }

            public SimulationOutput(SimulationResult summary, List<CustomerSimStat> stats)
            {
                Summary = summary;
                CustomerStats = stats;
            }
        }

        public SimulationOutput Run(string runLabel, List<SimArrival> arrivals)
        {
            double simClock = 0.0;
            double prevEventTime = 0.0;
            bool isServerBusy = false;
            double totalBusyTime = 0.0;

            // Area under Q(t) and L(t) curves for time-average number of customers
            double areaQueueLength = 0.0;
            double areaSystemLength = 0.0;

            // Queue for customers waiting in line (FCFS)
            Queue<SimArrival> waitingQueue = new Queue<SimArrival>();

            // Future Event List (FEL): sorted list of events
            List<SimEvent> fel = new List<SimEvent>();

            for (int i = 0; i < arrivals.Count; i++)
            {
                SimArrival arr = arrivals[i];
                SimEvent ev = new SimEvent();
                ev.Type = EventType.Arrival;
                // Preserve sequence for identical arrival times
                ev.Time = arr.ArrivalTime + (i * 0.000001);
                ev.CustomerId = arr.CustomerId;
                ev.ServiceDuration = arr.ServiceDuration;
                fel.Add(ev);
            }

            fel.Sort();

            Dictionary<int, CustomerSimStat> customerStatsMap = new Dictionary<int, CustomerSimStat>();

            int maxObservedQueueLength = 0;
            int totalArrivals = 0;
            int totalServed = 0;
            int totalBalked = 0;

            double firstArrivalTime = -1.0;
            double lastEventTime = 0.0;

            // Event loop: process until Future Event List is empty
            while (fel.Count > 0)
            {
                SimEvent currentEvent = fel[0];
                fel.RemoveAt(0);

                simClock = currentEvent.Time;
                lastEventTime = simClock;

                if (firstArrivalTime < 0)
                {
                    firstArrivalTime = simClock;
                    prevEventTime = simClock;
                }

                // Time-advance accumulation: integrate Q(t) and S(t) over [prevEventTime, simClock]
                double timeDelta = simClock - prevEventTime;
                if (timeDelta > 0)
                {
                    int currentQueueCount = waitingQueue.Count;
                    int currentServerCount = isServerBusy ? 1 : 0;
                    int currentSystemCount = currentQueueCount + currentServerCount;

                    areaQueueLength += currentQueueCount * timeDelta;
                    areaSystemLength += currentSystemCount * timeDelta;
                    if (isServerBusy)
                    {
                        totalBusyTime += timeDelta;
                    }
                }
                prevEventTime = simClock;

                if (currentEvent.Type == EventType.Arrival)
                {
                    totalArrivals++;

                    CustomerSimStat stat = new CustomerSimStat();
                    stat.CustomerId = currentEvent.CustomerId;
                    stat.ArrivalTime = Math.Round(currentEvent.Time, 4);
                    customerStatsMap[stat.CustomerId] = stat;

                    // Balking check: if waiting queue is full
                    if (_maxQueueLength.HasValue && waitingQueue.Count >= _maxQueueLength.Value)
                    {
                        stat.WasBalked = true;
                        totalBalked++;
                        continue;
                    }

                    // Server check: if idle, start service immediately
                    if (!isServerBusy)
                    {
                        isServerBusy = true;
                        stat.ServiceStartTime = simClock;
                        stat.ServiceEndTime = simClock + currentEvent.ServiceDuration;

                        SimEvent depEvent = new SimEvent();
                        depEvent.Type = EventType.Departure;
                        depEvent.Time = stat.ServiceEndTime;
                        depEvent.CustomerId = stat.CustomerId;
                        depEvent.ServiceDuration = currentEvent.ServiceDuration;

                        InsertEvent(fel, depEvent);
                    }
                    else
                    {
                        // Server is busy: customer joins waiting queue
                        waitingQueue.Enqueue(new SimArrival(currentEvent.CustomerId, stat.ArrivalTime, currentEvent.ServiceDuration));
                        if (waitingQueue.Count > maxObservedQueueLength)
                        {
                            maxObservedQueueLength = waitingQueue.Count;
                        }
                    }
                }
                else if (currentEvent.Type == EventType.Departure)
                {
                    totalServed++;

                    if (waitingQueue.Count > 0)
                    {
                        // Serve next customer in queue
                        SimArrival nextCust = waitingQueue.Dequeue();
                        CustomerSimStat nextStat = customerStatsMap[nextCust.CustomerId];
                        nextStat.ServiceStartTime = simClock;
                        nextStat.ServiceEndTime = simClock + nextCust.ServiceDuration;

                        SimEvent depEvent = new SimEvent();
                        depEvent.Type = EventType.Departure;
                        depEvent.Time = nextStat.ServiceEndTime;
                        depEvent.CustomerId = nextCust.CustomerId;
                        depEvent.ServiceDuration = nextCust.ServiceDuration;

                        InsertEvent(fel, depEvent);
                    }
                    else
                    {
                        // Queue empty: server becomes idle
                        isServerBusy = false;
                    }
                }
            }

            // ── Statistical Compilation ───────────────────────────────────────
            double totalSimTime = (lastEventTime > firstArrivalTime && firstArrivalTime >= 0)
                ? (lastEventTime - firstArrivalTime)
                : 0.0;

            List<CustomerSimStat> servedList = new List<CustomerSimStat>();
            double sumWaitQueue = 0.0;
            double sumWaitSystem = 0.0;
            double sumService = 0.0;

            foreach (CustomerSimStat s in customerStatsMap.Values)
            {
                if (!s.WasBalked && s.ServiceStartTime >= 0)
                {
                    servedList.Add(s);
                    sumWaitQueue += s.WaitTime;
                    sumWaitSystem += s.TimeInSystem;
                    sumService += s.ServiceTime;
                }
            }

            // Customer averages: Wq and W
            double avgWq = servedList.Count > 0 ? (sumWaitQueue / servedList.Count) : 0.0;
            double avgW = servedList.Count > 0 ? (sumWaitSystem / servedList.Count) : 0.0;
            double avgServiceTime = servedList.Count > 0 ? (sumService / servedList.Count) : 0.0;

            // Inter-arrival calculation
            double sumInterArrival = 0.0;
            int iatCount = 0;
            for (int i = 1; i < arrivals.Count; i++)
            {
                double iat = arrivals[i].ArrivalTime - arrivals[i - 1].ArrivalTime;
                if (iat >= 0)
                {
                    sumInterArrival += iat;
                    iatCount++;
                }
            }
            double meanIat = iatCount > 0 ? (sumInterArrival / iatCount) : 0.0;

            // Arrival and Service Rates
            double lambda = meanIat > 0 ? (1.0 / meanIat) : (totalSimTime > 0 ? (double)totalArrivals / totalSimTime : 0.0);
            double mu = avgServiceTime > 0 ? (1.0 / avgServiceTime) : 0.0;

            // Utilization: rho = totalBusyTime / totalSimTime
            double rho = totalSimTime > 0 ? Math.Min(1.0, totalBusyTime / totalSimTime) : 0.0;

            // Time-averaged customer counts in queue (Lq) and in system (L)
            // Lq = integral(Q(t) dt) / T;  L = integral(L(t) dt) / T
            double lq = totalSimTime > 0 ? (areaQueueLength / totalSimTime) : 0.0;
            double l = totalSimTime > 0 ? (areaSystemLength / totalSimTime) : 0.0;

            SimulationResult summary = new SimulationResult();
            summary.Label = runLabel;
            summary.TotalArrivals = totalArrivals;
            summary.CustomersServed = totalServed;
            summary.CustomersBalked = totalBalked;
            summary.MeanInterArrivalTime = meanIat;
            summary.MeanServiceTime = avgServiceTime;
            summary.Lambda = lambda;
            summary.Mu = mu;
            summary.ServerUtilization = rho;
            summary.Lq = lq;
            summary.L = l;
            summary.Wq = avgWq;
            summary.W = avgW;
            summary.MaxQueueLength = maxObservedQueueLength;
            summary.TotalSimulationTime = totalSimTime;

            List<CustomerSimStat> allStatsSorted = new List<CustomerSimStat>(customerStatsMap.Values);
            allStatsSorted.Sort((a, b) => a.ArrivalTime.CompareTo(b.ArrivalTime));

            return new SimulationOutput(summary, allStatsSorted);
        }

        private static void InsertEvent(List<SimEvent> fel, SimEvent ev)
        {
            int idx = fel.BinarySearch(ev);
            if (idx < 0)
            {
                idx = ~idx;
            }
            fel.Insert(idx, ev);
        }
    }
}
