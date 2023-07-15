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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;
    using MSNPSharp.Utilities;

    #region ConversationCreatedEvent

    /// <summary>
    /// Used when a new switchboard session is created.
    /// </summary>
    public class ConversationCreatedEventArgs : EventArgs
    {
        private object _initiator;
        private Conversation _conversation;

        /// <summary>
        /// The affected conversation
        /// </summary>
        public Conversation Conversation
        {
            get
            {
                return _conversation;
            }
            set
            {
                _conversation = value;
            }
        }

        /// <summary>
        /// The object that requested the switchboard. Null if the conversation was initiated by a
        /// remote client.
        /// </summary>
        public object Initiator
        {
            get
            {
                return _initiator;
            }
            set
            {
                _initiator = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public ConversationCreatedEventArgs(Conversation conversation, object initiator)
        {
            _conversation = conversation;
            _initiator = initiator;
        }
    }

    #endregion

    /// <summary>
    /// Provides an easy interface for the client programmer.
    /// </summary>
    /// <remarks>
    /// Messenger is an important class for the client programmer. It provides an
    /// easy interface to communicate with the network. Messenger is a facade which hides all
    /// lower abstractions like message processors, protocol handlers, etc.
    /// Messenger passes through events from underlying objects. This way the client programmer
    /// can connect eventhandlers just once.
    /// </remarks>
    public class Messenger
    {
        #region Events

        /// <summary>
        /// Occurs when a new conversation is created. Either by a local or remote invitation.
        /// </summary>
        /// <remarks>
        /// You can check the initiator object in the event arguments to see which party initiated the
        /// conversation. This event is called after the messenger server has created a switchboard handler,
        /// so there is always a valid messageprocessor.
        /// </remarks>
        public event EventHandler<ConversationCreatedEventArgs> ConversationCreated;

        /// <summary>
        /// Occurs when a remote client has send an invitation for a filetransfer session.
        /// </summary>
        public event EventHandler<MSNSLPInvitationEventArgs> TransferInvitationReceived;

        internal void OnTransferInvitationReceived(object sender, MSNSLPInvitationEventArgs args)
        {
            if (TransferInvitationReceived != null)
                TransferInvitationReceived(sender, args);
        }

        protected virtual void OnConversationCreated(Conversation conversation, object initiator)
        {
            if (ConversationCreated != null)
                ConversationCreated(this, new ConversationCreatedEventArgs(conversation, initiator));
        }

        #endregion

        #region Members

        private NSMessageProcessor nsMessageProcessor = null;
        private NSMessageHandler nsMessageHandler = null;
        private ConnectivitySettings connectivitySettings = null;
        private Credentials credentials = new Credentials(MsnProtocol.MSNP18);
        private ArrayList conversations = ArrayList.Synchronized(new ArrayList());
        private MessageManager messageManager = null;
        private bool shouldReconnect = false;

        #endregion

        #region .ctor

        /// <summary>
        /// Basic constructor to instantiate a Messenger object.
        /// </summary>
        public Messenger()
        {
            connectivitySettings = new ConnectivitySettings();
            nsMessageProcessor = new NSMessageProcessor(connectivitySettings);
            nsMessageHandler = new NSMessageHandler(this);
            messageManager = new MessageManager(this);

            Nameserver.SBCreated += delegate(object sender, SBCreatedEventArgs ce)
            {
                // check if the request is remote or on our initiative
                if (ce.Initiator != null)
                {
                    return;
                }

                // create a conversation object to handle with the switchboard
                Conversation c = new Conversation(this, ce.Switchboard);
                OnConversationCreated(c, ce.Initiator);
                return;
            };

            NameserverProcessor.ConnectionClosed += new EventHandler<EventArgs>(NameserverProcessor_ConnectionClosed);
        }

        

        #endregion

        #region Properties

        /// <summary>
        /// Provide a simple way to send and receive messages.
        /// </summary>
        public MessageManager MessageManager
        {
            get
            {
                return messageManager;
            }
        }

        /// <summary>
        /// The message processor that is used to send and receive nameserver messages.
        /// </summary>
        /// <remarks>
        /// This processor is mainly used by the nameserver handler.
        /// </remarks>
        public NSMessageProcessor NameserverProcessor
        {
            get
            {
                return nsMessageProcessor;
            }
        }

        /// <summary>
        /// Specifies the connection capabilities of the local machine.
        /// </summary>
        /// <remarks>
        /// Use this property to set specific connectivity settings like proxy servers and custom messenger servers.
        /// </remarks>
        public ConnectivitySettings ConnectivitySettings
        {
            get
            {
                return connectivitySettings;
            }
            set
            {
                connectivitySettings = value;
                NameserverProcessor.ConnectivitySettings = ConnectivitySettings;
            }
        }

        /// <summary>
        /// The credentials which identify the messenger account and the client authentication.
        /// </summary>
        /// <remarks>
        /// This property must be set before logging in the messenger service. <b>Both</b> the account 
        /// properties and the client identifier codes must be set. The first, the account, specifies the
        /// account which represents the local user, for example 'account@hotmail.com'. The second, the
        /// client codes, specifies how this client will authenticate itself against the messenger server.
        /// See <see cref="Credentials"/> for more information about this.
        /// </remarks>
        public Credentials Credentials
        {
            get
            {
                return credentials;
            }
            set
            {
                credentials = value;
            }
        }

        /// <summary>
        /// The message handler that is used to handle incoming nameserver messages.
        /// </summary>
        public NSMessageHandler Nameserver
        {
            get
            {
                return nsMessageHandler;
            }
        }

        /// <summary>
        /// Returns whether there is a connection with the messenger server.
        /// </summary>
        public bool Connected
        {
            get
            {
                return nsMessageProcessor.Connected;
            }
        }

        /// <summary>
        /// A list of all contacts.
        /// </summary>
        public ContactList ContactList
        {
            get
            {
                return Nameserver.ContactList;
            }
        }

        /// <summary>
        /// A list of all contactgroups.
        /// </summary>
        public ContactGroupList ContactGroups
        {
            get
            {
                return Nameserver.ContactGroups;
            }
        }

        /// <summary>
        /// A collection of all circles which are defined by the user who logged into the messenger network.
        /// </summary>
        public CircleList CircleList
        {
            get
            {
                return Nameserver.CircleList;
            }
        }

        /// <summary>
        /// Offline message service.
        /// </summary>
        public OIMService OIMService
        {
            get
            {
                return Nameserver.OIMService;
            }
        }

        /// <summary>
        /// Storage service to get/update display name, personal status, display picture etc.
        /// </summary>
        public MSNStorageService StorageService
        {
            get
            {
                return Nameserver.StorageService;
            }
        }

        /// <summary>
        /// What's Up service
        /// </summary>
        public WhatsUpService WhatsUpService
        {
            get
            {
                return Nameserver.WhatsUpService;
            }
        }

        /// <summary>
        /// Contact service.
        /// </summary>
        public ContactService ContactService
        {
            get
            {
                return Nameserver.ContactService;
            }
        }

        /// <summary>
        /// The local user logged into the network. It will remain null until user successfully login.
        /// </summary>
        public Owner Owner
        {
            get
            {
                return Nameserver.ContactList.Owner;
            }
        }

        /// <summary>
        /// The handler that handles all incoming P2P framework messages.
        /// </summary>
        public P2PHandler P2PHandler
        {
            get
            {
                return Nameserver.P2PHandler;
            }
        }

        public ArrayList Conversations
        {
            get
            {
                return conversations;
            }
        }

        #endregion

        #region Methods

        private void DoConnect()
        {
            NameserverProcessor.ConnectivitySettings = connectivitySettings;
            NameserverProcessor.RegisterHandler(nsMessageHandler);
            Nameserver.MessageProcessor = NameserverProcessor;
            Nameserver.Credentials = credentials;

            NameserverProcessor.Connect();
        }

        private void NameserverProcessor_ConnectionClosed(object sender, EventArgs e)
        {
            if (shouldReconnect)
            {
                shouldReconnect = false;
                DoConnect();
                
            }
        }

        /// <summary>
        /// Connect to the messenger network.
        /// </summary>
        public virtual void Connect()
        {
            if (NameserverProcessor == null)
                throw new MSNPSharpException("No message processor defined");

            if (Nameserver == null)
                throw new MSNPSharpException("No message handler defined");

            if (Credentials == null)
                throw new MSNPSharpException("No credentials defined");

            if (Credentials.Account.Length == 0)
                throw new MSNPSharpException("The specified account is empty");

            if (Credentials.Password.Length == 0)
                throw new MSNPSharpException("The specified password is empty");

            if (Credentials.ClientCode.Length == 0 || credentials.ClientID.Length == 0)
                throw new MSNPSharpException("The local messengerclient credentials (client-id and client code) are not specified. This is necessary in order to authenticate the local client with the messenger server. See for more info about the values to use the documentation of the Credentials class.");

            if (NameserverProcessor.Connected)
            {
                shouldReconnect = true;
                Disconnect();
            }
            else
            {
                // everything is okay, resume
                DoConnect();
            }
        }

        /// <summary>
        /// Disconnect from the messenger network.
        /// </summary>
        public virtual void Disconnect()
        {
            if (NameserverProcessor.Connected)
            {
                if (nsMessageHandler != null && Nameserver.ContactList.Owner != null)
                {
                    Nameserver.ContactList.Owner.SetStatus(PresenceStatus.Offline);
                }

                NameserverProcessor.Disconnect();
            }
        }

        /// <summary>
        /// Creates a conversation.
        /// </summary>
        /// <remarks>
        /// This method will fire the <see cref="ConversationCreated"/> event. The initiator object of the created switchboard will be <b>this</b> messenger object.
        /// </remarks>
        /// <returns></returns>
        public Conversation CreateConversation()
        {
            Conversation conversation = new Conversation(this);
            OnConversationCreated(conversation, this);
            return conversation;
        }


        /// <summary>
        /// Returns a MSNSLPHandler, associated with a P2P session. The returned object can be used to send
        /// or receive invitations from the remote contact.
        /// </summary>
        /// <param name="remoteContact"></param>
        /// <returns></returns>
        public MSNSLPHandler GetMSNSLPHandler(Contact remoteContact)
        {
            if (!Nameserver.ContactList.HasContact(remoteContact.Mail, remoteContact.ClientType))
                throw new MSNPSharpException("Function not supported. Only MSN user can create a P2P session.");

            P2PMessageSession p2pSession = nsMessageHandler.P2PHandler.GetSession(Nameserver.ContactList.Owner, Nameserver.ContactList.Owner.MachineGuid, remoteContact, remoteContact.SelectRandomEPID());
            return p2pSession.MasterSession;
        }

        public P2PTransferSession SendFile(Contact remoteContact, string filename, FileStream fileStream)
        {
            MSNSLPHandler msnslpHandler = GetMSNSLPHandler(remoteContact);
            return msnslpHandler.SendInvitation(Owner, remoteContact, Path.GetFileName(filename), fileStream);
        }

        public P2PTransferSession RequestMsnObject(Contact remoteContact, MSNObject msnObject)
        {
            MSNSLPHandler msnslpHandler = GetMSNSLPHandler(remoteContact);
            return msnslpHandler.SendInvitation(Owner, remoteContact, msnObject);
        }

        #endregion
    }
};
