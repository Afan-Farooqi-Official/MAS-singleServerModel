using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using KfcQueueSim.Models;

namespace KfcQueueSim
{
    public static class DataLoader
    {
        public static List<CustomerRecord> LoadFromCsv(string filePath)
        {
            List<CustomerRecord> records = new List<CustomerRecord>();

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Data file not found at: " + filePath);
            }

            string[] lines = File.ReadAllLines(filePath);
            if (lines.Length <= 1) return records;

            // Header line
            string headerLine = lines[0];
            char delimiter = headerLine.IndexOf('\t') >= 0 ? '\t' : ',';
            string[] rawHeaders = headerLine.Split(delimiter);
            string[] headers = new string[rawHeaders.Length];
            for (int i = 0; i < rawHeaders.Length; i++)
            {
                headers[i] = rawHeaders[i].Trim();
            }

            int idIdx = FindHeaderIndex(headers, "Customer ID", "CustomerID", "ID");
            int sessIdx = FindHeaderIndex(headers, "Session");
            int arrIdx = FindHeaderIndex(headers, "Arrival Time", "ArrivalTime");
            int servIdx = FindHeaderIndex(headers, "Service Time", "ServiceTime");
            int startIdx = FindHeaderIndex(headers, "Service Start Time", "ServiceStart");
            int endIdx = FindHeaderIndex(headers, "Service End Time", "ServiceEnd");
            int waitIdx = FindHeaderIndex(headers, "Wait Time", "WaitTime");
            int statusIdx = FindHeaderIndex(headers, "Status");

            List<string[]> validDataLines = new List<string[]>();

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] rawParts = line.Split(delimiter);
                string[] parts = new string[rawParts.Length];
                for (int p = 0; p < rawParts.Length; p++)
                {
                    parts[p] = rawParts[p].Trim();
                }

                if (parts.Length == 0 || string.IsNullOrEmpty(parts[0])) continue;

                int dummyId;
                if (!int.TryParse(parts[0], out dummyId)) continue;

