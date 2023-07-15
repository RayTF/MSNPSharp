#region Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions (http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice.
All rights reserved. http://code.google.com/p/msnp-sharp/

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and/or other materials provided with the distribution.
* Neither the names of Bas Geertsema or Xih Solutions nor the names of its
  contributors may be used to endorse or promote products derived from this
  software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 'AS IS'
AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE
LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
THE POSSIBILITY OF SUCH DAMAGE. 
*/
#endregion

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    partial class P2PMessageSession
    {
        #region Events

        /// <summary>
        /// Occurs when a direct connection is succesfully established.
        /// </summary>
        public event EventHandler<EventArgs> DirectConnectionEstablished;

        /// <summary>
        /// Occurs when a direct connection attempt has failed.
        /// </summary>
        public event EventHandler<EventArgs> DirectConnectionFailed;

        #endregion

        // A list of all direct processors trying to establish a connection.
        private List<P2PDirectProcessor> pendingProcessors = new List<P2PDirectProcessor>();
        private bool autoAcknowledgeHandshake = true;
        private bool directConnectionAttempt;
        private bool directConnected;

        // Tracked to know when an acknowledgement for the handshake is received.
        private uint DCHandshakeAckV1;

        // Tracked to know when an acknowledgement for the handshake is received.
        internal P2PMessage DCHandshakeAckV2;

        #region Properties

        /// <summary>
        /// Defines whether a direct connection handshake is automatically send to the remote client, or
        /// replied with an acknowledgement. Setting this to true means the remote client will start the
        /// transfer immediately. Setting this to false means the client programmer must send a handhsake
        /// message and an acknowledgement message after which the transfer will begin.
        /// </summary>
        public bool AutoHandshake
        {
            get
            {
                return autoAcknowledgeHandshake;
            }
            set
            {
                autoAcknowledgeHandshake = value;
            }
        }

        /// <summary>
        /// Defines whether an attempt has been made to create a direct connection
        /// </summary>
        public bool DirectConnectionAttempt
        {
            get
            {
                return directConnectionAttempt;
            }
        }

        /// <summary>
        /// Defines whether the message session runs over a direct session or is routed via the messaging server
        /// </summary>
        public bool DirectConnected
        {
            get
            {
                return directConnected;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a direct connection with the remote client.
        /// </summary>
        /// <returns></returns>
        public IMessageProcessor CreateDirectConnection(string host, int port, Guid nonce, bool hashed)
        {
            P2PDirectProcessor processor = new P2PDirectProcessor(
                new ConnectivitySettings(host, port), Version, nonce, hashed);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Trying to setup direct connection with remote host " + host + ":" + port.ToString(System.Globalization.CultureInfo.InvariantCulture), GetType().Name);

            AddPendingProcessor(processor);

            processor.Connect();

            return processor;
        }

        /// <summary>
        /// Setups a P2PDirectProcessor to listen for incoming connections. After a connection has been
        /// established the P2PDirectProcessor will become the main MessageProcessor to send messages.
        /// </summary>
        /// <returns></returns>
        public IMessageProcessor ListenForDirectConnection(IPAddress host, int port, Guid nonce, bool hashed)
        {
            ConnectivitySettings cs = new ConnectivitySettings();
            if (NSMessageHandler.ConnectivitySettings.LocalHost == string.Empty)
            {
                cs.LocalHost = host.ToString();
                cs.LocalPort = port;
            }
            else
            {
                cs.LocalHost = NSMessageHandler.ConnectivitySettings.LocalHost;
                cs.LocalPort = NSMessageHandler.ConnectivitySettings.LocalPort;
            }

            P2PDirectProcessor processor = new P2PDirectProcessor(cs, Version, nonce, hashed);

            // add to the list of processors trying to establish a connection
            AddPendingProcessor(processor);

            // start to listen
            processor.Listen(IPAddress.Parse(cs.LocalHost), cs.LocalPort);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Listening on " + cs.LocalHost + ":" + cs.LocalPort.ToString(System.Globalization.CultureInfo.InvariantCulture), GetType().Name);

            return processor;
        }

        /// <summary>
        /// Closes the direct connection with the remote client, if available. A closing p2p message will be send first.
        /// The session will fallback to the previous (SB) message processor.
        /// </summary>
        public void CloseDirectConnection()
        {
            if (DirectConnected)
            {
                CleanUpDirectConnection();
            }
        }

        #endregion

        #region Pending Processor Methods

        /// <summary>
        /// Add the processor to the pending list.
        /// </summary>
        /// <param name="processor"></param>
        protected void AddPendingProcessor(P2PDirectProcessor processor)
        {
            // we want to handle message from this processor
            processor.RegisterHandler(this);

            // inform the session of connected/disconnected events
            processor.ConnectionEstablished += (OnDirectProcessorConnected);
            processor.ConnectionClosed += (OnDirectProcessorDisconnected);
            processor.ConnectingException += (OnDirectProcessorException);
            processor.HandshakeCompleted += (OnDirectProcessorHandshakeCompleted);

            lock (pendingProcessors)
            {
                pendingProcessors.Add(processor);
            }
        }

        /// <summary>
        /// Use the given processor as the DC processor. And disconnect all other pending processors.
        /// </summary>
        /// <param name="processor"></param>
        protected void UsePendingProcessor(P2PDirectProcessor processor)
        {
            lock (pendingProcessors)
            {
                if (pendingProcessors.Contains(processor))
                {
                    pendingProcessors.Remove(processor);
                }
            }

            // stop all remaining attempts
            StopAllPendingProcessors();

            // set the direct processor as the main processor
            lock (this)
            {
                directConnected = true;
                directConnectionAttempt = true;
                preDCProcessor = MessageProcessor;
                MessageProcessor = processor;
            }

            if (DirectConnectionEstablished != null)
                DirectConnectionEstablished(this, EventArgs.Empty);
        }

        /// <summary>
        /// Disconnect all processors that are trying to establish a connection.
        /// </summary>
        protected void StopAllPendingProcessors()
        {
            lock (pendingProcessors)
            {
                foreach (P2PDirectProcessor processor in pendingProcessors)
                {
                    processor.Disconnect();
                    processor.UnregisterHandler(this);
                }
                pendingProcessors.Clear();
            }
        }

        #endregion


        /// <summary>
        /// Sends the handshake message (NONCE) in a direct connection.
        /// </summary>
        protected virtual void SendHandshakeMessage(IMessageProcessor processor)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Preparing to send handshake message", GetType().Name);

            P2PDirectProcessor p2pDP = (P2PDirectProcessor)processor;

            if (p2pDP.Nonce == Guid.Empty)
            {
                // don't throw an exception because the file transfer can continue over the switchboard
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Handshake could not be send because none is specified.", GetType().Name);

                // but close the direct connection
                p2pDP.Disconnect();
                return;
            }

            P2PDCHandshakeMessage hm = new P2PDCHandshakeMessage(p2pDP.Version);
            hm.Guid = p2pDP.Nonce;

            if (hm.Version == P2PVersion.P2PV1)
            {
                IncreaseLocalIdentifier();

                hm.Header.Identifier = LocalIdentifier;
                //hm.V1Header.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
                // AckSessionId is set by hm.Guid=NONCE...
                DCHandshakeAckV1 = hm.V1Header.AckSessionId;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Sending handshake message:\r\n " +
                hm.ToDebugString(), GetType().Name);

            p2pDP.SendMessage(hm);

            p2pDP.DCState = DirectConnectionState.HandshakeReply;
        }

        /// <summary>
        /// Occurs when an acknowledgement to a send handshake has been received, or a handshake is received.
        /// This will start the data transfer, provided the local client is the sender.
        /// </summary>
        private void OnDirectProcessorHandshakeCompleted(object sender, P2PHandshakeMessageEventArgs e)
        {
            P2PDirectProcessor dp = sender as P2PDirectProcessor;
            P2PDCHandshakeMessage p2pMessage = e.HandshakeMessage;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Handshake accepted", GetType().Name);

            if (p2pMessage.Version == P2PVersion.P2PV1 && p2pMessage.V1Header.Flags == P2PFlag.DirectHandshake)
            {
                // Check whether it is an acknowledgement to data preparation message
                if (DCHandshakeAckV1 != 0)
                {
                    UsePendingProcessor(dp);
                    return;
                }

                // check if it's a direct connection handshake
                if (AutoHandshake)
                {
                    // create a handshake message based on the incoming p2p message and send it
                    dp.SendMessage(p2pMessage.CreateAcknowledgement());

                    UsePendingProcessor(dp);
                    return;
                }
            }

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                if (DCHandshakeAckV2 != null)
                {
                    DCHandshakeAckV2.V2Header.OperationCode = (byte)OperationCode.RAK;
                    dp.SendMessage(DCHandshakeAckV2);
                    DCHandshakeAckV2 = null;

                    UsePendingProcessor(dp);
                    return;
                }

                if (AutoHandshake)
                {
                    UsePendingProcessor(dp);
                }
            }
        }


        /// <summary>
        /// Sets the message processor back to the switchboard message processor.
        /// </summary>
        private void CleanUpDirectConnection()
        {
            if (DirectConnected)
            {
                lock (this)
                {
                    SocketMessageProcessor directProcessor = (SocketMessageProcessor)MessageProcessor;

                    directConnected = false;
                    directConnectionAttempt = false;
                    MessageProcessor = preDCProcessor;

                    if (directProcessor != null)
                        directProcessor.Disconnect();

                    P2PDirectProcessor dp = directProcessor as P2PDirectProcessor;
                    if (dp != null)
                    {
                        dp.HandshakeCompleted -= (OnDirectProcessorHandshakeCompleted);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans up the direct connection.
        /// </summary>
        private void OnDirectProcessorDisconnected(object sender, EventArgs e)
        {
            ((SocketMessageProcessor)sender).ConnectionClosed -= (OnDirectProcessorDisconnected);

            CleanUpDirectConnection();
        }


        /// <summary>
        /// Sets the current message processor to the processor which has just connected succesfully.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDirectProcessorConnected(object sender, EventArgs e)
        {
            P2PDirectProcessor p2pdp = (P2PDirectProcessor)sender;
            p2pdp.ConnectionEstablished -= (OnDirectProcessorConnected);

            if (p2pdp.IsListener == false &&
                AutoHandshake &&
                p2pdp.Nonce != Guid.Empty &&
                p2pdp.DCState == DirectConnectionState.Handshake)
            {
                SendHandshakeMessage(p2pdp);
            }
        }

        /// <summary>
        /// Called when the direct processor could not connect. It will start the data transfer over the
        /// switchboard session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDirectProcessorException(object sender, ExceptionEventArgs e)
        {
            ((SocketMessageProcessor)sender).ConnectingException -= (OnDirectProcessorException);

            CleanUpDirectConnection();
            directConnectionAttempt = true;

            if (DirectConnectionFailed != null)
                DirectConnectionFailed(this, EventArgs.Empty);
        }
    }
};
