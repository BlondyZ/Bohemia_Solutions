namespace Bohemia_Solutions
{
    partial class SpConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SpConfigForm));
            txtNameSP = new TextBox();
            label1 = new Label();
            panel1 = new Panel();
            btnCancelSP = new Button();
            btnSaveSpConfig = new Button();
            lblWorkshopStatusSP = new Label();
            clbWorkshopModsSP = new CheckedListBox();
            label21 = new Label();
            clbClientFlagsSp = new CheckedListBox();
            btnEditClientParamsSP = new Button();
            btnOpenInitFolderSP = new Button();
            btnEditInitSP = new Button();
            btnBackupInitSP = new Button();
            label7 = new Label();
            btnOpenStorageSP = new Button();
            btnWipeStorageSP = new Button();
            btnBackupStorageSP = new Button();
            label12 = new Label();
            label17 = new Label();
            cb_chooseSP = new ComboBox();
            label5 = new Label();
            btnBrowseProfilesSP = new Button();
            txtProfilesFolder = new TextBox();
            cmbExeName = new ComboBox();
            label10 = new Label();
            cmbVersionSP = new ComboBox();
            label3 = new Label();
            cmbTypeSP = new ComboBox();
            label2 = new Label();
            tb_ingame_name = new TextBox();
            label4 = new Label();
            panel1.SuspendLayout();
            SuspendLayout();
            // 
            // txtNameSP
            // 
            txtNameSP.Location = new Point(101, 24);
            txtNameSP.Name = "txtNameSP";
            txtNameSP.Size = new Size(503, 23);
            txtNameSP.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(56, 27);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 3;
            label1.Text = "Name:";
            // 
            // panel1
            // 
            panel1.Controls.Add(label4);
            panel1.Controls.Add(tb_ingame_name);
            panel1.Controls.Add(btnCancelSP);
            panel1.Controls.Add(btnSaveSpConfig);
            panel1.Controls.Add(lblWorkshopStatusSP);
            panel1.Controls.Add(clbWorkshopModsSP);
            panel1.Controls.Add(label21);
            panel1.Controls.Add(clbClientFlagsSp);
            panel1.Controls.Add(btnEditClientParamsSP);
            panel1.Controls.Add(btnOpenInitFolderSP);
            panel1.Controls.Add(btnEditInitSP);
            panel1.Controls.Add(btnBackupInitSP);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(btnOpenStorageSP);
            panel1.Controls.Add(btnWipeStorageSP);
            panel1.Controls.Add(btnBackupStorageSP);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(label17);
            panel1.Controls.Add(cb_chooseSP);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(btnBrowseProfilesSP);
            panel1.Controls.Add(txtProfilesFolder);
            panel1.Controls.Add(cmbExeName);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(cmbVersionSP);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(cmbTypeSP);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(txtNameSP);
            panel1.Controls.Add(label1);
            panel1.Location = new Point(12, 33);
            panel1.Name = "panel1";
            panel1.Size = new Size(1146, 701);
            panel1.TabIndex = 4;
            // 
            // btnCancelSP
            // 
            btnCancelSP.Location = new Point(810, 666);
            btnCancelSP.Name = "btnCancelSP";
            btnCancelSP.Size = new Size(75, 23);
            btnCancelSP.TabIndex = 58;
            btnCancelSP.Text = "Cancel";
            btnCancelSP.UseVisualStyleBackColor = true;
            // 
            // btnSaveSpConfig
            // 
            btnSaveSpConfig.Location = new Point(891, 666);
            btnSaveSpConfig.Name = "btnSaveSpConfig";
            btnSaveSpConfig.Size = new Size(238, 23);
            btnSaveSpConfig.TabIndex = 57;
            btnSaveSpConfig.Text = "Save And Check Mods Dependencies";
            btnSaveSpConfig.UseVisualStyleBackColor = true;
            btnSaveSpConfig.Click += btnSaveSpConfig_Click;
            // 
            // lblWorkshopStatusSP
            // 
            lblWorkshopStatusSP.AutoSize = true;
            lblWorkshopStatusSP.Location = new Point(657, 258);
            lblWorkshopStatusSP.Name = "lblWorkshopStatusSP";
            lblWorkshopStatusSP.Size = new Size(79, 15);
            lblWorkshopStatusSP.TabIndex = 56;
            lblWorkshopStatusSP.Text = "........................";
            // 
            // clbWorkshopModsSP
            // 
            clbWorkshopModsSP.FormattingEnabled = true;
            clbWorkshopModsSP.Location = new Point(657, 277);
            clbWorkshopModsSP.Name = "clbWorkshopModsSP";
            clbWorkshopModsSP.Size = new Size(472, 346);
            clbWorkshopModsSP.TabIndex = 55;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(657, 6);
            label21.Name = "label21";
            label21.Size = new Size(103, 15);
            label21.TabIndex = 53;
            label21.Text = "Client parameters:";
            // 
            // clbClientFlagsSp
            // 
            clbClientFlagsSp.CheckOnClick = true;
            clbClientFlagsSp.FormattingEnabled = true;
            clbClientFlagsSp.IntegralHeight = false;
            clbClientFlagsSp.Items.AddRange(new object[] { "-window", "-nopause", "-disableCrashReport", "-nolauncher" });
            clbClientFlagsSp.Location = new Point(657, 24);
            clbClientFlagsSp.Name = "clbClientFlagsSp";
            clbClientFlagsSp.Size = new Size(472, 220);
            clbClientFlagsSp.TabIndex = 52;
            // 
            // btnEditClientParamsSP
            // 
            btnEditClientParamsSP.Location = new Point(766, 3);
            btnEditClientParamsSP.Name = "btnEditClientParamsSP";
            btnEditClientParamsSP.Size = new Size(60, 23);
            btnEditClientParamsSP.TabIndex = 54;
            btnEditClientParamsSP.Text = "Edit...";
            btnEditClientParamsSP.UseVisualStyleBackColor = true;
            // 
            // btnOpenInitFolderSP
            // 
            btnOpenInitFolderSP.BackgroundImage = (Image)resources.GetObject("btnOpenInitFolderSP.BackgroundImage");
            btnOpenInitFolderSP.BackgroundImageLayout = ImageLayout.Stretch;
            btnOpenInitFolderSP.Location = new Point(213, 240);
            btnOpenInitFolderSP.Name = "btnOpenInitFolderSP";
            btnOpenInitFolderSP.Size = new Size(29, 23);
            btnOpenInitFolderSP.TabIndex = 51;
            btnOpenInitFolderSP.UseVisualStyleBackColor = true;
            // 
            // btnEditInitSP
            // 
            btnEditInitSP.BackgroundImage = (Image)resources.GetObject("btnEditInitSP.BackgroundImage");
            btnEditInitSP.BackgroundImageLayout = ImageLayout.Stretch;
            btnEditInitSP.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnEditInitSP.Location = new Point(242, 240);
            btnEditInitSP.Name = "btnEditInitSP";
            btnEditInitSP.Size = new Size(29, 23);
            btnEditInitSP.TabIndex = 50;
            btnEditInitSP.UseVisualStyleBackColor = true;
            // 
            // btnBackupInitSP
            // 
            btnBackupInitSP.Location = new Point(101, 239);
            btnBackupInitSP.Name = "btnBackupInitSP";
            btnBackupInitSP.Size = new Size(111, 23);
            btnBackupInitSP.TabIndex = 49;
            btnBackupInitSP.Text = "Backup init";
            btnBackupInitSP.UseVisualStyleBackColor = true;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(47, 243);
            label7.Name = "label7";
            label7.Size = new Size(48, 15);
            label7.TabIndex = 48;
            label7.Text = "Init File:";
            // 
            // btnOpenStorageSP
            // 
            btnOpenStorageSP.BackgroundImage = (Image)resources.GetObject("btnOpenStorageSP.BackgroundImage");
            btnOpenStorageSP.BackgroundImageLayout = ImageLayout.Stretch;
            btnOpenStorageSP.Location = new Point(213, 210);
            btnOpenStorageSP.Name = "btnOpenStorageSP";
            btnOpenStorageSP.Size = new Size(29, 23);
            btnOpenStorageSP.TabIndex = 47;
            btnOpenStorageSP.UseVisualStyleBackColor = true;
            // 
            // btnWipeStorageSP
            // 
            btnWipeStorageSP.Location = new Point(242, 210);
            btnWipeStorageSP.Name = "btnWipeStorageSP";
            btnWipeStorageSP.Size = new Size(111, 23);
            btnWipeStorageSP.TabIndex = 46;
            btnWipeStorageSP.Text = "Wipe storage";
            btnWipeStorageSP.UseVisualStyleBackColor = true;
            btnWipeStorageSP.Click += btnWipeStorageSP_Click;
            // 
            // btnBackupStorageSP
            // 
            btnBackupStorageSP.Location = new Point(101, 210);
            btnBackupStorageSP.Name = "btnBackupStorageSP";
            btnBackupStorageSP.Size = new Size(111, 23);
            btnBackupStorageSP.TabIndex = 45;
            btnBackupStorageSP.Text = "Backup storage";
            btnBackupStorageSP.UseVisualStyleBackColor = true;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(45, 214);
            label12.Name = "label12";
            label12.Size = new Size(50, 15);
            label12.TabIndex = 44;
            label12.Text = "Storage:";
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(25, 184);
            label17.Name = "label17";
            label17.Size = new Size(70, 15);
            label17.TabIndex = 36;
            label17.Text = "SP mission :";
            // 
            // cb_chooseSP
            // 
            cb_chooseSP.FormattingEnabled = true;
            cb_chooseSP.Location = new Point(101, 181);
            cb_chooseSP.Name = "cb_chooseSP";
            cb_chooseSP.Size = new Size(247, 23);
            cb_chooseSP.TabIndex = 35;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(10, 117);
            label5.Name = "label5";
            label5.Size = new Size(85, 15);
            label5.TabIndex = 27;
            label5.Text = "Profiles Folder:";
            // 
            // btnBrowseProfilesSP
            // 
            btnBrowseProfilesSP.Location = new Point(622, 113);
            btnBrowseProfilesSP.Name = "btnBrowseProfilesSP";
            btnBrowseProfilesSP.Size = new Size(29, 23);
            btnBrowseProfilesSP.TabIndex = 29;
            btnBrowseProfilesSP.Text = "...";
            btnBrowseProfilesSP.UseVisualStyleBackColor = true;
            // 
            // txtProfilesFolder
            // 
            txtProfilesFolder.Location = new Point(101, 113);
            txtProfilesFolder.Name = "txtProfilesFolder";
            txtProfilesFolder.ReadOnly = true;
            txtProfilesFolder.Size = new Size(503, 23);
            txtProfilesFolder.TabIndex = 28;
            // 
            // cmbExeName
            // 
            cmbExeName.FormattingEnabled = true;
            cmbExeName.Location = new Point(101, 84);
            cmbExeName.Name = "cmbExeName";
            cmbExeName.Size = new Size(232, 23);
            cmbExeName.TabIndex = 26;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(33, 87);
            label10.Name = "label10";
            label10.Size = new Size(62, 15);
            label10.TabIndex = 25;
            label10.Text = "Exe Name:";
            // 
            // cmbVersionSP
            // 
            cmbVersionSP.FormattingEnabled = true;
            cmbVersionSP.Location = new Point(101, 55);
            cmbVersionSP.Name = "cmbVersionSP";
            cmbVersionSP.Size = new Size(232, 23);
            cmbVersionSP.TabIndex = 6;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(50, 58);
            label3.Name = "label3";
            label3.Size = new Size(48, 15);
            label3.TabIndex = 7;
            label3.Text = "Version:";
            // 
            // cmbTypeSP
            // 
            cmbTypeSP.FormattingEnabled = true;
            cmbTypeSP.Location = new Point(377, 55);
            cmbTypeSP.Name = "cmbTypeSP";
            cmbTypeSP.Size = new Size(121, 23);
            cmbTypeSP.TabIndex = 4;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(339, 58);
            label2.Name = "label2";
            label2.Size = new Size(35, 15);
            label2.TabIndex = 5;
            label2.Text = "Type:";
            // 
            // tb_ingame_name
            // 
            tb_ingame_name.Location = new Point(101, 145);
            tb_ingame_name.Name = "tb_ingame_name";
            tb_ingame_name.Size = new Size(119, 23);
            tb_ingame_name.TabIndex = 59;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(5, 148);
            label4.Name = "label4";
            label4.Size = new Size(90, 15);
            label4.TabIndex = 60;
            label4.Text = "In-game Name:";
            // 
            // SpConfigForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1196, 892);
            Controls.Add(panel1);
            Name = "SpConfigForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SpConfigForm";
            Load += SpConfigForm_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TextBox txtNameSP;
        private Label label1;
        private Panel panel1;
        private ComboBox cmbTypeSP;
        private Label label2;
        private ComboBox cmbVersionSP;
        private Label label3;
        private ComboBox cmbExeName;
        private Label label10;
        private Label label5;
        private Button btnBrowseProfilesSP;
        private TextBox txtProfilesFolder;
        private Label label17;
        private ComboBox cb_chooseSP;
        private Button btnOpenStorageSP;
        private Button btnWipeStorageSP;
        private Button btnBackupStorageSP;
        private Label label12;
        private Button btnOpenInitFolderSP;
        private Button btnEditInitSP;
        private Button btnBackupInitSP;
        private Label label7;
        private Label label21;
        private CheckedListBox clbClientFlagsSp;
        private Button btnEditClientParamsSP;
        private Label lblWorkshopStatusSP;
        private CheckedListBox clbWorkshopModsSP;
        private Button btnCancelSP;
        private Button btnSaveSpConfig;
        private Label label4;
        private TextBox tb_ingame_name;
    }
}