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

namespace MSNPSharp.Core
{    
    using MSNPSharp;

    /// <summary>
    /// Stores incoming messages in a buffer and releases them only when all contents are received.
    /// </summary>
    /// <remarks>
    /// MessagePool buffers incoming raw byte data and releases this data only when the message is fully retrieved. 
    /// This supports when a single message is send in multiple packets.
    /// The descendants of this class have simple knowledge of the used protocol to identify whether a message is fully retrieved or not.
    /// </remarks>
    public abstract class MessagePool
    {
        /// <summary>
        /// Constructor to instantiate a message pool.
        /// </summary>
        protected MessagePool()
        {
        }

        /// <summary>
        /// Defines whether there is a message available to retrieve.
        /// </summary>
        public abstract bool MessageAvailable
        {
            get;
        }

        /// <summary>
        /// Buffers the incoming raw data internal. This method is often used after receiving incoming data from a socket or another source.
        /// </summary>
        /// <param name="reader"></param>
        public abstract void BufferData(BinaryReader reader);


        /// <summary>
        /// Retrieves the next message data from the buffer.
        /// </summary>
        /// <returns></returns>
        public abstract byte[] GetNextMessageData();
    }
};
