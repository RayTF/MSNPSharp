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
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;
using Org.Mentalis.Network.ProxySocket;

namespace MSNPSharp.Core
{
    using MSNPSharp;

    public abstract class SocketMessageProcessor : IMessageProcessor, IDisposable
    {
        private ConnectivitySettings connectivitySettings = new ConnectivitySettings();
        private byte[] socketBuffer = new byte[8192];
        private bool hasFiredDisconnectEvent = false;
        private IPEndPoint proxyEndPoint = null;
        private ProxySocket socket = null;
        private MessagePool messagePool = null;
        private List<IMessageHandler> messageHandlers = new List<IMessageHandler>();

        public event EventHandler<EventArgs> ConnectionEstablished;
        public event EventHandler<EventArgs> ConnectionClosed;
        public event EventHandler<ExceptionEventArgs> ConnectingException;
        public event EventHandler<ExceptionEventArgs> ConnectionException;

        public SocketMessageProcessor(ConnectivitySettings connectivitySettings)
        {
            ConnectivitySettings = connectivitySettings;
        }

        ~SocketMessageProcessor()
        {
            Dispose(false);
        }

        protected IPEndPoint ProxyEndPoint
        {
            get
            {
                return proxyEndPoint;
            }
            set
            {
                proxyEndPoint = value;
            }
        }

        protected MessagePool MessagePool
        {
            get
            {
                return messagePool;
            }
            set
            {
                messagePool = value;
            }
        }

        protected virtual ProxySocket GetPreparedSocket(IPAddress address, int port)
        {
            //Creates the Socket for sending data over TCP.
            ProxySocket socket = new ProxySocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // incorporate the connection settings like proxy's						
            // Note: ProxyType is in MSNPSharp namespace, ProxyTypes in ProxySocket namespace.
            if (ConnectivitySettings.ProxyType != ProxyType.None)
            {
                // set the proxy type
                socket.ProxyType = (ConnectivitySettings.ProxyType == ProxyType.Socks4)
                    ? Org.Mentalis.Network.ProxySocket.ProxyTypes.Socks4
                    : Org.Mentalis.Network.ProxySocket.ProxyTypes.Socks5;

                socket.ProxyUser = ConnectivitySettings.ProxyUsername;
                socket.ProxyPass = ConnectivitySettings.ProxyPassword;

                // resolve the proxy host
                if (proxyEndPoint == null)
                {
                    bool worked = false;
                    int retries = 0;
                    Exception exp = null;

                    //we retry a few times, because dns resolve failure is quite common
                    do
                    {
                        try
                        {
                            System.Net.IPHostEntry ipHostEntry = System.Net.Dns.GetHostEntry(ConnectivitySettings.ProxyHost);
                            System.Net.IPAddress ipAddress = ipHostEntry.AddressList[0];

                            // assign to the connection object so other sockets can make use of it quickly
                            proxyEndPoint = new IPEndPoint(ipAddress, ConnectivitySettings.ProxyPort);

                            worked = true;
                        }
                        catch (Exception e)
                        {
                            retries++;
                            exp = e;
                        }
                    } while (!worked && retries < 3);

                    if (!worked)
                        throw new ConnectivityException("DNS Resolve for the proxy server failed: " + ConnectivitySettings.ProxyHost + " failed.", exp);
                }

                socket.ProxyEndPoint = proxyEndPoint;
            }
            else
                socket.ProxyType = ProxyTypes.None;

            //Send operations will timeout of confirmation is not received within 3000 milliseconds.
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 3000);

            //Socket will linger for 2 seconds after close is called.
            LingerOption lingerOption = new LingerOption(true, 2);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);

