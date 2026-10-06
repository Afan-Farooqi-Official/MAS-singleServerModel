# KFC Tandem Queue Simulation & Analysis

A C# console application that models and analyzes a two-stage tandem queue (Order Counter → Collection Counter) using real KFC field observation data for a **Modeling & Simulation** course.

---

## System Overview

The system consists of two sequential queues:
- **Queue 1 (Order Queue)**: Customer arrives, waits in line, and places an order.
- **Queue 2 (Collection Queue)**: Customer waits for food preparation and receives the order.

---

## Key Results & Performance Metrics

### Session 1 (12:30 – 13:36 PM, 20 Customers)
- **Arrival Rate ($\lambda$)**: `0.2879 cust/min` (Mean Inter-Arrival: `3.47 min`)
- **Queue 1 Service Time ($1/\mu_1$)**: `1.85 min` | **Utilization ($\rho_1$)**: `53.26%`
- **Queue 2 Service Time ($1/\mu_2$)**: `8.40 min` | **Mean Time in Q2 ($W_2$)**: `8.40 min`
- **Mean Wait in Q1 Queue ($W_{q1}$)**: `1.60 min`
- **Total System Time ($W$)**: `11.85 min`
- **Average in System ($L$)**: `3.41 customers`

### Session 2 (14:01 – 15:29 PM, 60 Customers)
- **Arrival Rate ($\lambda$)**: `0.6705 cust/min` (Mean Inter-Arrival: `1.49 min`)
- **Queue 1 Service Time ($1/\mu_1$)**: `1.63 min` | **Utilization ($\rho_1$)**: `109.51%` (Rush hour bottleneck)
- **Queue 2 Service Time ($1/\mu_2$)**: `18.73 min`
- **Mean Wait in Q1 Queue ($W_{q1}$)**: `7.77 min`
- **Total System Time ($W$)**: `28.13 min`
- **Average in System ($L$)**: `18.86 customers`
- **Peak Line Length**: `10 customers`

---

## Chi-Square Goodness-of-Fit Test (Arrival Distribution)

- **$H_0$**: Arrival data follows a Poisson Distribution
- **$H_1$**: Arrival data does not follow a Poisson Distribution
- **Significance Level ($\alpha$)**: `0.05`
- **Degrees of Freedom ($df$)**: `3`
- **Mean Arrivals ($\lambda$)**: `2.2222` per 5 minutes
- **$\chi^2_{\text{calculated}}$**: `4.84864`
- **$\chi^2_{\text{critical}}$**: `7.81473`
- **Conclusion**: Since $\chi^2_{\text{calculated}} < \chi^2_{\text{critical}}$, we **fail to reject $H_0$**. The arrival data follows a **Poisson Distribution**.

---

## How to Run

### Method 1: Double-Click (Easiest)
Double-click **`run.bat`** (or **`TandemSim.exe`**). No installation or setup required.

### Method 2: From Terminal
```cmd
TandemSim.exe
```

---

## Files in Project

- **`TandemSim.cs`**: Source code for the tandem queue simulation and Chi-Square goodness-of-fit test.
- **`TandemSim.exe`**: Ready-to-run compiled executable.
- **`kfc_tandem_data.csv`**: Raw dataset with arrival times, order end times, and service end times.
- **`output.txt`**: Saved full console output and per-customer trace table.
- **`run.bat`**: One-click launcher.
