using System;
using System.Collections.Generic;

namespace Bohemia_Solutions.Models
{
    public sealed class ChangeLog
    {
        public List<ChangeLogVersion> Versions { get; set; } = new();
    }

    public sealed class ChangeLogVersion
    {
        public string Version { get; set; } = "";            // např. "3.0.1"
        public DateTime Date { get; set; }                    // datum vydání
        public List<ChangeItem> Items { get; set; } = new();  // položky
    }

    public enum ChangeType { ADDED, CHANGED, FIXED, REMOVED, IMPROVED, STABILITY }

    public sealed class ChangeItem
    {
        public ChangeType Type { get; set; }
        public string Text { get; set; } = "";
    }
}
