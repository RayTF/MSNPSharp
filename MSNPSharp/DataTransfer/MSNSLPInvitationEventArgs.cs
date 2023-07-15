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

namespace MSNPSharp.DataTransfer
{
    /// <summary>
    /// Used as event argument when an invitation is received.
    /// </summary>
    /// <remarks>
    /// The client programmer must set the Accept property to true (accept) or false (reject) to response
    /// to the invitation. By default the invitation is rejected.
    /// </remarks>
    [Serializable]
    public class MSNSLPInvitationEventArgs : EventArgs
    {
        private MSNSLPTransferProperties transferProperties;
        private SLPMessage invitationMessage;
        private ActivityInfo activity;
        private MSNObject msnObject;
        private string filename;
        private long fileSize;
        private bool accept;
        private bool delayprocess;

        [NonSerialized]
        private P2PTransferSession transferSession = null;
        [NonSerialized]
        private MSNSLPHandler transferhandler = null;


        public MSNSLPInvitationEventArgs(
            MSNSLPTransferProperties transferProperties,
            SLPMessage invitationMessage,
            P2PTransferSession transferSession,
            MSNSLPHandler handler)
        {
            this.transferProperties = transferProperties;
            this.invitationMessage = invitationMessage;
            this.transferSession = transferSession;
            this.transferhandler = handler;
        }

        /// <summary>
        /// The affected transfer session
        /// </summary>
        public MSNSLPTransferProperties TransferProperties
        {
            get
            {
                return transferProperties;
            }
            set
            {
                transferProperties = value;
            }
        }

        /// <summary>
        /// The affected transfer session
        /// </summary>
        public SLPMessage InvitationMessage
        {
            get
            {
                return invitationMessage;
            }
            set
            {
                invitationMessage = value;
            }
        }

        /// <summary>
        /// The activity properties.
        /// </summary>
        public ActivityInfo Activity
        {
            get
            {
                return activity;
            }
            set
            {
                activity = value;
            }
        }

        /// <summary>
        /// The corresponding msnobject defined in the invitation. Only available in case of an msn object
        /// transfer (image display, emoticons).
        /// </summary>
        /// <remarks>
        /// Created from the Context property of the <see cref="MSNSLPTransferProperties"/> object.
        /// </remarks>
        public MSNObject MSNObject
        {
            get
            {
                return msnObject;
            }
            set
            {
                msnObject = value;
            }
        }

        /// <summary>
        /// Name of the file the remote contact wants to send. Only available in case of a filetransfer session.
        /// </summary>
        public string Filename
        {
            get
            {
                return filename;
            }
            set
            {
                filename = value;
            }
        }

        /// <summary>
        /// The total size of the file in bytes. Only available in case of a filetransfer session.
        /// </summary>
        public long FileSize
        {
            get
            {
                return fileSize;
            }
            set
            {
                fileSize = value;
            }
        }

        /// <summary>
        /// Defines if the transfer is accepted. This must be set by the client programmer in a event handler.
        /// By default this property is set to false, which means the invitation is rejected. If this
        /// property is set to true, the invitation is accepted.
        /// </summary>
        public bool Accept
        {
            get
            {
                return accept;
            }
            set
            {
                accept = value;
            }
        }

        /// <summary>
        /// Whether process the invitation request right after the event was fired.
        /// </summary>
        public bool DelayProcess
        {
            get
            {
                return delayprocess;
            }
            set
            {
                delayprocess = value;
            }
        }

        /// <summary>
        /// The p2p transfer session that will transfer the session data.
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
        /// Master session (SLP Handler)
        /// </summary>
        public MSNSLPHandler TransferHandler
        {
            get
            {
                return transferhandler;
            }
        }
    }
};
