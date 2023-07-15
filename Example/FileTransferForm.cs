using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace MSNPSharpClient
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;


    public partial class FileTransferForm : Form
    {
        MSNSLPInvitationEventArgs invite;
        private bool transferFinished;

        public FileTransferForm(MSNSLPInvitationEventArgs invite)
        {
            this.invite = invite;
            InitializeComponent();
        }

        private void FileTransferForm_Load(object sender, EventArgs e)
        {
            string appPath = Path.GetFullPath(".");

            Text = "File Transfer: " + invite.TransferProperties.RemoteContact.Mail;
            txtFilePath.Text = Path.Combine(appPath, invite.Filename);
            lblSize.Text = invite.FileSize.ToString() + " bytes";

            invite.TransferSession.TransferStarted += (TransferSession_TransferStarted);
            invite.TransferSession.TransferProgressed += (TransferSession_TransferProgressed);
            invite.TransferSession.TransferAborted += (TransferSession_TransferAborted);
            invite.TransferSession.TransferFinished += (TransferSession_TransferFinished);
        }

        void TransferSession_TransferStarted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(TransferSession_TransferStarted), sender, e);
                return;
            }

            progressBar.Visible = true;
            lblSize.Text = "Transfer started";
        }

        void TransferSession_TransferProgressed(object sender, P2PTransferProgressedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<P2PTransferProgressedEventArgs>(TransferSession_TransferProgressed), sender, e);
                return;
            }
            progressBar.Visible = true;
            progressBar.Value = e.Percent;
            lblSize.Text = "Transferred: " + e.Transferred + " / " + e.TotalSize;
        }

        void TransferSession_TransferFinished(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(TransferSession_TransferFinished), sender, e);
                return;
            }

            transferFinished = true;

            btnOK.Text = "Open File";
            btnOK.Tag = "OPENFILE";
            btnCancel.Visible = true;

            lblSize.Text = "Transfer finished";
            progressBar.Visible = false;
            progressBar.Value = 0;
        }

        void TransferSession_TransferAborted(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<EventArgs>(TransferSession_TransferAborted), sender, e);
                return;
            }

            btnOK.Text = "Close";
            btnOK.Tag = "CLOSE";
            lblSize.Text = "Transfer aborted";

            progressBar.Visible = false;
            progressBar.Value = 0;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = saveFileDialog.FileName;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            if (transferFinished)
            {
                Close();
            }
            else
            {
                invite.Accept = false;
                invite.TransferHandler.RejectTransfer(invite);

                btnCancel.Visible = false;
                Close();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            switch (btnOK.Tag.ToString())
            {
                case "OK":

                    invite.TransferSession.DataStream = new FileStream(txtFilePath.Text, FileMode.Create, FileAccess.Write);
                    invite.TransferSession.AutoCloseStream = true;
                    invite.Accept = true;
                    invite.TransferHandler.AcceptTransfer(invite);

                    btnCancel.Visible = false;

                    lblSize.Text = "Waiting to start...";

                    btnOK.Text = "Abort Transfer";
                    btnOK.Tag = "ABORT";
                    break;

                case "ABORT":
                    invite.TransferHandler.CloseSession(invite.TransferSession);
                    btnOK.Text = "Close";
                    btnOK.Tag = "CLOSE";
                    break;

                case "OPENFILE":
                    Process.Start(txtFilePath.Text);
                    Close();
                    break;

                case "CLOSE":
                    Close();
                    break;
            }
        }

        private void FileTransferForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            invite.TransferSession.TransferStarted -= (TransferSession_TransferStarted);
            invite.TransferSession.TransferProgressed -= (TransferSession_TransferProgressed);
            invite.TransferSession.TransferAborted -= (TransferSession_TransferAborted);
            invite.TransferSession.TransferFinished -= (TransferSession_TransferFinished);
        }

    }
};