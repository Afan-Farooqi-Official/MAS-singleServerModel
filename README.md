# KFC Queue Simulation (M/M/1 Model)

A C# console application that simulates and validates a single-server queue using real field observations from a KFC branch for a **Modeling & Simulation** university course.

---

## What This Project Does

1. **Loads Real Data**: Reads 93 customer records from two observation sessions at KFC and detects errors (e.g. negative service times).
2. **Part A - Trace-Driven Validation**: Replays the real arrival and service times through a single-server Discrete-Event Simulator (using the Future Event List approach) and compares simulated wait times with real recorded wait times.
3. **Part B - Random Simulation**: Fits exponential distributions to the real data, generates synthetic customers using inverse-transform sampling, and runs 10 replications with a finite queue capacity ($K = 5$) to model customer balking (leaving without ordering).

---

## Key Results & Metrics

| Metric | Symbol | Field Data (Session 1) | Simulated Replay | 10-Run Average (Random) |
|---|:---:|:---:|:---:|:---:|
| **Mean Inter-Arrival Time** | $1 / \lambda$ | **3.47 min** | **3.47 min** | **3.47 min** |
| **Mean Service Time** | $1 / \mu$ | **3.42 min** | **3.42 min** | **3.62 min** |
| **Arrival Rate** | $\lambda$ | **0.288 cust/min** | **0.288 cust/min** | **0.288 cust/min** |
| **Service Rate** | $\mu$ | **0.292 cust/min** | **0.292 cust/min** | **0.276 cust/min** |
| **Server Utilization** | $\rho$ | **98.5%** | **100.0%** | **82.3%** |
| **Mean Customers in Queue** | $L_q$ | **1.34** | **1.49** | **1.51** |
| **Mean Customers in System** | $L$ | **2.32** | **2.49** | **2.33** |
| **Mean Wait in Queue** | $W_q$ | **4.65 min** | **5.10 min** | **6.76 min** |
| **Mean Wait in System** | $W$ | **8.07 min** | **8.52 min** | **10.38 min** |

---

## Important Findings (For Report / Viva)

- **Data Cleaning**: Customer 48 in Session 2 had a negative service time (-4 min) and was automatically detected and excluded.
- **Session 1 (Good Fit)**: The simulated wait time ($5.10$ min) closely matched the recorded wait time ($4.65$ min) with an error of only $0.44$ min. The single-server FCFS model works very well here.
- **Session 2 (Multi-Server Reality)**: In Session 2, multiple customers were served at the same time (527 overlapping windows). Because KFC opened multiple counters during the peak rush, forcing it into a single-server model creates unrealistic delays.
- **Balking**: Setting max queue capacity to 5 successfully models customer impatience, averaging ~2 balked customers per run.

---

## How to Run

### Method 1: Double-Click (Easiest)
Just double-click **`run.bat`** (or **`KfcQueueSim.exe`**). No setup or installation needed.

### Method 2: From Terminal
```bash
dotnet run
```
or run the executable directly:
```bash
./KfcQueueSim.exe
```

---

## Project Structure

- **`Program.cs`**: Orchestrates data loading, validation, and prints the report.
- **`SimulationEngine.cs`**: Future Event List (FEL) discrete-event simulation engine.
- **`DataLoader.cs`**: Reads `kfc_data.csv` and checks for bad data.
- **`RandomGen.cs`**: Inverse-transform exponential random variate generator ($X = -\text{mean} \cdot \ln(U)$).
- **`Models/`**: Data classes for customer records, events, and results.
- **`kfc_data.csv`**: Real observation data from the KFC branch.
- **`run.bat`**: Simple one-click runner for lab PCs.
