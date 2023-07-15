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
using System.Security.Permissions;

namespace MSNPSharp.Core
{
    using MSNPSharp;

    /// <summary>
    /// A multi-user stream.
    /// </summary>
    public class PersistentStream : Stream
    {
        /// <summary>
        /// </summary>
        private Stream innerStream = null;

        #region Stream overrides
        /// <summary>
        /// </summary>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return innerStream.BeginRead(buffer, offset, count, callback, state);
        }
        /// <summary>
        /// </summary>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            return innerStream.BeginWrite(buffer, offset, count, callback, state);
        }
        /// <summary>
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return innerStream.CanRead;
            }
        }
        /// <summary>
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return innerStream.CanSeek;
            }
        }
        /// <summary>
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return innerStream.CanWrite;
            }
        }
        /// <summary>
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override System.Runtime.Remoting.ObjRef CreateObjRef(Type requestedType)
        {
            return innerStream.CreateObjRef(requestedType);
        }

        /// <summary>
        /// </summary>
        public override int EndRead(IAsyncResult asyncResult)
        {
            return innerStream.EndRead(asyncResult);
        }
        /// <summary>
        /// </summary>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            innerStream.EndWrite(asyncResult);
        }
        /// <summary>
        /// </summary>
        public override bool Equals(object obj)
        {
            return innerStream.Equals(obj);
        }
        /// <summary>
        /// </summary>
        public override void Flush()
        {
            innerStream.Flush();
        }
        /// <summary>
        /// </summary>
        public override int GetHashCode()
        {
            return innerStream.GetHashCode();
        }
        /// <summary>
        /// </summary>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.Infrastructure)]
        public override object InitializeLifetimeService()
        {
            return innerStream.InitializeLifetimeService();
        }
        /// <summary>
        /// </summary>
        public override long Length
        {
            get
            {
                return innerStream.Length;
            }
        }
        /// <summary>
        /// </summary>
        public override long Position
        {
            get
            {
                return innerStream.Position;
            }
            set
            {
                innerStream.Position = value;
            }
        }
        /// <summary>
        /// </summary>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return innerStream.Read(buffer, offset, count);
        }
        /// <summary>
        /// </summary>
        public override int ReadByte()
        {
            return innerStream.ReadByte();
        }
        /// <summary>
        /// </summary>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }
        /// <summary>
        /// </summary>
        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }
        /// <summary>
        /// </summary>
        public override string ToString()
        {
            return innerStream.ToString();
        }

        /// <summary>
        /// </summary>
        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }
        /// <summary>
        /// </summary>
        public override void WriteByte(byte value)
        {
            innerStream.WriteByte(value);
        }
        #endregion

        /// <summary>
        /// Keeps track of the number of users using the stream.
        /// </summary>
        private int users;

        /// <summary>
        /// The number of users using the stream.
        /// </summary>
        public int Users
        {
            get
            {
                return users;
            }
        }

        /// <summary>
        /// Increases the number of users using this stream with 1.
        /// </summary>
        public void Open()
        {
            users++;
        }

        /// <summary>
        /// Decreases the number of users using this stream with 1. If the number of users is below 0 the stream will really be closed.
        /// </summary>
        public override void Close()
        {
            users--;
            if (users <= 0)
                innerStream.Close();
        }

        /// <summary>
        /// </summary>
        public PersistentStream(Stream stream)
        {
            innerStream = stream;
            Open();
        }
    }
};
