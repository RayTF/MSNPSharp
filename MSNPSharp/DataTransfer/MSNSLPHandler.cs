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
using System.Timers;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    public enum DCNonceType
    {
        None = 0,
        Plain = 1,
        Sha1 = 2
    }

    #region DataTransferType

    /// <summary>
    /// Defines the type of datatransfer for a MSNSLPHandler
    /// </summary>
    public enum DataTransferType
    {
        /// <summary>
        /// Unknown datatransfer type.
        /// </summary>
        Unknown,
        /// <summary>
        /// Filetransfer.
        /// </summary>
        File,
        /// <summary>
        /// Emoticon transfer.
        /// </summary>
        Emoticon,
        /// <summary>
        /// Displayimage transfer.
        /// </summary>
        DisplayImage,
        /// <summary>
        /// Activity invitation.
        /// </summary>
        Activity
    }

    #endregion

    #region ActivityInfo

    /// <summary>
    /// Holds the property of activity such as AppID and activity name.
    /// </summary>
    public class ActivityInfo
    {
        private uint appID = 0;

        /// <summary>
        /// The AppID of activity.
        /// </summary>
        public uint AppID
        {
            get
            {
                return appID;
            }
        }

        private string activityName = string.Empty;

        /// <summary>
        /// The name of activity.
        /// </summary>
        public string ActivityName
        {
            get
            {
                return activityName;
            }
        }

        protected ActivityInfo()
        {
        }

        public ActivityInfo(string contextString)
        {
            try
            {
                byte[] byts = Convert.FromBase64String(contextString);
                string activityUrl = System.Text.Encoding.Unicode.GetString(byts);
                string[] activityProperties = activityUrl.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (activityProperties.Length >= 3)
                {
                    uint.TryParse(activityProperties[0], out appID);
                    activityName = activityProperties[2];
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "An error occurs while parsing activity context, error info: " +
                    ex.Message);
            }
        }

        public override string ToString()
        {
            return "Activity info: " + appID.ToString() + " name: " + activityName;
        }

    }

    #endregion

    #region P2PTransferSessionEventArgs

    /// <summary>
    /// Used as event argument when a P2PTransferSession is affected.
    /// </summary>
    public class P2PTransferSessionEventArgs : EventArgs
    {
        /// <summary>
        /// </summary>
        private P2PTransferSession transferSession;

        /// <summary>
        /// The affected transfer session
        /// </summary>
        public P2PTransferSession TransferSession
        {
            get
            {
                return transferSession;
            }
            set
            {
                transferSession = value;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="transferSession"></param>
        public P2PTransferSessionEventArgs(P2PTransferSession transferSession)
        {
            this.transferSession = transferSession;
        }
    }

    #endregion

    /// <summary>
    /// Handles invitations and requests for file transfers, emoticons, user displays and other msn objects.
    /// </summary>
    /// <remarks>
    /// MSNSLPHandler is responsible for communicating with the remote client about the transfer properties.
    /// This means receiving and sending details about filelength, filename, user display context, etc.
    /// When an invitation request is received the client programmer is asked to accept or decline the
    /// invitation. This is done through the TransferInvitationReceived event. The client programmer must
    /// handle this event and set the Accept and DataStream property in the event argument, see
    /// <see cref="MSNSLPInvitationEventArgs"/>. When the receiver of the invitation has accepted a
    /// <see cref="P2PTransferSession"/> is created and used to actually send the data. In the case of user
    /// displays or other msn objects the data transfer always goes over the switchboard. In case of a file
    /// transfer there will be negotiating about the direct connection to setup. Depending on the
    /// connectivity of both clients, a request for a direct connection is send to associated the
    /// <see cref="P2PTransferSession"/> object.
    /// </remarks>
    public class MSNSLPHandler : IMessageHandler, IDisposable
    {
        #region Events

        /// <summary>
        /// Occurs when a transfer session is created.
        /// </summary>
        public event EventHandler<P2PTransferSessionEventArgs> TransferSessionCreated;

        /// <summary>
        /// Occurs when a transfer session is closed. Either because the transfer has finished or aborted.
        /// </summary>
        public event EventHandler<P2PTransferSessionEventArgs> TransferSessionClosed;

        /// <summary>
        /// Occurs when a remote client has send an invitation for a transfer session.
        /// </summary>
        public event EventHandler<MSNSLPInvitationEventArgs> TransferInvitationReceived;

        #endregion

        #region Members

        private IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 0);
        private IPEndPoint externalEndPoint = null;
        private P2PVersion version = P2PVersion.P2PV1;
        private int directConnectionExpireInterval = 6000;
        private IMessageProcessor messageProcessor = null;
        private Guid schedulerID = Guid.Empty;

        /// <summary>
        /// A dictionary containing MSNSLPTransferProperties objects. Indexed by CallId;
        /// </summary>
        private Dictionary<Guid, MSNSLPTransferProperties> transferProperties =
            new Dictionary<Guid, MSNSLPTransferProperties>(4);

        #endregion

        #region Constructors & Destructors

        protected MSNSLPHandler()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);
        }

        public MSNSLPHandler(P2PVersion ver, Guid invitationSchedulerId, IPAddress externalAddress)
        {
            version = ver;
            schedulerID = invitationSchedulerId;
            ExternalEndPoint = new IPEndPoint(externalAddress, 0);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                String.Format("Constructing object, version = {0}\r\n" +
                "A new {1} created, with scheduler id = {2}",
                Version, GetType().Name, SchedulerID.ToString("B")), GetType().Name);
        }

        /// <summary>
        /// Destructor calls Dispose(false)
        /// </summary>
        ~MSNSLPHandler()
        {
            Dispose(false);
        }

        /// <summary>
        /// Closes all sessions. Dispose() calls Dispose(true)
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Free managed resources
                CloseAllSessions();
            }

            // Free native resources if there are any.
        }

        #endregion

        #region Properties

        /// <summary>
        /// P2P version
        /// </summary>
        public P2PVersion Version
        {
            get
            {
                return version;
            }
        }

        /// <summary>
        /// The message processor to send outgoing p2p messages to.
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                if (value != null && object.ReferenceEquals(MessageProcessor, value))
                    return;

                if (value == null && MessageProcessor != null)
                {
                    MessageProcessor.UnregisterHandler(this);
                    messageProcessor = value;
                    return;
                }

                if (MessageProcessor != null)
                {
                    MessageProcessor.UnregisterHandler(this);
                }

                messageProcessor = value;
                messageProcessor.RegisterHandler(this);
            }
        }

        /// <summary>
        /// The message session to send message to. This is simply the MessageProcessor property,
        /// but explicitly casted as a P2PMessageSession.
        /// </summary>
        public P2PMessageSession MessageSession
        {
            get
            {
                return (P2PMessageSession)MessageProcessor;
            }
        }

        /// <summary>
        /// The client's local end-point. This can differ from the external endpoint through the use of
        /// routers. This value is used to determine how to set-up a direct connection.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get
            {
                IPAddress iphostentry = IPAddress.Any;
                IPAddress[] addrList = Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList;

                for (int IPC = 0; IPC < addrList.Length; IPC++)
                {
                    if (addrList[IPC].AddressFamily == AddressFamily.InterNetwork)
                    {
                        localEndPoint.Address = addrList[IPC];
                        break;
                    }
                }
                return localEndPoint;
            }
        }

        /// <summary>
        /// The client end-point as perceived by the server. This can differ from the actual local endpoint
        /// through the use of routers. This value is used to determine how to set-up a direct connection.
        /// </summary>
        public IPEndPoint ExternalEndPoint
        {
            get
            {
                return externalEndPoint;
            }

            private set
            {
                externalEndPoint = value;
            }
        }

        /// <summary>
        /// The P2P invitation scheduler guid.
        /// </summary>
        protected Guid SchedulerID
        {
            get
            {
                return schedulerID;
            }
        }

        #endregion

        #region SendInvitation

        #region MSNObject

        /// <summary>
        /// Sends the remote contact a request for the given context. The invitation message is send over the current MessageProcessor.
        /// </summary>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, MSNObject msnObject)
        {
            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(localContact, MessageSession.LocalContactEndPointID,
                remoteContact, MessageSession.RemoteContactEndPointID);
            properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));


            P2PMessage p2pMessage = new P2PMessage(Version);
            P2PTransferSession transferSession = new P2PTransferSession(p2pMessage.Version, properties, MessageSession);
            transferSession.TransferFinished += delegate
            {
                transferSession.SendDisconnectMessage(SLPRequestMessage.CreateClosingMessage(properties));
            };

            Contact remote = MessageSession.RemoteContact;
            string AppID = "1";

            string msnObjectContext = msnObject.ContextPlain;

            if (msnObject.ObjectType == MSNObjectType.Emoticon)
            {
                properties.DataType = DataTransferType.Emoticon;
                transferSession.MessageFooter = P2PConst.CustomEmoticonFooter11;
                transferSession.DataStream = msnObject.OpenStream();
                AppID = transferSession.MessageFooter.ToString();

            }
            else if (msnObject.ObjectType == MSNObjectType.UserDisplay)
            {
                if (remoteContact.DisplayImage == remoteContact.UserTileLocation)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "The current display image of remote contact: " + remoteContact + "is the same with it's displayImage context, invitation will not be processed.");
                    return null;  //If the display image is the same as current user tile string, do not process the invitation.
                }

                properties.DataType = DataTransferType.DisplayImage;
                transferSession.MessageFooter = P2PConst.DisplayImageFooter12;

                AppID = transferSession.MessageFooter.ToString();
                DisplayImage displayImage = msnObject as DisplayImage;

                if (displayImage == null)
                    throw new ArgumentNullException("msnObject is not a DisplayImage object.");

                msnObjectContext = remoteContact.UserTileLocation;


                transferSession.TransferFinished += delegate(object sender, EventArgs ea)
                {
                    //User display image is a special case, we use the default DataStream of a TransferSession.
                    //After the transfer finished, we create a new display image and fire the DisplayImageChanged event.
                    DisplayImage newDisplayImage = new DisplayImage(remoteContact.Mail.ToLowerInvariant(), transferSession.DataStream as MemoryStream);
                    remoteContact.SetDisplayImageAndFireDisplayImageChangedEvent(newDisplayImage);
                };
            }

            if (string.IsNullOrEmpty(msnObjectContext))
            {
                //This is a default displayImage or any object created by the client programmer.
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "[SendInvitation] msnObject does not have OriginalContext.");
                throw new InvalidOperationException("Parameter msnObject does not have valid OriginalContext property.");
            }

            byte[] contextArray = System.Text.Encoding.UTF8.GetBytes(msnObjectContext);

            string base64Context = Convert.ToBase64String(contextArray, 0, contextArray.Length);
            properties.Context = base64Context;

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            SLPRequestMessage slpMessage = new SLPRequestMessage(properties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = properties.RemoteContactEPIDString;
            slpMessage.FromMail = properties.LocalContactEPIDString;
            slpMessage.Branch = properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";

            slpMessage.BodyValues["EUF-GUID"] = P2PConst.UserDisplayGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();

            if (version == P2PVersion.P2PV1)
            {
                slpMessage.BodyValues["SChannelState"] = "0";
                slpMessage.BodyValues["Capabilities-Flags"] = "1";

                p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
                transferSession.MessageFlag = (uint)P2PFlag.MSNObjectData;
            }

            if (version == P2PVersion.P2PV2)
            {
                slpMessage.BodyValues["RequestFlags"] = "18";
            }

            slpMessage.BodyValues["AppID"] = AppID;
            slpMessage.BodyValues["Context"] = base64Context;

            p2pMessage.InnerMessage = slpMessage;

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.OperationCode = (byte)(OperationCode.SYN | OperationCode.RAK);
                p2pMessage.V2Header.AppendPeerInfoTLV();

                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    p2pMessage.V2Header.PackageNumber = (ushort)((p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) / 1202 + 1);
                }
                else
                {
                    p2pMessage.V2Header.PackageNumber = 0;
                }
                transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                p2pMessage.V2Header.TFCombination = TFCombination.First;

            }

            // store the transferproperties
            lock (transferProperties)
                transferProperties[properties.CallId] = properties;


            transferSession.IsSender = false;

            OnTransferSessionCreated(transferSession);

            // send the invitation
            Schedulers.P2PInvitationScheduler.Enqueue(transferSession, p2pMessage, SchedulerID);

            return transferSession;
        }

        #endregion

        #region Activity

        /// <summary>
        /// Sends the remote contact a invitation for the activity. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="applicationID">The ID of activity, that was register by Microsoft.</param>
        /// <param name="activityName">The name of Activity.</param>
        /// <returns></returns>
        /// <example>
        /// <code language="C#">
        /// //An example that invites a remote user to attend the "Music Mix" activity.
        /// 
        /// String remoteAccount = @"remoteUser@hotmail.com";
        /// 
        /// String activityID = "20521364";        //The activityID of Music Mix activity.
        /// String activityName = "Music Mix";     //The name of acticvity
        /// 
        /// P2PMessageSession session =  Conversation.Messenger.P2PHandler.GetSession(Conversation.Messenger.ContactList.Owner.Mail, remoteAccount);
        /// MSNSLPHandler slpHandler =  session.GetHandler(typeof(MSNSLPHandler)) as MSNSLPHandler ;
        /// slpHandler.SendInvitation(Conversation.Messenger.ContactList.Owner.Mail, remoteaccount, activityID, activityName);
        /// </code>
        /// </example>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, string applicationID, string activityName)
        {
            // set class variables
            return SendInvitation(localContact, remoteContact, applicationID, activityName, string.Empty, 0);
        }

        /// <summary>
        /// Sends the remote contact a invitation for the activity. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="applicationID">The ID of activity, that was register by Microsoft.</param>
        /// <param name="activityName">The name of Activity.</param>
        /// <param name="activityData">The parameter string that want to send to the activity window.</param>
        /// <returns></returns>
        /// <example>
        /// <code language="C#">
        /// //An example that invites a remote user to attend the "Music Mix" activity.
        /// 
        /// String remoteAccount = @"remoteUser@hotmail.com";
        /// 
        /// String activityID = "20521364";        //The activityID of Music Mix activity.
        /// String activityName = "Music Mix";     //The name of acticvity
        /// String userName = "Pang Wu";           //The parameter send to the client's activity window.
        /// 
        /// P2PMessageSession session =  Conversation.Messenger.P2PHandler.GetSession(Conversation.Messenger.ContactList.Owner.Mail, remoteAccount);
        /// MSNSLPHandler slpHandler =  session.GetHandler(typeof(MSNSLPHandler)) as MSNSLPHandler ;
        /// slpHandler.SendInvitation(Conversation.Messenger.ContactList.Owner.Mail, remoteaccount, activityID, activityName, activityData);
        /// </code>
        /// </example>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, string applicationID, string activityName, string activityData)
        {
            return SendInvitation(localContact, remoteContact, applicationID, activityName, activityData, 6000);
        }

        /// <summary>
        /// Sends the remote contact a invitation for the activity. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="applicationID">The ID of activity, that was register by Microsoft.</param>
        /// <param name="activityName">The name of Activity.</param>
        /// <param name="activityData">The parameter string that want to send to the activity window.</param>
        /// <param name="directConnectionExpireInterval">How long should the library wait (in ms.) to send out the activityData after the use accepted the invitation.</param>
        /// <returns></returns>
        /// <example>
        /// <code language="C#">
        /// //An example that invites a remote user to attend the "Music Mix" activity.
        /// 
        /// String remoteAccount = @"remoteUser@hotmail.com";
        /// 
        /// String activityID = "20521364";        //The activityID of Music Mix activity.
        /// String activityName = "Music Mix";     //The name of acticvity
        /// String activityData = "Pang Wu";           //The parameter send to the client's activity window.
        /// 
        /// P2PMessageSession session =  Conversation.Messenger.P2PHandler.GetSession(Conversation.Messenger.ContactList.Owner.Mail, remoteAccount);
        /// MSNSLPHandler slpHandler =  session.GetHandler(typeof(MSNSLPHandler)) as MSNSLPHandler ;
        /// slpHandler.SendInvitation(Conversation.Messenger.ContactList.Owner.Mail, remoteaccount, activityID, activityName, activityData, 5000);
        /// </code>
        /// </example>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, string applicationID, string activityName, string activityData, int directConnectionExpireInterval)
        {
            this.directConnectionExpireInterval = directConnectionExpireInterval;

            // set class variables
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(localContact, MessageSession.LocalContactEndPointID,
                remoteContact, MessageSession.RemoteContactEndPointID);
            properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));

            properties.DataType = DataTransferType.Activity;

            string activityID = applicationID + ";1;" + activityName;
            byte[] contextData = System.Text.UnicodeEncoding.Unicode.GetBytes(activityID);
            string base64Context = Convert.ToBase64String(contextData, 0, contextData.Length);

            properties.Context = base64Context;

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            SLPRequestMessage slpMessage = new SLPRequestMessage(properties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = properties.RemoteContactEPIDString;
            slpMessage.FromMail = properties.LocalContactEPIDString;
            slpMessage.Branch = properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["EUF-GUID"] = P2PConst.ActivityGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();
            slpMessage.BodyValues["SChannelState"] = "0";
            slpMessage.BodyValues["Capabilities-Flags"] = "1";
            slpMessage.BodyValues["AppID"] = applicationID.ToString();
            slpMessage.BodyValues["Context"] = base64Context;

            if (Version == P2PVersion.P2PV2)
            {
                slpMessage.BodyValues["RequestFlags"] = "16";
            }

            P2PMessage p2pMessage = new P2PMessage(Version);
            p2pMessage.InnerMessage = slpMessage;

            lock (transferProperties)
                transferProperties[properties.CallId] = properties;

            // create a transfer session to handle the actual data transfer
            P2PTransferSession transferSession = new P2PTransferSession(Version, properties, MessageSession);

            if (Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.OperationCode = (byte)(OperationCode.SYN | OperationCode.RAK);
                p2pMessage.V2Header.AppendPeerInfoTLV();

                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    p2pMessage.V2Header.PackageNumber = (ushort)((p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) / 1202 + 1);
                }
                else
                {
                    p2pMessage.V2Header.PackageNumber = 0;
                }
                transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                p2pMessage.V2Header.TFCombination = TFCombination.First;
            }

            if (activityData != string.Empty && activityData != null)
            {
                activityData += "\0";

                int urlLength = Encoding.Unicode.GetByteCount(activityData);

                MemoryStream urlDataStream = new MemoryStream();

                byte[] header = null;
                ;

                if (Version == P2PVersion.P2PV1)
                {
                    header = new byte[] { 0x80, 0x00, 0x00, 0x00 };
                }

                if (Version == P2PVersion.P2PV2)
                {
                    header = new byte[] { 0x80, 0x3f, 0x14, 0x05 };
                }

                urlDataStream.Write(header, 0, header.Length);
                urlDataStream.Write(BitUtility.GetBytes((ushort)0x08, true), 0, sizeof(ushort));  //data type: 0x08: string
                urlDataStream.Write(BitUtility.GetBytes(urlLength, true), 0, sizeof(int));
                urlDataStream.Write(Encoding.Unicode.GetBytes(activityData), 0, urlLength);

                urlDataStream.Seek(0, SeekOrigin.Begin);
                transferSession.DataStream = urlDataStream;
                transferSession.IsSender = true;
            }

            transferSession.MessageFlag = (uint)P2PFlag.Normal;
            transferSession.TransferFinished += delegate
            {
                transferSession.SendDisconnectMessage(SLPRequestMessage.CreateClosingMessage(properties));
            };


            OnTransferSessionCreated(transferSession);
            Schedulers.P2PInvitationScheduler.Enqueue(transferSession, p2pMessage, SchedulerID);

            return transferSession;
        }

        #endregion

        #region File

        /// <summary>
        /// Sends the remote contact a request for the filetransfer. The invitation message is send over the current MessageProcessor.
        /// </summary>
        /// <param name="localContact"></param>
        /// <param name="remoteContact"></param>
        /// <param name="filename"></param>
        /// <param name="file"></param>
        public P2PTransferSession SendInvitation(Contact localContact, Contact remoteContact, string filename, Stream file)
        {

            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(
                localContact, MessageSession.LocalContactEndPointID, remoteContact, MessageSession.RemoteContactEndPointID);

            do
            {
                properties.SessionId = (uint)(new Random().Next(50000, int.MaxValue));
            }
            while (MessageSession.GetTransferSession(properties.SessionId) != null);

            properties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
            properties.CallId = Guid.NewGuid();

            properties.DataType = DataTransferType.File;

            //0-7: location of the FT preview
            //8-15: file size
            //16-19: I don't know or need it so...
            //20-540: location I use to get the file name
            // set class variables
            // size: location (8) + file size (8) + unknown (4) + filename (236) = 256

            // create a new bytearray to build up the context
            byte[] contextArray = new byte[574];// + picturePreview.Length];
            BinaryWriter binWriter = new BinaryWriter(new MemoryStream(contextArray, 0, contextArray.Length, true));

            // length of preview data
            binWriter.Write((int)574);

            // don't know some sort of flag
            binWriter.Write((int)2);

            // send the total size of the file we are to transfer

            binWriter.Write((long)file.Length);

            // don't know some sort of flag
            binWriter.Write((int)1);

            // write the filename in the context
            binWriter.Write(System.Text.UnicodeEncoding.Unicode.GetBytes(filename));

            // convert to base 64 string
            //base64Context = "PgIAAAIAAAAVAAAAAAAAAAEAAABkAGMALgB0AHgAdAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            string base64Context = Convert.ToBase64String(contextArray, 0, contextArray.Length);

            // set our current context
            properties.Context = base64Context;

            // store the transferproperties
            lock (transferProperties)
                transferProperties[properties.CallId] = properties;

            // create the message
            SLPRequestMessage slpMessage = new SLPRequestMessage(properties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = properties.RemoteContactEPIDString;
            slpMessage.FromMail = properties.LocalContactEPIDString;
            slpMessage.Branch = properties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = properties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["EUF-GUID"] = P2PConst.FileTransferGuid;
            slpMessage.BodyValues["SessionID"] = properties.SessionId.ToString();
            slpMessage.BodyValues["AppID"] = P2PConst.FileTransFooter2.ToString();
            slpMessage.BodyValues["Context"] = base64Context;

            if (Version == P2PVersion.P2PV2)
            {
                slpMessage.BodyValues["RequestFlags"] = "16";
            }

            P2PMessage p2pMessage = new P2PMessage(Version);
            p2pMessage.InnerMessage = slpMessage;

            // create a transfer session to handle the actual data transfer
            P2PTransferSession transferSession = new P2PTransferSession(Version, properties, MessageSession);

            if (Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.OperationCode = (byte)(OperationCode.RAK | OperationCode.SYN);
                p2pMessage.V2Header.AppendPeerInfoTLV();

                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    p2pMessage.V2Header.PackageNumber = (ushort)((p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) / 1202 + 1);
                }
                else
                {
                    p2pMessage.V2Header.PackageNumber = 0;
                }
                transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                p2pMessage.V2Header.TFCombination = TFCombination.First;
            }

            transferSession.MessageFlag = (uint)P2PFlag.FileData;
            transferSession.MessageFooter = P2PConst.FileTransFooter2;

            transferSession.TransferFinished += delegate
            {
                transferSession.SendDisconnectMessage(SLPRequestMessage.CreateClosingMessage(properties));
            };

            // set the data stream to read from
            transferSession.DataStream = file;
            transferSession.IsSender = true;

            OnTransferSessionCreated(transferSession);

            Schedulers.P2PInvitationScheduler.Enqueue(transferSession, p2pMessage, SchedulerID);

            return transferSession;
        }

        #endregion

        #endregion

        #region Accept/Reject Transfer

        /// <summary>
        /// The client programmer should call this if he wants to reject the transfer
        /// </summary>
        public void RejectTransfer(MSNSLPInvitationEventArgs invitationArgs)
        {
            if (invitationArgs.TransferSession != null)
            {
                invitationArgs.TransferSession.SendMessage(SLPStatusMessage.CreateDeclineMessage(invitationArgs.TransferProperties));
                invitationArgs.TransferSession.SendMessage(SLPRequestMessage.CreateClosingMessage(invitationArgs.TransferProperties));

            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[RejectTransfer Error] No transfer session attached.");
            }
        }

        /// <summary>
        /// The client programmer should call this if he wants to accept the transfer
        /// </summary>
        public void AcceptTransfer(MSNSLPInvitationEventArgs invitationArgs)
        {
            MSNSLPTransferProperties properties = invitationArgs.TransferProperties;
            P2PTransferSession transferSession = invitationArgs.TransferSession;
            SLPMessage message = invitationArgs.InvitationMessage;

            // check for a valid datastream
            if (invitationArgs.TransferSession.DataStream == null)
                throw new MSNPSharpException("The invitation was accepted, but no datastream to read to/write from has been specified.");

            // the client programmer has accepted, continue !
            lock (transferProperties)
                transferProperties[properties.CallId] = properties;

            // we want to be notified of messages through this session
            transferSession.RegisterHandler(this);
            transferSession.DataStream = invitationArgs.TransferSession.DataStream;


            switch (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture))
            {
                case P2PConst.UserDisplayGuid:
                    {
                        // for some kind of weird behavior, our local identifier must now subtract 4 ?
                        transferSession.IsSender = true;
                        transferSession.MessageFlag = (uint)P2PFlag.MSNObjectData;
                        transferSession.MessageFooter = P2PConst.DisplayImageFooter12;

                        break;
                    }

                case P2PConst.FileTransferGuid:
                    {
                        transferSession.IsSender = false;
                        transferSession.MessageFlag = (uint)P2PFlag.FileData;
                        transferSession.MessageFooter = P2PConst.FileTransFooter2;
                        break;
                    }

                case P2PConst.ActivityGuid:
                    {
                        transferSession.MessageFlag = (uint)P2PFlag.Normal;
                        if (message.BodyValues.ContainsKey("AppID"))
                        {
                            uint appID = 0;
                            uint.TryParse(message.BodyValues["AppID"].Value, out appID);
                            transferSession.MessageFooter = appID;
                        }
                        transferSession.IsSender = false;
                        break;
                    }
            }

            OnTransferSessionCreated(transferSession);
            transferSession.AcceptInvitation(SLPStatusMessage.CreateAcceptanceMessage(properties));
        }

        #endregion

        #region CloseSession & CloseAllSessions

        /// <summary>
        /// Close the specified session
        /// </summary>
        /// <param name="transferSession">Session to close</param>
        public void CloseSession(P2PTransferSession transferSession)
        {
            if (transferSession != null)
            {
                transferSession.SendMessage(SLPRequestMessage.CreateClosingMessage(transferSession.TransferProperties));
                System.Threading.Thread.Sleep(300);
                transferSession.AbortTransfer();
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[CloseSessions Error] No transfer session attached on transfer layer.");
            }
        }

        /// <summary>
        /// Closes all sessions by sending the remote client a closing message for each session available.
        /// </summary>
        public void CloseAllSessions()
        {
            foreach (MSNSLPTransferProperties properties in transferProperties.Values)
            {
                P2PMessageSession session = (P2PMessageSession)MessageProcessor;
                P2PTransferSession transferSession = session.GetTransferSession(properties.SessionId);
                if (transferSession != null)
                {
                    CloseSession(transferSession);
                }
                else
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "[CloseAllSessions Error] No transfer session attached on transfer layer.");
                }
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Fires the TransferInvitationReceived event.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransferInvitationReceived(MSNSLPInvitationEventArgs e)
        {
            if (TransferInvitationReceived != null)
                TransferInvitationReceived(this, e);
        }

        /// <summary>
        /// Fires the TransferSessionClosed event.
        /// </summary>
        protected virtual void OnTransferSessionClosed(P2PTransferSession session)
        {
            if (TransferSessionClosed != null)
                TransferSessionClosed(this, new P2PTransferSessionEventArgs(session));
        }

        /// <summary>
        /// Fires the TransferSessionCreated event and registers event handlers.
        /// </summary>
        protected virtual void OnTransferSessionCreated(P2PTransferSession session)
        {
            session.TransferFinished += new EventHandler<EventArgs>(MSNSLPHandler_TransferFinished);
            session.TransferAborted += new EventHandler<EventArgs>(MSNSLPHandler_TransferAborted);

            if (TransferSessionCreated != null)
                TransferSessionCreated(this, new P2PTransferSessionEventArgs(session));
        }

        /// <summary>
        /// Cleans up the transfer session.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MSNSLPHandler_TransferAborted(object sender, EventArgs e)
        {
            P2PTransferSession session = (P2PTransferSession)sender;
            RemoveTransferSession(session);
        }

        /// <summary>
        /// Closes the datastream and sends the closing message, if the local client is the receiver. 
        /// Afterwards the associated <see cref="P2PTransferSession"/> object is removed from the class's
        /// <see cref="P2PMessageSession"/> object.
        /// </summary>
        private void MSNSLPHandler_TransferFinished(object sender, EventArgs e)
        {
            P2PTransferSession transferSession = (P2PTransferSession)sender;
            Contact remote = MessageSession.RemoteContact;
            MSNSLPTransferProperties properties = transferSession.TransferProperties;

            if (properties.RemoteInvited == false)
            {
                if (Version == P2PVersion.P2PV1)
                {
                    // we are the receiver. send close message back
                    P2PMessage p2pMessage = new P2PMessage(transferSession.Version);
                    p2pMessage.InnerMessage = SLPRequestMessage.CreateClosingMessage(properties);
                    p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
                    MessageProcessor.SendMessage(p2pMessage);
                }

                // close it
                RemoveTransferSession(transferSession);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Returns the MSNSLPTransferProperties object associated with the specified call id.
        /// </summary>
        /// <param name="callId"></param>
        /// <returns></returns>
        protected MSNSLPTransferProperties GetTransferProperties(Guid callId)
        {
            return transferProperties.ContainsKey(callId) ? transferProperties[callId] : null;
        }

        /// <summary>
        /// Closes the session's datastream and removes the transfer sessions from the class' <see cref="P2PMessageSession"/> object (MessageProcessor property).
        /// </summary>
        /// <param name="session"></param>
        protected virtual void RemoveTransferSession(P2PTransferSession session)
        {
            // remove the session
            OnTransferSessionClosed(session);

            if (session.AutoCloseStream)
                session.DataStream.Close();

            MessageSession.RemoveTransferSession(session);
        }

        /// <summary>
        /// Extracts the checksum (SHA1C/SHA1D field) from the supplied context.
        /// </summary>
        /// <remarks>The context must be a plain string, no base-64 decoding will be done</remarks>
        /// <param name="context"></param>
        /// <returns></returns>
        private static string ExtractChecksum(string context)
        {
            Regex shaRe = new Regex("SHA1C=\"([^\"]+)\"");
            Match match = shaRe.Match(context);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                shaRe = new Regex("SHA1D=\"([^\"]+)\"");
                match = shaRe.Match(context);
                if (match.Success)
                    return match.Groups[1].Value;
            }
            throw new MSNPSharpException("SHA field could not be extracted from the specified context: " + context);
        }

        internal static Guid ParseDCNonce(MimeDictionary bodyValues, out DCNonceType dcNonceType)
        {
            dcNonceType = DCNonceType.None;
            Guid nonce = Guid.Empty;

            if (bodyValues.ContainsKey("Hashed-Nonce"))
            {
                nonce = new Guid(bodyValues["Hashed-Nonce"].Value);
                dcNonceType = DCNonceType.Sha1;
            }
            else if (bodyValues.ContainsKey("Nonce"))
            {
                nonce = new Guid(bodyValues["Nonce"].Value);
                dcNonceType = DCNonceType.Plain;
            }

            return nonce;
        }

        /// <summary>
        /// Parses the incoming invitation message. This will set the class's properties for later retrieval in following messages.
        /// </summary>
        /// <param name="message"></param>
        protected virtual MSNSLPTransferProperties ParseInvitationMessage(SLPMessage message)
        {
            MSNSLPTransferProperties properties = new MSNSLPTransferProperties(MessageSession.LocalContact, message.ToEndPoint, MessageSession.RemoteContact, message.FromEndPoint);

            properties.RemoteInvited = true;

            if (message.BodyValues.ContainsKey("EUF-GUID"))
            {
                properties.DataTypeGuid = message.BodyValues["EUF-GUID"].ToString();
                if (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) == P2PConst.UserDisplayGuid)
                {
                    // create a temporary msn object to extract the data type
                    if (message.BodyValues.ContainsKey("Context"))
                    {
                        MSNObject msnObject = new MSNObject();
                        msnObject.SetContext(message.BodyValues["Context"].ToString(), true);

                        if (msnObject.ObjectType == MSNObjectType.UserDisplay)
                            properties.DataType = DataTransferType.DisplayImage;
                        else if (msnObject.ObjectType == MSNObjectType.Emoticon)
                            properties.DataType = DataTransferType.Emoticon;
                        else
                            properties.DataType = DataTransferType.Unknown;

                        properties.Context = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(message.BodyValues["Context"].ToString()));
                        properties.Checksum = ExtractChecksum(properties.Context);
                    }
                    else
                    {
                        properties.DataType = DataTransferType.Unknown;
                    }
                }
                else if (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) == P2PConst.FileTransferGuid)
                {
                    properties.DataType = DataTransferType.File;
                }
                else if (message.BodyValues["EUF-GUID"].ToString().ToUpper(System.Globalization.CultureInfo.InvariantCulture) == P2PConst.ActivityGuid)
                {
                    properties.DataType = DataTransferType.Activity;
                }
                else
                {
                    properties.DataType = DataTransferType.Unknown;
                }

                // store the branch for use in the OK Message
                properties.LastBranch = message.Branch;
                properties.LastCSeq = message.CSeq;
                properties.CallId = message.CallId;
                properties.SessionId = uint.Parse(message.BodyValues["SessionID"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
            }

            return properties;
        }

        /// <summary>
        /// Sends the invitation request for a direct connection
        /// </summary>
        protected virtual void SendDCInvitation(MSNSLPTransferProperties transferProperties)
        {
            // Create a new branch, but keep the same callid as the first invitation
            transferProperties.LastBranch = Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);

            transferProperties.DCNonceType = DCNonceType.Plain; // We prefer this :)
            transferProperties.Nonce = Guid.NewGuid();
            transferProperties.HashedNonce = HashedNonceGenerator.HashNonce(transferProperties.Nonce);

            string connectionType = "Unknown-Connect";
            string netId = "2042264281"; // unknown variable

            #region Check and determine connectivity

            if (LocalEndPoint == null || this.ExternalEndPoint == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "LocalEndPoint or ExternalEndPoint are not set. Connection type will be set to unknown.", GetType().Name);
            }
            else
            {
                if (LocalEndPoint.Address.Equals(ExternalEndPoint.Address))
                {
                    if (LocalEndPoint.Port == ExternalEndPoint.Port)
                        connectionType = "Direct-Connect";
                    else
                        connectionType = "Port-Restrict-NAT";
                }
                else
                {
                    if (LocalEndPoint.Port == ExternalEndPoint.Port)
                    {
                        netId = "0";
                        connectionType = "IP-Restrict-NAT";
                    }
                    else
                        connectionType = "Symmetric-NAT";
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Connection type set to " + connectionType + " for session " + transferProperties.SessionId.ToString(CultureInfo.InvariantCulture), GetType().Name);
            }

            #endregion

            // Create the message
            SLPRequestMessage slpMessage = new SLPRequestMessage(transferProperties.RemoteContactEPIDString, MSNSLPRequestMethod.INVITE);
            slpMessage.ToMail = transferProperties.RemoteContactEPIDString;
            slpMessage.FromMail = transferProperties.LocalContactEPIDString;
            slpMessage.Branch = transferProperties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = transferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-transreqbody";

            slpMessage.BodyValues["Bridges"] = "TCPv1 SBBridge";
            slpMessage.BodyValues["Capabilities-Flags"] = "1";
            slpMessage.BodyValues["NetID"] = netId;
            slpMessage.BodyValues["Conn-Type"] = connectionType;
            slpMessage.BodyValues["TCP-Conn-Type"] = connectionType;
            slpMessage.BodyValues["UPnPNat"] = "false"; // UPNP Enabled
            slpMessage.BodyValues["ICF"] = "false"; // Firewall enabled
            if (transferProperties.DCNonceType == DCNonceType.Sha1)
            {
                slpMessage.BodyValues["Hashed-Nonce"] = transferProperties.HashedNonce.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            }

            P2PMessage p2pMessage = new P2PMessage(Version);
            p2pMessage.InnerMessage = slpMessage;

            if (Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.TFCombination = TFCombination.First;

                if (p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength > 1202)
                {
                    p2pMessage.V2Header.PackageNumber = (ushort)((p2pMessage.V2Header.MessageSize - p2pMessage.V2Header.DataPacketHeaderLength) / 1202 + 1);
                }
                else
                {
                    p2pMessage.V2Header.PackageNumber = 0;
                }

            }

            MessageProcessor.SendMessage(p2pMessage);
        }



        /// <summary>
        /// Returns a port number which can be used to listen for a new direct connection.
        /// </summary>
        /// <returns>0 when no ports can be found</returns>
        protected virtual int GetNextDirectConnectionPort(IPAddress ipAddress)
        {
            int portAvail = 0;

            if (Settings.PublicPortPriority == PublicPortPriority.First)
            {
                portAvail = TryPublicPorts(ipAddress);

                if (portAvail != 0)
                {
                    return portAvail;
                }
            }

            if (portAvail == 0)
            {
                // Don't try all ports
                for (int p = 1119, maxTry = 100;
                    p <= IPEndPoint.MaxPort && maxTry < 1;
                    p++, maxTry--)
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        //s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        s.Bind(new IPEndPoint(ipAddress, p));
                        //s.Bind(new IPEndPoint(IPAddress.Any, p));
                        s.Close();
                        return p;
                    }
                    catch (SocketException ex)
                    {
                        // Permission denied
                        if (ex.ErrorCode == 10048 /*Address already in use*/ ||
                            ex.ErrorCode == 10013 /*Permission denied*/)
                        {
                            p += 100;
                        }
                        continue; // throw;
                    }
                    catch (System.Security.SecurityException)
                    {
                        break;
                    }
                }
            }

            if (portAvail == 0 && Settings.PublicPortPriority == PublicPortPriority.Last)
            {
                portAvail = TryPublicPorts(ipAddress);
            }

            return portAvail;
        }

        private int TryPublicPorts(IPAddress localIP)
        {
            foreach (int p in Settings.PublicPorts)
            {
                Socket s = null;
                try
                {
                    s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    //s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    s.Bind(new IPEndPoint(localIP, p));
                    return p;
                }
                catch (SocketException)
                {
                }
                finally
                {
                    if (s != null)
                        s.Close();
                }
            }
            return 0;
        }

        #endregion

        #region Message handling methods

        #region HandleMessage

        /// <summary>
        /// Handles incoming P2P Messages by extracting the inner contents and converting it to a SLP Message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;
            Debug.Assert(p2pMessage != null, "Message is not a P2P message in MSNSLP handler", GetType().Name);

            SLPMessage slpMessage = SLPMessage.Parse(p2pMessage.InnerBody);

            if (p2pMessage.Version == P2PVersion.P2PV1)
            {
                if (!(p2pMessage.Footer == 0 && p2pMessage.V1Header.SessionId == 0 &&
                    p2pMessage.V1Header.Flags != P2PFlag.Acknowledgement && p2pMessage.V1Header.MessageSize > 0))
                {
                    // We don't process any p2p message because this is a SIP message handler.
                    return;
                }
            }
            else if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                if (!(p2pMessage.V2Header.SessionId == 0 &&
                    p2pMessage.V2Header.MessageSize > 0 && p2pMessage.V2Header.TFCombination == TFCombination.First))
                {
                    // We don't process any p2p message because this is a SIP message handler.
                    return;
                }
            }

            if (slpMessage != null)
            {
                // Call the methods belonging to the content-type to handle the message
                switch (slpMessage.ContentType)
                {
                    case "application/x-msnmsgr-sessionreqbody":
                        OnSessionRequest(p2pMessage);
                        break;
                    case "application/x-msnmsgr-transreqbody":
                        OnDCRequest(p2pMessage);
                        break;
                    case "application/x-msnmsgr-transrespbody":
                        OnDCResponse(p2pMessage);
                        break;
                    case "application/x-msnmsgr-sessionclosebody":
                        OnSessionCloseRequest(p2pMessage);
                        break;
                    default:
                        {
                            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Content-type not supported: " + slpMessage.ContentType, GetType().Name);
                            break;
                        }
                }
            }
        }

        #endregion

        #region OnDCRequest

        /// <summary>
        /// Called when the remote client sends us it's direct-connect capabilities.
        /// A reply will be send with the local client's connectivity.
        /// </summary>
        /// <param name="p2pMessage"></param>
        protected virtual void OnDCRequest(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);

            if (message.BodyValues.ContainsKey("Bridges") &&
                message.BodyValues["Bridges"].ToString().Contains("TCPv1"))
            {
                SLPStatusMessage slpMessage = new SLPStatusMessage(message.ToMail, 200, "OK");

                // Initial NonceType. Remote side prefers this. But I prefer Nonce :)
                DCNonceType dcNonceType;
                Guid nonce = ParseDCNonce(message.BodyValues, out dcNonceType);

                if (dcNonceType == DCNonceType.None)
                {
                    nonce = Guid.NewGuid();
                }
                bool hashed = false;
                string nonceFieldName = "Nonce";

                // Find host by name
                IPAddress ipAddress = LocalEndPoint.Address;
                int port;

                if (p2pMessage.Version == P2PVersion.P2PV2 ||
                    false == ipAddress.Equals(ExternalEndPoint.Address) ||
                    (0 == (port = GetNextDirectConnectionPort(ipAddress))))
                {
                    slpMessage.BodyValues["Listening"] = "false";
                    slpMessage.BodyValues[nonceFieldName] = Guid.Empty.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    // Let's listen
                    MessageSession.ListenForDirectConnection(ipAddress, port, nonce, hashed);

                    
                    slpMessage.BodyValues["Listening"] = "true";
                    slpMessage.BodyValues["Capabilities-Flags"] = "1";
                    slpMessage.BodyValues["IPv6-global"] = string.Empty;
                    slpMessage.BodyValues["Nat-Trav-Msg-Type"] = "WLX-Nat-Trav-Msg-Direct-Connect-Resp";
                    slpMessage.BodyValues["UPnPNat"] = "false";

                    slpMessage.BodyValues["NeedConnectingEndpointInfo"] = "false";
                    slpMessage.BodyValues["Conn-Type"] = "Direct-Connect";
                    slpMessage.BodyValues["TCP-Conn-Type"] = "Direct-Connect";
                    slpMessage.BodyValues[nonceFieldName] = nonce.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
                    slpMessage.BodyValues["IPv4Internal-Addrs"] = ipAddress.ToString();
                    slpMessage.BodyValues["IPv4Internal-Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);

                    // check if client is behind firewall (NAT-ted)
                    // if so, send the public ip also the client, so it can try to connect to that ip
                    if (ExternalEndPoint != null && !ExternalEndPoint.Address.Equals(localEndPoint.Address))
                    {
                        slpMessage.BodyValues["IPv4External-Addrs"] = ExternalEndPoint.Address.ToString();
                        slpMessage.BodyValues["IPv4External-Port"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                slpMessage.BodyValues["Bridge"] = "TCPv1";

                slpMessage.ToMail = message.FromMail;
                slpMessage.FromMail = message.ToMail;
                slpMessage.Branch = message.Branch;
                slpMessage.CSeq = 1;
                slpMessage.CallId = message.CallId;
                slpMessage.MaxForwards = 0;
                slpMessage.ContentType = "application/x-msnmsgr-transrespbody";

                P2PMessage p2pReplyMessage = new P2PMessage(Version);
                p2pReplyMessage.InnerMessage = slpMessage;

                if (Version == P2PVersion.P2PV2)
                {
                    p2pReplyMessage.V2Header.TFCombination = TFCombination.First;
                    p2pReplyMessage.V2Header.PackageNumber = P2PTransferSession.GetNextSLPStatusDataPacketNumber(p2pMessage.V2Header.PackageNumber);
                }

                // and notify the remote client that he can connect
                MessageProcessor.SendMessage(p2pReplyMessage);

                MessageSession.DCHandshakeAckV2 = p2pReplyMessage;
            }
        }

        #endregion

        #region OnDCResponse

        /// <summary>
        /// Called when the remote client send us it's direct-connect capabilities
        /// </summary>
        /// <param name="p2pMessage"></param>
        protected virtual void OnDCResponse(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);
            MimeDictionary bodyValues = message.BodyValues;
            //MSNSLPTransferProperties properties = GetTransferProperties(message.CallId);

            DCNonceType dcNonceType;
            Guid nonce = ParseDCNonce(message.BodyValues, out dcNonceType);

            if (dcNonceType == DCNonceType.Sha1)
            {
                // Always needed
                //properties.RemoteNonce = nonce;
                //properties.DCNonceType = dcNonceType;
            }


            // Check the protocol
            if (bodyValues.ContainsKey("Bridge") &&
                bodyValues["Bridge"].ToString().IndexOf("TCPv1") >= 0 &&
                bodyValues.ContainsKey("Listening") &&
                bodyValues["Listening"].ToString().ToLower(System.Globalization.CultureInfo.InvariantCulture).IndexOf("true") >= 0)
            {
                if (dcNonceType == DCNonceType.Plain)
                {
                    // Only needed for listening side
                    //properties.Nonce = nonce;
                    //properties.DCNonceType = dcNonceType;
                }

                IPEndPoint selectedPoint = SelectIPEndPoint(bodyValues);
                if (selectedPoint != null)
                {
                    // We must connect to the remote client
                    ConnectivitySettings settings = new ConnectivitySettings();
                    settings.Host = selectedPoint.Address.ToString();
                    settings.Port = selectedPoint.Port;

                    MessageSession.CreateDirectConnection(settings.Host, settings.Port, nonce, (dcNonceType == DCNonceType.Sha1));
                }
            }
        }

        private IPEndPoint SelectIPEndPoint(MimeDictionary bodyValues)
        {
            List<IPAddress> ipAddrs = new List<IPAddress>();
            string[] addrs = new string[0];
            int port = 0;

            if (bodyValues.ContainsKey("IPv4External-Addrs") || bodyValues.ContainsKey("srddA-lanretxE4vPI"))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "Using external IP addresses", GetType().Name);

                if (bodyValues.ContainsKey("IPv4External-Addrs"))
                {
                    addrs = bodyValues["IPv4External-Addrs"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    port = int.Parse(bodyValues["IPv4External-Port"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    char[] revHost = bodyValues["srddA-lanretxE4vPI"].ToString().ToCharArray();
                    Array.Reverse(revHost);
                    addrs = new string(revHost).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    char[] revPort = bodyValues["troP-lanretxE4vPI"].ToString().ToCharArray();
                    Array.Reverse(revPort);
                    port = int.Parse(new string(revPort), System.Globalization.CultureInfo.InvariantCulture);
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                       String.Format("{0} external IP addresses found, with port {1}", addrs.Length, port), GetType().Name);

                for (int i = 0; i < addrs.Length; i++)
                {
                    IPAddress ip;
                    if (IPAddress.TryParse(addrs[i], out ip))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + addrs[i], GetType().Name);

                        ipAddrs.Add(ip);

                        if (ip.Equals(LocalEndPoint.Address))
                        {
                            // External IP matches our own, clearing external IPs
                            //addrs = new string[0];
                            //ipAddrs.Clear();
                            break;
                        }
                    }
                }
            }

            if ((ipAddrs.Count == 0) &&
                (bodyValues.ContainsKey("IPv4Internal-Addrs") || bodyValues.ContainsKey("srddA-lanretnI4vPI")))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "Using internal IP addresses", GetType().Name);

                if (bodyValues.ContainsKey("IPv4Internal-Addrs"))
                {
                    addrs = bodyValues["IPv4Internal-Addrs"].Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    port = int.Parse(bodyValues["IPv4Internal-Port"].ToString(), System.Globalization.CultureInfo.InvariantCulture);
                }
                else
                {
                    char[] revHost = bodyValues["srddA-lanretnI4vPI"].ToString().ToCharArray();
                    Array.Reverse(revHost);
                    addrs = new string(revHost).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    char[] revPort = bodyValues["troP-lanretnI4vPI"].ToString().ToCharArray();
                    Array.Reverse(revPort);
                    port = int.Parse(new string(revPort), System.Globalization.CultureInfo.InvariantCulture);
                }
            }

            if (addrs.Length == 0)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                   "Unable to find any remote IP addresses", GetType().Name);

                // Failed
                return null;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                      String.Format("{0} internal IP addresses found, with port {1}",
                      addrs.Length, port), GetType().Name);

            for (int i = 0; i < addrs.Length; i++)
            {
                IPAddress ip;
                if (IPAddress.TryParse(addrs[i], out ip))
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "\t" + addrs[i], GetType().Name);

                    ipAddrs.Add(ip);
                }
            }

            if (addrs.Length == 0)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                   "Unable to find any remote IP addresses", GetType().Name);

                // Failed
                return null;
            }

            // Try to find the correct IP
            IPAddress ipAddr = null;
            byte[] localBytes = LocalEndPoint.Address.GetAddressBytes();

            foreach (IPAddress ip in ipAddrs)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    // This is an IPv4 address
                    // Check if the first 3 octets match our local IP address
                    // If so, make use of that address (it's on our LAN)

                    byte[] bytes = ip.GetAddressBytes();

                    if ((bytes[0] == localBytes[0]) && (bytes[1] == localBytes[1]) && (bytes[2] == localBytes[2]))
                    {
                        ipAddr = ip;
                        break;
                    }
                }
            }

            if (ipAddr == null)
                ipAddr = ipAddrs[0];

            return new IPEndPoint(ipAddr, port);
        }

    

        #endregion

        #region OnSessionRequest
        /// <summary>
        /// Called when a remote client request a session
        /// </summary>
        protected virtual void OnSessionRequest(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);
            SLPStatusMessage slpStatus = message as SLPStatusMessage;

            #region SLP Status Response (Response for receive file request, accept activity request and send display image request.)

            if (slpStatus != null)
            {
                if (slpStatus.CSeq == 1 && slpStatus.Code == 603)
                {
                    OnSessionCloseRequest(p2pMessage);
                }
                else if (slpStatus.CSeq == 1 && slpStatus.Code == 200)
                {
                    Guid callGuid = message.CallId;
                    MSNSLPTransferProperties properties = GetTransferProperties(callGuid);
                    
                    if (properties == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Cannot find request transfer property, Guid: " + callGuid.ToString("B"));
                        return;
                    }

                    P2PTransferSession session = ((P2PMessageSession)MessageProcessor).GetTransferSession(properties.SessionId);

                    if (session == null)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Cannot find request transfer session, SessionId: " + properties.SessionId.ToString());
                        return;
                    }


                    #region File and Activity are invitor send
                    
                    int waitCounter = 0;
                    Timer timer = new Timer(1000);
                    timer.Elapsed += delegate(object sender, ElapsedEventArgs e)
                    {
                        if (waitCounter < directConnectionExpireInterval / 1000)
                        {
                            waitCounter++;
                            return;
                        }

                        timer.Stop();
                        timer = null;

                        switch (properties.DataType)
                        {
                            case DataTransferType.File:

                                // We can receive the OK message.
                                // We can now send the invitation to setup a transfer connection
                                if (MessageSession.DirectConnected == false && MessageSession.DirectConnectionAttempt == false)
                                    SendDCInvitation(properties);

                                session.StartDataTransfer(false);

                                break;

                            case DataTransferType.Activity:
                                if (session.DataStream != null && session.IsSender)
                                {
                                    session.StartDataTransfer(false);
                                }
                                break;
                        }
                    };

                    timer.Start();
                    #endregion


                }
                return;
            }

            #endregion

            #region SLP INVITE (Request for file receiving and send display image)

            SLPRequestMessage slpMessage = message as SLPRequestMessage;

            if (slpMessage.Method == "INVITE")
            {
                // create a properties object and add it to the transfer collection
                MSNSLPTransferProperties properties = ParseInvitationMessage(message);

                // create a p2p transfer
                P2PTransferSession transferSession = new P2PTransferSession(Version, properties, MessageSession);
                transferSession.TransferFinished += delegate
                {
                    transferSession.SendDisconnectMessage(SLPRequestMessage.CreateClosingMessage(properties));
                };

                if (Version == P2PVersion.P2PV2)
                {
                    transferSession.DataPacketNumber = p2pMessage.V2Header.PackageNumber;
                }

                // hold a reference to this argument object because we need the accept property later
                MSNSLPInvitationEventArgs invitationArgs =
                    new MSNSLPInvitationEventArgs(properties, message, transferSession, this);

                if (properties.DataType == DataTransferType.Unknown)  // If type is unknown, we reply an internal error.
                {

                    transferSession.SendMessage(SLPStatusMessage.CreateInternalErrorMessage(properties));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Unknown p2p datatype received: " +
                        properties.DataTypeGuid + ". 500 INTERNAL ERROR send.", GetType().ToString());
                    return;
                }

                if (properties.DataType == DataTransferType.File)
                {
                    // set the filetransfer values in the EventArgs object.
                    // see the SendInvitation(..) method for more info about the layout of the context string
                    MemoryStream memStream = new MemoryStream(Convert.FromBase64String(message.BodyValues["Context"].ToString()));
                    BinaryReader reader = new BinaryReader(memStream);
                    reader.ReadInt32(); // previewDataLength, 4
                    reader.ReadInt32(); // first flag, 4
                    invitationArgs.FileSize = reader.ReadInt64(); // 8
                    reader.ReadInt32(); // 2nd flag, 4

                    MemoryStream filenameStream = new MemoryStream();
                    byte val = 0;
                    while (reader.BaseStream.Position < reader.BaseStream.Length - 2 &&
                            (val = reader.ReadByte()) > 0)
                    {
                        filenameStream.WriteByte(val);
                        filenameStream.WriteByte(reader.ReadByte());
                    }
                    invitationArgs.Filename = System.Text.UnicodeEncoding.Unicode.GetString(filenameStream.ToArray());
                }
                else if (properties.DataType == DataTransferType.DisplayImage ||
                    properties.DataType == DataTransferType.Emoticon)
                {
                    // create a MSNObject based upon the send context
                    invitationArgs.MSNObject = new MSNObject();
                    invitationArgs.MSNObject.SetContext(properties.Context, false);
                }
                else if (properties.DataType == DataTransferType.Activity)
                {
                    if (message.BodyValues.ContainsKey("Context"))
                    {
                        string activityContextString = message.BodyValues["Context"].Value;
                        ActivityInfo info = new ActivityInfo(activityContextString);
                        invitationArgs.Activity = info;
                    }
                }

                OnTransferInvitationReceived(invitationArgs);

                if (!invitationArgs.DelayProcess)
                {
                    // check whether the client programmer wants to accept or reject this message
                    if (invitationArgs.Accept == false)
                    {
                        RejectTransfer(invitationArgs);
                        return;
                    }

                    AcceptTransfer(invitationArgs);
                }
            }

            #endregion

        }

        #endregion

        #region OnSessionCloseRequest

        /// <summary>
        /// Called when a remote client closes a session.
        /// </summary>
        protected virtual void OnSessionCloseRequest(P2PMessage p2pMessage)
        {
            SLPMessage message = SLPMessage.Parse(p2pMessage.InnerBody);

            Guid callGuid = message.CallId;
            MSNSLPTransferProperties properties = GetTransferProperties(callGuid);

            if (properties != null) // Closed before or never accepted?
            {
                properties.SessionCloseState--;
                P2PTransferSession session = MessageSession.GetTransferSession(properties.SessionId);

                if (session == null)
                    return;

                // remove the resources
                RemoveTransferSession(session);
                // and close the connection
                if (session.MessageSession.DirectConnected)
                    session.MessageSession.CloseDirectConnection();
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Warning: a session with the call-id " + callGuid + " not exists.", GetType().Name);
            }
        }

        #endregion

        #endregion
    }
};
