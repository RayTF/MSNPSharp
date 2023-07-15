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
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// Holds all properties for a single data transfer.
    /// </summary>
    [Serializable]
    public class MSNSLPTransferProperties
    {
        private Contact localContact = null;
        private Guid localContactEndPointID = Guid.Empty;
        private Contact remoteContact = null;
        private Guid remoteContactEndPointID = Guid.Empty;
        private P2PVersion transferStackVersion = P2PVersion.P2PV1;

        private uint sessionId = 0;
        private bool remoteInvited = false;
        private int sessionCloseState = (int)SessionCloseState.None;

        private int lastCSeq = 0;
        private Guid nonce = Guid.Empty;
        private Guid hashedNonce = Guid.Empty;
        private Guid remoteNonce = Guid.Empty;
        private Guid callId = Guid.Empty;
        private DCNonceType dcNonceType = DCNonceType.Plain;
        private string lastBranch = Guid.Empty.ToString("B").ToUpper(CultureInfo.InvariantCulture);

        private uint dataSize = 0;
        private string dataTypeGuid = String.Empty;
        private DataTransferType dataType = DataTransferType.Unknown;
        private string context = String.Empty;
        private string checksum = String.Empty;


        protected MSNSLPTransferProperties()
        {
        }

        public MSNSLPTransferProperties(Contact local, Guid localEndPointID, Contact remote, Guid remoteEndPointID)
        {
            remoteContact = remote;
            localContact = local;
            localContactEndPointID = localEndPointID;
            remoteContactEndPointID = remoteEndPointID;

            transferStackVersion = JudgeP2PStackVersion(local, localContactEndPointID, remote, remoteContactEndPointID, false);
        }

        /// <summary>
        /// The the local contact in the transfer session.
        /// </summary>
        public Contact LocalContact
        {
            get
            {
                return localContact;
            }
        }

        /// <summary>
        /// The <see cref="EndPointData"/> id of local contact that involved in the transfer.
        /// </summary>
        public Guid LocalContactEndPointID
        {
            get
            {
                return localContactEndPointID;
            }
        }

        /// <summary>
        /// The the remote contact in the transfer session.
        /// </summary>
        public Contact RemoteContact
        {
            get
            {
                return remoteContact;
            }
        }

        /// <summary>
        /// The <see cref="EndPointData"/> id of remote contact that involved in the transfer.
        /// </summary>
        public Guid RemoteContactEndPointID
        {
            get
            {
                return remoteContactEndPointID;
            }
        }

        public string LocalContactEPIDString
        {
            get
            {
                if (TransferStackVersion == P2PVersion.P2PV1)
                {
                    return LocalContact.Mail.ToLowerInvariant();
                }

                return LocalContact.Mail.ToLowerInvariant() + ";" + LocalContactEndPointID.ToString("B").ToLowerInvariant();
            }
        }

        public string RemoteContactEPIDString
        {
            get
            {
                if (TransferStackVersion == P2PVersion.P2PV1)
                {
                    return RemoteContact.Mail.ToLowerInvariant();
                }

                return RemoteContact.Mail.ToLowerInvariant() + ";" + RemoteContactEndPointID.ToString("B").ToLowerInvariant();
            }
        }

        /// <summary>
        /// The transfer stack that transfer layer (P2PMessageSession) used for this data transfer.
        /// </summary>
        public P2PVersion TransferStackVersion
        {
            get
            {
                return transferStackVersion;
            }
        }

        /// <summary>
        /// The unique session id for the transfer
        /// </summary>
        public uint SessionId
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
        /// Defines whether the remote client has invited the transfer (true) or the local client has
        /// initiated the transfer (false).
        /// </summary>
        public bool RemoteInvited
        {
            get
            {
                return remoteInvited;
            }
            set
            {
                remoteInvited = value;
            }
        }

        /// <summary>
        /// Indicates whether we should remove this transfer session from its transferlayer (P2PMessageSession).
        /// </summary>
        internal SessionCloseState SessionCloseState
        {
            get
            {
                return (SessionCloseState)sessionCloseState;
            }

            set
            {
                sessionCloseState = (int)value;
            }
        }

        /// <summary>
        /// CSeq identifier
        /// </summary>
        public int LastCSeq
        {
            get
            {
                return lastCSeq;
            }
            set
            {
                lastCSeq = value;
            }
        }

        public DCNonceType DCNonceType
        {
            get
            {
                return dcNonceType;
            }
            set
            {
                dcNonceType = value;
            }
        }

        /// <summary>
        /// The Nonce used in the handshake message for direct connections.
        /// </summary>
        public Guid Nonce
        {
            get
            {
                return nonce;
            }
            set
            {
                nonce = value;
            }
        }

        /// <summary>
        /// The Hashed-Nonce used in the handshake message for direct connections. This is SHA1 value of
        /// <see cref="Nonce"/> if remote contact supports hashed-guids, otherwise this is Guid.Empty.
        /// </summary>
        public Guid HashedNonce
        {
            get
            {
                return hashedNonce;
            }
            set
            {
                hashedNonce = value;
            }
        }

        /// <summary>
        /// The remote user's Hashed-Nonce used in the handshake message for direct connections.
        /// </summary>
        public Guid RemoteNonce
        {
            get
            {
                return remoteNonce;
            }
            set
            {
                remoteNonce = value;
            }
        }

        /// <summary>
        /// The unique call id for this transfer
        /// </summary>
        public Guid CallId
        {
            get
            {
                return callId;
            }
            set
            {
                callId = value;
            }
        }

        /// <summary>
        /// The branch last received in the message session
        /// </summary>
        public string LastBranch
        {
            get
            {
                return lastBranch;
            }
            set
            {
                lastBranch = value;
            }
        }

        /// <summary>
        /// The total length of the data, in bytes
        /// </summary>
        public uint DataSize
        {
            get
            {
                return dataSize;
            }
            set
            {
                dataSize = value;
            }
        }

        /// <summary>
        /// The kind of data that will be transferred
        /// </summary>
        public DataTransferType DataType
        {
            get
            {
                return dataType;
            }
            set
            {
                dataType = value;
            }
        }

        internal string DataTypeGuid
        {
            get
            {
                return dataTypeGuid;
            }
            set
            {
                dataTypeGuid = value;
            }
        }

        /// <summary>
        /// The context send in the invitation. This informs the client about the type of transfer,
        /// filename, file-hash, msn object settings, etc.
        /// </summary>
        public string Context
        {
            get
            {
                return context;
            }
            set
            {
                context = value;
            }
        }

        /// <summary>
        /// The checksum of the fields used in the context
        /// </summary>
        public string Checksum
        {
            get
            {
                return checksum;
            }
            set
            {
                checksum = value;
            }
        }


        internal static P2PVersion JudgeP2PStackVersion(
            Contact local, Guid localEPID,
            Contact remote, Guid remoteEPID,
            bool dumpJudgeProcedure)
        {
            P2PVersion result = P2PVersion.P2PV1;

            if (!local.EndPointData.ContainsKey(localEPID))
            {
                string errorMessage = "Invalid parameter localEndPointID, EndPointData with id = " +
                    localEPID.ToString("B") + " not exists in contact: " + local;
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError && dumpJudgeProcedure, "[JudgeP2PStackVersion] " + errorMessage);

            }

            if (!remote.EndPointData.ContainsKey(remoteEPID))
            {
                string errorMessage = "Invalid parameter remoteEndPointID, EndPointData with id = " +
                    remoteEPID.ToString("B") + " not exists in contact: " + remote;
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError && dumpJudgeProcedure, "[JudgeP2PStackVersion] " + errorMessage);
            }

            bool supportMPOP = (localEPID != Guid.Empty && remoteEPID != Guid.Empty);

            if (local.EndPointData.ContainsKey(localEPID) && remote.EndPointData.ContainsKey(remoteEPID))
            {
                bool supportMSNC10 = ((local.EndPointData[localEPID].ClientCapacities & ClientCapacities.CanHandleMSNC10) > 0 &&
                                      (remote.EndPointData[remoteEPID].ClientCapacities & ClientCapacities.CanHandleMSNC10) > 0);
                bool supportP2Pv2 = ((local.EndPointData[localEPID].ClientCapacitiesEx & ClientCapacitiesEx.SupportsPeerToPeerV2) > 0 &&
                                     (remote.EndPointData[remoteEPID].ClientCapacitiesEx & ClientCapacitiesEx.SupportsPeerToPeerV2) > 0);



                if (supportMPOP /*&&  supportP2Pv2 &&  supportMSNC10 */) //It seems that supportP2Pv2 is not a consideration.
                    result = P2PVersion.P2PV2;
                else
                    result = P2PVersion.P2PV1;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose && dumpJudgeProcedure,
                    "Version Triggers: supportMPOP = " + supportMPOP + ", supportMSNC10 = " + supportMSNC10 + ", supportP2Pv2 = " + supportP2Pv2 + ", Result = " + result);
            }
            else
            {

                if (localEPID != Guid.Empty && remoteEPID != Guid.Empty)
                {
                    result = P2PVersion.P2PV2;
                }

                result = P2PVersion.P2PV1;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceError && dumpJudgeProcedure, "[JudgeP2PStackVersion] Judge only based on EPIDs, result:" + result);
            }

            return result;
        }
    }
};
