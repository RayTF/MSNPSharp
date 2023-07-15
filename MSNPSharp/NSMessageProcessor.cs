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
using System.Collections;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    public class NSMessageProcessor : SocketMessageProcessor
    {
        private int transactionID = 0;

        public event EventHandler<ExceptionEventArgs> HandlerException;

        protected internal NSMessageProcessor(ConnectivitySettings connectivitySettings)
            : base(connectivitySettings)
        {
            MessagePool = new NSMessagePool();
        }

        public int TransactionID
        {
            get
            {
                return transactionID;
            }
            private set
            {
                transactionID = value;
            }
        }

        /// <summary>
        /// Reset the transactionID to zero.
        /// </summary>
        internal void ResetTransactionID()
        {
            TransactionID = 0;
        }

        protected internal int IncreaseTransactionID()
        {
            return ++transactionID;
        }

        protected override void OnMessageReceived(byte[] data)
        {
            NSMessage message = new NSMessage();

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Parsing incoming NS command...", GetType().Name);
            message.ParseBytes(data);
            DispatchMessage(message);
        }

        public override void SendMessage(NetworkMessage message)
        {
            SendMessage(message, IncreaseTransactionID());
        }

        public virtual void SendMessage(NetworkMessage message, int transactionID)
        {
            NSMessage nsMessage = message as NSMessage;

            if (nsMessage == null)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "Cannot use this Message Processor to send a " + message.GetType().ToString() + " message.",
                    GetType().Name);
                return;
            }

            nsMessage.TransactionID = transactionID;
            nsMessage.PrepareMessage();

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing message:\r\n" + nsMessage.ToDebugString() + "\r\n", GetType().Name);

            // convert to bytes and send it over the socket
            SendSocketData(nsMessage.GetBytes());
        }

        public override void Disconnect()
        {
            SendMessage(new NSMessage("OUT", new string[] { }));
            base.Disconnect();
        }

        protected virtual void DispatchMessage(NetworkMessage message)
        {
            // copy the messageHandlers array because the collection can be 
            // modified during the message handling. (Handlers are registered/unregistered)
            IMessageHandler[] handlers = MessageHandlers.ToArray();

            // now give the handlers the opportunity to handle the message
            foreach (IMessageHandler handler in handlers)
            {
                try
                {
                    //I think the person who first write this make a big mistake, C# is NOT C++,
                    //message class passes as reference, one change, all changed.
                    //Mabe we need to review all HandleMessage calling.
                    ICloneable imessageClone = (message as NSMessage) as ICloneable;
                    NSMessage messageClone = imessageClone.Clone() as NSMessage;
                    handler.HandleMessage(this, messageClone);
                }
                catch (Exception e)
                {
                    if (HandlerException != null)
                        HandlerException(this, new ExceptionEventArgs(new MSNPSharpException("An exception occured while handling a nameserver message. See inner exception for more details.", e)));
                }
            }
        }
    }
};
