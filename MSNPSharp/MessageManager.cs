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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp.Utilities
{
    public class MessageManager : IDisposable
    {
        #region Events

        /// <summary>
        /// Fired when a user message arrived. Use MessageType property to determine what kind of message it is.
        /// </summary>
        public event EventHandler<MessageArrivedEventArgs> MessageArrived;

        #endregion

        #region Fields and Properties

        private Dictionary<ConversationID, Conversation> conversationIndex = new Dictionary<ConversationID, Conversation>(100, new ConversationIDComparer());
        private Dictionary<ConversationID, Contact> pendingConversations = new Dictionary<ConversationID, Contact>(100, new ConversationIDComparer());
        private List<Conversation> conversations = new List<Conversation>(100);

        private Messenger messenger = null;

        /// <summary>
        /// The <see cref="Messenger"/> instance this manager connected to.
        /// </summary>
        public Messenger Messenger
        {
            get { return messenger; }
        }

        private object syncObject = new object();

        protected object SyncObject
        {
            get { return syncObject; }
        }


        #endregion

        #region .ctor

        private MessageManager()
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="messenger">The <see cref="Messenger"/> instance this manager connected to.</param>
        public MessageManager(Messenger messenger)
        {
            this.messenger = messenger;
            Messenger.ConversationCreated += new EventHandler<ConversationCreatedEventArgs>(ConversationCreated);
            Messenger.Nameserver.CrossNetworkMessageReceived += new EventHandler<CrossNetworkMessageEventArgs>(CrossNetworkMessageReceived);
            Messenger.Nameserver.MobileMessageReceived += new EventHandler<CrossNetworkMessageEventArgs>(CrossNetworkMessageReceived);
        }

        #endregion

        #region Event handlers

        private void ConversationCreated(object sender, ConversationCreatedEventArgs e)
        {
            // We do not listen to ContactJoin event. If a conversation has no text/typing/nudge/emoticon message
            // arrived, this conversation will only be placed into conversation list.
            AttatchEvents(e.Conversation);
            AddConversationToConversationList(e.Conversation);
        }

        private void ConversationEnded(object sender, ConversationEndEventArgs e)
        {
            DetatchEvents(e.Conversation);
            RemoveConversationFromConversationIndex(e.Conversation);
            RemoveConversationFromConversationList(e.Conversation);
        }

        private void CrossNetworkMessageReceived(object sender, CrossNetworkMessageEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(new ConversationID(e.From));

            switch (e.MessageType)
            {
                case NetworkMessageType.Typing:
                case NetworkMessageType.Nudge:
                    OnMessageArrived(new MessageArrivedEventArgs(id, e.From, e.MessageType));
                    break;
                case NetworkMessageType.Text:
                    OnMessageArrived(new TextMessageArrivedEventArgs(id, e.From, e.Message as TextMessage));
                    break;
            }

        }

        private void PassportMemberUserTyping(object sender, ContactEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(new ConversationID(sender as Conversation));
            OnMessageArrived(new MessageArrivedEventArgs(id, e.Contact, NetworkMessageType.Typing));
        }

        private void PassportMemberTextMessageReceived(object sender, TextMessageEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(new ConversationID(sender as Conversation));
            OnMessageArrived(new TextMessageArrivedEventArgs(id, e.Sender, e.Message));
        }

        private void PassportMemberNudgeReceived(object sender, ContactEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(new ConversationID(sender as Conversation));
            OnMessageArrived(new MessageArrivedEventArgs(id, e.Contact, NetworkMessageType.Nudge));
        }

        private void PassportMemberMSNObjectDataTransferCompleted(object sender, ConversationMSNObjectDataTransferCompletedEventArgs e)
        {
            ConversationID id = ProcessArrivedConversation(new ConversationID(sender as Conversation));
            if (e.ClientData is Emoticon)
            {
                Emoticon emoticon = e.ClientData as Emoticon;
                OnMessageArrived(new EmoticonArrivedEventArgs(id, e.RemoteContact, emoticon));
            }
        }

        #endregion

        protected virtual void OnMessageArrived(MessageArrivedEventArgs e)
        {
            if (MessageArrived != null)
                MessageArrived(this, e);
        }

        private ConversationID ProcessArrivedConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                bool created = HasConversation(cId);
                bool pending = IsPendingConversation(cId);
                if (pending)
                {
                    if (!created)
                    {
                        AddConversationToConversationIndex(cId, cId.Conversation);  //We fix this bug.
                    }

                    if (created)
                    {
                        //What happends?!
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[ProcessArrivedConversation Error]: A conversation is both in pending and created status.");
                        
                    }

                    RemovePendingConversation(cId);
                }

                if (!pending)
                {
                    if (!created)
                    {
                        AddConversationToConversationIndex(cId, cId.Conversation);
                    }
                }

                return cId;
            }
        }

        private bool HasConversation(ConversationID cId)
        {
            lock (syncObject)
                return conversationIndex.ContainsKey(cId);
        }

        private bool IsPendingConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                return pendingConversations.ContainsKey(cId);
            }
        }

        /// <summary>
        /// Add the specific <see cref="Conversation"/> object into conversatoin list. And listen to its message events (i.e. user typing messages, text messages).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="conversation"></param>
        /// <returns>Return true if added successfully, false if the conversation with the specific id already exists.</returns>
        private bool AddConversationToConversationIndex(ConversationID id, Conversation conversation)
        {
            lock (SyncObject)
            {
                if (conversationIndex.ContainsKey(id)) 
                    return false;
                conversationIndex[id] = conversation;
                return true;
            }
        }

        private void AddConversationToConversationList(Conversation conversation)
        {
            lock (SyncObject)
            {
                conversations.Add(conversation);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "A new conversation has been added into conversation list, " +
                    "current conversation count: " + conversations.Count + ".");
            }
        }

        private void AddPending(ConversationID cId, Contact remoteOwner)
        {
            lock (SyncObject)
            {
                pendingConversations[cId] = remoteOwner;
            }
        }

        private bool RemovePendingConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                if (IsPendingConversation(cId))
                {
                    pendingConversations.Remove(cId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool RemoveConversationFromConversationIndex(ConversationID cId)
        {
            lock (SyncObject)
            {
                if (conversationIndex.ContainsKey(cId))
                {
                    conversationIndex.Remove(cId);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private bool RemoveConversationFromConversationIndex(Conversation conversation)
        {
            lock (SyncObject)
            {
                Dictionary<ConversationID, Conversation> cp = new Dictionary<ConversationID, Conversation>(conversationIndex, new ConversationIDComparer());
                foreach (ConversationID id in cp.Keys)
                {
                    if (object.ReferenceEquals(cp[id], conversation))
                    {
                        conversationIndex.Remove(id);
                        return true;
                    }
                }

                return false;
            }
        }

        private bool RemoveConversationFromConversationList(Conversation conversation)
        {
            lock (SyncObject)
            {
                bool returnValue = conversations.Remove(conversation);
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "A conversation has been removed, there is/are " +
                    conversations.Count + " conversation(s) in conversation list.");
                return returnValue;
            }
        }

        private void DetatchEvents(Conversation conversation)
        {
            if (conversation != null)
            {
                conversation.TextMessageReceived -= PassportMemberTextMessageReceived;
                conversation.NudgeReceived -= PassportMemberNudgeReceived;
                conversation.UserTyping -= PassportMemberUserTyping;
                conversation.ConversationEnded -= ConversationEnded;
                conversation.MSNObjectDataTransferCompleted -= PassportMemberMSNObjectDataTransferCompleted;
            }
        }

        private void AttatchEvents(Conversation conversation)
        {
            DetatchEvents(conversation);

            conversation.TextMessageReceived += new EventHandler<TextMessageEventArgs>(PassportMemberTextMessageReceived);
            conversation.NudgeReceived += new EventHandler<ContactEventArgs>(PassportMemberNudgeReceived);
            conversation.UserTyping += new EventHandler<ContactEventArgs>(PassportMemberUserTyping);
            conversation.ConversationEnded += new EventHandler<ConversationEndEventArgs>(ConversationEnded);
            conversation.MSNObjectDataTransferCompleted += new EventHandler<ConversationMSNObjectDataTransferCompletedEventArgs>(PassportMemberMSNObjectDataTransferCompleted);
        }


        /// <summary>
        /// Test whether the <see cref="Messenger"/> conntected to this manager is still in signed in status.
        /// </summary>
        /// <exception cref="InvalidOperationException">Messenger not sign in.</exception>
        private void CheckMessengerStatus()
        {
            if (!Messenger.Nameserver.IsSignedIn)
            {
                throw new InvalidOperationException("Messenger not sign in. Please sign in first.");
            }
        }

        /// <summary>
        /// Test whether a contact can be the receiver of a user message.
        /// </summary>
        /// <param name="messengerContact"></param>
        /// <param name="messageObject"></param>
        /// <exception cref="MSNPSharpException">The target <see cref="Contact"/> is not a messenger contact.</exception>
        /// <exception cref="NotSupportedException">The message is not compatible to the specific contact.</exception>
        private void CheckContact(Contact messengerContact, MessageObject messageObject)
        {
            if (!messengerContact.IsMessengerUser && !messengerContact.NSMessageHandler.BotMode)
            {
                throw new MSNPSharpException("This is not a MSN contact.");
                }

            if (messengerContact.ClientType == ClientType.EmailMember && (messageObject is EmoticonObject))
            {
                throw new NotSupportedException("A Yahoo Messenger contact cannot receive custom emoticon.");
            }

            if ((messengerContact.ClientType == ClientType.PhoneMember || messengerContact.MobileAccess) && (messageObject is EmoticonObject || messageObject is NudgeObject || messageObject is UserTypingObject))
            {
                throw new NotSupportedException("A Phone Contact cannot receive " + messageObject.GetType().ToString() + " messages.");
            }

            if (messengerContact.Status == PresenceStatus.Offline && (messageObject is TextMessageObject) == false)
            {
                throw new NotSupportedException("The specific message cannot send to an offline contact: " + messengerContact);
            }
        }

        /// <summary>
        /// Send a cross network message to Yahoo! Messenger network
        /// </summary>
        /// <param name="yimContact"></param>
        /// <param name="messageObject"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Throw when sending a custom emoticon to a Yahoo Messenger contact.</exception>
        private void SendYIMMessage(Contact yimContact, MessageObject messageObject)
        {
            if (yimContact.IsMessengerUser && yimContact.ClientType == ClientType.EmailMember)
            {
                if (messageObject is EmoticonObject)
                {
                    throw new InvalidOperationException("Cannot send custom emoticon to a Email messenger contact.");
                }

                try
                {

                    if (messageObject is NudgeObject)
                    {
                        Messenger.Nameserver.SendCrossNetworkMessage(yimContact, NetworkMessageType.Nudge);
                    }

                    if (messageObject is UserTypingObject)
                    {
                        Messenger.Nameserver.SendCrossNetworkMessage(yimContact, NetworkMessageType.Typing);
                    }

                    if (messageObject is TextMessageObject)
                    {
                        Messenger.Nameserver.SendCrossNetworkMessage(yimContact, messageObject.InnerObject as TextMessage);
                    }

                }

                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot send message to email contact: " + yimContact);
            }

        }

        /// <summary>
        /// Send a cross network message to Mobile Messenger.
        /// </summary>
        /// <param name="mobileContact"></param>
        /// <param name="messageObject"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Throw when sending a custom emoticon to a Yahoo Messenger contact.</exception>
        private void SendMobileMessage(Contact mobileContact, MessageObject messageObject)
        {
            if (mobileContact.ClientType == ClientType.PhoneMember || mobileContact.MobileAccess)
            {
                if (messageObject is EmoticonObject || messageObject is NudgeObject || messageObject is TextMessageObject)
                {
                    throw new InvalidOperationException("Cannot send a " + messageObject.GetType().ToString() + " to a Phone Contact.");
                }

                try
                {
                    Messenger.Nameserver.SendMobileMessage(mobileContact, (messageObject.InnerObject as TextMessage).Text);

                }

                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot send message to Phone Contact: " + mobileContact);
            }

        }

        /// <summary>
        /// Send the message through the specific <see cref="Conversation"/>
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="messageObject"></param>
        /// <exception cref="InvalidOperationException">Thrown when a conversation is already ended.</exception>
        private void SendConversationMessage(Conversation conversation, MessageObject messageObject)
        {
            if (conversation.Ended)
                throw new InvalidOperationException("Cannot send a message through an ended conversation.");

            try
            {
                if (messageObject is NudgeObject)
                {
                    conversation.SendNudge();
                }

                if (messageObject is UserTypingObject)
                {
                    conversation.SendTypingMessage();
                }

                if (messageObject is TextMessageObject)
                {
                    conversation.SendTextMessage(messageObject.InnerObject as TextMessage);
                }

                if (messageObject is EmoticonObject)
                {
                    conversation.SendEmoticonDefinitions(messageObject.InnerObject as List<Emoticon>, (messageObject as EmoticonObject).Type);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Send a message to a remote contact by all means. This method always send the message through a single chat conversation.
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="messageObject"></param>
        /// <returns>The ID of conversation that send this message</returns>
        private void SendMessage(Contact contact, MessageObject messageObject)
        {
            CheckMessengerStatus();

            //Verify messenger contact.
            CheckContact(contact, messageObject);

            //Process YIM contact.
            if (contact.ClientType == ClientType.EmailMember)
            {
                SendYIMMessage(contact, messageObject);
                return;
            }

            if (contact.ClientType == ClientType.PhoneMember)
            {
                SendMobileMessage(contact, messageObject);
            }

            //Process OIM.
            if (contact.Status == PresenceStatus.Offline)
            {
                Messenger.Nameserver.OIMService.SendOIMMessage(contact, (messageObject as TextMessageObject).InnerObject as TextMessage);
                return;
            }
        }

        /// <summary>
        /// Send a message to the spcific conversation.
        /// </summary>
        /// <param name="cId"></param>
        /// <param name="messageObject"></param>
        /// <returns>Guid.Empty if conversatio has ended or not exists.</returns>
        private ConversationID SendMessage(ConversationID cId, MessageObject messageObject)
        {
            if (cId == null)
            {
                throw new ArgumentNullException("cId is null.");
            }

            //If the messenger is not signed in, this calling will throw an exception.
            CheckMessengerStatus();

            lock (SyncObject)
            {
                bool created = HasConversation(cId);
                bool pending = IsPendingConversation(cId);
                if (cId.NetworkType != ClientType.EmailMember)
                {
                    if (cId.RemoteOwner.Status != PresenceStatus.Offline)
                    {
                        if ((!pending) && created)  //Send message through exisiting conversations.
                        {
                            SendConversationMessage(GetConversation(cId), messageObject);
                        }
                        else
                        {

                            //In the following case, the conversation object is not actually created.
                            //However, if the message is user typing, we just do nothing.
                            if (!(messageObject is UserTypingObject))
                            {
                                cId = CreateNewConversation(cId);
                                CheckContact(cId.RemoteOwner, messageObject);
                                SendConversationMessage(cId.Conversation, messageObject);
                            }
                        }
                    }
                    else
                    {
                        if (!(messageObject is UserTypingObject))  //You cannot send typing messages as OIM messages.
                        {
                            RemovePendingConversation(cId);
                            //Verify messenger contact.
                            CheckContact(cId.RemoteOwner, messageObject);
                            SendMessage(cId.RemoteOwner, messageObject);
                        }
                    }
                }

                if (cId.NetworkType == ClientType.EmailMember) //Yahoo!
                {
                    CheckContact(cId.RemoteOwner, messageObject);
                    SendMessage(cId.RemoteOwner, messageObject);
                    RemovePendingConversation(cId);
                }
            }
            return cId;

        }

        private ConversationID CreateNewConversation(ConversationID pendingId)
        {
            bool created = HasConversation(pendingId);
            bool pending = IsPendingConversation(pendingId);
            bool otherNetwork = (pendingId.RemoteOwner.ClientType != ClientType.PassportMember);

            if (pending)
                RemovePendingConversation(pendingId);
            if (created || otherNetwork)
                return pendingId;

            pendingId.SetConversation(Messenger.CreateConversation());
            AddConversationToConversationIndex(pendingId, pendingId.Conversation);
            pendingId.Conversation.Invite(pendingId.RemoteOwner);

            return pendingId;

        }

        #region Public methods

        /// <summary>
        /// Get the corresponding conversation from conversation Id.
        /// </summary>
        /// <param name="cId"></param>
        /// <returns>A conversation object will returned if found, null otherwise.</returns>
        public Conversation GetConversation(ConversationID cId)
        {
            lock (SyncObject)
            {
                if (conversationIndex.ContainsKey(cId))
                    return conversationIndex[cId];
                return null;
            }
        }

        public ConversationID SendTyping(ConversationID conversationID)
        {
            return SendMessage(conversationID, new UserTypingObject());
        }

        public ConversationID SendNudge(ConversationID conversationID)
        {
            return SendMessage(conversationID, new NudgeObject());
        }


        public ConversationID SendTextMessage(ConversationID conversationID, TextMessage message)
        {
            return SendMessage(conversationID, new TextMessageObject(message));
        }


        public ConversationID SendEmoticonDefinitions(ConversationID conversationID, List<Emoticon> emoticons, EmoticonType icontype)
        {
            return SendMessage(conversationID, new EmoticonObject(emoticons, icontype));
        }

        public ConversationID GetID(Contact remoteOwner)
        {
            lock (SyncObject)
            {
                ConversationID id = new ConversationID(remoteOwner);
                bool created = HasConversation(id);
                bool pending = IsPendingConversation(id);
                if (created || pending)
                    return id;

                AddPending(id, remoteOwner);
                return id;
            }
        }

        /// <summary>
        /// Invite another user to a conversation.
        /// </summary>
        /// <param name="conversationID"></param>
        /// <param name="remoteContact"></param>
        /// <returns>The updated conversation Id.</returns>
        /// <exception cref="InvalidOperationException">The remote contact is not a <see cref="ClientType.PassportMember"/></exception>
        public ConversationID InviteContactToConversation(ConversationID conversationID, Contact remoteContact)
        {
            ConversationID cId = conversationID;
            if (remoteContact.IsSibling(cId.RemoteOwner))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Connot invite the remote owner into this conversation again.");
                return cId;
            }

            if (remoteContact.ClientType != ClientType.PassportMember)
                throw new InvalidOperationException("The remoteContact: " + remoteContact + " is not a PassportMember.");

            Conversation activeConversation = null;
            if (HasConversation(cId))
            {
                activeConversation = GetConversation(cId);
            }
            else
            {
                //The conversation object not exist, we need to create one first.
                //Then invite the remote owner to the newly created conversation.
                cId = CreateNewConversation(cId);
                activeConversation = cId.Conversation;
            }

            if (activeConversation.Ended)  //If conversation exists, but ended.
            {
                //We dump the old conversation and start the whole process again.
                RemoveConversationFromConversationIndex(activeConversation);
                RemovePendingConversation(cId);
                return InviteContactToConversation(cId, remoteContact);
            }

            activeConversation.Invite(remoteContact);


            return cId;
        }

        /// <summary>
        /// Invite another user to a conversation.
        /// </summary>
        /// <param name="conversationID"></param>
        /// <param name="contacts"></param>
        /// <returns>The updated conversation Id.</returns>
        /// <exception cref="InvalidOperationException">The remote contact is not a <see cref="ClientType.PassportMember"/></exception>
        public ConversationID InviteContactToConversation(ConversationID conversationID, Contact[] contacts)
        {
            ConversationID cId = conversationID;
            foreach (Contact contact in contacts)
            {
                cId = InviteContactToConversation(cId, contact);
            }

            return cId;
        }

        /// <summary>
        /// End the specific conversation and release the resources it used.
        /// </summary>
        /// <param name="conversationId"></param>
        public void EndConversation(ConversationID conversationId)
        {
            ConversationID cId = conversationId;
            bool created = HasConversation(cId);
            bool pending = IsPendingConversation(cId);

            if (pending)
                RemovePendingConversation(cId);

            if (created)
            {
                if (cId.NetworkType == ClientType.PassportMember) //Only passport conversation contains a conversation object. Other conversation, like Yahoo does not.
                {
                    List<Conversation> overflowConversations = new List<Conversation>(10);
                    lock (SyncObject)
                    {
                        foreach (Conversation conversation in conversations)
                        {
                            if (cId == new ConversationID(conversation))
                            {
                                overflowConversations.Add(conversation);
                            }
                        }
                    }

                    foreach (Conversation conversation in overflowConversations)
                    {
                        conversation.End();
                    }
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "A none Passprt Member conversation has been removed from the conversation index");
                }
                RemoveConversationFromConversationIndex(cId);
            }
        }

        #endregion

        #region IDisposable ≥…‘±

        public void Dispose()
        {
            if (Messenger != null)
            {
                Messenger.ConversationCreated -= ConversationCreated;
                Messenger.Nameserver.CrossNetworkMessageReceived -= CrossNetworkMessageReceived;
                Messenger.Nameserver.MobileMessageReceived -= CrossNetworkMessageReceived;
            }

        }

        #endregion
    }
}
