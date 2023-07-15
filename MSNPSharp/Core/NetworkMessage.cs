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

namespace MSNPSharp.Core
{
    using MSNPSharp;

    public abstract class NetworkMessage
    {
        byte[] innerBody;
        NetworkMessage parentMessage;
        NetworkMessage innerMessage;

        protected NetworkMessage()
        {
        }

        public virtual void CreateFromParentMessage(NetworkMessage containerMessage)
        {
            ParentMessage = containerMessage;
            ParentMessage.InnerMessage = this;

            if (ParentMessage.InnerBody != null)
                ParseBytes(ParentMessage.InnerBody);
        }

        protected virtual void OnInnerMessageSet()
        {
            if (InnerMessage != null && InnerMessage.ParentMessage != this)
                InnerMessage.ParentMessage = this;
        }

        protected virtual void OnParentMessageSet()
        {
            if (ParentMessage != null && ParentMessage.InnerMessage != this)
                ParentMessage.InnerMessage = this;
        }

        /// <summary>
        /// The byte array contains in the message stream
        /// </summary>
        public byte[] InnerBody
        {
            get
            {
                return innerBody;
            }
            set
            {
                innerBody = value;
            }
        }

        /// <summary>
        /// Usually the payload of a payload message.
        /// </summary>
        public NetworkMessage InnerMessage
        {
            get
            {
                return innerMessage;
            }
            set
            {
                innerMessage = value;
                OnInnerMessageSet();
            }
        }

        public NetworkMessage ParentMessage
        {
            get
            {
                return parentMessage;
            }
            set
            {
                parentMessage = value;
                OnParentMessageSet();
            }
        }

        /// <summary>
        /// Format the message and make it ready to send.
        /// </summary>
        public virtual void PrepareMessage()
        {
            if (InnerMessage != null)
                InnerMessage.PrepareMessage();
        }

        public abstract byte[] GetBytes();
        public abstract void ParseBytes(byte[] data);

        public string ToDebugString()
        {
            if (InnerMessage != null)
                return ToString() + "\r\n" + InnerMessage.ToDebugString();
            else
                return ToString();
        }

        public static byte[] AppendArray(byte[] originalArray, byte[] appendingArray)
        {
            if (appendingArray != null)
            {
                byte[] newArray = new byte[originalArray.Length + appendingArray.Length];
                Array.Copy(originalArray, 0, newArray, 0, originalArray.Length);
                Array.Copy(appendingArray, 0, newArray, originalArray.Length, appendingArray.Length);
                return newArray;
            }
            else
                return originalArray;
        }

    }
};
