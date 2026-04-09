namespace ZipBuilder
{
    partial class Form1
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
            this.listBoxItems = new System.Windows.Forms.ListBox();
            this.btnAddFiles = new System.Windows.Forms.Button();
            this.btnAddFolder = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnCreateZip = new System.Windows.Forms.Button();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.lblStatus = new System.Windows.Forms.Label();
            this.groupBoxOS = new System.Windows.Forms.GroupBox();
            this.radioWindows = new System.Windows.Forms.RadioButton();
            this.radioLinux = new System.Windows.Forms.RadioButton();
            this.groupBoxOS.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBoxItems
            // 
            this.listBoxItems.FormattingEnabled = true;
            this.listBoxItems.ItemHeight = 16;
            this.listBoxItems.Location = new System.Drawing.Point(79, 12);
            this.listBoxItems.Name = "listBoxItems";
            this.listBoxItems.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.listBoxItems.Size = new System.Drawing.Size(448, 180);
            this.listBoxItems.TabIndex = 0;
            // 
            // btnAddFiles
            // 
            this.btnAddFiles.Location = new System.Drawing.Point(625, 12);
            this.btnAddFiles.Name = "btnAddFiles";
            this.btnAddFiles.Size = new System.Drawing.Size(130, 44);
            this.btnAddFiles.TabIndex = 1;
            this.btnAddFiles.Text = "Add Files";
            this.btnAddFiles.UseVisualStyleBackColor = true;
            this.btnAddFiles.Click += new System.EventHandler(this.btnAddFiles_Click);
            // 
            // btnAddFolder
            // 
            this.btnAddFolder.Location = new System.Drawing.Point(625, 74);
            this.btnAddFolder.Name = "btnAddFolder";
            this.btnAddFolder.Size = new System.Drawing.Size(130, 45);
            this.btnAddFolder.TabIndex = 2;
            this.btnAddFolder.Text = "Add Folder";
            this.btnAddFolder.UseVisualStyleBackColor = true;
            this.btnAddFolder.Click += new System.EventHandler(this.btnAddFolder_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(625, 134);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(130, 44);
            this.btnRemove.TabIndex = 3;
            this.btnRemove.Text = "Remove Selected";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnCreateZip
            // 
            this.btnCreateZip.Location = new System.Drawing.Point(342, 429);
            this.btnCreateZip.Name = "btnCreateZip";
            this.btnCreateZip.Size = new System.Drawing.Size(164, 34);
            this.btnCreateZip.TabIndex = 4;
            this.btnCreateZip.Text = "Create Zip";
            this.btnCreateZip.UseVisualStyleBackColor = true;
            this.btnCreateZip.Click += new System.EventHandler(this.btnCreateZip_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(79, 290);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(448, 23);
            this.progressBar1.TabIndex = 5;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(270, 347);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(48, 16);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.Text = "Ready";
            // 
            // groupBoxOS
            // 
            this.groupBoxOS.Controls.Add(this.radioLinux);
            this.groupBoxOS.Controls.Add(this.radioWindows);
            this.groupBoxOS.Location = new System.Drawing.Point(625, 215);
            this.groupBoxOS.Name = "groupBoxOS";
            this.groupBoxOS.Size = new System.Drawing.Size(130, 115);
            this.groupBoxOS.TabIndex = 7;
            this.groupBoxOS.TabStop = false;
            this.groupBoxOS.Text = "Target OS";
            // 
            // radioWindows
            // 
            this.radioWindows.AutoSize = true;
            this.radioWindows.Location = new System.Drawing.Point(6, 37);
            this.radioWindows.Name = "radioWindows";
            this.radioWindows.Size = new System.Drawing.Size(83, 20);
            this.radioWindows.TabIndex = 0;
            this.radioWindows.TabStop = true;
            this.radioWindows.Text = "Windows";
            this.radioWindows.UseVisualStyleBackColor = true;
            // 
            // radioLinux
            // 
            this.radioLinux.AutoSize = true;
            this.radioLinux.Location = new System.Drawing.Point(6, 76);
            this.radioLinux.Name = "radioLinux";
            this.radioLinux.Size = new System.Drawing.Size(107, 20);
            this.radioLinux.TabIndex = 1;
            this.radioLinux.TabStop = true;
            this.radioLinux.Text = "Linux/macOS";
            this.radioLinux.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(807, 475);
            this.Controls.Add(this.groupBoxOS);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.btnCreateZip);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.btnAddFolder);
            this.Controls.Add(this.btnAddFiles);
            this.Controls.Add(this.listBoxItems);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Package Files and Folders";
            this.groupBoxOS.ResumeLayout(false);
            this.groupBoxOS.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox listBoxItems;
        private System.Windows.Forms.Button btnAddFiles;
        private System.Windows.Forms.Button btnAddFolder;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnCreateZip;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.GroupBox groupBoxOS;
        private System.Windows.Forms.RadioButton radioLinux;
        private System.Windows.Forms.RadioButton radioWindows;
    }
}

