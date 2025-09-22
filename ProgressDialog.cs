using System;
using System.Drawing;
using System.Windows.Forms;

namespace Bohemia_Solutions
{
    internal sealed class ProgressDialog : Form
    {
        private readonly Label lbl;
        private readonly ProgressBar bar;

        public ProgressDialog()
        {
            Text = "Extraction in progress…";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            ShowIcon = false;
            Width = 420;
            Height = 140;

            lbl = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12, 10, 12, 0),
                AutoSize = false
            };

            bar = new ProgressBar
            {
                Dock = DockStyle.Bottom,
                Height = 22,
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            Controls.Add(lbl);
            Controls.Add(bar);
        }

        public void SetStatus(string text)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetStatus), text); return; }
            lbl.Text = text;
        }

        // Přepne progress bar do „určité“ podoby a nastaví procenta (0–100)
        public void SetProgress(int percent)
        {
            if (InvokeRequired) { BeginInvoke(new Action<int>(SetProgress), percent); return; }

            if (bar.Style != ProgressBarStyle.Blocks)
            {
                bar.Style = ProgressBarStyle.Blocks;
                bar.MarqueeAnimationSpeed = 0;
                bar.Minimum = 0;
                bar.Maximum = 100;
                bar.Value = 0;
            }

            percent = Math.Max(bar.Minimum, Math.Min(bar.Maximum, percent));
            bar.Value = percent;
        }

        public void Done(string finalText = null)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Done), finalText); return; }
            if (!string.IsNullOrWhiteSpace(finalText)) lbl.Text = finalText;
            SetProgress(100);
        }
    }
}