            try
            {
                socket.Bind(new IPEndPoint(address, port));
            }
            catch (SocketException ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "An error occured while trying to bind to a local address, error code: " + ex.ErrorCode + ".");
            }

            return socket;
        }

        protected virtual void EndSendCallback(IAsyncResult ar)
        {
            ProxySocket socket = (ProxySocket)ar.AsyncState;
            socket.EndSend(ar);
        }

        protected void SendSocketData(byte[] data)
        {
            if (socket == null || !Connected)
            {
                // the connection is closed
                OnDisconnected();
                return;
            }

            SendSocketData(socket, data);
        }

        protected void SendSocketData(Socket psocket, byte[] data)
        {
            try
            {
                if (psocket != null && IsSocketConnected(psocket))
                {
                    lock (psocket)
                    {
                        psocket.Send(data);
                    }
                }
                else
                {
                    OnDisconnected();
                }
            }
            catch (SocketException sex)
            {
                if (sex.NativeErrorCode != 10035)  //10035: WSAEWOULDBLOCK
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Error while sending network message. Error message: " + sex.Message);
                    OnDisconnected();
                }

                return;
            }
            catch (ObjectDisposedException)
            {
                // the connection is closed
                OnDisconnected();
            }
            catch (Exception e)
            {
                throw new MSNPSharpException("Error while sending network message. See the inner exception for more details.", e);
            }
        }

        protected virtual void EndReceiveCallback(IAsyncResult ar)
        {
            int cnt = 0;
            try
            {
                System.Diagnostics.Debug.Assert(messagePool != null, "Field messagepool must be defined in derived class of SocketMessageProcessor.");

                Socket socket = (Socket)ar.AsyncState;
                cnt = socket.EndReceive(ar);
                if (cnt == 0)
                {
                    // No data is received. We are disconnected.
                    OnDisconnected();
                    return;
                }

                // read the messages and dispatch to handlers
                using (BinaryReader reader = new BinaryReader(new MemoryStream(socketBuffer, 0, cnt)))
                {
                    messagePool.BufferData(reader);
                }
                while (messagePool.MessageAvailable)
                {
                    // retrieve the message
                    byte[] incomingMessage = messagePool.GetNextMessageData();


                    // call the virtual method to perform polymorphism, descendant classes can take care of it
                    OnMessageReceived(incomingMessage);
                }

                // start a new read				
                BeginDataReceive(socket);
            }
            catch (SocketException e)
            {
                // close the socket upon a exception
                if (socket != null && Connected)
                    socket.Close();

                OnDisconnected();

                // an exception Occurred, pass it through
                if (ConnectionException != null)
                    ConnectionException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor encountered a socket exception while retrieving data. See the inner exception for more information.", e)));
            }
            catch (ObjectDisposedException)
            {
                // the connection is closed
                OnDisconnected();
            }
            catch (Exception e)
            {
                // close the socket upon a exception
                if (socket != null && Connected)
                    socket.Close();

                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.ToString() + "\r\n" + e.StackTrace + "\r\n", GetType().Name);

                OnDisconnected();

                if (ConnectionException != null)
                    ConnectionException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor encountered a general exception while retrieving data. See the inner exception for more information.", e)));
            }
        }


        protected virtual void EndConnectCallback(IAsyncResult ar)
        {
            try
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "End Connect Callback", GetType().Name);

                ((ProxySocket)socket).EndConnect(ar);

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "End Connect Callback Daarna", GetType().Name);

                hasFiredDisconnectEvent = false;
                OnConnected();

                // Begin receiving data
                BeginDataReceive(socket);
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "** EndConnectCallback exception **" + e.ToString(), GetType().Name);

                if (ConnectingException != null)
                    ConnectingException(this, new ExceptionEventArgs(new ConnectivityException("SocketMessageProcessor failed to connect to the specified endpoint. See the inner exception for more information.", e)));
            }
        }

        protected virtual void BeginDataReceive(Socket socket)
        {
            try
            {
                socketBuffer = new byte[socketBuffer.Length];
                socket.BeginReceive(socketBuffer, 0, socketBuffer.Length, SocketFlags.None, new AsyncCallback(EndReceiveCallback), socket);
            }
            catch (ObjectDisposedException)
            {
                OnDisconnected();
            }
        }

        protected virtual void OnConnected()
        {
            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Connected", GetType().Name);

            if (ConnectionEstablished != null)
                ConnectionEstablished(this, new EventArgs());
        }

        protected virtual void OnDisconnected()
        {
            if (hasFiredDisconnectEvent)
            {
                return;
            }
            else
            {
                hasFiredDisconnectEvent = true;
            }

            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Disconnected", GetType().Name);

            if (ConnectionClosed != null)
                ConnectionClosed(this, new EventArgs());
        }

        public ConnectivitySettings ConnectivitySettings
        {
            get
            {
                return connectivitySettings;
            }

            set
            {
                if (Connected)
                {
                    string errorString = "Cannot set the ConnectivitySettings property of a connected " + GetType().ToString() + ".";
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, errorString);
                    throw new InvalidOperationException(errorString);
                }

                connectivitySettings = value;
            }
        }

        public bool Connected
        {
            get
            {
                if (socket == null)
                    return false;

                lock (socket)
                {
                    return IsSocketConnected(socket);
                }
            }
        }

        /// <summary>
        /// Show whether the socket is connected at a certain moment.
        /// </summary>
        /// <param name="socket"></param>
        /// <returns>true if socket is connected, false if socket is disconnected.</returns>
        public static bool IsSocketConnected(Socket socket)
        {
            bool returnValue = false;

            if (socket != null)
            {
                // Socket.Connected doesn't tell us if the socket is actually connected...
                // http://msdn2.microsoft.com/en-us/library/system.net.sockets.socket.connected.aspx

                bool disposed = false;
                bool blocking = socket.Blocking;

                try
                {
                    socket.Blocking = false;

                    int pollWait = 1;

                    if (socket.Poll(pollWait, SelectMode.SelectRead) && socket.Available == 0)
                    {
                        returnValue = false;
                    }
                    else
                    {
                        returnValue = true;
                    }

                }
                catch (SocketException ex)
                {
                    // 10035 == WSAEWOULDBLOCK
                    if (ex.NativeErrorCode.Equals(10035))
                        returnValue = true;
                }
                catch (ObjectDisposedException)
                {
                    disposed = true;
                    returnValue = false;
                }
                finally
                {
                    if (!disposed)
                    {
                        socket.Blocking = blocking;
                    }
                }
            }

            return returnValue;
        }

        public EndPoint LocalEndPoint
        {
            get
            {
                return socket.LocalEndPoint;
            }
        }

        protected List<IMessageHandler> MessageHandlers
        {
            get
            {
                return messageHandlers;
            }
        }

        public virtual void RegisterHandler(IMessageHandler handler)
        {
            if (handler != null && !messageHandlers.Contains(handler))
            {
                lock (messageHandlers)
                {
                    messageHandlers.Add(handler);
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                       handler.ToString() + " added to handler list.", GetType().Name);
                }
            }
        }

        public virtual void UnregisterHandler(IMessageHandler handler)
        {
            if (handler != null)
            {
                lock (messageHandlers)
                {
                    while (messageHandlers.Remove(handler))
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                            handler.ToString() + " removed from handler list.", GetType().Name);
                    }
                }
            }
        }

        /// <summary>
        /// Connect to the target through ConnectivitySettins.
        /// </summary>
        /// <exception cref="InvalidOperationException">Socket already connected.</exception>
        public virtual void Connect()
        {
            if (socket != null && Connected)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Connect() called, but already a socket available.", GetType().Name);

                // If you have to fail, fail noisily and as soon as possible.
                throw new InvalidOperationException("Socket already connected.");
            }

            try
            {
                // Create a socket
                socket = GetPreparedSocket((ConnectivitySettings.LocalHost == string.Empty) ? IPAddress.Any : IPAddress.Parse(ConnectivitySettings.LocalHost), ConnectivitySettings.LocalPort);

                IPAddress hostIP = null;

                if (IPAddress.TryParse(ConnectivitySettings.Host, out hostIP))
                {
                    // start connecting				
                    ((ProxySocket)socket).BeginConnect(new System.Net.IPEndPoint(IPAddress.Parse(ConnectivitySettings.Host), ConnectivitySettings.Port), new AsyncCallback(EndConnectCallback), socket);
                }
                else
                {
                    ((ProxySocket)socket).BeginConnect(ConnectivitySettings.Host, ConnectivitySettings.Port, new AsyncCallback(EndConnectCallback), socket);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Connecting exception: " + e.ToString(), GetType().Name);

                if (ConnectingException != null)
                    ConnectingException(this, new ExceptionEventArgs(e));

                // re-throw the exception since the exception is thrown while in a blocking call
                throw; //RethrowToPreserveStackDetails (without e)
            }
        }

        public virtual void Disconnect()
        {
            // clean up the socket properly
            if (socket != null)
            {
                try
                {
                    if (Connected)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
                    socket.Close();
                }

                socket = null;
                // We don't need to call OnDisconnect here since EndReceiveCallback will be call automatically later on. (This is not valid if disconnected remotelly)
                // We need to call OnDisconnect after EndReceiveCallback if disconnected locally.
            }
        }

        public abstract void SendMessage(NetworkMessage message);
        protected abstract void OnMessageReceived(byte[] data);

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose managed resources
                Disconnect();
            }

            // Free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
};
