namespace Bohemia_Solutions
{
    partial class ConfigForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigForm));
            txtName = new TextBox();
            label1 = new Label();
            cmbType = new ComboBox();
            label2 = new Label();
            cmbVersion = new ComboBox();
            label3 = new Label();
            label4 = new Label();
            txtServerConfigFile = new TextBox();
            btnBrowseServerConfig = new Button();
            label5 = new Label();
            txtProfilesFolder = new TextBox();
            btnBrowseProfiles = new Button();
            numPort = new NumericUpDown();
            label6 = new Label();
            panel1 = new Panel();
            btnBrowseStorage = new Button();
            btnBrowseInit = new Button();
            btnOpenInitFile = new Button();
            btnBackupInit = new Button();
            label7 = new Label();
            label21 = new Label();
            label20 = new Label();
            clbClientFlags = new CheckedListBox();
            label17 = new Label();
            cb_chooseMP = new ComboBox();
            pnlServerConfig = new Panel();
            label19 = new Label();
            cb_cfg_enviromenttype = new ComboBox();
            label18 = new Label();
            tb_cfg_shard_id = new TextBox();
            label16 = new Label();
            cb_cfg_mpmission = new ComboBox();
            nb_cfg_battleye = new NumericUpDown();
            label15 = new Label();
            nb_cfg_max_players = new NumericUpDown();
            label14 = new Label();
            label13 = new Label();
            tb_cfg_password = new TextBox();
            label8 = new Label();
            tb_cfg_hostname = new TextBox();
            lbl_cfg_title = new Label();
            btnWipeStorage = new Button();
            btnBackupStorage = new Button();
            label12 = new Label();
            lblWorkshopStatus = new Label();
            btnCancel = new Button();
            btnOK = new Button();
            cmbExeName = new ComboBox();
            numCpuCount = new NumericUpDown();
            label11 = new Label();
            label10 = new Label();
            label9 = new Label();
            txtServerName = new TextBox();
            btnOpenConfigFile = new Button();
            clbWorkshopMods = new CheckedListBox();
            clbServerFlags = new CheckedListBox();
            btnEditServerParams = new Button();
            btnEditClientParams = new Button();
            ((System.ComponentModel.ISupportInitialize)numPort).BeginInit();
            panel1.SuspendLayout();
            pnlServerConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nb_cfg_battleye).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nb_cfg_max_players).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCpuCount).BeginInit();
            SuspendLayout();
            // 
            // txtName
            // 
            txtName.Location = new Point(58, 20);
            txtName.Name = "txtName";
            txtName.Size = new Size(503, 23);
            txtName.TabIndex = 0;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(13, 23);
            label1.Name = "label1";
            label1.Size = new Size(42, 15);
            label1.TabIndex = 1;
            label1.Text = "Name:";
            // 
            // cmbType
            // 
            cmbType.FormattingEnabled = true;
            cmbType.Location = new Point(228, 53);
            cmbType.Name = "cmbType";
            cmbType.Size = new Size(121, 23);
            cmbType.TabIndex = 2;
            cmbType.SelectedIndexChanged += cmbType_SelectedIndexChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(190, 56);
            label2.Name = "label2";
            label2.Size = new Size(35, 15);
            label2.TabIndex = 3;
            label2.Text = "Type:";
            // 
            // cmbVersion
            // 
            cmbVersion.FormattingEnabled = true;
            cmbVersion.Location = new Point(59, 54);
            cmbVersion.Name = "cmbVersion";
            cmbVersion.Size = new Size(121, 23);
            cmbVersion.TabIndex = 4;
            cmbVersion.SelectedIndexChanged += cmbVersion_SelectedIndexChanged;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(8, 57);
            label3.Name = "label3";
            label3.Size = new Size(48, 15);
            label3.TabIndex = 5;
            label3.Text = "Version:";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(10, 223);
            label4.Name = "label4";
            label4.Size = new Size(102, 15);
            label4.TabIndex = 6;
            label4.Text = "Server Config File:";
            // 
            // txtServerConfigFile
            // 
            txtServerConfigFile.Location = new Point(118, 219);
            txtServerConfigFile.Name = "txtServerConfigFile";
            txtServerConfigFile.Size = new Size(444, 23);
            txtServerConfigFile.TabIndex = 7;
            txtServerConfigFile.TextChanged += txtServerConfigFile_TextChanged;
            // 
            // btnBrowseServerConfig
            // 
            btnBrowseServerConfig.Location = new Point(564, 219);
            btnBrowseServerConfig.Name = "btnBrowseServerConfig";
            btnBrowseServerConfig.Size = new Size(29, 23);
            btnBrowseServerConfig.TabIndex = 8;
            btnBrowseServerConfig.Text = "...";
            btnBrowseServerConfig.UseVisualStyleBackColor = true;
            btnBrowseServerConfig.Click += btnBrowseServerConfig_Click;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(28, 252);
            label5.Name = "label5";
            label5.Size = new Size(85, 15);
            label5.TabIndex = 9;
            label5.Text = "Profiles Folder:";
            // 
            // txtProfilesFolder
            // 
            txtProfilesFolder.Location = new Point(119, 248);
            txtProfilesFolder.Name = "txtProfilesFolder";
            txtProfilesFolder.Size = new Size(444, 23);
            txtProfilesFolder.TabIndex = 10;
            // 
            // btnBrowseProfiles
            // 
            btnBrowseProfiles.Location = new Point(564, 248);
            btnBrowseProfiles.Name = "btnBrowseProfiles";
            btnBrowseProfiles.Size = new Size(29, 23);
            btnBrowseProfiles.TabIndex = 11;
            btnBrowseProfiles.Text = "...";
            btnBrowseProfiles.UseVisualStyleBackColor = true;
            btnBrowseProfiles.Click += btnBrowseProfiles_Click;
            // 
            // numPort
            // 
            numPort.Location = new Point(396, 54);
            numPort.Maximum = new decimal(new int[] { 9999999, 0, 0, 0 });
            numPort.Name = "numPort";
            numPort.Size = new Size(120, 23);
            numPort.TabIndex = 12;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(358, 56);
            label6.Name = "label6";
            label6.Size = new Size(32, 15);
            label6.TabIndex = 13;
            label6.Text = "Port:";
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.Fixed3D;
            panel1.Controls.Add(btnBrowseStorage);
            panel1.Controls.Add(btnBrowseInit);
            panel1.Controls.Add(btnOpenInitFile);
            panel1.Controls.Add(btnBackupInit);
            panel1.Controls.Add(label7);
            panel1.Controls.Add(label21);
            panel1.Controls.Add(label20);
            panel1.Controls.Add(clbClientFlags);
            panel1.Controls.Add(label17);
            panel1.Controls.Add(cb_chooseMP);
            panel1.Controls.Add(pnlServerConfig);
            panel1.Controls.Add(btnWipeStorage);
            panel1.Controls.Add(btnBackupStorage);
            panel1.Controls.Add(label12);
            panel1.Controls.Add(lblWorkshopStatus);
            panel1.Controls.Add(btnCancel);
            panel1.Controls.Add(btnOK);
            panel1.Controls.Add(cmbExeName);
            panel1.Controls.Add(numCpuCount);
            panel1.Controls.Add(label11);
            panel1.Controls.Add(label10);
            panel1.Controls.Add(label9);
            panel1.Controls.Add(txtServerName);
            panel1.Controls.Add(btnOpenConfigFile);
            panel1.Controls.Add(clbWorkshopMods);
            panel1.Controls.Add(clbServerFlags);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(cmbVersion);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(label4);
            panel1.Controls.Add(numPort);
            panel1.Controls.Add(txtName);
            panel1.Controls.Add(btnBrowseProfiles);
            panel1.Controls.Add(txtProfilesFolder);
            panel1.Controls.Add(label1);
            panel1.Controls.Add(cmbType);
            panel1.Controls.Add(btnBrowseServerConfig);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(txtServerConfigFile);
            panel1.Controls.Add(label3);
            panel1.Controls.Add(btnEditServerParams);
            panel1.Controls.Add(btnEditClientParams);
            panel1.Location = new Point(12, 33);
            panel1.Name = "panel1";
            panel1.Size = new Size(1146, 701);
            panel1.TabIndex = 14;
            panel1.Paint += panel1_Paint;
            // 
            // btnBrowseStorage
            // 
            btnBrowseStorage.BackgroundImage = (Image)resources.GetObject("btnBrowseStorage.BackgroundImage");
            btnBrowseStorage.BackgroundImageLayout = ImageLayout.Stretch;
            btnBrowseStorage.Location = new Point(229, 308);
            btnBrowseStorage.Name = "btnBrowseStorage";
            btnBrowseStorage.Size = new Size(29, 23);
            btnBrowseStorage.TabIndex = 43;
            btnBrowseStorage.UseVisualStyleBackColor = true;
            btnBrowseStorage.Click += btnBrowseStorage_Click;
            // 
            // btnBrowseInit
            // 
            btnBrowseInit.BackgroundImage = (Image)resources.GetObject("btnBrowseInit.BackgroundImage");
            btnBrowseInit.BackgroundImageLayout = ImageLayout.Stretch;
            btnBrowseInit.Location = new Point(229, 279);
            btnBrowseInit.Name = "btnBrowseInit";
            btnBrowseInit.Size = new Size(29, 23);
            btnBrowseInit.TabIndex = 42;
            btnBrowseInit.UseVisualStyleBackColor = true;
            btnBrowseInit.Click += btnBrowseInit_Click;
            // 
            // btnOpenInitFile
            // 
            btnOpenInitFile.BackgroundImage = (Image)resources.GetObject("btnOpenInitFile.BackgroundImage");
            btnOpenInitFile.BackgroundImageLayout = ImageLayout.Stretch;
            btnOpenInitFile.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnOpenInitFile.Location = new Point(258, 279);
            btnOpenInitFile.Name = "btnOpenInitFile";
            btnOpenInitFile.Size = new Size(29, 23);
            btnOpenInitFile.TabIndex = 41;
            btnOpenInitFile.UseVisualStyleBackColor = true;
            btnOpenInitFile.Click += btnOpenInitFile_Click;
            // 
            // btnBackupInit
            // 
            btnBackupInit.Location = new Point(117, 278);
            btnBackupInit.Name = "btnBackupInit";
            btnBackupInit.Size = new Size(111, 23);
            btnBackupInit.TabIndex = 39;
            btnBackupInit.Text = "Backup init";
            btnBackupInit.UseVisualStyleBackColor = true;
            btnBackupInit.Click += btnBackupInit_Click;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new Point(63, 282);
            label7.Name = "label7";
            label7.Size = new Size(48, 15);
            label7.TabIndex = 38;
            label7.Text = "Init File:";
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.Location = new Point(896, 35);
            label21.Name = "label21";
            label21.Size = new Size(103, 15);
            label21.TabIndex = 37;
            label21.Text = "Client parameters:";
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.Location = new Point(640, 36);
            label20.Name = "label20";
            label20.Size = new Size(104, 15);
            label20.TabIndex = 36;
            label20.Text = "Server parameters:";
            // 
            // clbClientFlags
            // 
            clbClientFlags.CheckOnClick = true;
            clbClientFlags.FormattingEnabled = true;
            clbClientFlags.IntegralHeight = false;
            clbClientFlags.Items.AddRange(new object[] { "-window", "-nopause", "-disableCrashReport", "-nolauncher" });
            clbClientFlags.Location = new Point(896, 53);
            clbClientFlags.Name = "clbClientFlags";
            clbClientFlags.Size = new Size(243, 220);
            clbClientFlags.TabIndex = 35;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.Location = new Point(37, 193);
            label17.Name = "label17";
            label17.Size = new Size(75, 15);
            label17.TabIndex = 34;
            label17.Text = "MP mission :";
            // 
            // cb_chooseMP
            // 
            cb_chooseMP.FormattingEnabled = true;
            cb_chooseMP.Location = new Point(118, 190);
            cb_chooseMP.Name = "cb_chooseMP";
            cb_chooseMP.Size = new Size(247, 23);
            cb_chooseMP.TabIndex = 33;
            cb_chooseMP.SelectedIndexChanged += cb_chooseMP_SelectedIndexChanged;
            // 
            // pnlServerConfig
            // 
            pnlServerConfig.BorderStyle = BorderStyle.Fixed3D;
            pnlServerConfig.Controls.Add(label19);
            pnlServerConfig.Controls.Add(cb_cfg_enviromenttype);
            pnlServerConfig.Controls.Add(label18);
            pnlServerConfig.Controls.Add(tb_cfg_shard_id);
            pnlServerConfig.Controls.Add(label16);
            pnlServerConfig.Controls.Add(cb_cfg_mpmission);
            pnlServerConfig.Controls.Add(nb_cfg_battleye);
            pnlServerConfig.Controls.Add(label15);
            pnlServerConfig.Controls.Add(nb_cfg_max_players);
            pnlServerConfig.Controls.Add(label14);
            pnlServerConfig.Controls.Add(label13);
            pnlServerConfig.Controls.Add(tb_cfg_password);
            pnlServerConfig.Controls.Add(label8);
            pnlServerConfig.Controls.Add(tb_cfg_hostname);
            pnlServerConfig.Controls.Add(lbl_cfg_title);
            pnlServerConfig.Location = new Point(13, 380);
            pnlServerConfig.Name = "pnlServerConfig";
            pnlServerConfig.Size = new Size(609, 281);
            pnlServerConfig.TabIndex = 32;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.Location = new Point(17, 193);
            label19.Name = "label19";
            label19.Size = new Size(99, 15);
            label19.TabIndex = 33;
            label19.Text = "Enviroment Type:";
            // 
            // cb_cfg_enviromenttype
            // 
            cb_cfg_enviromenttype.FormattingEnabled = true;
            cb_cfg_enviromenttype.Location = new Point(122, 190);
            cb_cfg_enviromenttype.Name = "cb_cfg_enviromenttype";
            cb_cfg_enviromenttype.Size = new Size(154, 23);
            cb_cfg_enviromenttype.TabIndex = 32;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.Location = new Point(59, 222);
            label18.Name = "label18";
            label18.Size = new Size(57, 15);
            label18.TabIndex = 31;
            label18.Text = "Shard ID :";
            // 
            // tb_cfg_shard_id
            // 
            tb_cfg_shard_id.Location = new Point(122, 219);
            tb_cfg_shard_id.Name = "tb_cfg_shard_id";
            tb_cfg_shard_id.Size = new Size(136, 23);
            tb_cfg_shard_id.TabIndex = 30;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.Location = new Point(41, 164);
            label16.Name = "label16";
            label16.Size = new Size(75, 15);
            label16.TabIndex = 29;
            label16.Text = "MP mission :";
            // 
            // cb_cfg_mpmission
            // 
            cb_cfg_mpmission.FormattingEnabled = true;
            cb_cfg_mpmission.Location = new Point(122, 161);
            cb_cfg_mpmission.Name = "cb_cfg_mpmission";
            cb_cfg_mpmission.Size = new Size(247, 23);
            cb_cfg_mpmission.TabIndex = 28;
            cb_cfg_mpmission.SelectedIndexChanged += cb_cfg_mpmission_SelectedIndexChanged;
            // 
            // nb_cfg_battleye
            // 
            nb_cfg_battleye.Location = new Point(122, 132);
            nb_cfg_battleye.Maximum = new decimal(new int[] { 1, 0, 0, 0 });
            nb_cfg_battleye.Name = "nb_cfg_battleye";
            nb_cfg_battleye.Size = new Size(55, 23);
            nb_cfg_battleye.TabIndex = 27;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new Point(52, 134);
            label15.Name = "label15";
            label15.Size = new Size(64, 15);
            label15.TabIndex = 26;
            label15.Text = "Battle Eye :";
            // 
            // nb_cfg_max_players
            // 
            nb_cfg_max_players.Location = new Point(122, 103);
            nb_cfg_max_players.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nb_cfg_max_players.Name = "nb_cfg_max_players";
            nb_cfg_max_players.Size = new Size(55, 23);
            nb_cfg_max_players.TabIndex = 25;
            nb_cfg_max_players.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new Point(41, 105);
            label14.Name = "label14";
            label14.Size = new Size(75, 15);
            label14.TabIndex = 24;
            label14.Text = "Max players :";
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new Point(53, 72);
            label13.Name = "label13";
            label13.Size = new Size(63, 15);
            label13.TabIndex = 22;
            label13.Text = "Password :";
            // 
            // tb_cfg_password
            // 
            tb_cfg_password.Location = new Point(120, 69);
            tb_cfg_password.Name = "tb_cfg_password";
            tb_cfg_password.Size = new Size(136, 23);
            tb_cfg_password.TabIndex = 21;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.Location = new Point(48, 43);
            label8.Name = "label8";
            label8.Size = new Size(68, 15);
            label8.TabIndex = 20;
            label8.Text = "Hostname :";
            // 
            // tb_cfg_hostname
            // 
            tb_cfg_hostname.Location = new Point(120, 40);
            tb_cfg_hostname.Name = "tb_cfg_hostname";
            tb_cfg_hostname.Size = new Size(413, 23);
            tb_cfg_hostname.TabIndex = 19;
            // 
            // lbl_cfg_title
            // 
            lbl_cfg_title.AutoSize = true;
            lbl_cfg_title.Location = new Point(5, 9);
            lbl_cfg_title.Name = "lbl_cfg_title";
            lbl_cfg_title.Size = new Size(25, 15);
            lbl_cfg_title.TabIndex = 7;
            lbl_cfg_title.Text = "......";
            // 
            // btnWipeStorage
            // 
            btnWipeStorage.Location = new Point(258, 308);
            btnWipeStorage.Name = "btnWipeStorage";
            btnWipeStorage.Size = new Size(111, 23);
            btnWipeStorage.TabIndex = 31;
            btnWipeStorage.Text = "Wipe storage";
            btnWipeStorage.UseVisualStyleBackColor = true;
            btnWipeStorage.Click += btnWipeStorage_Click;
            // 
            // btnBackupStorage
            // 
            btnBackupStorage.Location = new Point(117, 308);
            btnBackupStorage.Name = "btnBackupStorage";
            btnBackupStorage.Size = new Size(111, 23);
            btnBackupStorage.TabIndex = 30;
            btnBackupStorage.Text = "Backup storage";
            btnBackupStorage.UseVisualStyleBackColor = true;
            btnBackupStorage.Click += btnBackupStorage_Click;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new Point(61, 312);
            label12.Name = "label12";
            label12.Size = new Size(50, 15);
            label12.TabIndex = 29;
            label12.Text = "Storage:";
            // 
            // lblWorkshopStatus
            // 
            lblWorkshopStatus.AutoSize = true;
            lblWorkshopStatus.Location = new Point(640, 297);
            lblWorkshopStatus.Name = "lblWorkshopStatus";
            lblWorkshopStatus.Size = new Size(79, 15);
            lblWorkshopStatus.TabIndex = 28;
            lblWorkshopStatus.Text = "........................";
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(820, 671);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 23);
            btnCancel.TabIndex = 26;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(901, 671);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(238, 23);
            btnOK.TabIndex = 25;
            btnOK.Text = "Save And Check Mods Dependencies";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // cmbExeName
            // 
            cmbExeName.FormattingEnabled = true;
            cmbExeName.Location = new Point(117, 126);
            cmbExeName.Name = "cmbExeName";
            cmbExeName.Size = new Size(232, 23);
            cmbExeName.TabIndex = 24;
            cmbExeName.SelectedIndexChanged += cmbExeName_SelectedIndexChanged;
            // 
            // numCpuCount
            // 
            numCpuCount.Location = new Point(454, 129);
            numCpuCount.Maximum = new decimal(new int[] { 16, 0, 0, 0 });
            numCpuCount.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numCpuCount.Name = "numCpuCount";
            numCpuCount.Size = new Size(62, 23);
            numCpuCount.TabIndex = 23;
            numCpuCount.Value = new decimal(new int[] { 2, 0, 0, 0 });
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new Point(371, 133);
            label11.Name = "label11";
            label11.Size = new Size(69, 15);
            label11.TabIndex = 22;
            label11.Text = "CPU Count:";
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new Point(49, 129);
            label10.Name = "label10";
            label10.Size = new Size(62, 15);
            label10.TabIndex = 20;
            label10.Text = "Exe Name:";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.Location = new Point(36, 165);
            label9.Name = "label9";
            label9.Size = new Size(77, 15);
            label9.TabIndex = 18;
            label9.Text = "Server Name:";
            // 
            // txtServerName
            // 
            txtServerName.Location = new Point(117, 162);
            txtServerName.Name = "txtServerName";
            txtServerName.Size = new Size(446, 23);
            txtServerName.TabIndex = 17;
            // 
            // btnOpenConfigFile
            // 
            btnOpenConfigFile.BackgroundImage = (Image)resources.GetObject("btnOpenConfigFile.BackgroundImage");
            btnOpenConfigFile.BackgroundImageLayout = ImageLayout.Stretch;
            btnOpenConfigFile.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnOpenConfigFile.Location = new Point(593, 219);
            btnOpenConfigFile.Name = "btnOpenConfigFile";
            btnOpenConfigFile.Size = new Size(29, 23);
            btnOpenConfigFile.TabIndex = 16;
            btnOpenConfigFile.UseVisualStyleBackColor = true;
            btnOpenConfigFile.Click += btnOpenConfigFile_Click;
            // 
            // clbWorkshopMods
            // 
            clbWorkshopMods.FormattingEnabled = true;
            clbWorkshopMods.Location = new Point(640, 316);
            clbWorkshopMods.Name = "clbWorkshopMods";
            clbWorkshopMods.Size = new Size(499, 346);
            clbWorkshopMods.TabIndex = 15;
            clbWorkshopMods.ItemCheck += clbWorkshopMods_ItemCheck;
            // 
            // clbServerFlags
            // 
            clbServerFlags.FormattingEnabled = true;
            clbServerFlags.IntegralHeight = false;
            clbServerFlags.Items.AddRange(new object[] { "-doLogs", "-adminLog", "-doActionLog", "-ceFullRemote", "-disableCrashReport", "-filePatching", "-netLog", "-freezeCheck", "-newErrorsAreWarnings=1", "-headlessMode=0" });
            clbServerFlags.Location = new Point(640, 54);
            clbServerFlags.Name = "clbServerFlags";
            clbServerFlags.Size = new Size(243, 220);
            clbServerFlags.TabIndex = 14;
            // 
            // btnEditServerParams
            // 
            btnEditServerParams.Location = new Point(745, 32);
            btnEditServerParams.Name = "btnEditServerParams";
            btnEditServerParams.Size = new Size(60, 23);
            btnEditServerParams.TabIndex = 44;
            btnEditServerParams.Text = "Edit...";
            btnEditServerParams.UseVisualStyleBackColor = true;
            btnEditServerParams.Click += btnEditServerParams_Click;
            // 
            // btnEditClientParams
            // 
            btnEditClientParams.Location = new Point(1005, 32);
            btnEditClientParams.Name = "btnEditClientParams";
            btnEditClientParams.Size = new Size(60, 23);
            btnEditClientParams.TabIndex = 45;
            btnEditClientParams.Text = "Edit...";
            btnEditClientParams.UseVisualStyleBackColor = true;
            btnEditClientParams.Click += btnEditClientParams_Click;
            // 
            // ConfigForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1196, 892);
            Controls.Add(panel1);
            Name = "ConfigForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "ConfigForm";
            Load += ConfigForm_Load;
            ((System.ComponentModel.ISupportInitialize)numPort).EndInit();
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            pnlServerConfig.ResumeLayout(false);
            pnlServerConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nb_cfg_battleye).EndInit();
            ((System.ComponentModel.ISupportInitialize)nb_cfg_max_players).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCpuCount).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TextBox txtName;
        private Label label1;
        private ComboBox cmbType;
        private Label label2;
        private ComboBox cmbVersion;
        private Label label3;
        private Label label4;
        private TextBox txtServerConfigFile;
        private Button btnBrowseServerConfig;
        private Label label5;
        private TextBox txtProfilesFolder;
        private Button btnBrowseProfiles;
        private NumericUpDown numPort;
        private Label label6;
        private Panel panel1;
        private CheckedListBox clbServerFlags;
        private CheckedListBox clbWorkshopMods;
        private Button btnOpenConfigFile;
        private Label label10;
        private Label label9;
        private TextBox txtServerName;
        private NumericUpDown numCpuCount;
        private Label label11;
        private ComboBox cmbExeName;
        private Button btnOK;
        private Button btnCancel;
        private Label lblWorkshopStatus;
        private Button btnWipeStorage;
        private Button btnBackupStorage;
        private Label label12;
        private Panel pnlServerConfig;
        private Label label14;
        private Label label13;
        private TextBox tb_cfg_password;
        private Label label8;
        private TextBox tb_cfg_hostname;
        private Label lbl_cfg_title;
        private NumericUpDown nb_cfg_max_players;
        private Label label16;
        private ComboBox cb_cfg_mpmission;
        private NumericUpDown nb_cfg_battleye;
        private Label label15;
        private Label label17;
        private ComboBox cb_chooseMP;
        private Label label18;
        private TextBox tb_cfg_shard_id;
        private Label label19;
        private ComboBox cb_cfg_enviromenttype;
        private CheckedListBox clbClientFlags;
        private Label label20;
        private Label label21;
        private Button btnOpenInitFile;
        private Button btnBackupInit;
        private Label label7;
        private Button btnBrowseInit;
        private Button btnBrowseStorage;
    }
}