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
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// A pool for P2P Direct-connection messages.
    /// </summary>
    /// <remarks>
    /// This message pool will read in the first 4 bytes for the length of the message. And after that the
    /// lenth is read and inserted in a buffer.
    /// </remarks>
    public class P2PDCPool : MSNPSharp.Core.MessagePool
    {
        private Queue messages = Queue.Synchronized(new Queue());
        private byte[] lastMessage;
        private int bytesLeft;

        /// <summary>
        /// Constructor.
        /// </summary>
        public P2PDCPool()
        {
        }

        /// <summary>
        /// Buffers incoming P2P direct connected messages.
        /// </summary>
        /// <param name="reader"></param>
        public override void BufferData(System.IO.BinaryReader reader)
        {
            lock (reader.BaseStream)
            {
                // make sure we read the last retrieved message if available
                if (bytesLeft > 0 && lastMessage != null)
                {
                    // make sure no overflow occurs
                    int length = (int)Math.Min(bytesLeft, (uint)(reader.BaseStream.Length - reader.BaseStream.Position));
                    reader.Read(lastMessage, lastMessage.Length - bytesLeft, (int)length);
                    bytesLeft -= length;


                    if (bytesLeft == 0)
                    {
                        // insert it into the temporary buffer for later retrieval
                        messages.Enqueue(lastMessage);
                        lastMessage = null;
                    }
                }


                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    // read the length of the message
                    uint messageLength = reader.ReadUInt32();

                    // make sure no overflow occurs
                    int length = (int)Math.Min(messageLength, (uint)(reader.BaseStream.Length - reader.BaseStream.Position));

                    // read in the bytes
                    byte[] message = new byte[messageLength];
                    reader.Read(message, 0, (int)length);

                    if (length < messageLength)
                    {
                        bytesLeft = (int)messageLength - length;
                        lastMessage = message;
                    }
                    else
                    {
                        lastMessage = null;

                        // insert it into the temporary buffer for later retrieval
                        messages.Enqueue(message);
                    }
                }
            }
        }

        public override byte[] GetNextMessageData()
        {
            return (byte[])messages.Dequeue();
        }

        public override bool MessageAvailable
        {
            get
            {
                return messages.Count > 0;
            }
        }
    }
};
