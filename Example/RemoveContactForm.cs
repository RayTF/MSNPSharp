using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace MSNPSharpClient
{
    public partial class RemoveContactForm : Form
    {

        public bool RemoveFromAddressBook
        {
            get
            {
                return cbRemove.Checked;
            }
        }

        public bool Block
        {
            get
            {
                return cbBlock.Checked;
            }
        }

        public RemoveContactForm()
        {
            InitializeComponent();
        }

        private void btn_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}