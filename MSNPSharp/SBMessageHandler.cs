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
using System.Web;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    #region Event argument classes
    /// <summary>
    /// Used when a new switchboard session is affected.
    /// </summary>
    public class SBEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private SBMessageHandler switchboard;

        /// <summary>
        /// The affected switchboard
        /// </summary>
        public SBMessageHandler Switchboard
        {
            get
            {
                return switchboard;
            }
            set
            {
                switchboard = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SBEventArgs(SBMessageHandler switchboard)
        {
            this.switchboard = switchboard;
        }
    }

    /// <summary>
    /// Used when a new switchboard session is created.
    /// </summary>
    public class SBCreatedEventArgs : EventArgs
    {
        private object initiator;
        private SBMessageHandler switchboard;
        private string account = string.Empty;
        private string name = string.Empty;
        private bool anonymous = false;

        /// <summary>
        /// The affected switchboard
        /// </summary>
        public SBMessageHandler Switchboard
        {
            get
            {
                return switchboard;
            }
            set
            {
                switchboard = value;
            }
        }


        /// <summary>
        /// The object that requested the switchboard. Null if the switchboard session was initiated by a remote client.
        /// </summary>
        public object Initiator
        {
            get
            {
                return initiator;
            }
        }

        /// <summary>
        /// The account of user that requested the switchboard.
        /// </summary>
        public string Account
        {
            get
            {
                return account;
            }
        }

        /// <summary>
        ///  The nick name of user that requested the switchboard.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Anonymous request, usually from webchat users.
        /// </summary>
        public bool Anonymous
        {
            get
            {
                return anonymous;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SBCreatedEventArgs(SBMessageHandler switchboard, object initiator)
        {
            this.switchboard = switchboard;
            this.initiator = initiator;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public SBCreatedEventArgs(SBMessageHandler switchboard, object initiator, string account, string name, bool anonymous)
        {
            this.switchboard = switchboard;
            this.initiator = initiator;
            this.account = account;
            this.name = name;
            this.anonymous = anonymous;
        }
    }

    /// <summary>
    /// Use when the acknowledgement of a message received.
    /// </summary>
    public class SBMessageDeliverResultEventArgs : SBEventArgs
    {
        private bool success = false;

        /// <summary>
        /// Whether the specified message has been successfully delivered.
        /// </summary>
        public bool Success
        {
            get { return success; }
        }

        private int messageID = 0;

        /// <summary>
        /// The transision ID of the message.
        /// </summary>
        public int MessageID
        {
            get { return messageID; }
        }

        public SBMessageDeliverResultEventArgs(bool success, int transId, SBMessageHandler switchBoard)
            : base(switchBoard)
        {
            this.success = success;
            messageID = transId;
        }
    }

    #endregion

    /// <summary>
    /// Handles the text messages from the switchboard server.<br/>
    /// Text messages includes the user typing message, plain text messages and nudge messages.
    /// </summary>
    public class SBMessageHandler : IMessageHandler
    {
        private SBMessageHandler()
        {
        }

        protected virtual void Initialize(NSMessageHandler handler)
        {
            nsMessageHandler = handler;
            SetNewProcessor();

            ResigerHandlersToProcessor(MessageProcessor);
            NSMessageHandler.P2PHandler.AddSwitchboardSession(this);
            NSMessageHandler.ContactOffline += new EventHandler<ContactEventArgs>(ContactOfflineHandler);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected internal SBMessageHandler(NSMessageHandler handler)
        {
            Initialize(handler);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        protected internal SBMessageHandler(NSMessageHandler handler, Contact callingContact, string sessionHash, string sessionId)
        {
            Initialize(handler);

            SessionId = sessionId;
            SessionHash = sessionHash;
            this.invited = true;

            NSMessageHandler.SetSession(SessionId, SessionHash);

        }

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
        /// Fired when a switchboard connection has been made and the initial handshaking commands are send. This indicates that the session is ready to invite or accept other contacts.
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

        /// <summary>
        /// Fired when a contact sends a wink.
        /// </summary>
        public event EventHandler<WinkEventArgs> WinkReceived;

        /// <summary>
        /// Fired when a contact sends a nudge.
        /// </summary>
        public event EventHandler<ContactEventArgs> NudgeReceived;

        /// <summary>
        /// Fired when any of the other contacts is typing a message.
        /// </summary>
        public event EventHandler<ContactEventArgs> UserTyping;

        /// <summary>
        /// Fired when an exception is thrown while handling the incoming or outgoing messages.
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionOccurred;

        /// <summary>
        /// Fired when the MSN Switchboard Server sends us an error.
        /// </summary>
        public event EventHandler<MSNErrorEventArgs> ServerErrorReceived;

        /// <summary>
        /// Fired when the MSN Switchboard Server sends us an acknowledgement (ACK or NAK).
        /// </summary>
        public event EventHandler<SBMessageDeliverResultEventArgs> MessageAcknowledgementReceived;

        #endregion

        #region Members

        private Dictionary<string, string> rosterName = new Dictionary<string, string>(0);
        private Dictionary<string, string> rosterCapacities = new Dictionary<string, string>(0);
        private Dictionary<string, ContactConversationState> rosterState = new Dictionary<string, ContactConversationState>(0);

        private Dictionary<string, MimeMessage> multiPacketMessages = new Dictionary<string, MimeMessage>();
        private object syncObject = new object();
        private Queue<Contact> invitationQueue = new Queue<Contact>();

        protected SocketMessageProcessor messageProcessor = null;
        private NSMessageHandler nsMessageHandler = null;

        private string sessionId = string.Empty;
        private string sessionHash = string.Empty;
        protected bool sessionEstablished = false;
        protected bool invited = false;
        private bool waitingForRing = false;

        #endregion

        #region Properties

        /// <summary>
        /// The nameserver that received the request for the switchboard session
        /// </summary>
        internal NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        /// <summary>
        /// Indicates if the local client was invited to the session
        /// </summary>
        public bool Invited
        {
            get
            {
                return invited;
            }
        }

        /// <summary>
        /// Indicates if the session is ready to send/accept commands. E.g. the initial handshaking and identification has been completed.
        /// </summary>
        public bool IsSessionEstablished
        {
            get
            {
                return sessionEstablished;
            }

            private set
            {
                sessionEstablished = value;
            }
        }

        protected string SessionId
        {
            get
            {
                return sessionId;
            }

            set
            {
                sessionId = value;
            }
        }


        /// <summary>
        /// The hash identifier used to define this switchboard session.
        /// </summary>
        internal string SessionHash
        {
            get
            {
                return sessionHash;
            }

            private set
            {
                sessionHash = value;
            }
        }

        /// <summary>
        /// Implements the P2P framework. This object is automatically created when a succesfull connection was made to the switchboard.
        /// </summary>
        [Obsolete("Please use Messenger.P2PHandler.", true)]
        public P2PHandler P2PHandler
        {
            get
            {
                throw new MSNPSharpException("Please use Messenger.P2PHandler.");
            }
            set
            {
                throw new MSNPSharpException("Please use Messenger.P2PHandler.");
            }
        }

        #endregion

        #region Invitation

        /// <summary>
        /// Invites the specified contact to the switchboard.
        /// </summary>
        /// <remarks>
        /// If there is not yet a connection established the invitation will be stored in a queue and processed as soon as a connection is established.
        /// </remarks>
        /// <param name="contact">The contact's account to invite.</param>
        public virtual bool Invite(Contact contact)
        {
            return Invite(contact, Guid.Empty);
        }

        /// <summary>
        /// Called when a switchboard session is created on request of a local client.
        /// </summary>
        /// <param name="sessionHash"></param>
        public void SetInvitation(string sessionHash)
        {
            SessionHash = sessionHash;
            SessionId = SessionHash.Split('.')[0];

            NSMessageHandler.SetSession(SessionId, SessionHash);

            this.invited = false;
        }

        /// <summary>
        /// Left the current switchboard conversation.
        /// </summary>
        public void Left()
        {
            MessageProcessor.SendMessage(new SBMessage("OUT", new string[] { }));
        }

        public virtual void Close()
        {
            Close(false);
        }

        /// <summary>
        /// Left the conversation then closes the switchboard session by disconnecting from the server. 
        /// </summary>
        protected virtual void Close(bool causeByRemote)
        {
            if (MessageProcessor != null)
            {
                if (!causeByRemote)
                {
                    SendSwitchBoardClosedNotifyToNS();
                }

                try
                {
                    SocketMessageProcessor processor = MessageProcessor as SocketMessageProcessor;
                    if (processor != null)
                    {
                        if (processor.Connected)
                        {

                            //We want to left the conversation, say "OUT" to SB.
                            Left();
                            processor.Disconnect();
                        }
                        
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().ToString());
                }
                finally
                {

                    MessageProcessor.UnregisterHandler(this);
                }
            }

            if (IsSessionEstablished)
            {
                OnSessionClosed();
            }
        }

        #endregion

        #region Messaging

        /// <summary>
        /// Sends a plain text message to all other contacts in the conversation.
        /// </summary>
        /// <remarks>
        /// This method wraps the TextMessage object in a SBMessage object and sends it over the network.
        /// </remarks>
        /// <param name="message">The message to send.</param>
        public virtual void SendTextMessage(TextMessage message)
        {

            //First, we have to check whether the content of the message is not to big for one message
            //There's a maximum of 1600 bytes per message, let's play safe and take 1400 bytes
            UTF8Encoding encoding = new UTF8Encoding();

            if (encoding.GetByteCount(message.Text) > 1400)
            {
                //we'll have to use multi-packets messages
                Guid guid = Guid.NewGuid();
                byte[] text = encoding.GetBytes(message.Text);
                int chunks = Convert.ToInt32(Math.Ceiling((double)text.GetUpperBound(0) / (double)1400));
                for (int i = 0; i < chunks; i++)
                {
                    SBMessage sbMessage = new SBMessage();
                    sbMessage.Acknowledgement = "N";

                    //Clone the message
                    TextMessage chunkMessage = (TextMessage)message.Clone();

                    //Get the part of the message that we are going to send
                    if (text.GetUpperBound(0) - (i * 1400) > 1400)
                        chunkMessage.Text = encoding.GetString(text, i * 1400, 1400);
                    else
                        chunkMessage.Text = encoding.GetString(text, i * 1400, text.GetUpperBound(0) - (i * 1400));

                    MimeMessage msgMessage = WrapMessage(chunkMessage);

                    //Add the correct headers
                    msgMessage.MimeHeader.Add("Message-ID", "{" + guid.ToString() + "}");

                    if (i == 0)
                        msgMessage.MimeHeader.Add("Chunks", Convert.ToString(chunks));
                    else
                        msgMessage.MimeHeader.Add("Chunk", Convert.ToString(i));

                    sbMessage.InnerMessage = msgMessage;

                    //send it over the network
                    MessageProcessor.SendMessage(sbMessage);
                }
            }
            else
            {
                SBMessage sbMessage = new SBMessage();
                sbMessage.Acknowledgement = "N";

                sbMessage.InnerMessage = WrapMessage(message);

                // send it over the network
                MessageProcessor.SendMessage(sbMessage);
            }
        }

        /// <summary>
        /// Sends the definition for a list of emoticons to all other contacts in the conversation. The client-programmer must use this function if a text messages uses multiple emoticons in a single message.
        /// </summary>
        /// <remarks>Use this function before sending text messages which include the emoticon text. You can only send one emoticon message before the textmessage. So make sure that all emoticons used in the textmessage are included.</remarks>
        /// <param name="emoticons">A list of emoticon objects.</param>
        /// <param name="icontype">The type of current emoticons.</param>
        public virtual void SendEmoticonDefinitions(List<Emoticon> emoticons, EmoticonType icontype)
        {
            if (emoticons == null)
                throw new ArgumentNullException("emoticons");

            foreach (Emoticon emoticon in emoticons)
            {
                if (!NSMessageHandler.ContactList.Owner.Emoticons.ContainsKey(emoticon.Sha))
                {
                    // Add the emotions to owner's emoticon collection.
                    NSMessageHandler.ContactList.Owner.Emoticons.Add(emoticon.Sha, emoticon);
                }
            }

            EmoticonMessage emoticonMessage = new EmoticonMessage(emoticons, icontype);

            SBMessage sbMessage = new SBMessage();
            sbMessage.Acknowledgement = "N";
            sbMessage.InnerMessage = WrapMessage(emoticonMessage);

            // send it over the network
            MessageProcessor.SendMessage(sbMessage);
        }

        /// <summary>
        /// Sends a 'user is typing..' message to the switchboard, and is received by all participants.
        /// </summary>
        public virtual void SendTypingMessage()
        {
            SBMessage sbMessage = new SBMessage();
            sbMessage.Acknowledgement = "U";

            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-msmsgscontrol";

            mimeMessage.MimeHeader[MimeHeaderStrings.TypingUser] = NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant();

            mimeMessage.InnerMessage = new TextPayloadMessage("\r\n");

            sbMessage.InnerMessage = mimeMessage;

            MessageProcessor.SendMessage(sbMessage);
        }

        /// <summary>
        /// Sends a 'nudge' message to the switchboard, and is received by all participants.
        /// </summary>
        public virtual void SendNudge()
        {
            // send it over the network
            SBMessage sbMessage = new SBMessage();

            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-msnmsgr-datacast";

            MimeMessage innerMimeMessage = new MimeMessage(false);
            innerMimeMessage.MimeHeader["ID"] = "1";

            mimeMessage.InnerMessage = innerMimeMessage;

            sbMessage.InnerMessage = mimeMessage;

            MessageProcessor.SendMessage(sbMessage);
        }

        /// <summary>
        /// Send a keep-alive message to avoid the switchboard closing. This is useful for bots.
        /// </summary>
        public virtual void SendKeepAliveMessage()
        {
            SBMessage sbMessage = new SBMessage();

            MimeMessage mimeMessage = new MimeMessage();
            mimeMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-keepalive";

            mimeMessage.InnerMessage = new TextPayloadMessage("\r\n");

            sbMessage.InnerMessage = mimeMessage;

            MessageProcessor.SendMessage(sbMessage);
        }

        #endregion

        #region Protected User Methods

        /// <summary>
        /// Fires the <see cref="AllContactsLeft"/> event.
        /// </summary>		
        protected virtual void OnAllContactsLeft()
        {
            if (AllContactsLeft != null)
            {
                AllContactsLeft(this, new EventArgs());
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, ToString() + " all contacts left, disconnect automately.", GetType().ToString());
            Close(false);
        }

        /// <summary>
        /// Fires the <see cref="ContactJoined"/> event.
        /// </summary>
        /// <param name="contact">The contact who joined the session.</param>
        /// <param name="endPoint">The machine guid where this contact joined from.</param>
        protected virtual void OnContactJoined(Contact contact, Guid endPoint)
        {
            if (ContactJoined != null)
            {
                ContactJoined(this, new ContactConversationEventArgs(contact, endPoint));
            }
        }

        /// <summary>
        /// Fires the <see cref="ContactLeft"/> event.
        /// </summary>
        /// <param name="contact">The contact who left the session.</param>
        /// <param name="endPoint">The machine guid where this contact left from.</param>
        protected virtual void OnContactLeft(Contact contact, Guid endPoint)
        {
            if (ContactLeft != null)
            {
                ContactLeft(this, new ContactConversationEventArgs(contact, endPoint));
            }
        }

        /// <summary>
        /// Fires the <see cref="UserTyping"/> event.
        /// </summary>
        /// <param name="message">The emoticon message.</param>
        /// <param name="contact">The contact who is sending the definition.</param>
        protected virtual void OnEmoticonDefinition(MimeMessage message, Contact contact)
        {
            EmoticonMessage emoticonMessage = new EmoticonMessage();
            emoticonMessage.CreateFromParentMessage(message);

            if (EmoticonDefinitionReceived != null)
            {
                foreach (Emoticon emoticon in emoticonMessage.Emoticons)
                {
                    EmoticonDefinitionReceived(this, new EmoticonDefinitionEventArgs(contact, emoticon));
                }
            }
        }

        /// <summary>
        /// Fires the <see cref="NudgeReceived"/> event.
        /// </summary>
        /// <param name="contact">The contact who is sending the nudge.</param>
        protected virtual void OnNudgeReceived(Contact contact)
        {
            if (NudgeReceived != null)
            {
                NudgeReceived(this, new ContactEventArgs(contact));
            }
        }

        /// <summary>
        /// Fires the <see cref="UserTyping"/> event.
        /// </summary>
        /// <param name="contact">The contact who is typing.</param>
        protected virtual void OnUserTyping(Contact contact)
        {
            // make sure we don't parse the rest of the message in the next loop											
            if (UserTyping != null)
            {
                // raise the event
                UserTyping(this, new ContactEventArgs(contact));
            }
        }

        protected virtual void OnWinkReceived(MimeMessage message, Contact contact)
        {
            string body = System.Text.Encoding.UTF8.GetString(message.InnerBody);

            Wink obj = new Wink();
            obj.SetContext(body, false);

            if (WinkReceived != null)
                WinkReceived(this, new WinkEventArgs(contact, obj));
        }

        /// <summary>
        /// Fires the <see cref="TextMessageReceived"/> event.
        /// </summary>
        /// <param name="message">The message send.</param>
        /// <param name="contact">The contact who sended the message.</param>
        protected virtual void OnTextMessageReceived(TextMessage message, Contact contact)
        {
            if (TextMessageReceived != null)
                TextMessageReceived(this, new TextMessageEventArgs(message, contact));
        }

        protected virtual void OnMessageAcknowledgementReceived(SBMessageDeliverResultEventArgs args)
        {
            if (MessageAcknowledgementReceived != null)
                MessageAcknowledgementReceived(this, args);
        }

        /// <summary>
        /// Fires the SessionClosed event.
        /// </summary>
        protected virtual void OnSessionClosed()
        {
            IsSessionEstablished = false;
            ClearAll();  //Session closed, force all contact left this conversation.

            if (SessionClosed != null)
                SessionClosed(this, new EventArgs());
        }

        /// <summary>
        /// Fires the SessionEstablished event and processes invitations in the queue.
        /// </summary>
        protected virtual void OnSessionEstablished()
        {
            IsSessionEstablished = true;
            if (SessionEstablished != null)
                SessionEstablished(this, new EventArgs());

            // process ant remaining invitations
            SendOneQueuedInvitation();
        }

        /// <summary>
        /// Handles all remaining invitations. If no connection is yet established it will do nothing.
        /// </summary>
        protected virtual void SendOneQueuedInvitation()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Processing invitations for switchboard...", GetType().Name);

            lock (syncObject)
            {
                if (IsSessionEstablished && !waitingForRing)
                {
                    if (invitationQueue.Count > 0)
                    {
                        Contact targetContact = invitationQueue.Dequeue();
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Invitation to contact " + targetContact + " has been sent.", GetType().Name);
                        SendInvitationCommand(targetContact);
                        waitingForRing = true;
                    }
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Invitation to contact request has been denied since SwitchBoard session is in waitingForRING status.");
                }
            }
        }

        /// <summary>
        /// Sends the invitation command to the switchboard server.
        /// </summary>
        /// <param name="contact"></param>
        protected virtual void SendInvitationCommand(Contact contact)
        {
            MessageProcessor.SendMessage(new SBMessage("CAL", new string[] { contact.Mail }));
        }

        /// <summary>
        /// Send the first message to the server.
        /// </summary>
        /// <remarks>
        /// Depending on the <see cref="Invited"/> field a ANS command (if true), or a USR command (if false) is send.
        /// </remarks>
        protected virtual void SendInitialMessage()
        {
            string auth = NSMessageHandler.ContactList.Owner.Mail + ";" + NSMessageHandler.MachineGuid.ToString("B");

            if (Invited)
                MessageProcessor.SendMessage(new SBMessage("ANS", new string[] { auth, SessionHash, SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture) }));
            else
                MessageProcessor.SendMessage(new SBMessage("USR", new string[] { auth, SessionHash }));
        }

        protected virtual void SendSwitchBoardClosedNotifyToNS()
        {
            NSMessageHandler.SendSwitchBoardClosedNotify(SessionId.ToString());
        }

        protected virtual void SetNewProcessor()
        {
            messageProcessor = new SBMessageProcessor(NSMessageHandler.ConnectivitySettings);

            // catch the connect event to start sending the USR command upon initiating
            messageProcessor.ConnectionEstablished += OnProcessorConnectCallback;
            messageProcessor.ConnectionClosed += OnProcessorDisconnectCallback;
        }

        protected virtual void ResigerHandlersToProcessor(IMessageProcessor processor)
        {
            if (processor != null)
            {
                processor.RegisterHandler(this);
                processor.RegisterHandler(NSMessageHandler.P2PHandler);
            }
        }

        protected virtual void SetRosterProperty(string key, string value, RosterProperties property)
        {
            switch (property)
            {
                case RosterProperties.Name:
                    lock (rosterName)
                        rosterName[key.ToLowerInvariant()] = value;
                    break;
                case RosterProperties.ClientCapacityString:
                    lock (rosterCapacities)
                        rosterCapacities[key.ToLowerInvariant()] = value;
                    break;
                case RosterProperties.Status:
                    lock (rosterState)
                        rosterState[key.ToLowerInvariant()] = (ContactConversationState)Enum.Parse(typeof(ContactConversationState), value);
                    break;
            }
        }

        protected virtual bool IsAllContactsInRosterLeft()
        {
            lock (rosterState)
            {
                foreach (string key in rosterState.Keys)
                {
                    if (rosterState[key] != ContactConversationState.Left &&
                        NSMessageHandler.ContactList.Owner != null &&
                        EndPointData.GetAccountFromUniqueEPIDString(key) != NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant())
                    {
                        // If: There is only one owner without any endpoint id, the switchboard is ended.
                        // If: There is/are owner(s) with endpoint id(s) and status is/are not left, keep the switch available.
                        return false;
                    }
                }

                return true;
            }
        }

        protected virtual bool Invite(Contact contact, Guid endPoint)
        {
            string fullaccount = contact.Mail.ToLowerInvariant();
            object status = null;

            if (endPoint != Guid.Empty)
            {
                fullaccount += ";" + endPoint.ToString("B").ToLowerInvariant();
            }

            status = GetRosterProperty(fullaccount, RosterProperties.Status);
            if (status != null)
            {
                if (((ContactConversationState)status) != ContactConversationState.Left)
                {
                    //Invite repeatly.
                    return false;
                }
            }

            //Add "this contact"
            SetRosterProperty(fullaccount, ContactConversationState.Invited.ToString(), RosterProperties.Status);

            if (endPoint == Guid.Empty)
            {
                //Add "all contact"
                SetRosterProperty(contact.Mail.ToLowerInvariant(), ContactConversationState.Invited.ToString(), RosterProperties.Status);
                if (contact.HasSignedInWithMultipleEndPoints)  //Set every enpoints as Invited.
                {
                    foreach (Guid epId in contact.EndPointData.Keys)
                    {
                        if (epId == Guid.Empty) continue;
                        string currentAccount = contact.Mail.ToLowerInvariant() + ";" + epId.ToString("B").ToLowerInvariant();
                        SetRosterProperty(currentAccount, ContactConversationState.Invited.ToString(), RosterProperties.Status);
                    }
                }
            }

            invitationQueue.Enqueue(contact);
            SendOneQueuedInvitation();

            return true;
        }

        protected virtual object GetRosterProperty(string key, RosterProperties property)
        {
            string value = string.Empty;
            string lowerKey = key.ToLowerInvariant();

            switch (property)
            {
                case RosterProperties.Name:
                    lock (rosterName)
                    {
                        if (!rosterName.ContainsKey(lowerKey))
                            return null;
                        return rosterName[lowerKey];
                    }

                case RosterProperties.ClientCapacities:
                    lock (rosterCapacities)
                    {
                        if (!rosterCapacities.ContainsKey(lowerKey))
                            return null;

                        value = rosterCapacities[lowerKey];
                        int cap = 0;
                        if (value.Contains(":"))
                        {
                            int.TryParse(value.Split(':')[0], out cap);
                            return (ClientCapacities)cap;
                        }

                        return ClientCapacities.None;
                    }

                case RosterProperties.ClientCapacitiesEx:
                    lock (rosterCapacities)
                    {
                        if (!rosterCapacities.ContainsKey(lowerKey))
                            return null;

                        value = rosterCapacities[lowerKey];
                        int cap = 0;
                        if (value.Contains(":"))
                        {
                            int.TryParse(value.Split(':')[1], out cap);
                            return (ClientCapacitiesEx)cap;
                        }

                        return ClientCapacitiesEx.None;
                    }
                case RosterProperties.ClientCapacityString:
                    lock (rosterCapacities)
                    {
                        if (!rosterCapacities.ContainsKey(lowerKey))
                            return null;
                        return rosterCapacities[lowerKey];
                    }
                case RosterProperties.Status:
                    lock (rosterState)
                    {
                        if (!rosterState.ContainsKey(lowerKey))
                            return null;
                        return rosterState[lowerKey];
                    }
            }

            return null;
        }

        protected virtual void ContactOfflineHandler(object sender, ContactEventArgs e)
        {
            Contact contact = e.Contact;

            if (HasContact(contact) && GetRosterUniqueUserCount() == 1)
            {
                Close(true);
            }
        }

        internal virtual int GetRosterUserCount()
        {
            int count = 0;
            lock (rosterState)
            {
                foreach (string key in rosterState.Keys)
                {
                    if (NSMessageHandler.ContactList.Owner != null)
                    {
                        if (EndPointData.GetAccountFromUniqueEPIDString(key) != NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant())
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }

        internal virtual int GetRosterUniqueUserCount()
        {
            Dictionary<string, string> uniqueUsers = new Dictionary<string, string>(0);

            lock (rosterState)
            {
                foreach (string key in rosterState.Keys)
                {
                    if (NSMessageHandler.ContactList.Owner != null)
                    {
                        uniqueUsers[EndPointData.GetAccountFromUniqueEPIDString(key)] = string.Empty;
                    }
                }

                return uniqueUsers.Count;
            }
        }

        internal bool HasContact(Contact contact)
        {
            lock (rosterState)
            {
                if (HasContact(contact.Mail))
                    return true;

                if (HasContact(contact.Mail, contact.Guid))
                    return true;

                lock (contact.SyncObject)
                {
                    foreach (Guid epId in contact.EndPointData.Keys)
                    {
                        if (epId == Guid.Empty) continue;

                        if (HasContact(contact.Mail, epId))
                            return true;
                    }
                }

                return false;
            }
        }

        internal bool HasContact(string uniqueEndPointIDString)
        {
            lock (rosterState)
                return rosterState.ContainsKey(uniqueEndPointIDString.ToLowerInvariant());
        }

        internal bool HasContact(string account, Guid place)
        {
            string fullaccount = account.ToLowerInvariant() + ";" + place.ToString("B").ToLowerInvariant();
            lock (rosterState)
                return rosterState.ContainsKey(fullaccount);
        }

        #endregion

        #region Protected CMDs

        /// <summary>
        /// Called when a ANS command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has replied to our identification ANS command.
        /// <code>ANS [Transaction] ['OK']</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnANSReceived(SBMessage message)
        {
            if (message.CommandValues[0].ToString() == "OK")
            {
                // we are now ready to invite other contacts. Notify the client of this.
                OnSessionEstablished();
            }
        }

        /// <summary>
        /// Called when a BYE command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a remote contact has leaved the session.
        /// This will fire the <see cref="ContactLeft"/> event. Or, if all contacts have left, the <see cref="AllContactsLeft"/> event.
        /// <code>BYE [account[;GUID]] [Client Type]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnBYEReceived(SBMessage message)
        {
            string fullaccount = message.CommandValues[0].ToString().ToLowerInvariant();

            ContactConversationState oldStatus = ContactConversationState.None;
            object result = GetRosterProperty(fullaccount, RosterProperties.Status);
            if (result != null)
            {
                oldStatus = (ContactConversationState)(result);
            }

            if (oldStatus == ContactConversationState.Left || oldStatus == ContactConversationState.None)
                return;

            SetRosterProperty(fullaccount, ContactConversationState.Left.ToString(), RosterProperties.Status);

            Guid endPointId = Guid.Empty;
            string account = fullaccount;

            if (fullaccount.Contains(";"))
            {
                string[] accountGuid = fullaccount.Split(';');
                account = accountGuid[0];
                endPointId = new Guid(accountGuid[1]);
            }

            if (NSMessageHandler.ContactList.Owner != null && account == NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant())
            {
                if (IsAllContactsInRosterLeft())
                {
                    OnAllContactsLeft();
                }
                return;
            }

            Contact contact = (message.CommandValues.Count >= 2) ?
                NSMessageHandler.ContactList.GetContact(account, (ClientType)Enum.Parse(typeof(ClientType), message.CommandValues[1].ToString()))
                :
                NSMessageHandler.ContactList.GetContact(account, ClientType.PassportMember);

            OnContactLeft(contact, endPointId); // notify the client programmer

            if (IsAllContactsInRosterLeft())
            {
                OnAllContactsLeft();  //Indicates whe should end the conversation and disconnect.
            }
        }

        /// <summary>
        /// Called when a CAL command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has replied to our request to invite a contact.
        /// <code>CAL [Transaction] ['RINGING'] [sessionId]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnCALReceived(SBMessage message)
        {
            lock (syncObject)
            {
                if (waitingForRing)
                {
                    waitingForRing = false;
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "CAL RINGING received, watingForRING status has been reset.");
                    SendOneQueuedInvitation();

                }
            }
        }

        /// <summary>
        /// Called when a NAK command has been received. Inidcates switch board failed to deliver a message to the target contact.
        /// </summary>
        /// <remarks>
        /// <code>NAK [MSGTransid]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnNAKReceived(SBMessage message)
        {
            OnMessageAcknowledgementReceived(new SBMessageDeliverResultEventArgs(false, message.TransactionID, this));
        }

        /// <summary>
        /// Called when a IRO command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates contacts in the session that have joined.
        /// <code>IRO [Transaction] [Current] [Total] [account[;GUID]] [DisplayName] [Caps]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnIROReceived(SBMessage message)
        {
            string fullaccount = message.CommandValues[2].ToString().ToLowerInvariant();
            string displayName = MSNHttpUtility.NSDecode(message.CommandValues[3].ToString());
            string capacitiesString = message.CommandValues[4].ToString();

            ContactConversationState oldStatus = ContactConversationState.None;
            object result = GetRosterProperty(fullaccount, RosterProperties.Status);
            if (result != null)
            {
                oldStatus = (ContactConversationState)(result);
            }

            SetRosterProperty(fullaccount, displayName, RosterProperties.Name);
            SetRosterProperty(fullaccount, capacitiesString, RosterProperties.ClientCapacityString);
            SetRosterProperty(fullaccount, ContactConversationState.Joined.ToString(), RosterProperties.Status);

            string account = fullaccount;
            bool supportMPOP = false;
            Guid endpointGuid = Guid.Empty;

            if (fullaccount.Contains(";"))
            {
                supportMPOP = true;
                account = fullaccount.Split(';')[0];
                endpointGuid = new Guid(fullaccount.Split(';')[1]);
            }

            if (NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant() == account)
                return;

            // Get the contact.
            Contact contact = NSMessageHandler.ContactList.GetContact(account, ClientType.PassportMember);

            // Not in contact list (anonymous). Update it's name and caps.
            if (contact.Lists == MSNLists.None || NSMessageHandler.BotMode)
            {
                contact.SetStatus(PresenceStatus.Online);

                if (supportMPOP)
                {
                    if (contact.PersonalMessage == null)
                    {
                        PersonalMessage personalMessage = new PersonalMessage("", MediaType.None, new string[] { }, endpointGuid);
                        contact.SetPersonalMessage(personalMessage);
                    }
                    else
                    {
                        PersonalMessage personalMessage = new PersonalMessage(contact.PersonalMessage.Message,
                            contact.PersonalMessage.MediaType, contact.PersonalMessage.CurrentMediaContent, endpointGuid);
                        contact.SetPersonalMessage(personalMessage);
                    }
                }
            }

            if (message.CommandValues.Count >= 4)
                contact.SetName(MSNHttpUtility.NSDecode(message.CommandValues[3].ToString()));

            if (message.CommandValues.Count >= 5)
            {
                string caps = message.CommandValues[4].ToString();
                UpdateContactEndPointData(contact, endpointGuid, caps, supportMPOP);
            }


            // Notify the client programmer.
            if (oldStatus != ContactConversationState.Joined)
            {
                OnContactJoined(contact, endpointGuid);
            }
        }

        /// <summary>
        /// Called when a JOI command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a remote contact has joined the session.
        /// This will fire the <see cref="ContactJoined"/> event.
        /// <code>JOI [account[;GUID]] [DisplayName] [Caps]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnJOIReceived(SBMessage message)
        {
            string fullaccount = message.CommandValues[0].ToString().ToLowerInvariant();
            string displayName = MSNHttpUtility.NSDecode(message.CommandValues[1].ToString());
            string capacitiesString = message.CommandValues[2].ToString();

            ContactConversationState oldStatus = ContactConversationState.None;
            object result = GetRosterProperty(fullaccount, RosterProperties.Status);
            if (result != null)
            {
                oldStatus = (ContactConversationState)(result);
            }

            SetRosterProperty(fullaccount, displayName, RosterProperties.Name);
            SetRosterProperty(fullaccount, capacitiesString, RosterProperties.ClientCapacityString);
            SetRosterProperty(fullaccount, ContactConversationState.Joined.ToString(), RosterProperties.Status);

            string account = fullaccount;
            bool supportMPOP = false;
            Guid endpointGuid = Guid.Empty;

            if (fullaccount.Contains(";"))
            {
                supportMPOP = true;
                account = fullaccount.Split(';')[0];
                endpointGuid = new Guid(fullaccount.Split(';')[1]);
            }

            if (NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant() != account)
            {
                // Get the contact.
                Contact contact = NSMessageHandler.ContactList.GetContact(account, ClientType.PassportMember);

                // Not in contact list (anonymous). Update it's name and caps.
                if (contact.Lists == MSNLists.None || NSMessageHandler.BotMode)
                {
                    contact.SetStatus(PresenceStatus.Online);

                    if (supportMPOP)
                    {
                        if (contact.PersonalMessage == null)
                        {
                            PersonalMessage personalMessage = new PersonalMessage("", MediaType.None, new string[] { }, endpointGuid);
                            contact.SetPersonalMessage(personalMessage);
                        }
                        else
                        {
                            PersonalMessage personalMessage = new PersonalMessage(contact.PersonalMessage.Message,
                                contact.PersonalMessage.MediaType, contact.PersonalMessage.CurrentMediaContent, endpointGuid);
                            contact.SetPersonalMessage(personalMessage);
                        }
                    }
                }

                if (message.CommandValues.Count >= 2)
                    contact.SetName(MSNHttpUtility.NSDecode(message.CommandValues[1].ToString()));

                if (message.CommandValues.Count >= 3)
                {
                    string caps = message.CommandValues[2].ToString();
                    UpdateContactEndPointData(contact, endpointGuid, caps, supportMPOP);
                }


                // Notify the client programmer.
                if (oldStatus != ContactConversationState.Joined)
                {
                    OnContactJoined(contact, endpointGuid);
                }
            }
        }

        /// <summary>
        /// Called when a USR command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that the server has replied to our identification USR command.
        /// <code>USR [Transaction] ['OK'] [account[;GUID]] [name]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnUSRReceived(SBMessage message)
        {
            if (message.CommandValues[0].ToString() == "OK")
            {
                string account = message.CommandValues[1].ToString().ToLowerInvariant();
                if (account.Contains(";"))
                {
                    account = account.Split(';')[0];
                }

                if (NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant() == account)
                {
                    // update the owner's name. Just to be sure.
                    // NSMessageHandler.ContactList.Owner.SetName(message.CommandValues[2].ToString());
                    if (NSMessageHandler != null)
                    {
                        Invite(NSMessageHandler.ContactList.Owner);
                    }
                    // we are now ready to invite other contacts. Notify the client of this.
                    OnSessionEstablished();
                }
            }
        }

        /// <summary>
        /// Called when a MSG command has been received.
        /// </summary>
        /// <remarks>
        /// Indicates that a remote contact has send us a message. This can be a plain text message,
        /// an invitation, or an application specific message.
        /// <code>MSG [Account] [Name] [Bodysize]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnMSGReceived(MSNMessage message)
        {
            // the MSG command is the most versatile one. These are all the messages
            // between clients. Like normal messages, file transfer invitations, P2P messages, etc.
            Contact contact = NSMessageHandler.ContactList.GetContact(message.CommandValues[0].ToString(), ClientType.PassportMember);

            // update the name to make sure we have it up-to-date
            //contact.SetName(message.CommandValues[1].ToString());

            // get the corresponding SBMSGMessage object
            MimeMessage sbMSGMessage = new MimeMessage();
            sbMSGMessage.CreateFromParentMessage(message);

            //first check if we are dealing with multi-packet-messages
            if (sbMSGMessage.MimeHeader.ContainsKey("Message-ID"))
            {
                //is this the first message?
                if (sbMSGMessage.MimeHeader.ContainsKey("Chunks"))
                {
                    multiPacketMessages.Add(sbMSGMessage.MimeHeader["Message-ID"] + "/0", sbMSGMessage);
                    return;
                }

                else if (sbMSGMessage.MimeHeader.ContainsKey("Chunk"))
                {
                    //Is this the last message?
                    if (Convert.ToInt32(sbMSGMessage.MimeHeader["Chunk"]) + 1 == Convert.ToInt32(multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/0"].MimeHeader["Chunks"]))
                    {
                        //Paste all the pieces together
                        MimeMessage completeMessage = multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/0"];
                        multiPacketMessages.Remove(sbMSGMessage.MimeHeader["Message-ID"] + "/0");

                        int chunksToProcess = Convert.ToInt32(completeMessage.MimeHeader["Chunks"]) - 2;
                        List<byte> completeText = new List<byte>(completeMessage.InnerBody);
                        for (int i = 0; i < chunksToProcess; i++)
                        {
                            MimeMessage part = multiPacketMessages[sbMSGMessage.MimeHeader["Message-ID"] + "/" + Convert.ToString(i + 1)];
                            completeText.AddRange(part.InnerBody);

                            //Remove the part from the buffer
                            multiPacketMessages.Remove(sbMSGMessage.MimeHeader["Message-ID"] + "/" + Convert.ToString(i + 1));
                        }

                        completeText.AddRange(sbMSGMessage.InnerBody);
                        completeMessage.InnerBody = completeText.ToArray();

                        //process the message
                        sbMSGMessage = completeMessage;
                    }
                    else
                    {
                        multiPacketMessages.Add(sbMSGMessage.MimeHeader["Message-ID"] + "/" + sbMSGMessage.MimeHeader["Chunk"], sbMSGMessage);
                        return;
                    }
                }
                else
                    throw new Exception("Multi-packetmessage with damaged headers received");
            }

            if (sbMSGMessage.MimeHeader.ContainsKey(MimeHeaderStrings.Content_Type))
            {
                NetworkMessage actualMessage = null;
                switch (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture))
                {
                    case "text/x-msmsgscontrol":
                        actualMessage = new TextPayloadMessage(string.Empty);
                        break;
                    case "text/x-mms-emoticon":
                    case "text/x-mms-animemoticon":
                        actualMessage = new EmoticonMessage();
                        break;
                    case "text/x-msnmsgr-datacast":
                        actualMessage = new MimeMessage();
                        break;
                    default:
                        if (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("text/plain") >= 0)
                        {
                            actualMessage = new TextMessage();
                        }
                        break;
                }

                if (actualMessage != null)
                {
                    actualMessage.CreateFromParentMessage(sbMSGMessage);
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, message.ToDebugString(), GetType().Name);
                }

                switch (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture))
                {
                    case "text/x-msmsgscontrol":
                        // make sure we don't parse the rest of the message in the next loop											
                        OnUserTyping(NSMessageHandler.ContactList.GetContact(sbMSGMessage.MimeHeader["TypingUser"], ClientType.PassportMember));
                        break;

                    case "text/x-mms-emoticon":
                    case "text/x-mms-animemoticon":
                        OnEmoticonDefinition(sbMSGMessage, contact);
                        break;

                    case "text/x-msnmsgr-datacast":
                        if ((actualMessage as MimeMessage).MimeHeader.ContainsKey("ID"))
                        {
                            if ((actualMessage as MimeMessage).MimeHeader["ID"] == "1")
                                OnNudgeReceived(contact);
                        }
                        else if (message.CommandValues[2].Equals("1325"))
                            OnWinkReceived(sbMSGMessage, contact);
                        break;

                    default:
                        if (sbMSGMessage.MimeHeader[MimeHeaderStrings.Content_Type].ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("text/plain") >= 0)
                        {
                            // a normal message has been sent, notify the client programmer
                            TextMessage msg = actualMessage as TextMessage;
                            OnTextMessageReceived(msg, contact);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Called when a ACK command has been received.
        /// </summary>
        /// <remarks>
        /// <code>ACK [MSGTransid]</code>
        /// </remarks>
        /// <param name="message"></param>
        protected virtual void OnACKReceived(SBMessage message)
        {
            OnMessageAcknowledgementReceived(new SBMessageDeliverResultEventArgs(true, message.TransactionID, this));
        }

        #endregion

        private void ClearAll()
        {
            lock (rosterState)
                rosterState.Clear();
            lock (rosterName)
                rosterName.Clear();
            lock (rosterCapacities)
                rosterCapacities.Clear();
        }

        private MimeMessage WrapMessage(EmoticonMessage message)
        {
            MimeMessage msgParentMessage = new MimeMessage();
            if (message.EmoticonType == EmoticonType.StaticEmoticon)
                msgParentMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/x-mms-emoticon";
            else if (message.EmoticonType == EmoticonType.AnimEmoticon)
                msgParentMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text-/x-mms-animemoticon";

            msgParentMessage.InnerMessage = message;

            return msgParentMessage;
        }

        private MimeMessage WrapMessage(TextMessage message)
        {
            MimeMessage msgParentMessage = new MimeMessage();
            msgParentMessage.MimeHeader[MimeHeaderStrings.Content_Type] = "text/plain; charset=UTF-8";
            msgParentMessage.MimeHeader[MimeHeaderStrings.X_MMS_IM_Format] = message.GetStyleString();

            if (message.CustomNickname != string.Empty)
                msgParentMessage.MimeHeader[MimeHeaderStrings.P4_Context] = message.CustomNickname;

            msgParentMessage.InnerMessage = message;

            return msgParentMessage;
        }

        private void UpdateContactEndPointData(Contact contact, Guid endpointGuid, string caps, bool supportMPOP)
        {
            bool dump = false;

            if (caps.Contains(":"))
            {
                if (!contact.EndPointData.ContainsKey(endpointGuid))
                {
                    EndPointData epData = new EndPointData(contact.Mail, endpointGuid);
                    epData.ClientCapacities = (ClientCapacities)Convert.ToInt64(caps.Split(':')[0]);
                    epData.ClientCapacitiesEx = (ClientCapacitiesEx)Convert.ToInt64(caps.Split(':')[1]);
                    contact.EndPointData[endpointGuid] = epData;
                    dump = true;
                }

                if (supportMPOP)
                {
                    contact.EndPointData[endpointGuid].ClientCapacities = (ClientCapacities)Convert.ToInt64(caps.Split(':')[0]);
                    contact.EndPointData[endpointGuid].ClientCapacitiesEx = (ClientCapacitiesEx)Convert.ToInt64(caps.Split(':')[1]);
                }
                else
                {
                    contact.EndPointData[Guid.Empty].ClientCapacities = (ClientCapacities)Convert.ToInt64(caps.Split(':')[0]);
                    contact.EndPointData[Guid.Empty].ClientCapacitiesEx = (ClientCapacitiesEx)Convert.ToInt64(caps.Split(':')[1]);
                }
            }
            else
            {
                if (!contact.EndPointData.ContainsKey(endpointGuid))
                {
                    EndPointData epData = new EndPointData(contact.Mail, endpointGuid);
                    epData.ClientCapacities = (ClientCapacities)Convert.ToInt64(caps);
                    contact.EndPointData[endpointGuid] = epData;
                    dump = true;
                }

                if (supportMPOP)
                {
                    contact.EndPointData[endpointGuid].ClientCapacities = (ClientCapacities)Convert.ToInt64(caps);
                }
                else
                {
                    contact.EndPointData[Guid.Empty].ClientCapacities = (ClientCapacities)Convert.ToInt64(caps);
                }
            }

            if (dump)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "EndPoint ID " + endpointGuid.ToString("B") + " not found in " + contact.ToString() + " new EndPointData added.");
            }
        }

        #region Switchboard Handling



        /// <summary>
        /// Called when the message processor has established a connection. This function will 
        /// begin the login procedure by sending the USR or ANS command.
        /// </summary>
        protected virtual void OnProcessorConnectCallback(object sender, EventArgs e)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "OnProcessorConnectCallback: SB processor connected.", GetType().Name);
            SendInitialMessage();
        }

        /// <summary>
        /// Called when the message processor has disconnected. This function will 
        /// set the IsSessionEstablished to false.
        /// </summary>
        protected virtual void OnProcessorDisconnectCallback(object sender, EventArgs e)
        {
            if (IsSessionEstablished)  //This means some exception occured and we drop out of the network.
            {
                Close(true);
            }
        }

        /// <summary>
        /// The processor to handle the messages
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            
            set
            {
                throw new InvalidOperationException("This property is read-only.");
            }
        }


        /// <summary>
        /// Handles message from the processor.
        /// </summary>
        /// <remarks>
        /// This is one of the most important functions of the class.
        /// It handles incoming messages and performs actions based on the commands in the messages.
        /// Exceptions which occur in this method are redirected via the <see cref="ExceptionOccurred"/> event.
        /// </remarks>
        /// <param name="sender"></param>
        /// <param name="message">The network message received from the notification server</param>
        public virtual void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            try
            {
                // We expect at least a SBMessage object
                SBMessage sbMessage = (SBMessage)message;

                switch (sbMessage.Command)
                {
                    case "ACK":
                    case "ANS":
                    case "BYE":
                    case "CAL":
                    case "IRO":
                    case "JOI":
                    case "USR":
                    case "NAK":
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, sbMessage.ToDebugString(), GetType().Name);
                        break;
                }

                switch (sbMessage.Command)
                {
                    case "MSG":
                        OnMSGReceived(sbMessage);
                        return;
                    case "ACK":
                        OnACKReceived(sbMessage);
                        return;
                    case "NAK":
                        OnNAKReceived(sbMessage);
                        return;
                    case "ANS":
                        OnANSReceived(sbMessage);
                        return;
                    case "BYE":
                        OnBYEReceived(sbMessage);
                        return;
                    case "CAL":
                        OnCALReceived(sbMessage);
                        return;
                    case "IRO":
                        OnIROReceived(sbMessage);
                        return;
                    case "JOI":
                        OnJOIReceived(sbMessage);
                        return;
                    case "USR":
                        OnUSRReceived(sbMessage);
                        return;
                }

                // Check whether it is a numeric error command
                if (sbMessage.Command[0] >= '0' && sbMessage.Command[0] <= '9')
                {
                    try
                    {
                        int errorCode = int.Parse(sbMessage.Command, System.Globalization.CultureInfo.InvariantCulture);
                        OnServerErrorReceived((MSNError)errorCode);
                    }
                    catch (FormatException fe)
                    {
                        throw new MSNPSharpException("Exception occurred while parsing an error code received from the switchboard server", fe);
                    }
                }
                else
                {
                    // It is a unknown command
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "UNKNOWN COMMAND: " + sbMessage.Command + "\r\n" + sbMessage.ToDebugString(), GetType().ToString());
                }
            }
            catch (Exception e)
            {
                OnExceptionOccurred(e);
                throw; //RethrowToPreserveStackDetails (without e)
            }
        }

        /// <summary>
        /// Fires the ServerErrorReceived event.
        /// </summary>
        protected virtual void OnServerErrorReceived(MSNError serverError)
        {
            if (ServerErrorReceived != null)
                ServerErrorReceived(this, new MSNErrorEventArgs(serverError));
        }

        /// <summary>
        /// Fires the <see cref="ExceptionOccurred"/> event.
        /// </summary>
        /// <param name="e">The exception which was thrown</param>
        protected virtual void OnExceptionOccurred(Exception e)
        {
            if (ExceptionOccurred != null)
                ExceptionOccurred(this, new ExceptionEventArgs(e));
        }

        #endregion

        /// <summary>
        /// Debug string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return GetType().ToString() + " SessionHash: " + SessionHash;
        }

    }
};