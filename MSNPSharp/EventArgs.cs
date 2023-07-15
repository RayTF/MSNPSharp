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

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.DataTransfer;

    /// <summary>
    /// Used when contact changed its status.
    /// </summary>
    [Serializable()]
    public class ContactStatusChangedEventArgs : StatusChangedEventArgs
    {
        Contact contact;

        /// <summary>
        /// The contact who changed its status.
        /// </summary>
        public Contact Contact
        {
            get
            {
                return contact;
            }
            set
            {
                contact = value;
            }
        }

        public ContactStatusChangedEventArgs(Contact contact,
                                            PresenceStatus oldStatus)
            :base(oldStatus)
        {
            Contact = contact;
        }
    }

    /// <summary>
    /// Used when any contect event occured.
    /// </summary>
    [Serializable()]
    public class BaseContactEventArgs : EventArgs
    {
        protected Contact contact = null;

        public BaseContactEventArgs(Contact contact)
        {
            this.contact = contact;
        }
    }

    [Serializable()]
    public class ContactEventArgs : BaseContactEventArgs
    {
        /// <summary>
        /// The contact raise the event.
        /// </summary>
        public Contact Contact
        {
            get
            {
                return contact;
            }
            set
            {
                contact = value;
            }
        }

        public ContactEventArgs(Contact contact)
            : base(contact)
        {
        }
    }

    [Serializable()]
    public class ContactConversationEventArgs : ContactEventArgs
    {
        private Guid epoint = Guid.Empty;

        public Guid EndPoint
        {
            get { return epoint; }
        }

        public ContactConversationEventArgs(Contact contact, Guid endPoint)
            :base(contact)
        {
            epoint = endPoint;
        }
    }

    /// <summary>
    /// Use when user's sign in places changed.
    /// </summary>
    public class PlaceChangedEventArgs : EventArgs
    {
        private string placeName = string.Empty;
        private Guid placeId = Guid.Empty;
        private PlaceChangedReason reason = PlaceChangedReason.None;

        public PlaceChangedReason Reason
        {
            get { return reason; }
        }

        public string PlaceName
        {
            get { return placeName; }
        }


        public Guid PlaceId
        {
            get { return placeId; }
        }

        private PlaceChangedEventArgs()
            : base()
        {
        }

        public PlaceChangedEventArgs(Guid id, string name, PlaceChangedReason action)
            : base()
        {
            placeId = id;
            placeName = name;
            reason = action;
        }

    }

    /// <summary>
    /// Used when a contact changed its status.
    /// </summary>
    [Serializable()]
    public class StatusChangedEventArgs : EventArgs
    {
        private PresenceStatus oldStatus;

        public PresenceStatus OldStatus
        {
            get
            {
                return oldStatus;
            }
            set
            {
                oldStatus = value;
            }
        }

        public StatusChangedEventArgs(PresenceStatus oldStatus)
        {
            OldStatus = oldStatus;
        }
    }

    /// <summary>
    /// Used in events where a exception is raised. Via these events the client programmer
    /// can react on these exceptions.
    /// </summary>
    [Serializable()]
    public class ExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private Exception _exception;

        /// <summary>
        /// The exception that was raised
        /// </summary>
        public Exception Exception
        {
            get
            {
                return _exception;
            }
            set
            {
                _exception = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="e"></param>
        public ExceptionEventArgs(Exception e)
        {
            _exception = e;
        }
    }

    /// <summary>
    /// Base class for all message received event args.
    /// </summary>
    [Serializable()]
    public class BaseMessageReceivedEventArgs : BaseContactEventArgs
    {
        /// <summary>
        /// The sender.
        /// </summary>
        public Contact Sender
        {
            get
            {
                return contact;
            }
        }

        internal BaseMessageReceivedEventArgs(Contact sender)
            : base(sender)
        {
        }
    }


    /// <summary>
    /// Used as event argument when a textual message is send.
    /// </summary>
    [Serializable()]
    public class TextMessageEventArgs : BaseMessageReceivedEventArgs
    {
        /// <summary>
        /// </summary>
        private TextMessage message;

        /// <summary>
        /// The message send.
        /// </summary>
        public TextMessage Message
        {
            get
            {
                return message;
            }
            set
            {
                message = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        public TextMessageEventArgs(TextMessage message, Contact sender)
            : base(sender)
        {
            Message = message;
        }
    }

    [Serializable()]
    public class WinkEventArgs : BaseMessageReceivedEventArgs
    {
        private Wink wink;

        public Wink Wink
        {
            get
            {
                return wink;
            }
            set
            {
                wink = value;
            }
        }

        public WinkEventArgs(Contact contact, Wink wink)
            :base(contact)
        {
            this.wink = wink;
        }
    }

    /// <summary>
    /// Used as event argument when a emoticon definition is send.
    /// </summary>
    [Serializable()]
    public class EmoticonDefinitionEventArgs : BaseMessageReceivedEventArgs
    {

        /// <summary>
        /// </summary>
        private Emoticon emoticon;

        /// <summary>
        /// The emoticon which is defined
        /// </summary>
        public Emoticon Emoticon
        {
            get
            {
                return emoticon;
            }
            set
            {
                emoticon = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="emoticon"></param>
        public EmoticonDefinitionEventArgs(Contact sender, Emoticon emoticon)
            :base(sender)
        {
            this.emoticon = emoticon;
        }
    }

    /// <summary>
    /// Used when a list (FL, Al, BL, RE) is received via synchronize or on request.
    /// </summary>
    [Serializable()]
    public class ListReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private MSNLists affectedList = MSNLists.None;

        /// <summary>
        /// The list which was send by the server
        /// </summary>
        public MSNLists AffectedList
        {
            get
            {
                return affectedList;
            }
            set
            {
                affectedList = value;
            }
        }

        /// <summary>
        /// Constructory.
        /// </summary>
        /// <param name="affectedList"></param>
        public ListReceivedEventArgs(MSNLists affectedList)
        {
            AffectedList = affectedList;
        }
    }

    /// <summary>
    /// Used when the local user is signed off.
    /// </summary>
    [Serializable()]
    public class SignedOffEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private SignedOffReason signedOffReason;

        /// <summary>
        /// The list which was send by the server
        /// </summary>
        public SignedOffReason SignedOffReason
        {
            get
            {
                return signedOffReason;
            }
            set
            {
                signedOffReason = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="signedOffReason"></param>
        public SignedOffEventArgs(SignedOffReason signedOffReason)
        {
            this.signedOffReason = signedOffReason;
        }
    }

    /// <summary>
    /// Used as event argument when an answer to a ping is received.
    /// </summary>
    [Serializable()]
    public class PingAnswerEventArgs : EventArgs
    {
        /// <summary>
        /// The number of seconds to wait before sending another PNG, 
        /// and is reset to 50 every time a command is sent to the server. 
        /// In environments where idle connections are closed after a short time, 
        /// you should send a command to the server (even if it's just a PNG) at least this often.
        /// Note: MSNPSharp does not handle this! E.g. if you experience unexpected connection dropping call the Ping() method.
        /// </summary>
        public int SecondsToWait
        {
            get
            {
                return secondsToWait;
            }
            set
            {
                secondsToWait = value;
            }
        }

        /// <summary>
        /// </summary>
        private int secondsToWait;


        /// <summary>
        /// </summary>
        /// <param name="seconds"></param>
        public PingAnswerEventArgs(int seconds)
        {
            SecondsToWait = seconds;
        }
    }

    /// <summary>
    /// Used as event argument when any contact list mutates.
    /// </summary>
    [Serializable()]
    public class ListMutateEventArgs : ContactEventArgs
    {
        /// <summary>
        /// </summary>
        private MSNLists affectedList = MSNLists.None;

        /// <summary>
        /// The list which mutated.
        /// </summary>
        public MSNLists AffectedList
        {
            get
            {
                return affectedList;
            }
            set
            {
                affectedList = value;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="affectedList"></param>
        public ListMutateEventArgs(Contact contact, MSNLists affectedList)
            : base(contact)
        {
            AffectedList = affectedList;
        }
    }

    /// <summary>
    /// Used as event argument when msn sends us an error.
    /// </summary>	
    [Serializable()]
    public class MSNErrorEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private MSNError msnError;

        /// <summary>
        /// The error that occurred
        /// </summary>
        public MSNError MSNError
        {
            get
            {
                return msnError;
            }
            set
            {
                msnError = value;
            }
        }

        /// <summary>
        /// Constructory.
        /// </summary>
        /// <param name="msnError"></param>
        public MSNErrorEventArgs(MSNError msnError)
        {
            this.msnError = msnError;
        }
    }

    /// <summary>
    /// Base class for circle event arg.
    /// </summary>
    public class BaseCircleEventArgs : EventArgs
    {
        protected Circle circle = null;
        internal BaseCircleEventArgs(Circle circle)
        {
            this.circle = circle;
        }
    }

    /// <summary>
    /// Used as event argument when a <see cref="Circle"/> is affected.
    /// </summary>
    [Serializable()]
    public class CircleEventArgs : BaseCircleEventArgs
    {
        protected Contact remoteMember = null;

        /// <summary>
        /// The affected contact group
        /// </summary>
        public Circle Circle
        {
            get
            {
                return circle;
            }
        }

        /// <summary>
        /// Constructor, mostly used internal by the library.
        /// </summary>
        /// <param name="circle"></param>
        internal CircleEventArgs(Circle circle)
            :base(circle)
        {
        }

        /// <summary>
        /// Constructor, mostly used internal by the library.
        /// </summary>
        /// <param name="circle"></param>
        /// <param name="remote">The affected Contact.</param>
        internal CircleEventArgs(Circle circle, Contact remote)
            :base(circle)
        {
            remoteMember = remote;
        }
    }

    /// <summary>
    /// Used when a event related to circle member operaion fired.
    /// </summary>
    [Serializable()]
    public class CircleMemberEventArgs : CircleEventArgs
    {
        /// <summary>
        /// The contact member raise the event.
        /// </summary>
        public Contact Member
        {
            get
            {
                return remoteMember;
            }
        }

        internal CircleMemberEventArgs(Circle circle, Contact member)
            : base(circle, member)
        {
        }
    }

    [Serializable()]
    public class CircleStatusChangedEventArgs: StatusChangedEventArgs
    {
        protected Circle circle = null;

        /// <summary>
        /// The circle which changed its status.
        /// </summary>
        public Circle Circle
        {
            get { return circle; }
        }

        internal CircleStatusChangedEventArgs(Circle circle, PresenceStatus oldStatus)
            : base(oldStatus)
        {
            this.circle = circle;
        }
    }

    [Serializable()]
    public class CircleMemberStatusChanged : CircleStatusChangedEventArgs
    {
        private Contact circleMember = null;

        protected Contact CircleMember
        {
            get { return circleMember; }
        }

        internal CircleMemberStatusChanged(Circle circle, Contact member, PresenceStatus oldStatus)
            : base(circle, oldStatus)
        {
            circleMember = member;
        }

    }

    /// <summary>
    /// Event argument used for ContactService.JoinCircleInvitationReceived event.
    /// </summary>
    [Obsolete("Inviter is no more supported by Microsoft.")]
    [Serializable()]
    public class JoinCircleInvitationEventArgs : CircleEventArgs
    {

        /// <summary>
        /// <see cref="Contact"/> who send this invitation.
        /// </summary>
        public CircleInviter Inviter
        {
            get { return remoteMember as CircleInviter; }
        }

        internal JoinCircleInvitationEventArgs(Circle circle, CircleInviter invitor)
            : base(circle)
        {
        }
    }

    /// <summary>
    /// Event argument used for receiving text messages from a circle.
    /// </summary>
    [Serializable()]
    public class CircleTextMessageEventArgs : TextMessageEventArgs
    {
        protected Contact triggerMember = null;

        public CircleTextMessageEventArgs(TextMessage textMessage, Circle sender, Contact triggerMember)
            : base(textMessage, sender)
        {
            this.triggerMember = triggerMember;
        }

        /// <summary>
        /// The circle message send from.
        /// </summary>
        public new Circle Sender
        {
            get
            {
                return base.Sender as Circle;
            }
        }

        /// <summary>
        /// The circle member who send this message.
        /// </summary>
        public Contact TriggerMember
        {
            get
            {
                return triggerMember;
            }
        }
    }

    /// <summary>
    /// Event argument used when a user's <see cref="DisplayImage"/> property has been changed.
    /// </summary>
    public class DisplayImageChangedEventArgs : EventArgs
    {
        private bool callFromContactManager = false;
        private DisplayImageChangedType status = DisplayImageChangedType.None;
        private DisplayImage newDisplayImage = null;

        public DisplayImage NewDisplayImage
        {
            get 
            { 
                return newDisplayImage; 
            }
        }

        /// <summary>
        ///  The reason that fires <see cref="Contact.DisplayImageChanged"/> event.
        /// </summary>
        public DisplayImageChangedType Status
        {
            get 
            { 
                return status; 
            }
        }

        /// <summary>
        /// Whether we need to do display image synchronize.
        /// </summary>
        internal bool CallFromContactManager
        {
            get 
            { 
                return callFromContactManager; 
            }
        }

        private DisplayImageChangedEventArgs()
        {
        }

        internal DisplayImageChangedEventArgs(DisplayImage dispImage, DisplayImageChangedType type, bool needSync)
        {
            status = type;
            callFromContactManager = needSync;
            newDisplayImage = dispImage;
        }

        internal DisplayImageChangedEventArgs(DisplayImageChangedEventArgs arg, bool needSync)
        {
            status = arg.Status;
            callFromContactManager = needSync;
            newDisplayImage = arg.NewDisplayImage;
        }

        public DisplayImageChangedEventArgs(DisplayImage dispImage, DisplayImageChangedType type)
        {
            status = type;
            callFromContactManager = false;
            newDisplayImage = dispImage;
        }
    }

    /// <summary>
    /// Use when receiving messages from IM network other than MSN.
    /// </summary>
    public class CrossNetworkMessageEventArgs : EventArgs
    {
        private Contact from = null;

        /// <summary>
        /// The sender of message.
        /// </summary>
        public Contact From
        {
            get { return from; }
        }

        private Contact to = null;

        /// <summary>
        /// The receiver of the message.
        /// </summary>
        public Contact To
        {
            get { return to; }
        }

        private int messageType = 0;

        /// <summary>
        /// The type of message received. Please refer to <see cref="NetworkMessageType"/>
        /// </summary>
        public NetworkMessageType MessageType
        {
            get 
            { 
                return (NetworkMessageType)messageType; 
            }
        }


        private NetworkMessage message = null;

        /// <summary>
        /// The message received.
        /// </summary>
        public NetworkMessage Message
        {
            get { return message; }
        }

        /// <summary>
        /// The IM network we get the message.
        /// </summary>
        public NetworkType Network
        {
            get
            {
                if (From != null)
                {
                    switch (From.ClientType)
                    {
                        case ClientType.EmailMember:
                            return NetworkType.Yahoo;

                        case ClientType.PhoneMember:
                            return NetworkType.Mobile;

                        default:
                            return NetworkType.WindowsLive;
                    }
                }

                return NetworkType.None;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sender">The message sender</param>
        /// <param name="receiver">The message receiver</param>
        /// <param name="type">Message type, cast from <see cref="NetworkMessageType"/></param>
        /// <param name="msg">The message body</param>
        public CrossNetworkMessageEventArgs(Contact sender, Contact receiver, int type, NetworkMessage msg)
        {
            from = sender;
            to = receiver;
            messageType = type;
            message = msg;
        }
    }

    /// <summary>
    /// Used to notify client programmer after the MSN data object transfer completed.
    /// </summary>
    public class MSNObjectDataTransferCompletedEventArgs : EventArgs
    {
        private MSNObject clientData = null;
        private bool aborted = false;
        private Contact remote = null;
        private Guid remoteEndPointID = Guid.Empty;

        /// <summary>
        /// Another site associated with this object's transfer.
        /// </summary>
        public Contact RemoteContact
        {
            get { return remote; }
        }

        /// <summary>
        /// The location of remote contact.
        /// </summary>
        public Guid RemoteContactEndPointID
        {
            get { return remoteEndPointID; }
        }

        /// <summary>
        /// Transfer failed.
        /// </summary>
        public bool Aborted
        {
            get { return aborted; }
        }

        /// <summary>
        /// The target msnobject.
        /// </summary>
        public MSNObject ClientData
        {
            get { return clientData; }
        }

        protected MSNObjectDataTransferCompletedEventArgs()
            : base()
        {
        }

        public MSNObjectDataTransferCompletedEventArgs(MSNObject clientdata, bool abort, Contact remoteContact, Guid remoteEPID)
        {
            if (clientdata == null)
                throw new ArgumentNullException("clientdata");

            clientData = clientdata;
            aborted = abort;
            remote = remoteContact;
            remoteEndPointID = remoteEPID;
        }
    }

    public class ConversationMSNObjectDataTransferCompletedEventArgs : MSNObjectDataTransferCompletedEventArgs
    {
        private P2PTransferSession transferSession = null;

        public P2PTransferSession TransferSession
        {
            get 
            { 
                return transferSession; 
            }

            private set 
            { 
                transferSession = value; 
            }
        }

        public ConversationMSNObjectDataTransferCompletedEventArgs(P2PTransferSession sender, MSNObjectDataTransferCompletedEventArgs e)
            : base(e.ClientData, e.Aborted, e.RemoteContact, e.RemoteContactEndPointID)
        {
            TransferSession = sender;
        }
    }

    /// <summary>
    /// Use to notify a <see cref="Conversation"/> has ended.
    /// </summary>
    public class ConversationEndEventArgs : EventArgs
    {
        private Conversation conversation = null;

        public Conversation Conversation
        {
            get { return conversation; }
        }

        protected ConversationEndEventArgs()
            : base()
        {
        }

        public ConversationEndEventArgs(Conversation convers)
        {
            conversation = convers;
        }
    }

    /// <summary>
    /// Used to notify client programmer that the remote owner of a conversation has been changed.
    /// </summary>
    public class ConversationRemoteOwnerChangedEventArgs : EventArgs
    {
        private Contact oldRemoteOwner = null;

        /// <summary>
        /// The remote owner of the conversation before change.
        /// </summary>
        public Contact OldRemoteOwner
        {
            get { return oldRemoteOwner; }
        }

        private Contact newRemoteOwner = null;

        /// <summary>
        /// The new remote owner after the old one has left the conversation.
        /// </summary>
        public Contact NewRemoteOwner
        {
            get { return newRemoteOwner; }
        }

        private ConversationRemoteOwnerChangedEventArgs()
        {
        }

        public ConversationRemoteOwnerChangedEventArgs(Contact oldOwner, Contact newOwner)
        {
            oldRemoteOwner = oldOwner;
            newRemoteOwner = newOwner;
        }
    }
};
