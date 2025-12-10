using System;
using System.Collections.Generic;

namespace project.Services
{
    public class SyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public DateTime SyncTime { get; set; } = DateTime.Now;
        public Dictionary<string, TableSyncSummary> TableSummaries { get; set; } = new Dictionary<string, TableSyncSummary>();
        public int TotalUploaded { get; set; }
        public int TotalDownloaded { get; set; }
        public int TotalUpdated { get; set; }
        public int TotalConflicts { get; set; }
    }

    public class TableSyncSummary
    {
        public string TableName { get; set; } = "";
        public int Uploaded { get; set; }
        public int Downloaded { get; set; }
        public int Updated { get; set; }
        public int Conflicts { get; set; }
    }
}














