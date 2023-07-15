using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Security.Permissions;

namespace MSNPSharpClient
{
    using MSNPSharp;

    public partial class TraceForm : Form
    {
        RichTextBoxTraceListener rtbTraceListener = null;
        public TraceForm()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.MSNPSharp_logo_small_ico;
            rtbTraceListener = new RichTextBoxTraceListener(rtbTrace);
            Trace.Listeners.Add(rtbTraceListener);

            FormClosing += new FormClosingEventHandler(TraceForm_FormClosing);
        }

        void TraceForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            rtbTraceListener.Close();
            Trace.Listeners.Remove(rtbTraceListener);
        }

        private void tsbClear_Click(object sender, EventArgs e)
        {
            rtbTrace.Clear();
        }

        private void tsbStop_Click(object sender, EventArgs e)
        {
            rtbTraceListener.Stop();
            tsbStart.Enabled = true;
            tsbStop.Enabled = false;
        }

        private void tsbStart_Click(object sender, EventArgs e)
        {
            rtbTraceListener.Resume();
            tsbStart.Enabled = false;
            tsbStop.Enabled = true;
        }

        private void TraceForm_Load(object sender, EventArgs e)
        {
            toolStripComboBoxLevel.SelectedItem = "Verbose";
        }

        private void toolStripComboBoxLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            MSNPSharp.Settings.TraceSwitch.Level = (TraceLevel)Enum.Parse(typeof(TraceLevel), toolStripComboBoxLevel.SelectedItem.ToString());
        }
    }
    
    
    public class TraceWriter : TextWriter
    {
        private RichTextBox richTextBox = null;
        private delegate void WriteHandler(string buffer, RichTextBox rtb);
        private const int MaxBufferLen = 1024;
        private StringBuilder buffer = new StringBuilder(MaxBufferLen);
        private DateTime lastInputTime = DateTime.Now;
        private Thread writeThread = null;
        private Queue<char> messageQueue = new Queue<char>(MaxBufferLen);
        private bool canClose = false;
        private bool userClick = false;
        private int selectionStart = 0;
        private int selectionLength = 0;

        protected virtual void WriteBuffer()
        {
            StringBuilder trace = new StringBuilder(MaxBufferLen);
            while (!canClose)
            {
                lock (messageQueue)
                {

                    while (messageQueue.Count > 0)
                    {
                        trace.EnsureCapacity(buffer.Length < MaxBufferLen ? MaxBufferLen : MaxBufferLen + 2);
                        trace.Append(messageQueue.Dequeue());
                    }
                }

                if (trace.Length > 0)
                {
                    try
                    {
                        richTextBox.BeginInvoke(new WriteHandler(OutPut), new object[] { trace.ToString(), richTextBox });
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    trace.Remove(0, trace.Length);
                }

                Thread.Sleep(Settings.IsMono ? 5000 : 100);
            }
        }

        protected virtual void OutPut(string buffer, RichTextBox rtb)
        {
            if (selectionStart == rtb.Text.Length)
                userClick = false;

            rtb.AppendText(buffer);
            if (userClick)
            {
                rtb.Select(selectionStart, selectionLength);
                rtb.ScrollToCaret();
            }
        }

        public TraceWriter(RichTextBox outputRTB)
        {
            richTextBox = outputRTB;
            richTextBox.Click += new EventHandler(richTextBox_Click);
            richTextBox.KeyDown += new KeyEventHandler(richTextBox_KeyDown);
            writeThread = new Thread(new ThreadStart(WriteBuffer));
            writeThread.Start();
        }

        void richTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            userClick = true;
            selectionStart = richTextBox.SelectionStart;
            selectionLength = richTextBox.SelectionLength;
        }

        void richTextBox_Click(object sender, EventArgs e)
        {
            userClick = true;
            selectionStart = richTextBox.SelectionStart;
            selectionLength = richTextBox.SelectionLength;
        }

        public override void Write(char value)
        {
            if (richTextBox != null && buffer != null)
            {
                lock (messageQueue)
                {
                    messageQueue.Enqueue(value);
                }
            }
        }

        public override void Close()
        {
            canClose = true;
            base.Close();
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }
    }

    [HostProtection(SecurityAction.LinkDemand, Synchronization = true)]
    public class RichTextBoxTraceListener : TraceListener
    {
        // Fields
        private TraceWriter writer = null;
        private object syncObject = new object();
        private bool stop = false;

        // Methods
        public RichTextBoxTraceListener()
            : base()
        {
        }

        public RichTextBoxTraceListener(RichTextBox rtb)
            : base(string.Empty)
        {
            writer = new TraceWriter(rtb);
        }


        public override void Close()
        {
            if (this.writer != null)
            {
                this.writer.Close();
            }
            this.writer = null;
            this.stop = true;
        }

        private bool EnsureWriter()
        {
            lock (syncObject)
            {
                if (writer == null || stop == true) return false;
                return true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Close();
            }
        }

        public override void Flush()
        {
            if (!EnsureWriter()) return;
            this.writer.Flush();
        }

        private static Encoding GetEncodingWithFallback(Encoding encoding)
        {
            Encoding encoding2 = (Encoding)encoding.Clone();
            encoding2.EncoderFallback = EncoderFallback.ReplacementFallback;
            encoding2.DecoderFallback = DecoderFallback.ReplacementFallback;
            return encoding2;
        }

        public override void Write (string message)
        {
            if (!EnsureWriter ())
                return;
            if (base.NeedIndent)
            {
                this.WriteIndent ();
            }
   
            if (!Settings.IsMono)
            {
                this.writer.Write ("[" + DateTime.Now.ToString ("u") + "] " + message);
            }
        }

        public override void WriteLine(string message)
        {
            if (!EnsureWriter()) return;
            if (base.NeedIndent)
            {
                this.WriteIndent();
            }
            this.writer.WriteLine("[" + DateTime.Now.ToString("u") + "] " + message);
            base.NeedIndent = true;
        }

        public void Stop()
        {
            lock (syncObject)
            {
                stop = true;
            }
        }

        public void Resume()
        {
            lock (syncObject)
            {
                stop = false;
            }
        }

        // Properties
        public TraceWriter Writer
        {
            get
            {
                return this.writer;
            }
            set
            {
                this.writer = value;
            }
        }
    }

}