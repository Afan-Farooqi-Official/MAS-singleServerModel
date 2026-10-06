using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace KfcQueueSim
{
    class Customer
    {
        public int    Id;
        public double Arrival;
        public double Q1Start;
        public double Q1End;
        public double Q2End;

        public double WaitQ1   { get { return Q1Start - Arrival; } }
        public double ServQ1   { get { return Q1End   - Q1Start; } }
        public double ServQ2   { get { return Q2End   - Q1End;   } }
        public double TotalW   { get { return Q2End   - Arrival; } }
    }

    class TandemSim
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Banner("KFC TANDEM QUEUE SIMULATION & ANALYSIS",
                   "Two-Stage Queue: Q1 = Order Queue  |  Q2 = Collection Queue");

            string csvPath = "kfc_tandem_data.csv";
            if (!File.Exists(csvPath))
            {
                Console.WriteLine("CSV not found: " + csvPath);
                Console.ReadLine(); return;
            }

            List<Customer> all = LoadCsv(csvPath);
            Console.WriteLine("Loaded " + all.Count + " customers total.\n");

            List<Customer> s1 = new List<Customer>();
            List<Customer> s2 = new List<Customer>();
            foreach (Customer c in all)
            {
                if (c.Arrival <= 90) s1.Add(c);
                else                 s2.Add(c);
            }

            AnalyseSession("SESSION 1  (12:30 - 13:36 PM)", s1);
            AnalyseSession("SESSION 2  (14:01 - 15:29 PM)", s2);
            ChiSquareTest(all);

            Console.WriteLine("\nDone. Press Enter to exit.");
            Console.ReadLine();
        }

        static void AnalyseSession(string label, List<Customer> sess)
        {
            if (sess.Count == 0) return;
            Section(label);

            int N = sess.Count;
            sess.Sort(delegate(Customer a, Customer b) { return a.Arrival.CompareTo(b.Arrival); });

            // ── Inter-arrival times ───────────────────────────────────────────
            double sumIAT = 0;
            for (int i = 1; i < N; i++) sumIAT += (sess[i].Arrival - sess[i-1].Arrival);
            double meanIAT = N > 1 ? sumIAT / (N - 1) : 0;
            double lambda  = meanIAT > 0 ? 1.0 / meanIAT : 0;

            // ── Service times ─────────────────────────────────────────────────
            double sumS1=0, sumS2=0, sumWq1=0, sumW=0;
            for (int i = 0; i < N; i++)
            {
                sumS1  += sess[i].ServQ1;
                sumS2  += sess[i].ServQ2;
                sumWq1 += sess[i].WaitQ1;
                sumW   += sess[i].TotalW;
            }
            double meanS1 = sumS1 / N;
            double meanS2 = sumS2 / N;
            double mu1    = meanS1 > 0 ? 1.0/meanS1 : 0;
            double mu2    = meanS2 > 0 ? 1.0/meanS2 : 0;
            double rho1   = mu1 > 0 ? lambda/mu1 : 0;
            double rho2   = mu2 > 0 ? lambda/mu2 : 0;

            // ── Wq, W ─────────────────────────────────────────────────────────
            double Wq1    = sumWq1 / N;
            double W1     = Wq1 + meanS1;
            double W2     = meanS2;          // tandem: no separate queue wait at Q2
            double Wq2    = 0.0;
            double W_tot  = sumW / N;

            // Little's Law ─────────────────────────────────────────────────────
            double Lq1 = lambda * Wq1;
            double L1  = lambda * W1;
            double Lq2 = lambda * Wq2;
            double L2  = lambda * W2;
            double L   = lambda * W_tot;

            // ── Peak Q1 queue length ──────────────────────────────────────────
            List<double[]> evts = new List<double[]>();
            for (int i = 0; i < N; i++)
            {
                evts.Add(new double[]{ sess[i].Arrival,  1 });
                evts.Add(new double[]{ sess[i].Q1Start, -1 });
            }
            evts.Sort(delegate(double[] a, double[] b) { return a[0].CompareTo(b[0]); });
            int maxQ=0, curQ=0;
            for (int i = 0; i < evts.Count; i++)
            {
                curQ += (int)evts[i][1];
                if (curQ > maxQ) maxQ = curQ;
            }

            // ── Print results ─────────────────────────────────────────────────
            Console.WriteLine("\n" + Pad("Metric", 50) + Pad("Value", 18));
            Console.WriteLine(new string('-', 70));
            R("Customers in Session (N)",         N.ToString());
            R("Mean Inter-Arrival Time  (1/λ)",   F(meanIAT) + " min");
            R("Arrival Rate             (λ)",      F(lambda)  + " cust/min");
            Console.WriteLine();

            Col(ConsoleColor.Cyan);
            Console.WriteLine("  -- QUEUE 1: Order Queue (" + label.Split('(')[0].Trim() + ")--");
            Reset();
            R("  Mean Service Time Q1    (1/μ₁)",  F(meanS1)      + " min");
            R("  Service Rate Q1         (μ₁)",     F(mu1)         + " cust/min");
            R("  Server Utilization Q1   (ρ₁)",     F(rho1*100)    + " %");
            R("  Mean Wait in Q1 Queue   (Wq₁)",    F(Wq1)         + " min");
            R("  Mean Time in Q1 System  (W₁)",     F(W1)          + " min");
            R("  Mean Customers in Queue (Lq₁)",    F(Lq1)         + " customers");
            R("  Mean Customers in System(L₁)",     F(L1)          + " customers");
            Console.WriteLine();

            Col(ConsoleColor.Yellow);
            Console.WriteLine("  -- QUEUE 2: Collection Queue --");
            Reset();
            R("  Mean Service Time Q2    (1/μ₂)",  F(meanS2)      + " min");
            R("  Service Rate Q2         (μ₂)",     F(mu2)         + " cust/min");
            R("  Server Utilization Q2   (ρ₂)",     F(rho2*100)    + " %");
            R("  Mean Wait in Q2 Queue   (Wq₂)",    F(Wq2)         + " min");
            R("  Mean Time in Q2 System  (W₂)",     F(W2)          + " min");
            R("  Mean Customers in Queue (Lq₂)",    F(Lq2)         + " customers");
            R("  Mean Customers in System(L₂)",     F(L2)          + " customers");
            Console.WriteLine();

            Col(ConsoleColor.Green);
            Console.WriteLine("  -- OVERALL SYSTEM (Arrival to Order Received) --");
            Reset();
            R("  Mean Total Sojourn Time  (W)",      F(W_tot)       + " min");
            R("  Mean Customers in System (L)",       F(L)           + " customers");
            R("  Peak Q1 Queue Length     (max Lq₁)", maxQ.ToString()+ " customers");

            // ── Per-customer table ────────────────────────────────────────────
            Console.WriteLine();
            Console.WriteLine("+------+-------+-------+-------+-------+-------+-------+-------+---------+");
            Console.WriteLine("| ID   | Arr   |Q1Start|Q1End  |Q2End  |WaitQ1 |ServQ1 |ServQ2 |TotalWait|");
            Console.WriteLine("+------+-------+-------+-------+-------+-------+-------+-------+---------+");
            for (int i = 0; i < N; i++)
            {
                Customer c = sess[i];
                Console.WriteLine(string.Format("| {0,4} | {1,5} | {2,5} | {3,5} | {4,5} | {5,5} | {6,5} | {7,5} | {8,7} |",
                    c.Id, c.Arrival, c.Q1Start, c.Q1End, c.Q2End,
                    c.WaitQ1, c.ServQ1, c.ServQ2, c.TotalW));
            }
            Console.WriteLine("+------+-------+-------+-------+-------+-------+-------+-------+---------+");
        }

        static void ChiSquareTest(List<Customer> all)
        {
            Section("CHI-SQUARE GOODNESS-OF-FIT  (Poisson Arrival Test, 5-min bins)");

            int[] obs = {
                5,1,2,1,1,2,1,1,1,1,1,1,1,1,0,0,0,0,
                3,3,4,4,4,3,3,4,4,4,4,3,3,2,3,2,3,4
            };
            int G = obs.Length;
            int totalArr = 0;
            for (int i = 0; i < G; i++) totalArr += obs[i];

            double lam = (double)totalArr / G;
            Console.WriteLine("\nH0: Arrival data follows a Poisson distribution");
            Console.WriteLine("H1: Arrival data does NOT follow a Poisson distribution");
            Console.WriteLine("Alpha = 0.05");
            Console.WriteLine(string.Format("Mean per 5-min interval (lambda) = {0:F4}  (Total = {1}, Intervals = {2})", lam, totalArr, G));

            // Count frequency of each arrival count per bin
            int[] freq = new int[8];
            for (int i = 0; i < G; i++)
            {
                int v = obs[i];
                if (v >= freq.Length - 1) freq[freq.Length - 1]++;
                else freq[v]++;
            }

            Console.WriteLine();
            Console.WriteLine("+--------+------+------+----------+---------+-----------+");
            Console.WriteLine("| Arrivl | Obs  |  fx  | P(x)     | Exp (E) |  Chi-Sq   |");
            Console.WriteLine("+--------+------+------+----------+---------+-----------+");

            double chiSq = 0;
            int cats = 0;

            // Count total obs for >=4 category
            int obsGe4 = 0;
            for (int k = 4; k < freq.Length; k++) obsGe4 += freq[k];

            // Use tail probability P(X>=4) = 1 - P(0) - P(1) - P(2) - P(3)
            double pGe4  = 1.0 - PoissonCDF(3, lam);
            double expGe4 = pGe4 * G;

            for (int k = 0; k <= 3; k++)
            {
                int o    = freq[k];
                double p = Poisson(k, lam);
                double e = p * G;
                double cs = e > 0 ? ((o - e) * (o - e)) / e : 0;
                chiSq += cs;
                cats++;
                Console.WriteLine(string.Format("| {0,6} | {1,4} | {2,4} | {3,8:F5} | {4,7:F5} | {5,9:F5} |",
                    k, o, o*k, p, e, cs));
            }

            // >=4 row using correct tail prob (matches Excel formula)
            double csR = expGe4 > 0 ? ((obsGe4 - expGe4)*(obsGe4 - expGe4)) / expGe4 : 0;
            chiSq += csR;
            cats++;
            Console.WriteLine(string.Format("| {0,6} | {1,4} | {2,4} | {3,8:F5} | {4,7:F5} | {5,9:F5} |",
                ">=4", obsGe4, "  —", pGe4, expGe4, csR));
            Console.WriteLine("+--------+------+------+----------+---------+-----------+");

            int df = cats - 1 - 1;
            double crit = 7.81473; // chi2 at df=3, alpha=0.05

            Console.WriteLine(string.Format("\nChi-Square calculated  = {0:F5}", chiSq));
            Console.WriteLine(string.Format("Degrees of Freedom     = {0}   (categories - 1 - 1 estimated param)", df));
            Console.WriteLine(string.Format("Critical Value (a=0.05)= {0:F5}  (df={1})", crit, df));
            Console.WriteLine();

            if (chiSq < crit)
            {
                Col(ConsoleColor.Green);
                Console.WriteLine("RESULT  : " + chiSq.ToString("F4") + " < " + crit.ToString("F4"));
                Console.WriteLine("DECISION: FAIL TO REJECT H0");
                Console.WriteLine("CONCLUSION: Arrival data follows a Poisson distribution. (Accept H0)");
            }
            else
            {
                Col(ConsoleColor.Red);
                Console.WriteLine("RESULT  : " + chiSq.ToString("F4") + " >= " + crit.ToString("F4"));
                Console.WriteLine("DECISION: REJECT H0");
                Console.WriteLine("CONCLUSION: Arrival data does NOT follow a Poisson distribution.");
            }
            Reset();
        }

        static double Poisson(int k, double lam)
        {
            if (lam <= 0) return k == 0 ? 1 : 0;
            double logP = k * Math.Log(lam) - lam;
            for (int i = 2; i <= k; i++) logP -= Math.Log(i);
            return Math.Exp(logP);
        }

        static double PoissonCDF(int kMax, double lam)
        {
            double s = 0;
            for (int k = 0; k <= kMax; k++) s += Poisson(k, lam);
            return s;
        }

        static List<Customer> LoadCsv(string path)
        {
            List<Customer> list = new List<Customer>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 1; i < lines.Length; i++)
            {
                string ln = lines[i].Trim();
                if (string.IsNullOrEmpty(ln)) continue;
                string[] p = ln.Split(',');
                if (p.Length < 5) continue;
                Customer c = new Customer();
                c.Id      = int.Parse(p[0].Trim());
                c.Arrival = double.Parse(p[1].Trim(), CultureInfo.InvariantCulture);
                c.Q1Start = double.Parse(p[2].Trim(), CultureInfo.InvariantCulture);
                c.Q1End   = double.Parse(p[3].Trim(), CultureInfo.InvariantCulture);
                c.Q2End   = double.Parse(p[4].Trim(), CultureInfo.InvariantCulture);
                list.Add(c);
            }
            return list;
        }

        static string F(double v)  { return v.ToString("F4", CultureInfo.InvariantCulture); }
        static string Pad(string s, int w) { return s.PadRight(w); }
        static void R(string lbl, string val) { Console.WriteLine("{0,-50}{1,18}", lbl, val); }
        static void Col(ConsoleColor c) { Console.ForegroundColor = c; }
        static void Reset() { Console.ResetColor(); }

        static void Banner(string title, string sub)
        {
            string line = new string('=', 80);
            Console.WriteLine(line);
            Console.WriteLine("  " + title);
            Console.WriteLine("  " + sub);
            Console.WriteLine(line + "\n");
        }

        static void Section(string title)
        {
            Console.WriteLine();
            Col(ConsoleColor.Cyan);
            Console.WriteLine(new string('-', 80));
            Console.WriteLine("  " + title);
            Console.WriteLine(new string('-', 80));
            Reset();
        }
    }
}
