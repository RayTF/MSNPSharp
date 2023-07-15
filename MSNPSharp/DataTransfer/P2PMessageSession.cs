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
using System.Net.Sockets;
using System.Globalization;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// P2PMessageSession routes all messages in the p2p framework between the local client and a single
    /// remote client. It is the Transfer Layer of MSNP2P Protocol.
    /// </summary>
    /// <remarks>
    /// A single message session can hold multiple p2p transfer sessions. This for example occurs when a
    /// contact sends two files directly after each other in the same switchboard session. This class keeps
    /// track of the message identifiers, dispatches messages to registered message handlers and routes data
    /// messages to the correct <see cref="P2PTransferSession"/> objects. Usually this class is a handler
    /// of a switchboard processor. A common handler for this class is <see cref="MSNSLPHandler"/>.
    /// </remarks>
    public partial class P2PMessageSession : IMessageHandler, IMessageProcessor
    {
        #region Events

        /// <summary>
        /// Occurs when the processor has been marked as invalid. Due to connection error, or message processor being null.
        /// </summary>
        public event EventHandler<EventArgs> ProcessorInvalid;

        /// <summary>
        /// Occurs when a P2P session is closed.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionClosed;

        #endregion

        #region Members

        private uint localBaseIdentifier = 0;
        private uint localIdentifier = 0;
        private uint remoteBaseIdentifier = 0;
        private uint remoteIdentifier = 0;
        private Contact remoteContact = null;
        private Contact localContact = null;
        private Guid localContactEndPointID = Guid.Empty;
        private Guid remoteContactEndPointID = Guid.Empty;
        private NSMessageHandler nsMessageHandler = null;
        private P2PVersion version = P2PVersion.P2PV1;

        private MSNSLPHandler masterSession = null;


        /// <summary>
        /// Keeps track of unsend messages
        /// </summary>
        private Queue<NetworkMessage> sendMessages = new Queue<NetworkMessage>();

        private bool processorValid = true;

        /// This is the processor used before a direct connection. Usually a SB processor.
        /// It is a fallback variables in case a direct connection fails.
        private IMessageProcessor preDCProcessor = null;

        /// A collection of all transfersessions
        private Dictionary<uint, P2PTransferSession> transferSessions = new Dictionary<uint, P2PTransferSession>();

        #endregion

        #region Constructor

        protected P2PMessageSession()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);
        }

        public P2PMessageSession(Contact local, Guid localEPID, Contact remote, Guid remoteEPID, NSMessageHandler handler)
        {
            version = MSNSLPTransferProperties.JudgeP2PStackVersion(local, localEPID, remote, remoteEPID, true);

            localContact = local;
            localContactEndPointID = localEPID;
            remoteContact = remote;
            remoteContactEndPointID = remoteEPID;

            nsMessageHandler = handler;
            NSMessageHandler.ContactOffline += (NSMessageHandler_ContactOffline);

            masterSession = new MSNSLPHandler(Version, NSMessageHandler.P2PInvitationSchedulerId, NSMessageHandler.ContactList.Owner.ClientIP);
            masterSession.MessageProcessor = this;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Initializing P2P Transfer Layer object, version = " + Version.ToString(), GetType().Name);
        }


        #endregion

        #region Properties



        public Guid LocalContactEndPointID
        {
            get
            {
                return localContactEndPointID;
            }
        }

        public Guid RemoteContactEndPointID
        {
            get
            {
                return remoteContactEndPointID;
            }
        }

        /// <summary>
        /// The P2P Version of the transfer layer.
        /// </summary>
        public P2PVersion Version
        {
            get
            {
                return version;
            }
        }

        protected NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        /// <summary>
        /// Get the <see cref="MSNSLPHandler"/> of the transfer layer, each transfer layer have only one <see cref="MSNSLPHandler"/>
        /// </summary>
        /// <returns></returns>
        public MSNSLPHandler MasterSession
        {
            get
            {
                return masterSession;
            }
        }

        /// <summary>
        /// Indicates whether the processor is invalid
        /// </summary>
        public bool ProcessorValid
        {
            get
            {
                return processorValid;
            }
        }


        /// <summary>
        /// The sequence number that local transfer starts from.
        /// </summary>
        public uint LocalBaseIdentifier
        {
            get
            {
                return localBaseIdentifier;
            }
            set
            {
                localBaseIdentifier = value;
            }
        }

        /// <summary>
        /// The local sequence number of transfer layer message packet.
        /// </summary>
        public uint LocalIdentifier
        {
            get
            {
                return localIdentifier;
            }
            set
            {
                localIdentifier = value;
            }
        }

        /// <summary>
        /// The sequence number that remote transfer starts from.
        /// </summary>
        public uint RemoteBaseIdentifier
        {
            get
            {
                return remoteBaseIdentifier;
            }
            set
            {
                remoteBaseIdentifier = value;
            }
        }

        /// <summary>
        /// The remote sequence number of transfer layer message packet.
        /// </summary>
        public uint RemoteIdentifier
        {
            get
            {
                return remoteIdentifier;
            }
            set
            {
                remoteIdentifier = value;
            }
        }

        /// <summary>
        /// The account of the local contact.
        /// </summary>
        public Contact LocalContact
        {
            get
            {
                return localContact;
            }

        }

        /// <summary>
        /// The account of the remote contact.
        /// </summary>
        public Contact RemoteContact
        {
            get
            {
                return remoteContact;
            }

        }

        private string LocalContactEPIDString
        {
            get
            {
                if (Version == P2PVersion.P2PV1)
                    return LocalContact.Mail.ToLowerInvariant();

                return LocalContact.Mail.ToLowerInvariant() + ";" + LocalContactEndPointID.ToString("B").ToLowerInvariant();
            }
        }

        private string RemoteContactEPIDString
        {
            get
            {
                if (Version == P2PVersion.P2PV1)
                    return RemoteContact.Mail.ToLowerInvariant();

                return RemoteContact.Mail.ToLowerInvariant() + ";" + RemoteContactEndPointID.ToString("B").ToLowerInvariant();
            }
        }



        #endregion

        #region IMessageProcessor Members

        private List<IMessageHandler> handlers = new List<IMessageHandler>();

        /// <summary>
        /// Registers a message handler. After registering the handler will receive incoming messages.
        /// </summary>
        /// <param name="handler"></param>
        public void RegisterHandler(IMessageHandler handler)
        {
            if (handler != null && !handlers.Contains(handler))
            {
                lock (handlers)
                {
                    handlers.Add(handler);
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                       handler.ToString() + " added to handler list.", GetType().Name);

                }
            }
        }

        /// <summary>
        /// Unregisters a message handler. After registering the handler will no longer receive incoming messages.
        /// </summary>
        /// <param name="handler"></param>
        public void UnregisterHandler(IMessageHandler handler)
        {
            if (handler != null)
            {
                lock (handlers)
                {
                    while (handlers.Remove(handler))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                            handler.ToString() + " removed from handler list.", GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Sends incoming p2p messages to the remote contact.
        /// </summary>
        /// <remarks>
        /// Before the message is send a couple of things are checked. If there is no identifier available,
        /// the local identifier will be increased by one and set as the message identifier.
        /// Second, if the acknowledgement identifier is not set it will be set to a random value. After this
        /// the method will check for the total length of the message. If the total length is too large,
        /// the message will be splitted into multiple messages. The maximum size for p2p messages over a
        /// switchboard is 1202 bytes. The maximum size for p2p messages over a direct connection is 1352
        /// bytes. As a result the length of the splitted messages will be 1202 or 1352 bytes or smaller,
        /// depending on the availability of a direct connection.
        /// 
        /// If a direct connection is available the message is wrapped in a <see cref="P2PDCMessage"/> object
        /// and send over the direct connection. Otherwise it will be send over a switchboard session.
        /// If there is no switchboard session available, or it has become invalid, a new switchboard session
        /// will be requested by asking this to the nameserver handler. Messages will be buffered until a
        /// switchboard session, or a direct connection, becomes available. Upon a new connection the
        /// buffered messages are directly send to the remote contact over the new connection.
        /// </remarks>
        /// <param name="message">The P2PMessage to send to the remote contact.</param>
        public void SendMessage(NetworkMessage message)
        {
            P2PMessage p2pMessage = (P2PMessage)message;

            SetSequenceNumber(p2pMessage);

            if (Version == P2PVersion.P2PV1)
            {
                DeliverMessageV1(p2pMessage);
            }
            else if (Version == P2PVersion.P2PV2)
            {
                DeliverMessageV2(p2pMessage);
            }
        }

        #endregion

        #region IMessageHandler Members

        private IMessageProcessor messageProcessor = null;

        /// <summary>
        /// The message processor that sends the P2P messages to the remote contact.
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                messageProcessor = value;

                if (messageProcessor != null &&
                    messageProcessor.GetType() != typeof(NSMessageProcessor))
                {
                    ValidateProcessor();
                    SendBuffer();
                }
            }
        }

        /// <summary>
        /// Handles P2PMessages. Other messages are ignored.
        /// All incoming messages are supposed to belong to this session.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            Debug.Assert(p2pMessage != null, "Incoming message is not a P2PMessage", GetType().Name);

            if (p2pMessage.Version == P2PVersion.P2PV1)
            {
                // Keep track of the remote identifier
                RemoteIdentifier = p2pMessage.Header.Identifier;

                if (p2pMessage.V1Header.Flags == P2PFlag.Error)
                {
                    P2PTransferSession session = GetTransferSession(p2pMessage.Header.SessionId);
                    if (session != null)
                    {
                        session.AbortTransfer();
                    }
                    return;
                }

                // Check if it is a content message
                if (p2pMessage.Header.SessionId > 0)
                {
                    // Get the session to handle this message
                    P2PTransferSession session = GetTransferSession(p2pMessage.Header.SessionId);

                    if (session != null)
                        session.HandleMessage(this, p2pMessage);

                    return;
                }
            }
            else if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                // Keep track of the remote identifier
                RemoteIdentifier = p2pMessage.Header.Identifier + p2pMessage.Header.MessageSize;

                // Check if it is a content message
                if (p2pMessage.InnerBody != null && p2pMessage.Header.SessionId > 0)
                {
                    // Get the session to handle this message
                    P2PTransferSession session = GetTransferSession(p2pMessage.Header.SessionId);

                    if (session != null)
                        session.HandleMessage(this, p2pMessage);

                    return;
                }
            }

            // It is not a datamessage. Extract the messages one-by-one and dispatch
            // it to all handlers. Usually the MSNSLP handler.
            IMessageHandler[] cpHandlers = handlers.ToArray();
            foreach (IMessageHandler handler in cpHandlers)
                handler.HandleMessage(this, p2pMessage);
        }

        #endregion

        #region Get/Add/Remove TransferSession

        /// <summary>
        /// Returns the transfer session associated with the specified session identifier.
        /// </summary>
        public P2PTransferSession GetTransferSession(uint sessionId)
        {
            return transferSessions.ContainsKey(sessionId) ? transferSessions[sessionId] : null;
        }

        /// <summary>
        /// Adds the specified transfer session to the collection and sets the transfer session's message
        /// processor to be the message processor of the p2p message session. This is usally a SB message
        /// processor. 
        /// </summary>
        /// <param name="session"></param>
        public void AddTransferSession(P2PTransferSession session)
        {
            if (session != null)
            {
                session.MessageProcessor = this;
                transferSessions.Add(session.TransferProperties.SessionId, session);
            }
        }

        /// <summary>
        /// Removes the specified transfer session from the collection.
        /// </summary>
        public void RemoveTransferSession(P2PTransferSession session)
        {
            if (session != null)
            {
                session.MessageProcessor = null;

                lock (transferSessions)
                    transferSessions.Remove(session.TransferProperties.SessionId);

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, session.GetType() + " with SessionId = " +
                    session.TransferProperties.SessionId + " has been removed\r\n" + "There is(are) " +
                    transferSessions.Count + " P2PTransferSession still in this " + GetType());
            }
        }

        #endregion

        #region Correct/Increase LocalIdentifier

        /// <summary>
        /// Corrects the local identifier with the specified correction.
        /// </summary>
        /// <param name="correction"></param>
        public void CorrectLocalIdentifier(int correction)
        {
            if (correction < 0)
                LocalIdentifier -= (uint)Math.Abs(correction);
            else
                LocalIdentifier += (uint)Math.Abs(correction);
        }

        /// <summary>
        /// The identifier of the local client, increases with each message send
        /// </summary>
        public void IncreaseLocalIdentifier()
        {
            localIdentifier++;
            if (localIdentifier == localBaseIdentifier)
                localIdentifier++;
        }

        #endregion

        #region SelectEndPointID

        public Guid SelectEndPointID(Contact contact)
        {
            if (Version == P2PVersion.P2PV1)
                return Guid.Empty;

            foreach (Guid epId in contact.EndPointData.Keys)
            {
                if (epId != Guid.Empty)
                    return epId;
            }

            return Guid.Empty;
        }

        #endregion

        #region Clean Up

        /// <summary>
        /// Aborts all running transfer sessions.
        /// </summary>
        public virtual void AbortAllTransfers()
        {
            List<P2PTransferSession> transferSessions_copy = new List<P2PTransferSession>(transferSessions.Values);
            foreach (P2PTransferSession session in transferSessions_copy)
            {
                session.AbortTransfer();
            }
        }

        /// <summary>
        /// Removes references to handlers and the messageprocessor. Also closes running transfer sessions
        /// and pending processors establishing connections.
        /// </summary>
        public virtual void CleanUp()
        {
            NSMessageHandler.ContactOffline -= (NSMessageHandler_ContactOffline);
            OnSessionClosed(this);
            NSMessageHandler.P2PHandler.OnSessionClosed(this);

            StopAllPendingProcessors();
            AbortAllTransfers();

            lock (handlers)
                handlers.Clear();

            MessageProcessor = null;

            lock (transferSessions)
                transferSessions.Clear();
        }

        /// <summary>
        /// Cleans up p2p resources associated with the offline contact.
        /// </summary>
        private void NSMessageHandler_ContactOffline(object sender, ContactEventArgs e)
        {
            if (e.Contact.IsSibling(RemoteContact))
            {
                CleanUp();
            }
        }

        #endregion

        #region Protected Methods

        #region Event Handlers

        /// <summary>
        /// Sets the processor as valid.
        /// </summary>
        protected virtual void ValidateProcessor()
        {
            processorValid = true;
        }

        /// <summary>
        /// Sets the processor as invalid, and requests the p2phandler for a new request.
        /// </summary>
        protected virtual void InvalidateProcessor()
        {
            if (ProcessorValid)
            {
                processorValid = false;
                OnProcessorInvalid();
            }
        }

        /// <summary>
        /// Fires the ProcessorInvalid event.
        /// </summary>
        protected virtual void OnProcessorInvalid()
        {
            if (ProcessorInvalid != null)
                ProcessorInvalid(this, EventArgs.Empty);
        }

        /// <summary>
        /// Fires the SessionClosed event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionClosed(P2PMessageSession session)
        {
            if (SessionClosed != null)
                SessionClosed(this, new P2PSessionAffectedEventArgs(session));
        }

        #endregion

        #region Wrap Message

        /// <summary>
        /// Wraps a P2PMessage in a MSGMessage and SBMessage.
        /// </summary>
        /// <returns></returns>
        protected P2PMimeMessage WrapToMimeMessage(NetworkMessage networkMessage)
        {
            return new P2PMimeMessage(RemoteContactEPIDString, LocalContactEPIDString, networkMessage);
        }

        /// <summary>
        /// Wrap the message to a P2P direct connection message or a switchboard message base on the transfer bridge.
        /// </summary>
        /// <param name="message">The message to wrap.</param>
        /// <returns></returns>
        protected virtual NetworkMessage WrapToProcessorMessage(NetworkMessage message)
        {
            if (MessageProcessor is P2PDirectProcessor)
            {
                return new P2PDCMessage(message as P2PMessage);
            }

            MimeMessage mimeMessage = WrapToMimeMessage(message);
            SBMessage sbMessage = new SBMessage();
            sbMessage.Acknowledgement = "D";

            sbMessage.InnerMessage = mimeMessage;
            return sbMessage;
        }

        #endregion

        #region SendBuffer / BufferMessage / TrySend

        /// <summary>
        /// Try to resend any messages that were stored in the buffer.
        /// </summary>
        protected virtual void SendBuffer()
        {
            if (MessageProcessor == null)
                return;

            try
            {
                while (sendMessages.Count > 0)
                {
                    NetworkMessage p2pMessage = sendMessages.Dequeue();
                    MessageProcessor.SendMessage(WrapToProcessorMessage(p2pMessage));
                }
            }
            catch (System.Net.Sockets.SocketException)
            {
                InvalidateProcessor();
            }
        }

        /// <summary>
        /// Buffer messages that can not be send because of an invalid message processor.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void BufferMessage(NetworkMessage message)
        {
            if (sendMessages.Count >= 100)
                System.Threading.Thread.CurrentThread.Join(200);

            sendMessages.Enqueue(message);
        }

        /// <summary>
        /// Buffer the message, then trigger <see cref="OnProcessorInvalid"/> event.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void RequestProcessorAndBufferMessage(NetworkMessage message)
        {
            BufferMessage(message);
            InvalidateProcessor();
        }

        /// <summary>
        /// Add the message to send queue and send all messages in the queue.
        /// </summary>
        /// <param name="message"></param>
        protected virtual void EnqueueAndSendMessage(NetworkMessage message)
        {
            BufferMessage(message);
            SendBuffer();
        }

        /// <summary>
        /// Try to deliver the message to network.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        protected virtual bool TrySend(P2PMessage message)
        {
            try
            {
                if (MessageProcessor != null &&
                   ((SocketMessageProcessor)MessageProcessor).Connected)
                {
                    EnqueueAndSendMessage(message);
                    return true;
                }
                else
                {
                    RequestProcessorAndBufferMessage(message);
                }
            }
            catch (SocketException)
            {
                RequestProcessorAndBufferMessage(message);
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Invalid message processor detected, message buffered.");
            return false;
        }

        #endregion

        #endregion

        #region Private methods

        private bool CheckTransferLayerVersion(P2PVersion dstVersion)
        {
            if (Version != dstVersion)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "A wrong tranfer layer used, can't deliver a " + dstVersion.ToString() + " message via " +
                    Version.ToString() + " transfer layer.");

                return false;
            }

            return true;
        }

        private bool CheckTransferLayerVersion(P2PMessage dstMessage)
        {
            if (Version != dstMessage.Version)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "A wrong tranfer layer used, can't deliver a " + dstMessage.Version.ToString() + " message via " +
                    Version.ToString() + " transfer layer.");

                return false;
            }

            return true;
        }


        private void DeliverMessageV1(P2PMessage p2pMessage)
        {
            if (!(CheckTransferLayerVersion(P2PVersion.P2PV1) && CheckTransferLayerVersion(p2pMessage)))
                return;

            // check whether we have a direct connection (send p2pdc messages) or not (send sb messages)
            int maxSize = DirectConnected ? 1352 : 1202;

            // split up large messages which go to the SB
            if (Version == P2PVersion.P2PV1)
            {
                if (p2pMessage.Header.MessageSize > maxSize)
                {
                    P2PMessage[] messages = p2pMessage.SplitMessage(maxSize);
                    foreach (P2PMessage chunkMessage in messages)
                    {
                        // now send it to propbably a SB processor
                        TrySend(chunkMessage);
                    }
                }
                else
                {
                    TrySend(p2pMessage);
                }
            }
        }

        private void DeliverMessageV2(P2PMessage p2pMessage)
        {
            if (!(CheckTransferLayerVersion(P2PVersion.P2PV2) && CheckTransferLayerVersion(p2pMessage)))
                return;

            int maxSize = DirectConnected ? 1352 : 1202;
            if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > maxSize)
            {
                CorrectLocalIdentifier(-(int)p2pMessage.V2Header.MessageSize);

                P2PMessage[] messages = p2pMessage.SplitMessage(maxSize);

                foreach (P2PMessage chunkMessage in messages)
                {
                    //chunkMessage.V2Header.Identifier = LocalIdentifier;
                    LocalIdentifier = chunkMessage.V2Header.Identifier + chunkMessage.V2Header.MessageSize;
                    // CorrectLocalIdentifier((int)chunkMessage.V2Header.MessageSize);

                    // now send it to propbably a SB processor
                    TrySend(chunkMessage);
                }
            }
            else
            {
                TrySend(p2pMessage);
            }
        }


        private void SetSequenceNumber(P2PMessage p2pMessage)
        {
            // Check whether the sequence number is already set. This is important to check for acknowledge messages.
            if (p2pMessage.Header.Identifier == 0)
            {
                if (Version == P2PVersion.P2PV1)
                {
                    IncreaseLocalIdentifier();
                    p2pMessage.Header.Identifier = LocalIdentifier;
                }
                else if (Version == P2PVersion.P2PV2)
                {
                    p2pMessage.V2Header.Identifier = LocalIdentifier;
                    CorrectLocalIdentifier((int)p2pMessage.V2Header.MessageSize);
                }
            }

            if (Version == P2PVersion.P2PV1 && p2pMessage.V1Header.AckSessionId == 0)
            {
                p2pMessage.V1Header.AckSessionId = (uint)new Random().Next(50000, int.MaxValue);
            }
        }

        #endregion
    }
};
