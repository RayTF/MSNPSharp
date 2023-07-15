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
    using MSNPSharp.DataTransfer;


    public class SBMessageProcessor : SocketMessageProcessor
    {
        int transactionID = 1;

        public event EventHandler<ExceptionEventArgs> HandlerException;

        protected internal SBMessageProcessor(ConnectivitySettings connectivitySettings)
            : base(connectivitySettings)
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Constructing object", GetType().Name);

            MessagePool = new SBMessagePool();
        }

        public int TransactionID
        {
            get
            {
                return transactionID;
            }
            set
            {
                transactionID = value;
            }
        }


        protected int IncreaseTransactionID()
        {
            return ++transactionID;
        }

        protected override void OnMessageReceived(byte[] data)
        {
            // first get the general expected switchboard message
            SBMessage message = new SBMessage();

            message.ParseBytes(data);

            // send the message
            DispatchMessage(message);
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
                    handler.HandleMessage(this, message);
                }
                catch (Exception e)
                {
                    OnHandlerException(e);
                }
            }
        }

        protected virtual void OnHandlerException(Exception e)
        {
            MSNPSharpException MSNPSharpException = new MSNPSharpException("An exception occured while handling a switchboard message. See inner exception for more details.", e);

            Trace.WriteLineIf(Settings.TraceSwitch.TraceError, MSNPSharpException.InnerException.ToString() + "\r\nStacktrace:\r\n" + MSNPSharpException.InnerException.StackTrace.ToString(), GetType().Name);

            if (HandlerException != null)
                HandlerException(this, new ExceptionEventArgs(MSNPSharpException));
        }

        protected virtual void DeliverToNetwork(SBMessage sbMessage)
        {
            sbMessage.TransactionID = IncreaseTransactionID();
            sbMessage.Acknowledgement = sbMessage.Acknowledgement;

            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Outgoing message:\r\n" + sbMessage.ToDebugString() + "\r\n", GetType().Name);


            int x = 0;

            if (sbMessage.CommandValues.Count > 0)
                int.TryParse(sbMessage.CommandValues[0].ToString(), out x);

            Debug.Assert(x < 1500, "?");



            // prepare the message
            sbMessage.PrepareMessage();

            // convert to bytes and send it over the socket
            SendSocketData(sbMessage.GetBytes());
        }



        public override void SendMessage(NetworkMessage message)
        {
            SBMessage sbMessage = message as SBMessage;

            if (sbMessage == null)
            {
                throw new MSNPSharpException("Cannot use " + GetType().ToString() + " to deliver a " + message.GetType().ToString() + " message.");
            }

            DeliverToNetwork(sbMessage);
        }

        public override void Disconnect()
        {
            base.Disconnect();
        }
    }
};