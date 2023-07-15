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
using System.Net;
using System.IO;
using System.Threading;
using System.Collections;
using System.Diagnostics;
using System.Net.Sockets;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    #region P2PTransferProgressedEventArgs

    [Serializable]
    public class P2PTransferProgressedEventArgs : EventArgs
    {
        private ulong _transferred = 0;        
        private ulong _totalSize = 0;
        private int _percent = 0;

        public P2PTransferProgressedEventArgs(ulong transferred, ulong totalSize)
        {
            if (totalSize == 0)
            {
                _percent = 100;
            }
            else
            {
                this._transferred = transferred;
                this._totalSize = totalSize;
                this._percent = (int)(((double)transferred / (double)totalSize) * 100);
            }
        }

        public ulong Transferred
        {
            get
            {
                return _transferred;
            }
        }

        public ulong TotalSize
        {
            get
            {
                return _totalSize;
            }
        }

        public int Percent
        {
            get
            {
                return _percent;
            }
        }
    }

    #endregion

    /// <summary>
    /// A single transfer of data within a p2p session. This is the Data Packet Layer of MSNP2P protocol.
    /// </summary>
    /// <remarks>
    /// P2PTransferSession handles all messages with a specified session id in the p2p header. Optional a
    /// direct connection can be created. It will try to connect to the remote client or listening for
    /// incoming connections. If that succeeds and the local client is the sender of the data a seperate
    /// thread will be started to send data messages over the direct connection. However, if the direct
    /// connection fails it will send the data messages over the switchboard session. These latter messages
    /// go via the messenger servers and is therefore quite slow compared to direct connections but it is 
    /// guaranteed to work even when both machines are behind a proxy, firewall or router.
    /// </remarks>
    public class P2PTransferSession : IMessageHandler, IMessageProcessor, IDisposable
    {
        #region Events

        /// <summary>
        /// Occurs when the sending of data messages has started.
        /// </summary>
        public event EventHandler<EventArgs> TransferStarted;

        /// <summary>
        /// Occurs when the sending/receiving of data messages has arrived.
        /// </summary>
        public event EventHandler<P2PTransferProgressedEventArgs> TransferProgressed;

        /// <summary>
        /// Occurs when the sending of data messages has finished.
        /// </summary>
        public event EventHandler<EventArgs> TransferFinished;

        /// <summary>
        /// Occurs when the transfer of data messages has been aborted.
        /// </summary>
        public event EventHandler<EventArgs> TransferAborted;

        #endregion

        #region Members

        private volatile int abortThread = 0;
        private Thread transferThread = null;
        private bool transferFinishedFired = false;
       
        private uint messageFlag = 0;
        private uint messageFooter = 0;
        private uint dataPreparationAck = 0;
        private ushort dataPacketNumber = 0;
        private uint dataMessageIdentifier = 0;

        private bool isSender = false;
        private bool autoCloseStream = false;
        private Stream dataStream = new MemoryStream();
        private object clientData = null;

        private P2PVersion version = P2PVersion.P2PV1;
        private P2PMessageSession messageSession = null;
        private MSNSLPTransferProperties transferProperties = null;
        private bool waitingDirectConnection = false;

        #endregion

        #region Constructors & Destructors

        protected P2PTransferSession()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing p2p transfer session object", GetType().Name);
        }

        public P2PTransferSession(P2PVersion ver, MSNSLPTransferProperties properties, P2PMessageSession transferLayer)
        {
            this.version = ver;
            this.transferProperties = properties;
            this.MessageSession = transferLayer;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing p2p transfer session object, version = " + ver.ToString(), GetType().Name);
        }

        ~P2PTransferSession()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                if (dataStream != null)
                    dataStream.Dispose();

                MessageSession.UnregisterHandler(this);
                MessageSession.RemoveTransferSession(this);
            }

            // Free native resources
        }

        #endregion

        #region Properties

        /// <summary>
        /// The message session which keeps track of the local / remote message identifiers and redirects
        /// messages to this handler based on the session id.
        /// </summary>
        public P2PMessageSession MessageSession
        {
            get
            {
                return messageSession;
            }
            set
            {
                if (messageSession != null)
                {
                    messageSession.RemoveTransferSession(this);
                    messageSession.DirectConnectionEstablished -= (messageSession_DirectConnectionEstablished);
                    messageSession.DirectConnectionFailed -= (messageSession_DirectConnectionFailed);
                }

                messageSession = value;

                if (messageSession != null)
                {
                    messageSession.AddTransferSession(this);  //The massage processor of transfer session will be set by AddTransferSession.
                    messageSession.DirectConnectionEstablished += (messageSession_DirectConnectionEstablished);
                    messageSession.DirectConnectionFailed += (messageSession_DirectConnectionFailed);
                }
            }
        }

        /// <summary>
        /// The transfer properties for this transfer session.
        /// </summary>
        public MSNSLPTransferProperties TransferProperties
        {
            get
            {
                return transferProperties;
            }
        }

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
        /// Defines whether the local client is sender or receiver
        /// </summary>
        public bool IsSender
        {
            get
            {
                return isSender;
            }
            set
            {
                isSender = value;
            }
        }

        /// <summary>
        /// Defines whether the stream is automatically closed after the transfer has finished or been aborted.
        /// </summary>
        public bool AutoCloseStream
        {
            get
            {
                return autoCloseStream;
            }
            set
            {
                autoCloseStream = value;
            }
        }

        /// <summary>
        /// This property can be used by the client-programmer to include application specific data.
        /// </summary>
        public object ClientData
        {
            get
            {
                return clientData;
            }
            set
            {
                clientData = value;
            }
        }

        /// <summary>
        /// The stream to read from when data is send, or to write to when data is received.
        /// Default is a MemorySteam.
        /// </summary>
        /// <remarks>
        /// In the eventhandler, when an invitation is received, the client programmer must set this property
        /// in order to enable the transfer to succeed. In the case of the filetransfer, when the local client
        /// is the receiver, the incoming data is written to the specified datastream. In the case of the
        /// invitation for a msn object (display picture, emoticons, background), when the local client is
        /// the sender, the outgoing data is read from the specified datastream.
        /// </remarks>
        public Stream DataStream
        {
            get
            {
                return dataStream;
            }
            set
            {
                dataStream = value;
            }
        }


        /// <summary>
        /// This value is set in the flag field in a p2p header.
        /// </summary>
        /// <remarks>
        /// For filetransfers this value is for example 0x1000030
        /// </remarks>
        public uint MessageFlag
        {
            get
            {
                return messageFlag;
            }
            set
            {
                messageFlag = value;
            }
        }

        /// <summary>
        /// This value is set in the footer field in a p2p header.
        /// </summary>
        public uint MessageFooter
        {
            get
            {
                return messageFooter;
            }
            set
            {
                messageFooter = value;
            }
        }

        /// <summary>
        /// Tracked to know when an acknowledgement for the (switchboards) data preparation message is received.
        /// </summary>
        internal uint DataPreparationAck
        {
            get
            {
                return dataPreparationAck;
            }
            set
            {
                dataPreparationAck = value;
            }
        }

        /// <summary>
        /// The PackageNumber field used by p2pv2 messages.
        /// </summary>
        internal ushort DataPacketNumber
        {
            get
            {
                return dataPacketNumber;
            }

            set
            {
                dataPacketNumber = value;
            }
        }

        /// <summary>
        /// Indicates whether the session is waiting for the result of a direct connection attempt
        /// </summary>
        protected bool WaitingDirectConnection
        {
            get
            {
                return waitingDirectConnection;
            }
            set
            {
                waitingDirectConnection = value;
            }
        }

        /// <summary>
        /// The thread in which the data messages are send
        /// </summary>
        protected Thread TransferThread
        {
            get
            {
                return transferThread;
            }
            set
            {
                transferThread = value;
            }
        }

        #endregion

        #region IMessageHandler Members

        private IMessageProcessor messageProcessor;

        /// <summary>
        /// The message processor to which p2p messages (this includes p2p data messages) will be send
        /// </summary>
        public IMessageProcessor MessageProcessor
        {
            get
            {
                return messageProcessor;
            }
            set
            {
                if (value != null && object.ReferenceEquals(messageProcessor, value))
                    return;

                if (value == null && messageProcessor == null)
                    return;

                if (value == null && messageProcessor != null)
                {
                    messageProcessor.UnregisterHandler(this);

                    messageProcessor = value;
                    return;
                }

                if (messageProcessor != null)
                {
                    messageProcessor.UnregisterHandler(this);
                }

                messageProcessor = value;
                messageProcessor.RegisterHandler(this);
            }
        }

        /// <summary>
        /// Handles P2PMessages. Other messages are ignored. All incoming messages are supposed to belong to this session.
        /// </summary>
        public void HandleMessage(IMessageProcessor sender, NetworkMessage message)
        {
            P2PMessage p2pMessage = message as P2PMessage;

            Trace.Assert(p2pMessage != null, "Incoming message is not a P2PMessage", GetType().Name);

            if (p2pMessage.Header.SessionId != TransferProperties.SessionId)
            {
                //The data is not for this transfer session, return.
                return;
            }

            bool handled = WriteToDataStream(p2pMessage);

            if (handled)
                return;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                "P2P Info message received, session Id: " + TransferProperties.SessionId + "\r\n" +
                p2pMessage.ToDebugString(), GetType().Name);

            // It is not a datamessage. Extract the messages one-by-one and dispatch it to all handlers.
            IMessageHandler[] cpHandlers = handlers.ToArray();
            foreach (IMessageHandler handler in cpHandlers)
                handler.HandleMessage(this, p2pMessage);

        }

        private bool WriteToDataStream(P2PMessage p2pMessage)
        {
            if (p2pMessage.Version == P2PVersion.P2PV1)
            {
                // Keep track of the remote identifier
                MessageSession.RemoteIdentifier = p2pMessage.Header.Identifier;

                #region P2P Version 1
                if (p2pMessage.V1Header.Flags == P2PFlag.TlpError)
                {
                    AbortTransfer();
                    return true;
                }

                // check to see if our session data has been transferred correctly
                if (p2pMessage.Header.SessionId > 0 &&
                    p2pMessage.Header.IsAcknowledgement &&
                    p2pMessage.V1Header.AckSessionId == dataMessageIdentifier)
                {
                    // inform the handlers
                    OnTransferFinished();
                    return true;
                }

                // check if it is a content message
                // if it is not a file transfer message, and the footer is not set to the corresponding value, ignore it.
                if (p2pMessage.InnerBody.Length > 0)
                {
                    if (
                        /* m$n 7.5 (MSNC5) >=, footer: dp=12,emo=11,file=2 */
                        ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data && p2pMessage.Footer == P2PConst.DisplayImageFooter12) ||
                        (p2pMessage.V1Header.Flags == P2PFlag.FileData && p2pMessage.Footer == P2PConst.FileTransFooter2) ||
                        ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data && p2pMessage.Footer == (uint)P2PConst.CustomEmoticonFooter11) ||
                        /* m$n 7.0 (MSNC4) <=, footer is 1 (dp and custom emoticon) */
                        ((p2pMessage.V1Header.Flags & P2PFlag.Data) == P2PFlag.Data || p2pMessage.Footer == 1)
                       )
                    {
                        // indicates whether we must stream this message
                        bool writeToStream = true;

                        // check if it is a data preparation message send via the SB
                        if (p2pMessage.Header.TotalSize == 4 &&
                            p2pMessage.Header.MessageSize == 4
                            && BitConverter.ToInt32(p2pMessage.InnerBody, 0) == 0)
                        {
                            writeToStream = false;
                        }

                        if (writeToStream)
                        {
                            // store the data message identifier because we want to reference it if we abort the transfer
                            dataMessageIdentifier = p2pMessage.Header.Identifier;

                            if (DataStream == null)
                                throw new MSNPSharpException("Data was received in a P2P session, but no datastream has been specified to write to.");

                            if (DataStream.CanWrite)
                            {
                                if (DataStream.Length < (long)p2pMessage.V1Header.Offset + (long)p2pMessage.InnerBody.Length)
                                    DataStream.SetLength((long)p2pMessage.V1Header.Offset + (long)p2pMessage.InnerBody.Length);

                                DataStream.Seek((long)p2pMessage.V1Header.Offset, SeekOrigin.Begin);
                                DataStream.Write(p2pMessage.InnerBody, 0, p2pMessage.InnerBody.Length);

                                try
                                {
                                    OnTransferProgressed(new P2PTransferProgressedEventArgs(
                                        (p2pMessage.V1Header.Offset + p2pMessage.Header.MessageSize), p2pMessage.Header.TotalSize));
                                }
                                catch (Exception xferExc)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Error occured when fired TransferProgressed: " + xferExc.ToString(), GetType().Name);
                                }

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                    "Data received, " + (p2pMessage.V1Header.Offset + (ulong)p2pMessage.Header.MessageSize).ToString() +
                                    " of " + p2pMessage.Header.TotalSize);
                            }
                            // check for end of file transfer
                            if (p2pMessage.V1Header.Offset + p2pMessage.Header.MessageSize == p2pMessage.Header.TotalSize)
                            {
                                // Close data stream before sending ack
                                if (AutoCloseStream)
                                    DataStream.Close();

                                P2PMessage ack = p2pMessage.CreateAcknowledgement();
                                try
                                {
                                    ack.Header.SessionId = p2pMessage.Header.SessionId;
                                    SendMessage(ack);
                                }
                                catch (Exception)
                                {
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning,
                                    "ACK couldn't be sent, closed remotely after last packet? The ACK was: " + ack.ToDebugString(), GetType().Name);
                                }

                                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                    "All data for transfer session " + TransferProperties.SessionId +
                                    " have been received, trigger OnTransferFinished event.");

                                OnTransferFinished();
                            }
                        }
                        // finished handling this message
                        return true;
                    }
                }
                #endregion
            }

            if (p2pMessage.Version == P2PVersion.P2PV2)
            {
                // Keep track of the remote identifier
                MessageSession.RemoteIdentifier = p2pMessage.Header.Identifier + p2pMessage.Header.MessageSize;

                #region P2P Version 2

                if (p2pMessage.InnerBody.Length == 4 &&
                    p2pMessage.V2Header.TFCombination == TFCombination.First &&
                    BitConverter.ToInt32(p2pMessage.InnerBody, 0) == 0)
                {
                    //Data preperation message.
                    return true;
                }

                // check if it is a content message
                // if it is not a file transfer message, and the footer is not set to the corresponding value, ignore it.
                if (p2pMessage.V2Header.MessageSize > 0)
                {
                    if (p2pMessage.V2Header.TFCombination == (TFCombination.MsnObject) ||
                        p2pMessage.V2Header.TFCombination == (TFCombination.MsnObject | TFCombination.First) ||
                        p2pMessage.V2Header.TFCombination == (TFCombination.FileTransfer) ||
                        p2pMessage.V2Header.TFCombination == (TFCombination.FileTransfer | TFCombination.First))
                    {
                        // store the data message identifier because we want to reference it if we abort the transfer
                        DataPacketNumber = p2pMessage.V2Header.PackageNumber;

                        if (DataStream == null)
                            throw new MSNPSharpException("Data was received in a P2P session, but no datastream has been specified to write to.");

                        if (DataStream.CanWrite)
                        {
                            DataStream.Seek(0, SeekOrigin.End);
                            DataStream.Write(p2pMessage.InnerBody, 0, p2pMessage.InnerBody.Length);

                            try
                            {
                                OnTransferProgressed(new P2PTransferProgressedEventArgs(
                                    (ulong)DataStream.Position, (ulong)DataStream.Position + p2pMessage.V2Header.DataRemaining));
                            }
                            catch (Exception xferExc)
                            {
                                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Error occured when fired TransferProgressed: " + xferExc.ToString(), GetType().Name);
                            }

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                "Data received, " + (ulong)DataStream.Position +
                                " of " + ((ulong)DataStream.Position + p2pMessage.V2Header.DataRemaining));


                        }
                        // check for end of file transfer
                        if (p2pMessage.V2Header.DataRemaining == 0)
                        {
                            if (AutoCloseStream)
                                DataStream.Close();

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose,
                                    "All data for transfer session " + TransferProperties.SessionId +
                                    " have been received, trigger OnTransferFinished event.");

                            OnTransferFinished();
                        }
                        // finished handling this message
                        return true;

                    }
                }

                #endregion
            }

            return false;
        }


        #endregion

        #region IMessageProcessor Members

        private List<IMessageHandler> handlers = new List<IMessageHandler>();

        /// <summary>
        /// Registers handlers for incoming p2p messages.
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
        /// Unregisters handlers.
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
        /// Sends a message for this session to the message processor. If a direct connection is established,
        /// the p2p message is directly send to the message processor. If there is no direct connection
        /// available, it will wrap the incoming p2p message in a MSGMessage with the correct parameters.
        /// It also sets the identifiers and acknowledge session, provided they're not already set.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(NetworkMessage message)
        {
            P2PMessage p2pMsg = message as P2PMessage;
            if (p2pMsg != null)
            {
                SendMessage(p2pMsg);
                return;
            }

            SLPMessage slpMsg = message as SLPMessage;
            if (slpMsg != null)
            {
                SendMessage(slpMsg);
                return;
            }

            throw new InvalidDataException("Can not send " + message.GetType().ToString() + " via P2P Transfer session.");
        }

        #endregion

        #region StartDataTransfer / AbortTransfer

        /// <summary>
        /// Starts a seperate thread to send the data in the stream to the remote client. It will first wait
        /// for a direct connection if tryDirectConnection is set to true.
        /// </summary>
        /// <remarks>This method will not open or close the specified datastream.</remarks>
        public void StartDataTransfer(bool tryDirectConnection)
        {
            if (transferThread != null)
            {
                throw new MSNPSharpException("Start of data transfer failed because there is already a thread sending the data.");
            }

            Debug.Assert(TransferProperties.SessionId != 0, "Trying to initiate p2p data transfer but no session is specified");
            Debug.Assert(dataStream != null, "Trying to initiate p2p data transfer but no session is specified");

            isSender = true;

            if (messageSession.DirectConnected == false &&
                messageSession.DirectConnectionAttempt == false &&
                tryDirectConnection == true)
            {
                waitingDirectConnection = true;
                return;
            }

            waitingDirectConnection = false;

            transferThread = new Thread(new ParameterizedThreadStart(TransferDataEntry));
            transferThread.Start(this);
        }

        /// <summary>
        /// Aborts the datatransfer, if available. This will send a P2P abort message and stop the sending 
        /// thread. It will not close a direct connection. If AutoCloseStream is set to true, the datastream 
        /// will be closed.
        /// <remarks>
        /// This function is called by internal.
        /// <para>If you want to abort the current transfer, call <see cref="MSNSLPHandler.CloseSession"/></para>
        /// </remarks>
        /// </summary>
        public void AbortTransfer()
        {
            if (transferThread != null)
            {
                if (transferThread.IsAlive)
                {
                    Thread.BeginCriticalRegion();
                    abortThread = 1; //transferThread.Abort();
                    Thread.EndCriticalRegion();
                }
                transferThread = null;
                OnTransferAborted();
            }

            MessageSession.RemoveTransferSession(this);

            if (AutoCloseStream)
                DataStream.Close();
        }

        #endregion

        #region GetNextSLP ID

        /// <summary>
        /// Get the next data package number for the SIP request text message, such as INVITE and BYE.
        /// </summary>
        /// <returns></returns>
        public static ushort GetNextSLPRequestDataPacketNumber(ushort baseDataPacketNumber)
        {
            if (baseDataPacketNumber < ushort.MaxValue)
                return ++baseDataPacketNumber;

            return baseDataPacketNumber;
        }

        /// <summary>
        /// Get the next data package number to the SIP status text message, such as 200 OK and 603 Decline.
        /// </summary>
        /// <returns></returns>
        public static ushort GetNextSLPStatusDataPacketNumber(ushort baseDataPacketNumber)
        {
            if (baseDataPacketNumber > 0)
                return --baseDataPacketNumber;

            return baseDataPacketNumber;
        }

        /// <summary>
        /// Get the next data package number for the SIP request text message, such as INVITE and BYE.
        /// </summary>
        /// <returns></returns>
        public ushort GetNextSLPRequestDataPacketNumber()
        {
            if (dataPacketNumber < ushort.MaxValue)
                return ++dataPacketNumber;

            return dataPacketNumber;
        }

        /// <summary>
        /// Get the next data package number to the SIP status text message, such as 200 OK and 603 Decline.
        /// </summary>
        /// <returns></returns>
        public ushort GetNextSLPStatusDataPacketNumber()
        {
            if (dataPacketNumber > 0)
                return --dataPacketNumber;

            return dataPacketNumber;
        }

        #endregion

        #region SendMessage / Wrap Message / DeliverToTransferLayer



        public P2PMessage SendMessage(P2PMessage p2pMessage)
        {
            DeliverToTransferLayer(p2pMessage);
            return p2pMessage;
        }

        public P2PMessage SendMessage(SLPMessage slpMessage)
        {
            if (slpMessage is SLPRequestMessage)
            {
                if ((slpMessage as SLPRequestMessage).Method == MSNSLPRequestMethod.BYE)
                    TransferProperties.SessionCloseState--;
            }
            return SendMessage(WrapSLPMessage(slpMessage));
        }

        private P2PMessage WrapSLPMessage(SLPMessage slpMessage)
        {
            P2PMessage p2pMessage = new P2PMessage(Version);
            p2pMessage.InnerMessage = slpMessage;

            if (Version == P2PVersion.P2PV2)
            {
                p2pMessage.V2Header.TFCombination = TFCombination.First;

                if (slpMessage is SLPRequestMessage)
                {
                    p2pMessage.V2Header.PackageNumber = GetNextSLPRequestDataPacketNumber();
                }
                else if (slpMessage is SLPStatusMessage)
                {
                    p2pMessage.V2Header.PackageNumber = GetNextSLPStatusDataPacketNumber();
                }
            }

            if (Version == P2PVersion.P2PV1)
            {
                p2pMessage.V1Header.Flags = P2PFlag.MSNSLPInfo;
            }

            return p2pMessage;
        }

        private bool DeliverToTransferLayer(P2PMessage p2pMessage)
        {
            if (TransferProperties != null)
            {
                if (p2pMessage.IsSLPData)
                {
                    p2pMessage.Header.SessionId = 0;
                }
                else
                {
                    p2pMessage.Header.SessionId = TransferProperties.SessionId;
                }
            }
            else
            {
                p2pMessage.Header.SessionId = 0;
            }

            if (MessageSession != null)
            {
                if (!MessageSession.DirectConnected)
                {
                    p2pMessage.PrepareMessage();
                }

                MessageSession.SendMessage(p2pMessage);
                return true;
            }

            return false;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Fires the TransferStarted event.
        /// </summary>
        protected virtual void OnTransferStarted()
        {
            if (TransferStarted != null)
                TransferStarted(this, EventArgs.Empty);
        }

        protected virtual void OnTransferProgressed(P2PTransferProgressedEventArgs e)
        {
            if (TransferProgressed != null)
                TransferProgressed(this, e);
        }

        /// <summary>
        /// Fires the TransferFinished event.
        /// </summary>
        protected virtual void OnTransferFinished()
        {
            if (TransferFinished != null)
            {
                if (!transferFinishedFired)
                {
                    transferFinishedFired = true;
                    TransferFinished(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Fires the TransferAborted event.
        /// </summary>
        protected virtual void OnTransferAborted()
        {
            if (TransferAborted != null)
                TransferAborted(this, EventArgs.Empty);
        }

        /// <summary>
        /// Start the transfer session if it is waiting for a direct connection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void messageSession_DirectConnectionEstablished(object sender, EventArgs e)
        {
            if (waitingDirectConnection)
            {
                waitingDirectConnection = false;
                StartDataTransfer(true);
            }
        }

        /// <summary>
        /// Start the transfer session if it is waiting for a direct connection. Because the direct 
        /// connection attempt failed the transfer will be over the switchboard.
        /// </summary>
        private void messageSession_DirectConnectionFailed(object sender, EventArgs e)
        {
            if (waitingDirectConnection)
            {
                waitingDirectConnection = false;
                StartDataTransfer(false);
            }
        }

        #endregion

        #region TransferDataEntry & AbortTransferThread

        /// <summary>
        /// Entry point for the thread. This thread will send the data messages to the message processor.
        /// In case it is a direct connection P2PDCMessages will be send. If no direct connection is
        /// established P2PMessage objects are wrapped in a SBMessage object and send to the message
        /// processor. Which is in the latter case probably a SB processor.
        /// </summary>
        protected void TransferDataEntry(object thisObject)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Starting transfer thread...", GetType().Name);

            OnTransferStarted();

            bool wasLastPacket = false;
            uint sessId = TransferProperties.SessionId;

            try
            {
                bool direct = MessageSession.DirectConnected;

                #region data preparation message for SB

                // Send the data preparation (4 x 0x00) message
                // This is a MUST for all the MSNObject transfer (DisplayImage, CustomEmoticon.. etc)
                if (direct == false && 
                    (TransferProperties.DataType == DataTransferType.DisplayImage || 
                    TransferProperties.DataType == DataTransferType.Emoticon))
                {
                    P2PDataMessage p2pDataMessage = new P2PDataMessage(Version);
                    p2pDataMessage.WritePreparationBytes();
                    p2pDataMessage.Header.SessionId = sessId;

                    if (Version == P2PVersion.P2PV1)
                    {
                        MessageSession.IncreaseLocalIdentifier();
                        p2pDataMessage.Header.Identifier = MessageSession.LocalIdentifier;

                        p2pDataMessage.V1Header.AckSessionId = DataPreparationAck;
                        p2pDataMessage.Footer = MessageFooter;
                    }

                    if (Version == P2PVersion.P2PV2)
                    {
                        p2pDataMessage.V2Header.TFCombination = TFCombination.First;
                    }

                    SendMessage(p2pDataMessage);
                }

                #endregion

                if (Version == P2PVersion.P2PV1)
                {
                    MessageSession.IncreaseLocalIdentifier();
                }

                uint messageIdentifier = MessageSession.LocalIdentifier;

                // Tracked to send the disconnecting message (0x40 flag) with the correct datamessage identifiers as
                // it's acknowledge identifier. (protocol)
                dataMessageIdentifier = messageIdentifier;

                long currentPosition = 0;
                long lastPosition = dataStream.Length;
                uint currentACK = (DataPreparationAck > 0) ? DataPreparationAck : (uint)new Random().Next(50000, int.MaxValue);

                int rakCounter = 128;
                TFCombination tfComb = TFCombination.First;
                if (MessageFlag == (uint)P2PFlag.MSNObjectData)
                {
                    tfComb |= TFCombination.MsnObject;
                }
                else if (MessageFlag == (uint)P2PFlag.FileData)
                {
                    tfComb |= TFCombination.FileTransfer;
                }

                while (currentPosition < lastPosition && (0 == abortThread))
                {
                    P2PDataMessage p2pDataMessage = new P2PDataMessage(Version);
                    p2pDataMessage.Header.SessionId = sessId;

                    #region setup packet

                    if (Version == P2PVersion.P2PV1)
                    {
                        p2pDataMessage.V1Header.Offset = (ulong)currentPosition;
                        p2pDataMessage.V1Header.TotalSize = (ulong)dataStream.Length;
                        p2pDataMessage.V1Header.Flags = (P2PFlag)MessageFlag;
                        p2pDataMessage.Header.Identifier = messageIdentifier;
                        p2pDataMessage.V1Header.AckSessionId = currentACK;
                        p2pDataMessage.Footer = MessageFooter;

                        if (currentACK < uint.MaxValue)
                        {
                            currentACK++;
                        }
                        else
                        {
                            currentACK--;
                        }
                    }
                    else if (Version == P2PVersion.P2PV2)
                    {
                        if (--rakCounter < 0)
                        {
                            p2pDataMessage.V2Header.OperationCode = (byte)OperationCode.RAK;
                            rakCounter = 128;
                        }

                        // Always sets to 1.
                        p2pDataMessage.V2Header.PackageNumber = 1;
                        p2pDataMessage.V2Header.TFCombination = tfComb;

                        if (currentPosition == 0)
                        {
                            tfComb &= ~TFCombination.First;
                        }
                    }

                    lock (dataStream)
                    {
                        // Write bytes to inner body.
                        // DataRemaining & MessageSize will be calculated automatically for p2pv2.
                        dataStream.Seek(currentPosition, SeekOrigin.Begin);
                        currentPosition += p2pDataMessage.WriteBytes(dataStream, 1202);

                        if (currentPosition == lastPosition)
                        {
                            wasLastPacket = true;
                        }
                    }

                    #endregion

                    System.Threading.Thread.CurrentThread.Join(300);

                    SendMessage(p2pDataMessage);

                    try
                    {
                        OnTransferProgressed(new P2PTransferProgressedEventArgs((ulong)currentPosition, (ulong)lastPosition));
                    }
                    catch (Exception xferExc)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Error occured when fired TransferProgressed: " + xferExc.ToString(), GetType().Name);
                    }

                }

            }
            catch (SocketException sex)
            {
                if (sex.ErrorCode == 10053)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "You've closed a connection: " + sex.ToString(), GetType().Name);
                }

                abortThread = 1;
                OnTransferAborted();
            }
            catch (ObjectDisposedException oex)
            {
                abortThread = 1;
                MessageSession.CloseDirectConnection();

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Exception in transfer thread: " + oex.ToString(), GetType().Name);
            }
            finally
            {
                if (wasLastPacket && (0 == abortThread))
                {
                    OnTransferFinished();
                }

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "Stopping transfer thread (" + Thread.CurrentThread.ManagedThreadId + ") for session " + sessId + "... Aborted: " + (abortThread == 1), GetType().Name);
            }
        }

        

        #endregion

        #region AcceptInvitation / SendAbortMessage / SendDisconnectMessage / AbortTransferThread

        /// <summary>
        /// Send the MSNSLP 200 OK message and start data transfer.
        /// </summary>
        /// <param name="acceptanceMessage"></param>
        internal void AcceptInvitation(SLPStatusMessage acceptanceMessage)
        {
            P2PMessage replyMessage = SendMessage(acceptanceMessage);

            if (Version == P2PVersion.P2PV1)
            {
                DataPreparationAck = replyMessage.V1Header.AckSessionId;
            }

            if (IsSender)
                StartDataTransfer(false);
        }

        /// <summary>
        /// Sends the remote client a p2p message with the 0x80 flag to abort.
        /// </summary>
        private void SendAbortMessage(SLPRequestMessage closingMessage)
        {
            if (Version == P2PVersion.P2PV2)  //In p2pv2, just send a MSNSLP BYE instead.
            {
                SendMessage(closingMessage);
            }

            if (Version == P2PVersion.P2PV1)
            {
                P2PMessage disconnectMessage = new P2PMessage(P2PVersion.P2PV1);
                disconnectMessage.V1Header.Flags = P2PFlag.TlpError;
                disconnectMessage.V1Header.SessionId = TransferProperties.SessionId;
                disconnectMessage.V1Header.AckSessionId = dataMessageIdentifier;
                SendMessage(disconnectMessage);
            }
        }

        /// <summary>
        /// Sends the remote client a p2p message with the 0x40 flag to indicate we are going to close the connection.
        /// </summary>
        internal void SendDisconnectMessage(SLPRequestMessage closingMessage)
        {
            if (Version == P2PVersion.P2PV2)  //In p2pv2, just send a MSNSLP BYE instead.
            {
                SendMessage(closingMessage);
            }

            if (Version == P2PVersion.P2PV1)
            {
                P2PMessage disconnectMessage = new P2PMessage(P2PVersion.P2PV1);
                disconnectMessage.V1Header.Flags = P2PFlag.CloseSession;
                disconnectMessage.Header.SessionId = TransferProperties.SessionId;
                disconnectMessage.V1Header.AckSessionId = dataMessageIdentifier; // aargh it took me long to figure this one out
                SendMessage(disconnectMessage);
            }
        }

        #endregion
    }
};
