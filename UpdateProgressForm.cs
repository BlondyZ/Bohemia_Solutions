using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Bohemia_Solutions
{
    internal partial class UpdateProgressForm : Form
    {
        private readonly ProgressBar bar = new() { Dock = DockStyle.Top, Height = 22, Style = ProgressBarStyle.Continuous };
        private readonly Label lbl = new() { Dock = DockStyle.Top, Height = 22, Text = "Preparing..." };
        public UpdateProgressForm()
        {
            Text = "Updating…";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent; // nebo CenterScreen
            ControlBox = false;
            ClientSize = new System.Drawing.Size(420, 90);
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };
            panel.Controls.AddRange(new Control[] { bar, lbl });
            Controls.Add(panel);
        }

        public void Report(int percent, string message)
        {
            if (percent < 0) percent = 0; if (percent > 100) percent = 100;
            if (InvokeRequired) { BeginInvoke(new Action(() => Report(percent, message))); return; }
            bar.Value = percent;
            lbl.Text = message;
        }

        private void UpdateProgressForm_Load(object sender, EventArgs e)
        {

        }
    }
}
