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

namespace MSNPSharp.Utilities
{

    public class MessageArrivedEventArgs : EventArgs
    {
        private ConversationID conversationID = null;

        /// <summary>
        /// The identifier of a <see cref="Conversation"/> in <see cref="MessageManager"/>.
        /// </summary>
        public ConversationID ConversationID
        {
            get { return conversationID; }
        }

        private Contact sender = null;

        /// <summary>
        /// The sender of message.
        /// </summary>
        public Contact Sender
        {
            get { return sender; }
        }

        private NetworkMessageType messageType = NetworkMessageType.None;

        /// <summary>
        /// The type of message received.
        /// </summary>
        public NetworkMessageType MessageType
        {
            get { return messageType; }
        }


        public MessageArrivedEventArgs(ConversationID conversationId, Contact sender, NetworkMessageType type)
        {
            conversationID = conversationId;
            this.sender = sender;
            messageType = type;
        }
    }

    public class TextMessageArrivedEventArgs : MessageArrivedEventArgs
    {
        private TextMessage textMessage = null;

        /// <summary>
        /// The text message received.
        /// </summary>
        public TextMessage TextMessage
        {
            get { return textMessage; }
        }


        public TextMessageArrivedEventArgs(ConversationID conversationId, Contact sender, TextMessage textMessage)
            : base(conversationId, sender, NetworkMessageType.Text)
        {
            this.textMessage = textMessage;
        }
    }

    public class EmoticonArrivedEventArgs : MessageArrivedEventArgs
    {
        private Emoticon emoticon = null;

        /// <summary>
        /// The emoicon data received.
        /// </summary>
        public Emoticon Emoticon
        {
            get { return emoticon; }
        }

        public EmoticonArrivedEventArgs(ConversationID conversationId, Contact sender, Emoticon emoticon)
            : base(conversationId, sender, NetworkMessageType.Emoticon)
        {
            this.emoticon = emoticon;
        }

    }
}
