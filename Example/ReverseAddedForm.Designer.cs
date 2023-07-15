namespace MSNPSharpClient
{
    partial class ReverseAddedForm
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
            this.lblAdded = new System.Windows.Forms.Label();
            this.gbMembership = new System.Windows.Forms.GroupBox();
            this.rbBlock = new System.Windows.Forms.RadioButton();
            this.rbAllow = new System.Windows.Forms.RadioButton();
            this.cbContactList = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbMembership.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblAdded
            // 
            this.lblAdded.AutoSize = true;
            this.lblAdded.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(162)));
            this.lblAdded.Location = new System.Drawing.Point(12, 9);
            this.lblAdded.Name = "lblAdded";
            this.lblAdded.Size = new System.Drawing.Size(226, 13);
            this.lblAdded.TabIndex = 0;
            this.lblAdded.Text = "{0} has added you to their contact list.";
            // 
            // gbMembership
            // 
            this.gbMembership.Controls.Add(this.rbBlock);
            this.gbMembership.Controls.Add(this.rbAllow);
            this.gbMembership.Location = new System.Drawing.Point(15, 36);
            this.gbMembership.Name = "gbMembership";
            this.gbMembership.Size = new System.Drawing.Size(441, 74);
            this.gbMembership.TabIndex = 1;
            this.gbMembership.TabStop = false;
            this.gbMembership.Text = "Add contact to:";
            // 
            // rbBlock
            // 
            this.rbBlock.AutoSize = true;
            this.rbBlock.Location = new System.Drawing.Point(15, 42);
            this.rbBlock.Name = "rbBlock";
            this.rbBlock.Size = new System.Drawing.Size(421, 17);
            this.rbBlock.TabIndex = 1;
            this.rbBlock.Text = "Blocked List (User can not see my online status and can not send instant messages" +
                ")";
            this.rbBlock.UseVisualStyleBackColor = true;
            this.rbBlock.CheckedChanged += new System.EventHandler(this.rbBlock_CheckedChanged);
            // 
            // rbAllow
            // 
            this.rbAllow.AutoSize = true;
            this.rbAllow.Checked = true;
            this.rbAllow.Location = new System.Drawing.Point(15, 19);
            this.rbAllow.Name = "rbAllow";
            this.rbAllow.Size = new System.Drawing.Size(352, 17);
            this.rbAllow.TabIndex = 0;
            this.rbAllow.TabStop = true;
            this.rbAllow.Text = "Allowed List (User can see my status and can send instant messages)";
            this.rbAllow.UseVisualStyleBackColor = true;
            // 
            // cbContactList
            // 
            this.cbContactList.AutoSize = true;
            this.cbContactList.Checked = true;
            this.cbContactList.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbContactList.Location = new System.Drawing.Point(30, 125);
            this.cbContactList.Name = "cbContactList";
            this.cbContactList.Size = new System.Drawing.Size(169, 17);
            this.cbContactList.TabIndex = 2;
            this.cbContactList.Text = "Add this user to my contact list";
            this.cbContactList.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(302, 125);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btn_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(381, 125);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btn_Click);
            // 
            // ReverseAddedForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(470, 156);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cbContactList);
            this.Controls.Add(this.gbMembership);
            this.Controls.Add(this.lblAdded);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ReverseAddedForm";
            this.Text = "Pending Contact {0}";
            this.gbMembership.ResumeLayout(false);
            this.gbMembership.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblAdded;
        private System.Windows.Forms.GroupBox gbMembership;
        private System.Windows.Forms.RadioButton rbAllow;
        private System.Windows.Forms.RadioButton rbBlock;
        private System.Windows.Forms.CheckBox cbContactList;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}