                validDataLines.Add(parts);
            }

            // Group into sessions: 1 or 2
            Dictionary<int, List<string[]>> sessionGroups = new Dictionary<int, List<string[]>>();
            sessionGroups[1] = new List<string[]>();
            sessionGroups[2] = new List<string[]>();

            foreach (string[] row in validDataLines)
            {
                int sNum = GetSessionNumber(GetString(row, sessIdx));
                if (!sessionGroups.ContainsKey(sNum))
                {
                    sessionGroups[sNum] = new List<string[]>();
                }
                sessionGroups[sNum].Add(row);
            }

            foreach (KeyValuePair<int, List<string[]>> kvp in sessionGroups)
            {
                int sessionNum = kvp.Key;
                List<string[]> rows = kvp.Value;

                // Base time for session in minutes of day (e.g. 12:30 = 750, 14:00 = 840)
                double sessionBaseMin = double.MaxValue;

                foreach (string[] row in rows)
                {
                    string arrStr = GetString(row, arrIdx);
                    double parsedMin;
                    if (TryParseTimeMinutes(arrStr, sessionNum, out parsedMin))
                    {
                        if (parsedMin < sessionBaseMin)
                        {
                            sessionBaseMin = parsedMin;
                        }
                    }
                }

                if (sessionBaseMin == double.MaxValue)
                {
                    sessionBaseMin = (sessionNum == 1) ? (12 * 60 + 30) : (14 * 60);
                }

                foreach (string[] row in rows)
                {
                    CustomerRecord rec = new CustomerRecord();
                    int cid;
                    rec.CustomerId = int.TryParse(GetString(row, idIdx), out cid) ? cid : 0;
                    rec.SessionLabel = GetString(row, sessIdx);
                    rec.SessionNumber = sessionNum;

                    rec.RawArrivalTime = GetString(row, arrIdx);
                    rec.RawServiceTime = GetString(row, servIdx);
                    rec.RawServiceStart = GetString(row, startIdx);
                    rec.RawServiceEnd = GetString(row, endIdx);
                    rec.RawWaitTime = GetString(row, waitIdx);
                    rec.Status = GetString(row, statusIdx);

                    // Convert Arrival Time
                    double arrM;
                    if (TryParseTimeMinutes(rec.RawArrivalTime, sessionNum, out arrM))
                    {
                        rec.ArrivalMin = arrM - sessionBaseMin;
                    }
                    else
                    {
                        rec.IsExcluded = true;
                        rec.ExclusionReason = "Invalid arrival time format '" + rec.RawArrivalTime + "'.";
                    }

                    // Convert Service Start/End
                    double startM;
                    if (TryParseTimeMinutes(rec.RawServiceStart, sessionNum, out startM))
                    {
                        rec.ServiceStartMin = startM - sessionBaseMin;
                    }
                    double endM;
                    if (TryParseTimeMinutes(rec.RawServiceEnd, sessionNum, out endM))
                    {
                        rec.ServiceEndMin = endM - sessionBaseMin;
                    }

                    // Recorded Wait Time
                    double wt;
                    if (double.TryParse(rec.RawWaitTime, NumberStyles.Any, CultureInfo.InvariantCulture, out wt))
                    {
                        rec.RecordedWaitMin = wt;
                    }

                    // Service Time
                    double st;
                    if (double.TryParse(rec.RawServiceTime, NumberStyles.Any, CultureInfo.InvariantCulture, out st))
                    {
                        rec.ServiceTimeMin = st;
                    }
                    else
                    {
                        if (!rec.IsBalker)
                        {
                            rec.IsExcluded = true;
                            rec.ExclusionReason = "Missing service time for customer marked as served.";
                        }
                    }

                    CheckAnomalies(rec);
                    records.Add(rec);
                }
            }

            return records;
        }

        private static void CheckAnomalies(CustomerRecord rec)
        {
            if (rec.IsBalker) return;

            // Check 1: Negative service time
            if (rec.ServiceTimeMin < 0)
            {
                rec.IsExcluded = true;
                rec.ExclusionReason = (rec.ExclusionReason != null ? rec.ExclusionReason + "; " : "") +
                    "Negative service time (" + rec.ServiceTimeMin + " min).";
            }

            // Check 2: Service end before service start
            if (rec.ServiceStartMin >= 0 && rec.ServiceEndMin >= 0 && rec.ServiceEndMin < rec.ServiceStartMin)
            {
                rec.IsExcluded = true;
                rec.ExclusionReason = (rec.ExclusionReason != null ? rec.ExclusionReason + "; " : "") +
                    "Service end time (" + rec.RawServiceEnd + ") is before start time (" + rec.RawServiceStart + ").";
            }

            // Warning 1: Service time is zero
            if (rec.ServiceTimeMin == 0 && !rec.IsExcluded)
            {
                rec.HasWarning = true;
                rec.WarningReason = (rec.WarningReason != null ? rec.WarningReason + "; " : "") +
                    "Zero service time recorded.";
            }

            // Warning 2: Extreme outlier service time (> 45 min)
            if (rec.ServiceTimeMin > 45)
            {
                rec.HasWarning = true;
                rec.WarningReason = (rec.WarningReason != null ? rec.WarningReason + "; " : "") +
                    "Extremely long service time (" + rec.ServiceTimeMin + " min), likely order issue or data outlier.";
            }
        }

        private static int FindHeaderIndex(string[] headers, params string[] candidates)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                for (int c = 0; c < candidates.Length; c++)
                {
                    string cand = candidates[c];
                    if (headers[i].Equals(cand, StringComparison.OrdinalIgnoreCase) ||
                        headers[i].Replace(" ", "").Equals(cand.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private static string GetString(string[] parts, int index)
        {
            if (index >= 0 && index < parts.Length) return parts[index];
            return "";
        }

        private static int GetSessionNumber(string sessionLabel)
        {
            if (sessionLabel.IndexOf("Session 1", StringComparison.OrdinalIgnoreCase) >= 0) return 1;
            if (sessionLabel.IndexOf("Session 2", StringComparison.OrdinalIgnoreCase) >= 0) return 2;
            return 1;
        }

        /// <summary>
        /// Converts time string (e.g., "12:30", "1:05", "2:04", "13:21") to total minutes from midnight.
        /// </summary>
        public static bool TryParseTimeMinutes(string timeStr, int sessionNum, out double totalMinutes)
        {
            totalMinutes = 0;
            if (string.IsNullOrEmpty(timeStr) || timeStr.Trim() == "-") return false;

            timeStr = timeStr.Trim();
            string[] parts = timeStr.Split(':');
            if (parts.Length < 2) return false;

            int hours, mins;
            if (!int.TryParse(parts[0], out hours)) return false;
            if (!int.TryParse(parts[1], out mins)) return false;

            // Session 1 is 12:30 PM to 1:38 PM.
            // If hours is 1 (e.g. 1:05), it is 13:05.
            if (sessionNum == 1)
            {
                if (hours >= 1 && hours < 12)
                {
                    hours += 12;
                }
            }
            // Session 2 is 2:00 PM to 3:02 PM.
            // If hours is 2 or 3, it is 14:00 / 15:00.
            else if (sessionNum == 2)
            {
                if (hours < 12)
                {
                    hours += 12;
                }
            }

            totalMinutes = hours * 60 + mins;
            return true;
        }
    }
}
