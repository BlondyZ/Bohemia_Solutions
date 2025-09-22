namespace Bohemia_Solutions
{
    partial class ChangeLogEditorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox lstVersions;
        private System.Windows.Forms.TextBox txtVersion;
        private System.Windows.Forms.DateTimePicker dtpDate;
        private System.Windows.Forms.DataGridView gridItems;
        private System.Windows.Forms.Button btnAddVersion;
        private System.Windows.Forms.Button btnDeleteVersion;
        private System.Windows.Forms.Button btnSave;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lstVersions = new ListBox();
            txtVersion = new TextBox();
            dtpDate = new DateTimePicker();
            gridItems = new DataGridView();
            colType = new DataGridViewComboBoxColumn();
            colText = new DataGridViewTextBoxColumn();
            btnAddVersion = new Button();
            btnDeleteVersion = new Button();
            btnSave = new Button();
            ((System.ComponentModel.ISupportInitialize)gridItems).BeginInit();
            SuspendLayout();
            // 
            // lstVersions
            // 
            lstVersions.ItemHeight = 15;
            lstVersions.Location = new Point(12, 12);
            lstVersions.Name = "lstVersions";
            lstVersions.Size = new Size(160, 394);
            lstVersions.TabIndex = 0;
            // 
            // txtVersion
            // 
            txtVersion.Location = new Point(188, 12);
            txtVersion.Name = "txtVersion";
            txtVersion.Size = new Size(366, 23);
            txtVersion.TabIndex = 1;
            // 
            // dtpDate
            // 
            dtpDate.Location = new Point(560, 12);
            dtpDate.Name = "dtpDate";
            dtpDate.Size = new Size(200, 23);
            dtpDate.TabIndex = 2;
            // 
            // gridItems
            // 
            gridItems.Columns.AddRange(new DataGridViewColumn[] { colType, colText });
            gridItems.EditMode = DataGridViewEditMode.EditOnEnter;
            gridItems.Location = new Point(188, 48);
            gridItems.Name = "gridItems";
            gridItems.Size = new Size(572, 330);
            gridItems.TabIndex = 3;
            // 
            // colType
            // 
            colType.Name = "colType";
            // 
            // colText
            // 
            colText.Name = "colText";
            // 
            // btnAddVersion
            // 
            btnAddVersion.Location = new Point(12, 414);
            btnAddVersion.Name = "btnAddVersion";
            btnAddVersion.Size = new Size(75, 23);
            btnAddVersion.TabIndex = 4;
            btnAddVersion.Text = "Add";
            // 
            // btnDeleteVersion
            // 
            btnDeleteVersion.Location = new Point(110, 414);
            btnDeleteVersion.Name = "btnDeleteVersion";
            btnDeleteVersion.Size = new Size(75, 23);
            btnDeleteVersion.TabIndex = 5;
            btnDeleteVersion.Text = "Delete";
            // 
            // btnSave
            // 
            btnSave.Location = new Point(685, 414);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            // 
            // ChangeLogEditorForm
            // 
            ClientSize = new Size(772, 450);
            Controls.Add(lstVersions);
            Controls.Add(txtVersion);
            Controls.Add(dtpDate);
            Controls.Add(gridItems);
            Controls.Add(btnAddVersion);
            Controls.Add(btnDeleteVersion);
            Controls.Add(btnSave);
            Name = "ChangeLogEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Edit Changelog";
            ((System.ComponentModel.ISupportInitialize)gridItems).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }
        private DataGridViewComboBoxColumn colType;
        private DataGridViewTextBoxColumn colText;
    }
}
