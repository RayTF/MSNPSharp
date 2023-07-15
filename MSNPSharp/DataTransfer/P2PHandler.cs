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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;
    using System.Collections.Generic;

    /// <summary>
    /// Used in events where a P2PMessageSession object is created, or in another way affected.
    /// </summary>
    public class P2PSessionAffectedEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private P2PMessageSession session;

        /// <summary>
        /// The affected session
        /// </summary>
        public P2PMessageSession Session
        {
            get
            {
                return session;
            }
            set
            {
                session = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="affectedSession"></param>
        public P2PSessionAffectedEventArgs(P2PMessageSession affectedSession)
        {
            session = affectedSession;
        }
    }

    /// <summary>
    /// Handles incoming P2P messages from the switchboardserver.
    /// </summary>
    public class P2PHandler : IMessageHandler
    {
        #region Members

        private NSMessageHandler nsMessageHandler = null;
        private Messenger messenger = null;
        private P2PMessagePool p2pMessagePool = new P2PMessagePool();
        private List<P2PMessageSession> messageSessions = new List<P2PMessageSession>();
        private List<SBMessageHandler> switchboardSessions = new List<SBMessageHandler>(0);

        #endregion

        /// <summary>
        /// Protected constructor.
        /// </summary>
        protected internal P2PHandler(NSMessageHandler nsHandler, Messenger parentMessenger)
        {
            nsMessageHandler = nsHandler;
            messenger = parentMessenger;
        }

        #region Properties

        /// <summary>
        /// The nameserver handler. This object is used to request new switchboard sessions.
        /// </summary>
        internal NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        /// <summary>
        /// The message processor that will send the created P2P messages to the remote contact.
        /// </summary>
        [Obsolete("This is always null. Don't use!", true)]
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        /// <summary>
        /// A list of all current p2p message sessions. Multiple threads can access this resource so make sure to lock this.
        /// </summary>
        public List<P2PMessageSession> MessageSessions
        {
            get
            {
                return messageSessions;
            }
        }

        /// <summary>
        /// A collection of all available switchboard sessions
        /// </summary>
        internal List<SBMessageHandler> SwitchboardSessions
        {
            get
            {
                return switchboardSessions;
            }
        }

        #endregion

        /// <summary>
        /// Aborts and cleans up all running messagesessions, transfersessions and switchboard sessions.
        /// </summary>
        public void Clear()
        {
            lock (MessageSessions)
            {
                foreach (P2PMessageSession session in MessageSessions)
                {
                    session.CleanUp();
                }
            }

            lock (MessageSessions)
                MessageSessions.Clear();

            lock (switchboardSessions)
                switchboardSessions.Clear();

            p2pMessagePool = new P2PMessagePool();
        }

        #region Events

        /// <summary>
        /// Occurs when a P2P session is created.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionCreated;

        /// <summary>
        /// Occurs when a P2P session is closed.
        /// </summary>
        public event EventHandler<P2PSessionAffectedEventArgs> SessionClosed;

        /// <summary>
        /// Fires the SessionCreated event.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void OnSessionCreated(P2PMessageSession session)
        {
            if (SessionCreated != null)
                SessionCreated(this, new P2PSessionAffectedEventArgs(session));
        }

        /// <summary>
        /// Fires the SessionClosed event.
        /// </summary>
        /// <param name="session"></param>
        protected internal virtual void OnSessionClosed(P2PMessageSession session)
        {
            if (SessionClosed != null)
                SessionClosed(this, new P2PSessionAffectedEventArgs(session));
        }

        #endregion

        /// <summary>
        /// Gets a reference to a p2p message session with the specified remote contact.
        /// In case a session does not exist a new session will be created and returned.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="localEPID"></param>
        /// <param name="remoteContact"></param>
        /// <param name="remoteEPID"></param>
        /// <returns></returns>
        public virtual P2PMessageSession GetSession(Contact localContact, Guid localEPID, Contact remoteContact, Guid remoteEPID)
        {
            // check for existing session
            P2PMessageSession existingSession = GetSessionByContactAndEPID(localContact, localEPID, remoteContact, remoteEPID);

            if (existingSession != null)
            {
                if (existingSession.MessageProcessor is SocketMessageProcessor)
                {
                    if ((existingSession.MessageProcessor as SocketMessageProcessor).Connected)
                    {
                        return existingSession;
                    }
                    else
                    {
                        RemoveP2PMessageSession(existingSession);
                    }
                }
            }

            // no session available, create a new session
            P2PMessageSession newSession = CreateSession(localContact, localEPID, remoteContact, remoteEPID, null, null);


            return newSession;
        }

        /// <summary>
        /// Creates a p2p session. The session is at the moment of return pure fictive; no actual messages
        /// have been sent to the remote client. The session will use the P2PHandler's messageprocessor as it's default messageprocessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="localEndPointID"></param>
        /// <param name="remoteContact"></param>
        /// <param name="remoteEndPointID"></param>
        /// <param name="initatorMessage"></param>
        /// <param name="sessionMessageProcessor"></param>
        /// <returns></returns>
        protected virtual P2PMessageSession CreateSession(Contact localContact, Guid localEndPointID, Contact remoteContact, Guid remoteEndPointID, P2PMessage initatorMessage, IMessageProcessor sessionMessageProcessor)
        {
            P2PMessageSession session = new P2PMessageSession(localContact, localEndPointID, remoteContact, remoteEndPointID, NSMessageHandler);
            session.MessageProcessor = sessionMessageProcessor;
            session.ProcessorInvalid += OnP2PMessageSessionProcessorInvalid;

            // generate a local base identifier.
            session.LocalBaseIdentifier = (uint)((new Random()).Next(10000, int.MaxValue));

            // uses -1 because the first message must be the localbaseidentifier and the identifier
            // is automatically increased
            session.LocalIdentifier = (uint)session.LocalBaseIdentifier;

            if (initatorMessage != null)
            {
                session.RemoteBaseIdentifier = initatorMessage.Header.Identifier;
                session.RemoteIdentifier = initatorMessage.Header.Identifier;

                if (initatorMessage.Version == P2PVersion.P2PV2)
                {
                    session.RemoteIdentifier += initatorMessage.V2Header.MessageSize;
                }
            }

            AddP2PMessageSession(session);

            // Accepts by default owner display images and contact emoticons.
            session.MasterSession.TransferInvitationReceived += delegate(object sender, MSNSLPInvitationEventArgs args)
            {
                if (args.TransferProperties.DataType == DataTransferType.DisplayImage)
                {
                    args.Accept = true;

                    args.TransferSession.DataStream = NSMessageHandler.ContactList.Owner.DisplayImage.OpenStream();
                    args.TransferSession.AutoCloseStream = false;
                    args.TransferSession.ClientData = NSMessageHandler.ContactList.Owner.DisplayImage;
                }
                else if (args.TransferProperties.DataType == DataTransferType.Emoticon)
                {
                    MSNObject msnObject = new MSNObject();
                    msnObject.SetContext(args.TransferProperties.Context);

                    // send an emoticon
                    foreach (Emoticon emoticon in NSMessageHandler.ContactList.Owner.Emoticons.Values)
                    {
                        if (emoticon.Sha == msnObject.Sha)
                        {
                            args.Accept = true;
                            args.TransferSession.AutoCloseStream = true;
                            args.TransferSession.DataStream = emoticon.OpenStream();
                            args.TransferSession.ClientData = emoticon;
                        }
                    }
                }
                else
                {
                    // forward the invitation to the client programmer
                    messenger.OnTransferInvitationReceived(sender, args);
                }
                return;
            };


            OnSessionCreated(session);
            return session;
        }

        /// <summary>
        /// Gets a switchboard session with the specified remote contact present in the session. Null is returned if no such session is found.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        protected SBMessageHandler GetSwitchboardForP2PMessageSession(Contact remoteContact)
        {
            lock (SwitchboardSessions)
            {
                foreach (SBMessageHandler switchboard in SwitchboardSessions)
                {
                    if (switchboard.HasContact(remoteContact) && switchboard.IsSessionEstablished)
                    {
                        return switchboard;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the p2p message session for which the remote identifier equals the identifier passed as a parameter.
        /// This is typically called when an incoming message is processed.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="localEPID"></param>
        /// <param name="remoteContact"></param>
        /// <param name="remoteEPID"></param>
        /// <returns></returns>
        protected P2PMessageSession GetSessionByContactAndEPID(Contact localContact, Guid localEPID, Contact remoteContact, Guid remoteEPID)
        {
            P2PVersion expectedStackVersion = MSNSLPTransferProperties.JudgeP2PStackVersion(localContact, localEPID, remoteContact, remoteEPID, false);
            lock (MessageSessions)
            {
                foreach (P2PMessageSession session in MessageSessions)
                {
                    if (session.Version == expectedStackVersion)
                    {
                        if (session.Version == P2PVersion.P2PV2)
                        {
                            if ((session.RemoteContact.IsSibling(remoteContact) && session.RemoteContactEndPointID == remoteEPID) &&
                                (session.LocalContact.IsSibling(localContact) && session.LocalContactEndPointID == localEPID))
                                return session;
                        }
                        else if (session.Version == P2PVersion.P2PV1)
                        {
                            if (session.RemoteContact.IsSibling(remoteContact) && session.LocalContact.IsSibling(localContact))
                                return session;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// After the first acknowledgement we must set the identifier of the remote client.
        /// </summary>
        /// <param name="receivedMessage"></param>
        protected P2PMessageSession SetSessionIdentifiersAfterAck(P2PMessage receivedMessage)
        {
            P2PMessageSession session = (receivedMessage.Version == P2PVersion.P2PV1)
                ? GetSessionFromSequenceNumber(receivedMessage.V1Header.AckSessionId)
                : null; // We only do things step by step.

            if (session == null)
                throw new MSNPSharpException("P2PHandler: an acknowledgement for the creation of a P2P session was received, but no local created session could be found with the specified identifier.");

            // set the remote identifiers. 
            session.RemoteBaseIdentifier = receivedMessage.Header.Identifier;
            session.RemoteIdentifier = (uint)(receivedMessage.Header.Identifier);// - (ulong)session.RemoteInitialCorrection);

            return session;
        }

        /// <summary>
        /// Gets the p2p message session for which the local identifier equals the identifier passed as a parameter.
        /// This is typically called when a message is created.
        /// </summary>
        /// <param name="identifier">The identifier used by the remote client</param>
        /// <returns></returns>
        protected P2PMessageSession GetSessionFromSequenceNumber(uint identifier)
        {
            lock (MessageSessions)
            {
                foreach (P2PMessageSession session in MessageSessions)
                {
                    if (session.LocalIdentifier == identifier)
                        return session;
                }
            }
            return null;
        }



        #region HandleMessage

        /// <summary>
        /// Handles incoming sb messages. Other messages are ignored.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            SBMessage sbMessage = message as SBMessage;

            if (sbMessage == null)
                return;

            if (sbMessage.Command != "MSG")
            {
                return;
            }

            if (NSMessageHandler.ContactList.Owner == null)
                return;

            // create a MSGMessage from the sb message
            MimeMessage msgMessage = new MimeMessage();
            try
            {
                msgMessage.CreateFromParentMessage(sbMessage);
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.ToString(), GetType().Name);
            }

            // check if it's a valid p2p message (chunk messages has no content type)
            if (!msgMessage.MimeHeader.ContainsKey(MimeHeaderStrings.Content_Type) ||
                msgMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToString() != "application/x-msnmsgrp2p")
            {
                return;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Parsing incoming p2p message", GetType().Name);

            P2PVersion version = P2PVersion.P2PV1;

            string remoteAccount = (sbMessage.CommandValues.Count > 0) ? sbMessage.CommandValues[0].ToString() : String.Empty;
            string localAccount = NSMessageHandler.ContactList.Owner.Mail;

            Guid remoteContactEPID = Guid.Empty;
            Guid localContactEPID = Guid.Empty;

            if (msgMessage.MimeHeader.ContainsKey("P2P-Dest"))
            {
                if (msgMessage.MimeHeader["P2P-Dest"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries).Length > 1)
                {
                    version = P2PVersion.P2PV2;
                    remoteAccount = msgMessage.MimeHeader["P2P-Src"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    remoteContactEPID = new Guid(msgMessage.MimeHeader["P2P-Src"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                    localAccount = msgMessage.MimeHeader["P2P-Dest"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[0];
                    localContactEPID = new Guid(msgMessage.MimeHeader["P2P-Dest"].ToString().Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries)[1]);

                    Trace.WriteLine("P2Pv2 incoming message found. P2P-Dest: " + msgMessage.MimeHeader["P2P-Dest"].ToString());
                }
                else
                {
                    localAccount = msgMessage.MimeHeader["P2P-Dest"].ToString();
                    remoteAccount = msgMessage.MimeHeader.ContainsKey("P2P-Src")
                        ? msgMessage.MimeHeader["P2P-Src"].ToString()
                        : sbMessage.CommandValues[0].ToString(); // CommandValues.Count=0, Clone issue????

                    Trace.WriteLine("P2Pv1 incoming message found. P2P-Dest: " + msgMessage.MimeHeader["P2P-Dest"].ToString());
                }
            }

            // Check destination
            if (version == P2PVersion.P2PV2 &&
                localContactEPID != Guid.Empty &&
                localContactEPID != NSMessageHandler.MachineGuid)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "The destination of p2pv2 message received is not owner.\r\n" +
                    "Destination GUID: " + localContactEPID.ToString("B") + "\r\n" +
                    "Owner GUID: " + NSMessageHandler.MachineGuid.ToString("B"));
                return; // This message is not for me
            }

            // Create a P2P Message from the msg message
            P2PMessage p2pMessage = new P2PMessage(version);
            p2pMessage.CreateFromParentMessage(msgMessage);

            if (Settings.TraceSwitch.TraceVerbose)
            {
                ulong dataRemaining = 0;

                if (version == P2PVersion.P2PV1)
                {
                    dataRemaining = p2pMessage.V1Header.TotalSize - (p2pMessage.V1Header.Offset + p2pMessage.V1Header.MessageSize);
                }
                else if (version == P2PVersion.P2PV2)
                {
                    dataRemaining = p2pMessage.V2Header.DataRemaining;
                }

                Trace.WriteLine("Incoming p2p message: DataRemaining: " + dataRemaining + "\r\n" +
                    p2pMessage.ToDebugString(), GetType().Name);
            }

            // Buffer splitted P2P SLP messages.
            if (p2pMessagePool.BufferMessage(ref p2pMessage))
            {
                // - Buffering: Not completed yet, wait next packets...
                // - Invalid packet: Just ignore it...
                return;
            }

            SLPMessage slp = p2pMessage.IsSLPData ? p2pMessage.InnerMessage as SLPMessage : null;

            Contact remoteContact = null;
            Contact localContact = null;

            if (remoteAccount.ToLowerInvariant() == NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant())
            {
                remoteContact = NSMessageHandler.ContactList.Owner;
            }
            else
            {
                if (!NSMessageHandler.ContactList.HasContact(remoteAccount, ClientType.PassportMember))
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "P2P remote contact not in contact list: " + remoteAccount + " Type: " + ClientType.PassportMember.ToString());
                    return;
                }

                remoteContact = NSMessageHandler.ContactList.GetContact(remoteAccount, ClientType.PassportMember);
            }

            if (localAccount.ToLowerInvariant() == NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant())
                localContact = NSMessageHandler.ContactList.Owner;

            P2PMessageSession session = GetSessionByContactAndEPID(localContact, localContactEPID, remoteContact, remoteContactEPID);

            if (session == null)
            {
                if (version == P2PVersion.P2PV1)
                {
                    if (p2pMessage.V1Header.IsAcknowledgement)
                    {
                        // if it is an acknowledgement then the local client initiated the session.
                        // this means the session alread exists, but the remote identifier are not yet set.
                        session = SetSessionIdentifiersAfterAck(p2pMessage);
                    }
                    else
                    {
                        // there is no session available at all. the remote client sends the first message
                        // in the session. So create a new session to handle following messages.
                        session = CreateSession(NSMessageHandler.ContactList.Owner, localContactEPID, remoteContact, remoteContactEPID, p2pMessage, sender);
                    }
                }

                if (version == P2PVersion.P2PV2)
                {
                    // there is no session available at all. the remote client sends the first message
                    // in the session. So create a new session to handle following messages.

                    if (slp != null)
                    {
                        SLPRequestMessage req = slp as SLPRequestMessage;

                        if (req != null &&
                            req.Method == "INVITE" &&
                            req.ContentType == "application/x-msnmsgr-sessionreqbody")
                        {
                            session = CreateSession(NSMessageHandler.ContactList.Owner, localContactEPID, remoteContact, remoteContactEPID, p2pMessage, sender);
                        }
                    }
                }

                if (session == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "P2PHandler get session failed.");
                    return;
                }
            }

            Debug.Assert(session != null, "Session is null", "P2PHandler");

            // Send an acknowledgement after the last message
            if (version == P2PVersion.P2PV1)
            {
                if (p2pMessage.Header.IsAcknowledgement == false &&
                    p2pMessage.V1Header.Offset + p2pMessage.Header.MessageSize == p2pMessage.Header.TotalSize)
                {
                    P2PMessage ack = p2pMessage.CreateAcknowledgement();
                    session.SendMessage(ack);
                }
            }
            if (version == P2PVersion.P2PV2)
            {
                //Case 1: 0x18 0x03 Invite
                if ((p2pMessage.V2Header.OperationCode & (byte)OperationCode.RAK) > 0)
                {
                    P2PMessage ack = p2pMessage.CreateAcknowledgement();
                    session.SendMessage(ack);
                }
            }

            // now handle the message
            session.HandleMessage(sender, p2pMessage);

        }

        #endregion

        internal virtual bool AddP2PMessageSession(P2PMessageSession session)
        {
            lock (MessageSessions)
            {
                if (MessageSessions.Contains(session))
                    return false;

                MessageSessions.Add(session);
                return true;
            }
        }


        /// <summary>
        /// Add a switchboard handler to the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        internal virtual bool AddSwitchboardSession(SBMessageHandler session)
        {
            lock (SwitchboardSessions)
            {
                if (SwitchboardSessions.Contains(session) == false)
                {
                    SwitchboardSessions.Add(session);
                    return true;
                }
            }

            return false;
        }

        internal virtual bool RemoveP2PMessageSession(P2PMessageSession session)
        {
            lock (MessageSessions)
            {
                return MessageSessions.Remove(session);
            }
        }

        /// <summary>
        /// Removes a switchboard handler from the list of switchboard sessions to send messages to.
        /// </summary>
        /// <param name="session"></param>
        internal virtual bool RemoveSwitchboardSession(SBMessageHandler session)
        {
            int remainCount = 0;
            bool succeed = true;
            lock (SwitchboardSessions)
            {
                succeed = SwitchboardSessions.Remove(session);
                remainCount = SwitchboardSessions.Count;
            }
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "A " + session.GetType().ToString() + " has been removed.");
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "There is/are " + remainCount.ToString() + " switchboard(s) remain(s) unclosed.");

            return succeed;
        }

        /// <summary>
        /// Closes a message session.
        /// </summary>
        /// <param name="session"></param>
        protected virtual void CloseMessageSession(P2PMessageSession session)
        {
            // make sure there are no sessions and references left
            session.AbortAllTransfers();
            session.CleanUp();

            // call the event
            OnSessionClosed(session);

            // and remove the session from the list
            RemoveP2PMessageSession(session);
        }

        /// <summary>
        /// Requests a new switchboard session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnP2PMessageSessionProcessorInvalid(object sender, EventArgs e)
        {
            P2PMessageSession session = (P2PMessageSession)sender;

            // create a new switchboard to fill the hole
            SBMessageHandler sbHandler = GetSwitchboardForP2PMessageSession(session.RemoteContact);

            // if the contact is offline, there is no need to request a new switchboard. close the session.
            if (session.RemoteContact.Status == PresenceStatus.Offline)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "[OnP2PMessageSessionProcessorInvalid]" + session.RemoteContact + " is already offline, P2PMessageSession closed.");
                CloseMessageSession(session);
                return;
            }

            // check whether the switchboard handler is valid. Otherwise request a new switchboard session.
            if (sbHandler == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "[OnP2PMessageSessionProcessorInvalid] A " + session.GetType().ToString() + "\r\n" +
                                " with remote contact " + session.RemoteContact + "\r\n" +
                                " and local contact " + session.LocalContact + "\r\n" +
                                " is requesting a new switchboard as its processor...");

                Conversation conversation = messenger.CreateConversation();
                try
                {
                    sbHandler = conversation.Invite(session.RemoteContact);
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "[OnP2PMessageSessionProcessorInvalid] Invite party to Conversation failed.\r\n" +
                                " with remote contact " + session.RemoteContact + "\r\n" +
                                " and local contact " + session.LocalContact + "\r\n" +
                                ex.Message);
                    Clear();
                    return;
                }

                //Note: for conversation's ContactJoined event, you will never see different endpoints' joining events.
                //So we need to listen to the corresponding events from switchboard.
                sbHandler.ContactJoined += delegate(object conv, ContactConversationEventArgs args)
                {
                    if ((!session.ProcessorValid) && args.Contact.IsSibling(session.RemoteContact) && args.EndPoint == session.RemoteContactEndPointID)
                    {
                        session.MessageProcessor = conversation.Switchboard.MessageProcessor;
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                            "A " + session.GetType().ToString() + "\r\n" +
                            " with remote contact " + session.RemoteContact + "\r\n" +
                            " and local contact " + session.LocalContact + "\r\n" +
                            " successfully request a new switchboard as its processor: " + sbHandler);
                    }
                };
            }
            else
            {
                //Set processor, trigger the SendBuffer().
                session.MessageProcessor = sbHandler.MessageProcessor;
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "A " + session.GetType().ToString() + "\r\n" +
                    " with remote contact " + session.RemoteContact + "\r\n" +
                    " and local contact " + session.LocalContact + "\r\n" +
                    " has switched its processor to another switchboard: " + sbHandler);
            }
        }
    }
};
