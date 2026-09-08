using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KfcQueueSim.Models;

namespace KfcQueueSim
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("=======================================================================================");
            Console.WriteLine("         KFC QUEUEING SIMULATION & SYSTEM PERFORMANCE ANALYSIS                         ");
            Console.WriteLine("                  (Modeling & Simulation Coursework)                                   ");
            Console.WriteLine("=======================================================================================");
            Console.WriteLine();

            string dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kfc_data.csv");
            if (!File.Exists(dataPath))
            {
                dataPath = "kfc_data.csv";
            }

            Console.WriteLine("[1] Loading field observation data from: " + Path.GetFullPath(dataPath));
            List<CustomerRecord> allRecords;
            try
            {
                allRecords = DataLoader.LoadFromCsv(dataPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error loading data: " + ex.Message);
                Console.ResetColor();
                return;
            }

            Console.WriteLine("    Total customer records parsed: " + allRecords.Count);
            Console.WriteLine();

            // ─────────────────────────────────────────────────────────────────────
            // DATA QUALITY & ANOMALY REPORT
            // ─────────────────────────────────────────────────────────────────────
            Console.WriteLine("=======================================================================================");
            Console.WriteLine("                          DATA QUALITY & ANOMALY REPORT                                ");
            Console.WriteLine("=======================================================================================");

            List<CustomerRecord> excludedRecords = new List<CustomerRecord>();
            List<CustomerRecord> warningRecords = new List<CustomerRecord>();

            foreach (CustomerRecord r in allRecords)
            {
                if (r.IsExcluded) excludedRecords.Add(r);
                else if (r.HasWarning) warningRecords.Add(r);
            }

            if (excludedRecords.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[!] EXCLUDED ENTRIES (" + excludedRecords.Count + " dropped from simulation):");
                Console.ResetColor();
                foreach (CustomerRecord exc in excludedRecords)
                {
                    Console.WriteLine("   * Cust ID " + exc.CustomerId + " (Session " + exc.SessionNumber + ", Arr: " + exc.RawArrivalTime + "): " + exc.ExclusionReason);
                }
                Console.WriteLine();
            }

            if (warningRecords.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[!] DATA WARNINGS / CAUTIONS (" + warningRecords.Count + " flagged but retained):");
                Console.ResetColor();
                foreach (CustomerRecord wrn in warningRecords)
                {
                    Console.WriteLine("   * Cust ID " + wrn.CustomerId + " (Session " + wrn.SessionNumber + ", Arr: " + wrn.RawArrivalTime + "): " + wrn.WarningReason);
                }
                Console.WriteLine();
            }

            DetectAndReportOverlappingServices(allRecords);

            // ─────────────────────────────────────────────────────────────────────
            // CORE QUEUEING METRICS - EMPIRICAL OBSERVED FIELD DATA
            // ─────────────────────────────────────────────────────────────────────
            Console.WriteLine("=======================================================================================");
            Console.WriteLine("     SECTION 1: EMPIRICAL QUEUEING PARAMETERS DIRECT FROM FIELD DATA                   ");
            Console.WriteLine("=======================================================================================");

            int[] sessionNums = new int[] { 1, 2 };

            foreach (int sId in sessionNums)
            {
                List<CustomerRecord> sRecs = new List<CustomerRecord>();
                List<CustomerRecord> sValid = new List<CustomerRecord>();
                List<CustomerRecord> sBalked = new List<CustomerRecord>();

                foreach (CustomerRecord r in allRecords)
                {
                    if (r.SessionNumber == sId)
                    {
                        sRecs.Add(r);
                        if (r.IsUsableForSimulation) sValid.Add(r);
                        if (r.IsBalker) sBalked.Add(r);
                    }
                }

                if (sValid.Count == 0) continue;

                sValid.Sort((a, b) => a.ArrivalMin.CompareTo(b.ArrivalMin));

                // Calculate Inter-arrival times
                List<double> iats = new List<double>();
                double sumServ = 0.0;
                double sumWaitQ = 0.0;

                for (int i = 0; i < sValid.Count; i++)
                {
                    sumServ += sValid[i].ServiceTimeMin;
                    sumWaitQ += sValid[i].RecordedWaitMin;
                    if (i > 0)
                    {
                        iats.Add(sValid[i].ArrivalMin - sValid[i - 1].ArrivalMin);
                    }
                }

                double meanIat = iats.Count > 0 ? (Sum(iats) / iats.Count) : 0.0;
                double meanServ = sumServ / sValid.Count;
                double lambda = meanIat > 0 ? (1.0 / meanIat) : 0.0;
                double mu = meanServ > 0 ? (1.0 / meanServ) : 0.0;
                double rhoEmpirical = mu > 0 ? (lambda / mu) : 0.0;
                double meanWaitQ = sumWaitQ / sValid.Count;
                double meanWaitSys = meanWaitQ + meanServ;

                // Little's Law empirical estimates: Lq = lambda * Wq, L = lambda * W
                double empLq = lambda * meanWaitQ;
                double empL = lambda * meanWaitSys;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("─── SESSION " + sId + " EMPIRICAL PARAMETERS (" + sRecs[0].SessionLabel + ") ───");
                Console.ResetColor();
                Console.WriteLine("  1. Mean Inter-Arrival Time:          " + meanIat.ToString("F2") + " min");
                Console.WriteLine("  2. Mean Service Time:                " + meanServ.ToString("F2") + " min");
                Console.WriteLine("  3. Arrival Rate (λ - Lambda):        " + lambda.ToString("F3") + " customers/min (" + (lambda * 60.0).ToString("F1") + " cust/hr)");
                Console.WriteLine("  4. Service Rate (μ - Mu):            " + mu.ToString("F3") + " customers/min (" + (mu * 60.0).ToString("F1") + " cust/hr)");
                Console.WriteLine("  5. Server Traffic Intensity (ρ):     " + rhoEmpirical.ToString("F2") + " (" + (rhoEmpirical * 100.0).ToString("F1") + "%)");
                Console.WriteLine("  6. Mean Wait of Cust in Queue (Wq):  " + meanWaitQ.ToString("F2") + " min");
                Console.WriteLine("  7. Mean Wait of Cust in System (W):  " + meanWaitSys.ToString("F2") + " min");
                Console.WriteLine("  8. Mean Cust in Queue (Lq - Little): " + empLq.ToString("F2") + " customers");
                Console.WriteLine("  9. Mean Cust in System (L - Little): " + empL.ToString("F2") + " customers");
                Console.WriteLine(" 10. Observed Balked Customers:        " + sBalked.Count + " / " + sRecs.Count);
                Console.WriteLine();
            }

            // ─────────────────────────────────────────────────────────────────────
            // PART A: TRACE-DRIVEN SIMULATION VALIDATION
            // ─────────────────────────────────────────────────────────────────────
            Console.WriteLine("=======================================================================================");
            Console.WriteLine("     SECTION 2: PART A - TRACE-DRIVEN DISCRETE-EVENT SIMULATION (FEL)                  ");
            Console.WriteLine("=======================================================================================");

            foreach (int sessId in sessionNums)
            {
                List<CustomerRecord> sessionRecords = new List<CustomerRecord>();
                List<CustomerRecord> validRecords = new List<CustomerRecord>();

                foreach (CustomerRecord r in allRecords)
                {
                    if (r.SessionNumber == sessId)
                    {
                        sessionRecords.Add(r);
                        if (r.IsUsableForSimulation) validRecords.Add(r);
                    }
                }

                if (validRecords.Count == 0) continue;

                SimulationEngine engine = new SimulationEngine();
                List<SimulationEngine.SimArrival> arrivalInputs = new List<SimulationEngine.SimArrival>();
                foreach (CustomerRecord r in validRecords)
                {
                    arrivalInputs.Add(new SimulationEngine.SimArrival(r.CustomerId, r.ArrivalMin, r.ServiceTimeMin));
                }

                SimulationEngine.SimulationOutput simOutput = engine.Run("Session " + sessId + " Trace", arrivalInputs);
                SimulationResult res = simOutput.Summary;

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("─── SESSION " + sessId + " SIMULATED QUEUEING METRICS ───");
                Console.ResetColor();
                Console.WriteLine("+---------------------------------------------+-----------------+");
                Console.WriteLine("| Metric Description                          | Simulated Value |");
                Console.WriteLine("+---------------------------------------------+-----------------+");
                Console.WriteLine(string.Format("| Mean Inter-Arrival Time (1 / λ)             | {0,12:F2} min |", res.MeanInterArrivalTime));
                Console.WriteLine(string.Format("| Mean Service Time (1 / μ)                   | {0,12:F2} min |", res.MeanServiceTime));
                Console.WriteLine(string.Format("| Arrival Rate (λ - Lambda)                   | {0,12:F3} /min |", res.Lambda));
                Console.WriteLine(string.Format("| Service Rate (μ - Mu)                       | {0,12:F3} /min |", res.Mu));
                Console.WriteLine(string.Format("| Server Utilization (ρ - Rho)                | {0,11:F1} % |", res.ServerUtilization * 100.0));
                Console.WriteLine(string.Format("| Mean Number of Customers in Queue (Lq)      | {0,12:F2} cust|", res.Lq));
                Console.WriteLine(string.Format("| Mean Number of Customers in System (L)      | {0,12:F2} cust|", res.L));
                Console.WriteLine(string.Format("| Mean Wait of Customers in Queue (Wq)        | {0,12:F2} min |", res.Wq));
                Console.WriteLine(string.Format("| Mean Wait of Customers in System (W)        | {0,12:F2} min |", res.W));
                Console.WriteLine(string.Format("| Maximum Peak Queue Length                   | {0,12} cust|", res.MaxQueueLength));
                Console.WriteLine("+---------------------------------------------+-----------------+");

                // Validation comparison
                double recAvgWq = 0.0;
                foreach (CustomerRecord r in validRecords) recAvgWq += r.RecordedWaitMin;
                recAvgWq /= validRecords.Count;
                double mae = Math.Abs(res.Wq - recAvgWq);

                Console.WriteLine("Validation vs Field Reality:");
                Console.WriteLine(string.Format("   * Field Recorded Mean Wait (Wq):  {0:F2} min", recAvgWq));
                Console.WriteLine(string.Format("   * Simulator Computed Wait (Wq):   {0:F2} min", res.Wq));
                Console.WriteLine(string.Format("   * Discrepancy (MAE):              {0:F2} min", mae));

                if (mae < 2.0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("   -> VERDICT: EXCELLENT FIT. Single-server FCFS model accurately matches reality.");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("   -> VERDICT: POOR FIT. Massive delay caused by forced single-server assumption.");
                    Console.WriteLine("      Field data shows multiple staff served simultaneously (multi-server reality).");
                }
                Console.ResetColor();
                Console.WriteLine();
            }

            // ─────────────────────────────────────────────────────────────────────
            // PART B: RANDOM-NUMBER-DRIVEN SIMULATION (10 REPLICATIONS)
            // ─────────────────────────────────────────────────────────────────────
            Console.WriteLine("=======================================================================================");
            Console.WriteLine("     SECTION 3: PART B - RANDOM MONTE CARLO SIMULATION (INVERSE TRANSFORM)             ");
            Console.WriteLine("               (Fitting Exponential M/M/1/K Model with Seed = 42)                      ");
            Console.WriteLine("=======================================================================================");

            List<CustomerRecord> s1Usable = new List<CustomerRecord>();
            foreach (CustomerRecord r in allRecords)
            {
                if (r.SessionNumber == 1 && r.IsUsableForSimulation)
                {
                    s1Usable.Add(r);
                }
            }
            s1Usable.Sort((a, b) => a.ArrivalMin.CompareTo(b.ArrivalMin));

            List<double> s1Iats = new List<double>();
            double s1SumServ = 0;
            for (int i = 0; i < s1Usable.Count; i++)
            {
                s1SumServ += s1Usable[i].ServiceTimeMin;
                if (i > 0) s1Iats.Add(s1Usable[i].ArrivalMin - s1Usable[i - 1].ArrivalMin);
            }

            double fittedMeanIat = Sum(s1Iats) / s1Iats.Count;
            double fittedMeanServ = s1SumServ / s1Usable.Count;
            double fitLambda = 1.0 / fittedMeanIat;
            double fitMu = 1.0 / fittedMeanServ;

            Console.WriteLine("Fitted Exponential Parameters (from Session 1 single-counter):");
            Console.WriteLine(string.Format("   * Mean Inter-Arrival Time:  {0:F2} min  -->  λ (Lambda) = {1:F3} cust/min", fittedMeanIat, fitLambda));
            Console.WriteLine(string.Format("   * Mean Service Time:        {0:F2} min  -->  μ (Mu)     = {1:F3} cust/min", fittedMeanServ, fitMu));
            Console.WriteLine(string.Format("   * Traffic Intensity (ρ):    {0:F2}", fitLambda / fitMu));
            Console.WriteLine();
            Console.WriteLine("Running 10 Replications of 30 Customers (Queue Capacity K = 5 for Balking)...");
            Console.WriteLine();

            const int replications = 10;
            const int custPerRun = 30;
            const int queueCap = 5;

            RandomGen rng = new RandomGen(42);
            List<SimulationResult> repResults = new List<SimulationResult>();

            Console.WriteLine("+-----+--------+--------+---------+---------+--------+--------+-------------+-------+");
            Console.WriteLine("| Run | Served | Balked |   Wq    |    W    |   Lq   |   L    | Utilization | Max Q |");
            Console.WriteLine("+-----+--------+--------+---------+---------+--------+--------+-------------+-------+");

            for (int r = 1; r <= replications; r++)
            {
                List<SimulationEngine.SimArrival> synArrivals = new List<SimulationEngine.SimArrival>();
                double curTime = 0.0;
                for (int c = 1; c <= custPerRun; c++)
                {
                    if (c > 1) curTime += rng.NextExponential(fittedMeanIat);
                    double st = Math.Max(0.2, rng.NextExponential(fittedMeanServ));
                    synArrivals.Add(new SimulationEngine.SimArrival(c, curTime, st));
                }

                SimulationEngine eng = new SimulationEngine(queueCap);
                SimulationEngine.SimulationOutput outP = eng.Run("Rep " + r, synArrivals);
                SimulationResult sm = outP.Summary;
                repResults.Add(sm);

                Console.WriteLine(string.Format("| {0,3} | {1,6} | {2,6} | {3,6:F2}m | {4,6:F2}m | {5,6:F2} | {6,6:F2} | {7,10:F1}% | {8,5} |",
                    r, sm.CustomersServed, sm.CustomersBalked, sm.Wq, sm.W, sm.Lq, sm.L, sm.ServerUtilization * 100.0, sm.MaxQueueLength));
            }
            Console.WriteLine("+-----+--------+--------+---------+---------+--------+--------+-------------+-------+");

            // Compute Grand Averages Across 10 Replications
            double avgServed = 0, avgBalk = 0, avgWq = 0, avgW = 0, avgLq = 0, avgL = 0, avgRho = 0, avgMaxQ = 0;
            foreach (SimulationResult res in repResults)
            {
                avgServed += res.CustomersServed;
                avgBalk += res.CustomersBalked;
                avgWq += res.Wq;
                avgW += res.W;
                avgLq += res.Lq;
                avgL += res.L;
                avgRho += res.ServerUtilization;
                avgMaxQ += res.MaxQueueLength;
            }

            avgServed /= replications;
            avgBalk /= replications;
            avgWq /= replications;
            avgW /= replications;
            avgLq /= replications;
            avgL /= replications;
            avgRho /= replications;
            avgMaxQ /= replications;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("=======================================================================================");
            Console.WriteLine("        GRAND ENSEMBLE SUMMARY TABLE (ALL 7 MANDATORY METRICS)                         ");
            Console.WriteLine("=======================================================================================");
            Console.ResetColor();
            Console.WriteLine("+----+---------------------------------------+---------------------+-------------------+");
            Console.WriteLine("| #  | Mandatory Coursework Metric           | Theoretical Formula | Averaged Result   |");
            Console.WriteLine("+----+---------------------------------------+---------------------+-------------------+");
            Console.WriteLine(string.Format("| 1  | Mean Inter-Arrival Time               | 1 / λ               | {0,14:F2} min   |", fittedMeanIat));
            Console.WriteLine(string.Format("| 2  | Mean Service Time                     | 1 / μ               | {0,14:F2} min   |", fittedMeanServ));
            Console.WriteLine(string.Format("| 3a | Arrival Rate (Lambda - λ)             | λ                   | {0,10:F3} cust/min  |", fitLambda));
            Console.WriteLine(string.Format("| 3b | Service Rate (Mu - μ)                 | μ                   | {0,10:F3} cust/min  |", fitMu));
            Console.WriteLine(string.Format("| 4  | Server Utilization (Rho - ρ)          | Busy Time / T       | {0,14:F1} %     |", avgRho * 100.0));
            Console.WriteLine(string.Format("| 5  | Mean Customers in Queue (Lq)          | ∫ Q(t)dt / T        | {0,14:F2} cust    |", avgLq));
            Console.WriteLine(string.Format("| 6  | Mean Customers in System (L)          | ∫ L(t)dt / T        | {0,14:F2} cust    |", avgL));
            Console.WriteLine(string.Format("| 7  | Mean Wait of Customers in Queue (Wq)  | Σ WaitQueue / N     | {0,14:F2} min     |", avgWq));
            Console.WriteLine(string.Format("| 8  | Mean Wait of Customers in System (W)  | Σ (Wait + Serv) / N | {0,14:F2} min     |", avgW));
            Console.WriteLine("+----+---------------------------------------+---------------------+-------------------+");
            Console.WriteLine(string.Format("| *  | Mean Customers Balked (Lost Sales)    | Queue >= 5 Capacity | {0,10:F1} ({1:F1}%)   |", avgBalk, (avgBalk / custPerRun) * 100.0));
            Console.WriteLine(string.Format("| *  | Mean Peak Queue Length                | Max(Q(t))           | {0,14:F1} cust    |", avgMaxQ));
            Console.WriteLine("+----+---------------------------------------+---------------------+-------------------+");
            Console.WriteLine();
        }

        private static double Sum(List<double> list)
        {
            double s = 0;
            for (int i = 0; i < list.Count; i++) s += list[i];
            return s;
        }

        private static void DetectAndReportOverlappingServices(List<CustomerRecord> records)
        {
            List<CustomerRecord> session2Served = new List<CustomerRecord>();
            foreach (CustomerRecord r in records)
            {
                if (r.SessionNumber == 2 && r.IsUsableForSimulation && r.ServiceStartMin >= 0 && r.ServiceEndMin >= 0)
                {
                    session2Served.Add(r);
                }
            }

            session2Served.Sort((a, b) => a.ServiceStartMin.CompareTo(b.ServiceStartMin));

            List<KeyValuePair<CustomerRecord, CustomerRecord>> overlaps = new List<KeyValuePair<CustomerRecord, CustomerRecord>>();

            for (int i = 0; i < session2Served.Count; i++)
            {
                for (int j = i + 1; j < session2Served.Count; j++)
                {
                    CustomerRecord a = session2Served[i];
                    CustomerRecord b = session2Served[j];

                    if (b.ServiceStartMin < a.ServiceEndMin)
                    {
                        overlaps.Add(new KeyValuePair<CustomerRecord, CustomerRecord>(a, b));
                    }
                }
            }

            if (overlaps.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("[CRITICAL MODELLING FINDING] Multi-Server Evidence in Session 2:");
                Console.ResetColor();
                Console.WriteLine("   Detected " + overlaps.Count + " overlapping service windows in Session 2.");
                Console.WriteLine("   Examples of simultaneous service:");
                for (int o = 0; o < Math.Min(3, overlaps.Count); o++)
                {
                    CustomerRecord c1 = overlaps[o].Key;
                    CustomerRecord c2 = overlaps[o].Value;
                    Console.WriteLine("    - Cust " + c1.CustomerId + " (Served " + c1.RawServiceStart + " - " + c1.RawServiceEnd + ") overlaps with Cust " + c2.CustomerId + " (Served " + c2.RawServiceStart + " - " + c2.RawServiceEnd + ")");
                }
                Console.WriteLine("   -> Note for Report: This proves Session 2 operated with MULTIPLE servers / channels.");
                Console.WriteLine();
            }
        }
    }
}
