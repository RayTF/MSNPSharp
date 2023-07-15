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
    public class ConversationID
    {
        private Conversation conversation = null;
        private Contact remoteOwner = null;

        private string hashString = string.Empty;
        private string compareString = string.Empty;
        private int hashCode = 0;

        public Contact RemoteOwner
        {
            get { return remoteOwner; }
        }

        internal Conversation Conversation
        {
            get
            {
                return conversation;
            }
        }

        private object syncObject = new object();

        internal void SetConversation(Conversation conv)
        {
            conversation = conv;
            if (conversation != null)
            {
                if (conversation.RemoteOwner == null && RemoteOwner == null)
                    throw new ArgumentException("Invailid conversation.");

                if (conversation.RemoteOwner != null)
                    remoteOwner = conversation.RemoteOwner;
            }
        }

        public ClientType NetworkType
        {
            get
            {
                if (!remoteOwner.IsMessengerUser)
                    return ClientType.None;

                return remoteOwner.ClientType;
            }
        }

        public ConversationID(Conversation conversation)
        {
            if (conversation == null)
                throw new ArgumentNullException("conversation is null.");

            SetConversation(conversation);

            hashString = ToString();
            hashCode = hashString.GetHashCode();


            conversation.ConversationEnded += delegate
            {
                lock (syncObject)
                {
                    SetConversation(null);
                }
            };

            conversation.RemoteOwnerChanged += delegate(object sender, ConversationRemoteOwnerChangedEventArgs args)
            {
                lock (syncObject)
                {
                    try
                    {
                        SetConversation(conversation);
                    }
                    catch (Exception)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Remote owner changed to null.");
                    }
                }
            };
        }

        public ConversationID(Contact remote)
        {
            if (remote == null)
                throw new ArgumentNullException("remote is null.");

            remoteOwner = remote;
            hashString = ToString();
            hashCode = hashString.GetHashCode();
        }

        /// <summary>
        /// Whether the two conversation are logically equals.
        /// </summary>
        /// <param name="id1"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool operator ==(ConversationID id1, object other)
        {
            if (((object)id1 == null) && other == null)
                return true;

            if (((object)id1) == null || other == null)
                return false;

            if (other is ConversationID)
            {
                return id1.Equals(other);
            }

            if (other is Conversation)
            {
                if (id1.conversation == null) return false;
                return object.ReferenceEquals(id1.conversation, other);
            }


            return false;
        }

        public static bool operator !=(ConversationID id1, object other)
        {
            return !(id1 == other);
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            if (obj == null)
                return false;

            if (!(obj is ConversationID))
                return false;

            return ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            string remoteOwnerString = (remoteOwner == null ? "null" : remoteOwner.Mail.ToLowerInvariant());
            string conversationString = string.Empty;
            if (conversation != null)
            {
                if ((conversation.Type & ConversationType.MutipleUsers) > 0)
                {
                    conversationString = conversation.Switchboard.SessionHash;
                }
            }
            return string.Join(";", new string[] { NetworkType.ToString().ToLowerInvariant(), remoteOwnerString, conversationString });
        }
    }
}
