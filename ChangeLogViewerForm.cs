using System;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Bohemia_Solutions.Models;

namespace Bohemia_Solutions
{
    public partial class ChangeLogViewerForm : Form
    {
        private readonly ChangeLog _log;

        public ChangeLogViewerForm(ChangeLog log)
        {
            InitializeComponent();
            _log = log;
            Render();
        }

        private void Render()
        {
            // jednoduché RichText zobrazení
            var sb = new StringBuilder();
            foreach (var v in _log.Versions.OrderByDescending(v => v.Date))
            {
                sb.AppendLine($"{v.Version}  —  {v.Date:yyyy-MM-dd}");
                foreach (var it in v.Items)
                    sb.AppendLine($" • [{it.Type}] - {it.Text}");
                sb.AppendLine();
            }
            rtbOut.ReadOnly = true;
            rtbOut.Text = sb.ToString();
        }
    }
}
