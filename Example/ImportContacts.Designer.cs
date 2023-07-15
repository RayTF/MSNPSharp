namespace MSNPSharpClient
{
    partial class ImportContacts
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
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.lblInvitation = new System.Windows.Forms.Label();
            this.txtInvitation = new System.Windows.Forms.TextBox();
            this.browseFile = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // openFileDialog
            // 
            this.openFileDialog.DefaultExt = "ctt";
            this.openFileDialog.Filter = "Messenger Contact List|*.ctt";
            this.openFileDialog.Title = "Select Contact File";
            // 
            // lblInvitation
            // 
            this.lblInvitation.AutoSize = true;
            this.lblInvitation.Location = new System.Drawing.Point(12, 20);
            this.lblInvitation.Name = "lblInvitation";
            this.lblInvitation.Size = new System.Drawing.Size(96, 13);
            this.lblInvitation.TabIndex = 0;
            this.lblInvitation.Text = "Invitation Message";
            // 
            // txtInvitation
            // 
            this.txtInvitation.Location = new System.Drawing.Point(114, 17);
            this.txtInvitation.Name = "txtInvitation";
            this.txtInvitation.Size = new System.Drawing.Size(342, 20);
            this.txtInvitation.TabIndex = 1;
            this.txtInvitation.Text = "Accept me :)";
            // 
            // browseFile
            // 
            this.browseFile.Location = new System.Drawing.Point(114, 43);
            this.browseFile.Name = "browseFile";
            this.browseFile.Size = new System.Drawing.Size(134, 23);
            this.browseFile.TabIndex = 2;
            this.browseFile.Text = "Browse Contact File...";
            this.browseFile.UseVisualStyleBackColor = true;
            this.browseFile.Click += new System.EventHandler(this.browseFile_Click);
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(283, 73);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(84, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button_Click);
            // 
            // button2
            // 
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(373, 73);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(83, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button_Click);
            // 
            // ImportContacts
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(466, 108);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.browseFile);
            this.Controls.Add(this.txtInvitation);
            this.Controls.Add(this.lblInvitation);
            this.Name = "ImportContacts";
            this.Text = "Import Contacts";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Label lblInvitation;
        private System.Windows.Forms.TextBox txtInvitation;
        private System.Windows.Forms.Button browseFile;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}