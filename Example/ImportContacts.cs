using System;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MSNPSharp;

namespace MSNPSharpClient
{
    public partial class ImportContacts : Form
    {
        public ImportContacts()
        {
            InitializeComponent();
        }


        private string invitationMessage = String.Empty;
        public string InvitationMessage
        {
            get
            {
                return invitationMessage;
            }
        }

        private List<string> _contacts = new List<string>();
        public List<string> Contacts
        {
            get
            {
                return _contacts;
            }
        }

        private void browseFile_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == openFileDialog.ShowDialog(this))
            {
                Contacts.Clear();
                invitationMessage = txtInvitation.Text;
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(File.ReadAllText(openFileDialog.FileName));

                    XmlNodeList contacts = doc.GetElementsByTagName("contact");
                    foreach (XmlNode contactNode in contacts)
                    {
                        if (ClientType.PassportMember == (ClientType)Convert.ToInt32(contactNode.Attributes["type"].Value))
                        {
                            Contacts.Add(contactNode.InnerText.ToLower(System.Globalization.CultureInfo.InvariantCulture));
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                MessageBox.Show(Contacts.Count.ToString() + " contacts to be imported.");
            }
        }

        private void button_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}