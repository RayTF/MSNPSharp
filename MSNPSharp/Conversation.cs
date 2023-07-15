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
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    internal abstract class MessageObject
    {
        protected object innerObject = null;

        public object InnerObject
        {
            get { return innerObject; }
        }
    }

    internal class UserTypingObject : MessageObject
    {

    }

    internal class NudgeObject : MessageObject { }
    internal class TextMessageObject : MessageObject
    {
        public TextMessageObject(TextMessage message)
        {
            innerObject = message;
        }
    }

    internal class EmoticonObject : MessageObject
    {
        private EmoticonType type;

        public EmoticonType Type
        {
            get { return type; }
        }
        public EmoticonObject(List<Emoticon> iconlist, EmoticonType icontype)
        {
            innerObject = iconlist;
            type = icontype;
        }
    }

    /// <summary>
    /// A facade to the underlying switchboard and YIM session.
    /// </summary>
    /// <remarks>
    /// Conversation implements a few features for the ease of the application programmer. It provides
    /// directly basic common functionality. However, if you need to perform more advanced actions, or catch
    /// other events you have to directly use the underlying switchboard handler, or switchboard processor.
    /// Conversation automatically requests emoticons used by remote contacts.
    /// </remarks>
    public class Conversation
    {
        #region Private

        #region Members

        private Messenger _messenger = null;
        private SBMessageHandler _switchboard = null;
        private bool ended = false;
        private bool ending = false;
        private ConversationState conversationState = ConversationState.None;

        private bool autoRequestEmoticons = true;
        private bool autoKeepAlive = false;

        private List<Contact> _pendingInviteContacts = new List<Contact>(0);
        private List<Contact> _contacts = new List<Contact>(0);
        private Contact _firstContact = null;
        private object _syncObject = new object();

        private Queue<MessageObject> _messageQueues = new Queue<MessageObject>(0);
        private ConversationType _type = ConversationType.None;
        private int keepalivePeriod = 30000;

        private Timer keepaliveTimer = null; 
        #endregion

        private void transferSession_TransferAborted(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon aborted", GetType().Name);

            P2PTransferSession session = sender as P2PTransferSession;
            OnMSNObjectDataTransferCompleted(sender,
                new MSNObjectDataTransferCompletedEventArgs(session.ClientData as MSNObject, true, session.TransferProperties.RemoteContact, session.TransferProperties.RemoteContactEndPointID));
        }

        private void transferSession_TransferFinished(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Emoticon received", GetType().Name);

            P2PTransferSession session = sender as P2PTransferSession;
            OnMSNObjectDataTransferCompleted(sender,
                new MSNObjectDataTransferCompletedEventArgs(session.ClientData as MSNObject, false, session.TransferProperties.RemoteContact, session.TransferProperties.RemoteContactEndPointID));

        }

        private bool AddContact(Contact contact)
        {
            lock (_contacts)
            {
                if (_contacts.Contains(contact))
                    return false; ;
                _contacts.Add(contact);
            }

            return true;
        }

        private bool RemoveContact(Contact contact)
        {
            lock (_contacts)
            {
                return _contacts.Remove(contact);
            }
        }

        private void ClearContacts()
        {
            lock (_contacts)
                _contacts.Clear();
        }

        private static void KeepAlive(object state)
        {
            Conversation convers = state as Conversation;
            if (convers.AutoKeepAlive)
            {
                if ((convers.Type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
                {
                    if (convers.conversationState >= ConversationState.SwitchboardRequestSent && 
                        convers.conversationState < ConversationState.SwitchboardEnded)
                    {
                        convers._switchboard.SendKeepAliveMessage();
                    }
                }
            }
        }

        private void IniCommonSettings()
        {
            if (_switchboard == null)
            {
                _switchboard = new SBMessageHandler(Messenger.Nameserver);

            }

            //Must call after _switchboard and _yimHandler have been initialized.
            AttachEvents(_switchboard);

            conversationState = ConversationState.ConversationCreated;
        }

        private bool IsPendingContact(Contact contact)
        {
            lock (_pendingInviteContacts)
            {
                foreach (Contact pendingContact in _pendingInviteContacts)
                {
                    if (pendingContact.IsSibling(contact))
                        return true;
                }

                return false;
            }
        }

        private void PendingContactEnqueue(Contact pendingContact)
        {
            lock (_pendingInviteContacts)
            {
                if (!_pendingInviteContacts.Contains(pendingContact))
                    _pendingInviteContacts.Add(pendingContact);
            }
        }

        private void MessageEnqueue(MessageObject message)
        {
            lock (_messageQueues)
            {
                _messageQueues.Enqueue(message);
            }
        }

        private void SwitchBoardInvitePendingContacts()
        {
            List<Contact> pendingInviteContacts = new List<Contact>(_pendingInviteContacts);
            foreach (Contact contact in pendingInviteContacts)
            {
                if (contact.Status == PresenceStatus.Offline)
                {
                    lock (_pendingInviteContacts)
                    {
                        _pendingInviteContacts.Remove(contact);
                    }
                }
            }

            pendingInviteContacts = new List<Contact>(_pendingInviteContacts);
            foreach (Contact contact in pendingInviteContacts)
            {
                if (contact.Status != PresenceStatus.Offline)
                {
                    _switchboard.Invite(contact);
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "SwitchBoard " + _switchboard.SessionHash + " inviting user: " + contact.Mail);
                }
            }
        }

        private void AttachEvents(SBMessageHandler switchboard)
        {
            DetachEvents(switchboard);

            switchboard.AllContactsLeft += new EventHandler<EventArgs>(OnAllContactsLeft);
            switchboard.ContactJoined += new EventHandler<ContactConversationEventArgs>(OnContactJoined);
            switchboard.ContactLeft += new EventHandler<ContactConversationEventArgs>(OnContactLeft);
            switchboard.EmoticonDefinitionReceived += new EventHandler<EmoticonDefinitionEventArgs>(OnEmoticonDefinitionReceived);
            switchboard.ExceptionOccurred += new EventHandler<ExceptionEventArgs>(OnExceptionOccurred);
            switchboard.NudgeReceived += new EventHandler<ContactEventArgs>(OnNudgeReceived);
            switchboard.ServerErrorReceived += new EventHandler<MSNErrorEventArgs>(OnServerErrorReceived);
            switchboard.SessionClosed += new EventHandler<EventArgs>(OnSessionClosed);
            switchboard.SessionEstablished += new EventHandler<EventArgs>(OnSessionEstablished);
            switchboard.TextMessageReceived += new EventHandler<TextMessageEventArgs>(OnTextMessageReceived);
            switchboard.UserTyping += new EventHandler<ContactEventArgs>(OnUserTyping);
            switchboard.WinkReceived += new EventHandler<WinkEventArgs>(OnWinkReceived);
        }

        private void DetachEvents(SBMessageHandler switchboard)
        {
            switchboard.AllContactsLeft -= (OnAllContactsLeft);
            switchboard.ContactJoined -= (OnContactJoined);
            switchboard.ContactLeft -= (OnContactLeft);
            switchboard.EmoticonDefinitionReceived -= (OnEmoticonDefinitionReceived);
            switchboard.ExceptionOccurred -= (OnExceptionOccurred);
            switchboard.NudgeReceived -= (OnNudgeReceived);
            switchboard.ServerErrorReceived -= (OnServerErrorReceived);
            switchboard.SessionClosed -= (OnSessionClosed);
            switchboard.SessionEstablished -= (OnSessionEstablished);
            switchboard.TextMessageReceived -= (OnTextMessageReceived);
            switchboard.UserTyping -= (OnUserTyping);
            switchboard.WinkReceived -= (OnWinkReceived);
        }


        private void SwitchBoardEnd(object param)
        {
            try
            {
                if ((bool)param == false)
                {
                    Switchboard.Close();
                }
                
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[SwitchBoardEnd] An error occured while ending the switchboard: " + Switchboard.SessionHash
                                  + "\r\n  Error Message: " + ex.Message);
            }

            conversationState = ConversationState.SwitchboardEnded;
        }

        private Contact GetFirstJoinedContact()
        {
            lock (Contacts)
            {
                foreach (Contact contact in Contacts)
                {
                    if (!contact.IsSibling(Messenger.ContactList.Owner))
                    {
                        return contact;
                    }
                }
            }

            return null;
        }

        private bool SetFirstJoinedContact(Contact firstJoinedContact)
        {
            lock (_syncObject)
            {
                if (RemoteOwner == null)
                {
                    _firstContact = firstJoinedContact;
                    return true;
                }
            }

            return false;
        }

        private bool SetNextRemoteOwner()
        {
            lock (_syncObject)
            {
                Contact oldOwner = RemoteOwner;

                foreach (Contact contact in Contacts)
                {
                    if (!contact.IsSibling(Messenger.Nameserver.ContactList.Owner))
                    {
                        if (!contact.IsSibling(oldOwner))
                        {
                            _firstContact = contact;
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        #endregion


        #region Protected

        protected void OnMSNObjectDataTransferCompleted(object sender, MSNObjectDataTransferCompletedEventArgs e)
        {
            if (MSNObjectDataTransferCompleted != null)
                MSNObjectDataTransferCompleted(this, new ConversationMSNObjectDataTransferCompletedEventArgs(sender as P2PTransferSession, e));
        }


        protected virtual void OnConversationEnded(Conversation conversation)
        {
            if (Ended) return;
            Ended = true;

            conversationState = ConversationState.ConversationEnded;
            if (ending)
            {
                DetachEvents(_switchboard);
                ending = false;
            }

            _messenger.Conversations.Remove(this);

            
            ClearContacts();

            lock (_pendingInviteContacts)
                _pendingInviteContacts.Clear();

            if (ConversationEnded != null)
            {
                ConversationEnded(this, new ConversationEndEventArgs(conversation));
            }
        }

        #region Event operation


        protected virtual void OnWinkReceived(object sender, WinkEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (WinkReceived != null)
                WinkReceived(this, e);
        }

        protected virtual void OnUserTyping(object sender, ContactEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (UserTyping != null)
                UserTyping(this, e);
        }

        protected virtual void OnTextMessageReceived(object sender, TextMessageEventArgs e)
        {
            _type |= ConversationType.Chat;

            if (TextMessageReceived != null)
                TextMessageReceived(this, e);
        }

        protected virtual void OnSessionEstablished(object sender, EventArgs e)
        {
            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                SwitchBoardInvitePendingContacts();
            }

            if (SessionEstablished != null)
                SessionEstablished(this, e);
        }

        protected virtual void OnSessionClosed(object sender, EventArgs e)
        {
            if (sender.GetType() == typeof(SBMessageHandler))
            {
                conversationState = ConversationState.SwitchboardEnded;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, sender.GetType().ToString() + " session :" + _switchboard.SessionHash + " closed.");

            if (SessionClosed != null)
                SessionClosed(this, e);

            Messenger.P2PHandler.RemoveSwitchboardSession(_switchboard);

            OnConversationEnded(this);
        }

        protected virtual void OnServerErrorReceived(object sender, MSNErrorEventArgs e)
        {
            if (ServerErrorReceived != null)
                ServerErrorReceived(this, e);
        }

        protected virtual void OnNudgeReceived(object sender, ContactEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (NudgeReceived != null)
                NudgeReceived(this, e);
        }

        protected virtual void OnExceptionOccurred(object sender, ExceptionEventArgs e)
        {
            if (ExceptionOccurred != null)
                ExceptionOccurred(this, e);
        }

        protected virtual void OnEmoticonDefinitionReceived(object sender, EmoticonDefinitionEventArgs e)
        {
            _type |= ConversationType.Chat;
            if (AutoRequestEmoticons == false)
                return;

            MSNObject existing = MSNObjectCatalog.GetInstance().Get(e.Emoticon.CalculateChecksum());
            if (existing == null)
            {
                e.Sender.Emoticons[e.Emoticon.Sha] = e.Emoticon;

                // create a session and send the invitation
                P2PMessageSession session = Messenger.P2PHandler.GetSession(Messenger.ContactList.Owner, Messenger.ContactList.Owner.MachineGuid, e.Sender, e.Sender.SelectRandomEPID());

                MSNSLPHandler msnslpHandler = session.MasterSession;
                if (msnslpHandler != null)
                {
                    P2PTransferSession transferSession = msnslpHandler.SendInvitation(session.LocalContact, session.RemoteContact, e.Emoticon);
                    transferSession.DataStream = e.Emoticon.OpenStream();
                    transferSession.ClientData = e.Emoticon;

                    transferSession.TransferAborted += new EventHandler<EventArgs>(transferSession_TransferAborted);
                    transferSession.TransferFinished += new EventHandler<EventArgs>(transferSession_TransferFinished);

                    MSNObjectCatalog.GetInstance().Add(e.Emoticon);
                }
                else
                    throw new MSNPSharpException("No MSNSLPHandler was attached to the p2p message session. An emoticon invitation message could not be send.");
            }
            else
            {
                //If exists, fire the event.
                OnMSNObjectDataTransferCompleted(sender, new MSNObjectDataTransferCompletedEventArgs(existing, false, e.Sender, Guid.Empty));
            }


            if (EmoticonDefinitionReceived != null)
                EmoticonDefinitionReceived(this, e);
        }

        protected virtual void OnContactLeft(object sender, ContactConversationEventArgs e)
        {
            if (e.EndPoint != Guid.Empty)
                return;

            RemoveContact(e.Contact);

            if (ContactLeft != null)
                ContactLeft(this, e);

            if (e.Contact.IsSibling(RemoteOwner))
            {
                Contact oldOwner = RemoteOwner;
                if (SetNextRemoteOwner())
                {
                    OnRemoteOwnerChanged(new ConversationRemoteOwnerChangedEventArgs(oldOwner, RemoteOwner));
                }
            }
        }

        protected virtual void OnContactJoined(object sender, ContactConversationEventArgs e)
        {
            if (e.EndPoint != Guid.Empty)
            {
                //Wait until contacts from all locations have joined.
                return;
            }

            if (!AddContact(e.Contact))
                return;

            if (!_messenger.Conversations.Contains(this))
            {
                _messenger.Conversations.Add(this);
            }

            lock (_pendingInviteContacts)
                _pendingInviteContacts.Remove(e.Contact);

            conversationState = ConversationState.OneRemoteUserJoined;

            if (!e.Contact.IsSibling(Messenger.ContactList.Owner))
            {
                SetFirstJoinedContact(e.Contact);
            }

            lock (_messageQueues)
            {
                while (_messageQueues.Count > 0)
                {
                    MessageObject msgobj = _messageQueues.Dequeue();
                    if (msgobj is TextMessageObject)
                    {
                        SendTextMessage(msgobj.InnerObject as TextMessage);
                    }

                    if (msgobj is NudgeObject)
                    {
                        SendNudge();
                    }

                    if (msgobj is EmoticonObject)
                    {
                        SendEmoticonDefinitions(msgobj.InnerObject as List<Emoticon>, (msgobj as EmoticonObject).Type);
                    }
                }
            }


            if (ContactJoined != null)
                ContactJoined(this, e);
        }

        protected virtual void OnAllContactsLeft(object sender, EventArgs e)
        {
            if (AllContactsLeft != null)
                AllContactsLeft(this, e);
            EndSwitchBoardSession(true);
        }

        /// <summary>
        /// Create a new conversation which contains the same users as the expired one.
        /// </summary>
        /// <returns>A new conversation.</returns>
        /// <exception cref="InvalidOperationException">The current conversation is not expired.</exception>
        protected virtual Conversation ReCreate()
        {
            if (conversationState == ConversationState.OneRemoteUserJoined ||
                conversationState == ConversationState.SwitchboardRequestSent)
            {
                return this;
            }

            if (RemoteOwner.Status == PresenceStatus.Offline)
            {
                throw new InvalidOperationException("Contact " + RemoteOwner.Mail + " not online, please send offline message instead.");
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                Messenger.Nameserver.RequestSwitchboard(_switchboard, this);

                conversationState = ConversationState.SwitchboardRequestSent;
            }


            Ended = false;


            //This is a very important priciple:
            //If all contacts left, we try to re-invite the first contact ONLY.
            PendingContactEnqueue(RemoteOwner);

            return this;
        }

        protected virtual void OnRemoteOwnerChanged(ConversationRemoteOwnerChangedEventArgs e)
        {
            if (RemoteOwnerChanged != null)
                RemoteOwnerChanged(this, e);
        }


        #endregion

        #endregion

        #region Internal

        #endregion

        #region Public Events

        /// <summary>
        /// Fired when the owner is the only contact left. If the owner leaves too the connection is automatically closed by the server.
        /// </summary>
        public event EventHandler<EventArgs> AllContactsLeft;

        /// <summary>
        /// Fired when the session is closed, either by the server or by the local client.
        /// </summary>
        public event EventHandler<EventArgs> SessionClosed;

        /// <summary>
        /// Occurs when a switchboard connection has been made and the initial handshaking commands are send. This indicates that the session is ready to invite or accept other contacts.
        /// </summary>
        public event EventHandler<EventArgs> SessionEstablished;

        /// <summary>
        /// Fired when a contact joins. In case of a conversation with two people in it this event is called with the remote contact specified in the event argument.
        /// </summary>
        public event EventHandler<ContactConversationEventArgs> ContactJoined;
        /// <summary>
        /// Fired when a contact leaves the conversation.
        /// </summary>
        public event EventHandler<ContactConversationEventArgs> ContactLeft;

        /// <summary>
        /// Fired when a message is received from any of the other contacts in the conversation.
        /// </summary>
        public event EventHandler<TextMessageEventArgs> TextMessageReceived;

        /// <summary>
        /// Fired when a contact sends a emoticon definition.
        /// </summary>
        public event EventHandler<EmoticonDefinitionEventArgs> EmoticonDefinitionReceived;

        public event EventHandler<WinkEventArgs> WinkReceived;

        /// <summary>
        /// Fired when a contact sends a nudge
        /// </summary>
        public event EventHandler<ContactEventArgs> NudgeReceived;

        /// <summary>
        /// Fired when any of the other contacts is typing a message.
        /// </summary>
        public event EventHandler<ContactEventArgs> UserTyping;

        /// <summary>
        /// Occurs when an exception is thrown while handling the incoming or outgoing messages.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// Occurs when the MSN Switchboard Server sends us an error.
        /// </summary>
        public event EventHandler<MSNErrorEventArgs> ServerErrorReceived;

        /// <summary>
        /// Occurs after the remote owner left a multiple user conversation.
        /// </summary>
        public event EventHandler<ConversationRemoteOwnerChangedEventArgs> RemoteOwnerChanged;

        #endregion

        #region Public
        /// <summary>
        /// Fired when the data transfer for a MSNObject finished or aborted.
        /// </summary>
        public event EventHandler<ConversationMSNObjectDataTransferCompletedEventArgs> MSNObjectDataTransferCompleted;

        /// <summary>
        /// Occurs when a new conversation is ended (all contacts in the conversation have left or <see cref="Conversation.End()"/> is called).
        /// </summary>
        public event EventHandler<ConversationEndEventArgs> ConversationEnded;

        #region Properties

        /// <summary>
        /// Contacts once or currently in the conversation.
        /// </summary>
        public List<Contact> Contacts
        {
            get 
            { 
                return _contacts; 
            }
        }

        /// <summary>
        /// Indicates the type of current available switchboard in this conversation.
        /// </summary>
        public ConversationType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Indicates whether the conversation is ended by user.<br/>
        /// If a conversation is ended, it can't be used to send any message.
        /// </summary>
        public bool Ended
        {
            get 
            {
                lock (_syncObject)
                    return ended;
            }

            internal set
            {
                lock (_syncObject)
                    ended = value;
            }
        }

        /// <summary>
        /// Indicates whether emoticons from remote contacts are automatically retrieved
        /// </summary>
        public bool AutoRequestEmoticons
        {
            get
            {
                return autoRequestEmoticons;
            }
            set
            {
                autoRequestEmoticons = value;
            }
        }

        /// <summary>
        /// Indicates whether the conversation will never expired until the owner close it. <br/>
        /// If true, <see cref="Conversation.SessionClosed"/> will never fired and a keep-alive message will send to the switchboard every <see cref="KeepAliveMessagePeriod"/> seconds.
        /// </summary>
        /// <exception cref="InvalidOperationException">Setting this property on an ended conversation.</exception>
        /// <exception cref="NotSupportedException">Setting this property for a YIM conversation or an expired conversation.</exception>
        public bool AutoKeepAlive
        {
            get
            {
                if ((_type & ConversationType.YIM) == ConversationType.YIM)
                    return true;

                return autoKeepAlive;
            }

            set
            {
                if (Ended)
                {
                    throw new InvalidOperationException("Conversation ended.");
                }

                if ((_type & ConversationType.YIM) == ConversationType.YIM)
                {
                    //YIM handlers, expired handlers. Should I throw an exception here?
                    throw new NotSupportedException("Cannot set keep-alive property to true for this conversation type.");
                }


                if (value && autoKeepAlive != value)
                {
                    if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard || _type == ConversationType.None)
                    {
                        autoKeepAlive = value;
                        keepaliveTimer = new Timer(new TimerCallback(KeepAlive), this, keepalivePeriod, keepalivePeriod);
                        return;
                    }

                }

                if (!value && autoKeepAlive != value)
                {
                    autoKeepAlive = value;
                    keepaliveTimer.Dispose();
                    keepaliveTimer = null;
                }
            }
        }

        /// <summary>
        /// The period between two keep-alive messages sent (In second).
        /// </summary>
        public int KeepAliveMessagePeriod
        {
            get { return keepalivePeriod / 1000; }
            set
            {
                keepalivePeriod = value * 1000;
                if (keepaliveTimer != null)
                    keepaliveTimer.Change(keepalivePeriod, keepalivePeriod);
                else
                    keepaliveTimer = new Timer(new TimerCallback(KeepAlive), this, keepalivePeriod, keepalivePeriod);
            }
        }

        /// <summary>
        /// Messenger that created the conversation
        /// </summary>
        public Messenger Messenger
        {
            get
            {
                return _messenger;
            }
            set
            {
                _messenger = value;
            }
        }

        /// <summary>
        /// The switchboard handler. Handles incoming/outgoing messages.<br/>
        /// If the conversation ended, this property will be null.
        /// </summary>
        public SBMessageHandler Switchboard
        {
            get
            {
                if ((_type & ConversationType.SwitchBoard) != ConversationType.SwitchBoard)
                {
                    return null;
                }
                else
                {
                    return _switchboard;
                }
            }
        }

        /// <summary>
        /// The remote owner of this conversation.
        /// </summary>
        public Contact RemoteOwner
        {
            get { return _firstContact; }
        }

        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The messenger object that requests the conversation.</param>
        /// <param name="sbHandler">The switchboard to interface to.</param>		
        internal Conversation(Messenger parent, SBMessageHandler sbHandler)
        {
            _switchboard = sbHandler;
            _type = ConversationType.SwitchBoard;
            _messenger = parent;
            IniCommonSettings();
            conversationState = ConversationState.SwitchboardRequestSent;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">The messenger object that requests the conversation.</param>
        internal Conversation(Messenger parent)
        {
            _messenger = parent;

            _type = ConversationType.None;
            IniCommonSettings();
        }

        /// <summary>
        /// Invite a remote contact to join the conversation.
        /// </summary>
        /// <param name="contactMail">Contact account</param>
        /// <param name="type">Contact type</param>
        /// <exception cref="InvalidOperationException">Operating on an ended conversation.</exception>
        /// <exception cref="NotSupportedException">Inviting mutiple YIM users into a YIM conversation, invite YIM users to a switchboard conversation, or passport members are invited into YIM conversation.</exception>
        public SBMessageHandler Invite(string contactMail, ClientType type)
        {
            if ((_type & ConversationType.YIM) == ConversationType.YIM && 
                type != ClientType.EmailMember)
            {
                throw new NotSupportedException("Only Yahoo messenger users can be invited in a YIM conversation.");
            }


            if ((_type & ConversationType.YIM) == ConversationType.YIM && 
                type == ClientType.EmailMember)
            {
                if (_contacts.Count > 1)
                    throw new NotSupportedException("Mutiple user not supported in YIM conversation.");
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard &&
                (type != ClientType.PassportMember && 
                type != ClientType.LCS))
            {
                throw new NotSupportedException("Only Passport members can be invited in a switchboard conversation.");
            }

            if (_type == ConversationType.None)
            {
                switch (type)
                {
                    case ClientType.EmailMember:
                        _type = ConversationType.YIM;
                        break;

                    case ClientType.LCS:
                    case ClientType.PassportMember:
                        _type = ConversationType.SwitchBoard;
                        break;
                }
            }

            if (!Messenger.ContactList.HasContact(contactMail, type))
            {
                throw new MSNPSharpException("Contact not on your contact list.");
            }

            Contact contact = Messenger.ContactList.GetContact(contactMail, type);  //Only contacts on default addressbook can join conversations.
            if (contact.Status == PresenceStatus.Offline)
            {
                throw new InvalidOperationException("Contact " + contactMail + " not online, please send offline message instead.");
            }

            if (Ended)
            {
                ReCreate();
                PendingContactEnqueue(contact);  //Must added after recreate.
            }
            else
            {
                if (RemoteOwner == null)
                {
                    _firstContact = contact;
                }

                #region Initialize SBMessageHandler and invite.

                if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
                {

                    if (conversationState == ConversationState.ConversationCreated ||
                        conversationState == ConversationState.SwitchboardEnded ||
                        conversationState == ConversationState.ConversationEnded)
                    {
                        PendingContactEnqueue(contact);  //Enqueue the contact if user send message before it join.
                        Messenger.Nameserver.RequestSwitchboard(_switchboard, this);

                        conversationState = ConversationState.SwitchboardRequestSent;
                        return _switchboard;
                    }

                    bool inviteResult = _switchboard.Invite(contact);
                    if (inviteResult && _switchboard.GetRosterUniqueUserCount() > 1)
                    {
                        _type |= ConversationType.MutipleUsers;
                    }
                   

                }
                 #endregion

            }

            return _switchboard;
        }

        /// <summary>
        /// Invite a remote contact to join the conversation.
        /// </summary>
        /// <param name="contact">The remote contact to invite.</param>
        /// <exception cref="InvalidOperationException">Operating on an expired conversation will get this exception.</exception>
        /// <exception cref="NotSupportedException">Inviting mutiple YIM users into a YIM conversation, invite YIM users to a switchboard conversation, or passport members are invited into YIM conversation.</exception>
        public SBMessageHandler Invite(Contact contact)
        {
            return Invite(contact.Mail, contact.ClientType);
        }

        /// <summary>
        /// End this conversation.
        /// </summary>
        public void End()
        {
            ending = true;
            EndSwitchBoardSession(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="remoteDisconnect">
        /// If all the contacts left the conversation, this should be true.
        /// if the current user want to left, this should set to false.</param>
        private void EndSwitchBoardSession(bool remoteDisconnect)
        {
            if (conversationState >= ConversationState.SwitchboardRequestSent &&
                conversationState < ConversationState.SwitchboardEnded)
            {
                Thread endthread = new Thread(new ParameterizedThreadStart(SwitchBoardEnd));  //Avoid blocking the UI thread.
                endthread.Start(remoteDisconnect);
            }

            if (keepaliveTimer != null)
                keepaliveTimer.Dispose();
        }

        /// <summary>
        /// Whether the specified contact or its sibling is in the conversation.
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public bool HasContact(Contact contact)
        {
            if (_type == ConversationType.None)
                return false;


            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                if (_switchboard.HasContact(contact))
                    return true;

                if (IsPendingContact(contact))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Sends a plain text message to all other contacts in the conversation.
        /// </summary>
        /// <remarks>
        /// This method wraps the TextMessage object in a SBMessage object and sends it over the network.
        /// </remarks>
        /// <param name="message">The message to send.</param>
        /// <exception cref="InvalidOperationException">Sending messages from an ended conversation.</exception>
        public void SendTextMessage(TextMessage message)
        {
            _type |= ConversationType.Chat;

            if (Ended)
            {
                ReCreate();
            }

            if (conversationState != ConversationState.OneRemoteUserJoined)
            {
                MessageEnqueue(new TextMessageObject(message));
                return;
            }


            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                _switchboard.SendTextMessage(message);
            }
        }

        /// <summary>
        /// Sends a 'user is typing..' message to the switchboard, and is received by all participants.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sending messages from an ended conversation.</exception>
        public void SendTypingMessage()
        {
            _type |= ConversationType.Chat;

            if (Ended)
            {
                return;
            }


            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard && conversationState == ConversationState.OneRemoteUserJoined)
            {
                _switchboard.SendTypingMessage();
            }
        }

        /// <summary>
        /// Sends a 'nudge' message to the switchboard, and is received by all participants.
        /// </summary>
        /// <exception cref="InvalidOperationException">Sending messages from an ended conversation.</exception>
        public void SendNudge()
        {
            _type |= ConversationType.Chat;

            if (Ended)
            {
                ReCreate();
            }

            if (conversationState != ConversationState.OneRemoteUserJoined)
            {
                MessageEnqueue(new NudgeObject());
                return;
            }


            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                _switchboard.SendNudge();
            }
        }

        /// <summary>
        /// Sends the definition for a list of emoticons to all other contacts in the conversation. The client-programmer must use this function if a text messages uses multiple emoticons in a single message.
        /// </summary>
        /// <remarks>Use this function before sending text messages which include the emoticon text. You can only send one emoticon message before the textmessage. So make sure that all emoticons used in the textmessage are included.</remarks>
        /// <param name="emoticons">A list of emoticon objects.</param>
        /// <param name="icontype">The type of current emoticons.</param>
        /// <exception cref="InvalidOperationException">Operating on an ended conversation.</exception>
        /// <exception cref="NotSupportedException">Sending custom emoticons from a YIM conversation.</exception>
        public void SendEmoticonDefinitions(List<Emoticon> emoticons, EmoticonType icontype)
        {
            _type |= ConversationType.Chat;
            if (Ended)
            {
                ReCreate();
            }

            if (conversationState != ConversationState.OneRemoteUserJoined)
            {
                MessageEnqueue(new EmoticonObject(emoticons, icontype));
                return;
            }

            if ((_type & ConversationType.YIM) == ConversationType.YIM)
            {
                throw new NotSupportedException("YIM conversation not support sending custom emoticons.");
            }

            if ((_type & ConversationType.SwitchBoard) == ConversationType.SwitchBoard)
            {
                _switchboard.SendEmoticonDefinitions(emoticons, icontype);
            }
        }

        #endregion


    }
};
