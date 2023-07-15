using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MSNPSharp;

namespace MSNPSharpClient
{
    public partial class ReverseAddedForm : Form
    {
        public ReverseAddedForm(Contact contact)
        {
            InitializeComponent();

            Text = String.Format(Text, contact.Mail);
            lblAdded.Text = String.Format(lblAdded.Text, contact.Name + " (" + contact.Mail + ")");
        }

        public bool Blocked
        {
            get
            {
                return rbBlock.Checked;
            }
        }

        public bool AddToContactList
        {
            get
            {
                return cbContactList.Checked;
            }
        }

        private void rbBlock_CheckedChanged(object sender, EventArgs e)
        {
            if (rbBlock.Checked)
            {
                cbContactList.Checked = false;
            }
        }

        private void btn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}