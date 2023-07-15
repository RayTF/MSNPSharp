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
using System.Net;
using System.Web;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    public class MSNServiceCertificatePolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint srvPoint, X509Certificate certificate, WebRequest request, int certificateProblem)
        {
            return true;
        }
    }

    /// <summary>
    /// Defines the way how connections must be set up.
    /// </summary>
    /// <remarks>
    /// Via ConnectivitySettings the client can specify to which MSN server must be connected,
    /// whether or not proxy servers are used for internet connections and whether
    /// web proxys are used for accessing HTTP resources. The most common HTTP resource
    /// is the authentication with Passport.com during the login phase.
    /// </remarks>
    [Serializable()]
    public class ConnectivitySettings
    {
        #region Constructors

        static ConnectivitySettings()
        {
            try
            {
                ServicePointManager.ServerCertificateValidationCallback += delegate(object sender, X509Certificate certificate,
                    X509Chain chain, SslPolicyErrors sslPolicyErrors)
                    {
                        return true;
                    };
            }
            catch (Exception)
            {
#pragma warning disable 0618
                ServicePointManager.CertificatePolicy = new MSNServiceCertificatePolicy();
#pragma warning restore 0618
            }
        }

        /// <summary>
        /// Constructor to instantiate a ConnectivitySettings object.
        /// </summary>
        public ConnectivitySettings()
        {
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="x"></param>
        public ConnectivitySettings(ConnectivitySettings x)
            : this(x.LocalHost, x.LocalPort, x.Host, x.Port, x.ProxyHost, x.Port, x.ProxyUsername, x.ProxyPassword, x.ProxyType, x.WebProxy)
        {
        }


        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        public ConnectivitySettings(IPEndPoint remoteEndPoint)
            : this(remoteEndPoint.Address.ToString(), remoteEndPoint.Port)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="localEndPoint">Local endpoint you want to bind to.</param>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        public ConnectivitySettings(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
            : this(localEndPoint.Address.ToString(), localEndPoint.Port, remoteEndPoint.Address.ToString(), remoteEndPoint.Port)
        {
        }


        /// <summary>
        /// Constructs a ConnectivitySettings with custom host and port.
        /// </summary>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        public ConnectivitySettings(string host, int port)
            : this(host, port, string.Empty, 0, string.Empty, string.Empty, ProxyType.None, null)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings with custom host and port.
        /// </summary>
        /// <param name="localHost">The local address you want to bind to.</param>
        /// <param name="localPort">The local port you want to bind to.</param>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        public ConnectivitySettings(string localHost, int localPort, string host, int port)
            : this(localHost, localPort, host, port, string.Empty, 0, string.Empty, string.Empty, ProxyType.None, null)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(IPEndPoint remoteEndPoint, WebProxy webProxy)
            : this(remoteEndPoint.Address.ToString(), remoteEndPoint.Port, webProxy)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="localEndPoint">Local endpoint you want to bind to.</param>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, WebProxy webProxy)
            : this(localEndPoint.Address.ToString(), localEndPoint.Port, remoteEndPoint.Address.ToString(), remoteEndPoint.Port, webProxy)
        {
        }


        /// <summary>
        /// Constructs a ConnectivitySettings which uses a Web proxy for all HTTP connections made.
        /// </summary>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(string host, int port, WebProxy webProxy)
            : this(host, port, string.Empty, 0, string.Empty, string.Empty, ProxyType.None, webProxy)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a Web proxy for all HTTP connections made.
        /// </summary>
        /// <param name="localHost">The local address you want to bind to.</param>
        /// <param name="localPort">The local port you want to bind to.</param>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(string localHost, int localPort, string host, int port, WebProxy webProxy)
            : this(localHost, localPort, host, port, string.Empty, 0, string.Empty, string.Empty, ProxyType.None, webProxy)
        {
        }



        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        /// <param name="proxyEndPoint">Host endpoint of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        public ConnectivitySettings(IPEndPoint remoteEndPoint, IPEndPoint proxyEndPoint, string proxyUsername, string proxyPassword, ProxyType proxyType)
            : this(remoteEndPoint.Address.ToString(), remoteEndPoint.Port, proxyEndPoint.Address.ToString(), proxyEndPoint.Port, proxyUsername, proxyPassword, proxyType, null)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="localEndPoint">Local endpoint you want to bind to.</param>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        /// <param name="proxyEndPoint">Host endpoint of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        public ConnectivitySettings(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, IPEndPoint proxyEndPoint, string proxyUsername, string proxyPassword, ProxyType proxyType)
            : this(localEndPoint, remoteEndPoint, proxyUsername, proxyPassword, proxyType, null)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a proxy in all direct TCP communications with the messenger servers. This means HTTP resources for authenticating the user with Passport.com are accessed directly.
        /// </summary>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        /// <param name="proxyHost">Host of the proxy server</param>
        /// <param name="proxyPort">Port of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        public ConnectivitySettings(string host, int port, string proxyHost, int proxyPort, string proxyUsername, string proxyPassword, ProxyType proxyType)
            : this(host, port, proxyHost, proxyPort, proxyUsername, proxyPassword, proxyType, null)
        {

        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="localHost">The local address you want to bind to.</param>
        /// <param name="localPort">The local port you want to bind to.</param>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        /// <param name="proxyHost">Host of the proxy server</param>
        /// <param name="proxyPort">Port of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        public ConnectivitySettings(string localHost, int localPort, string host, int port, string proxyHost, int proxyPort, string proxyUsername, string proxyPassword, ProxyType proxyType)
            : this(localHost, localPort, host, port, proxyHost, proxyPort, proxyUsername, proxyPassword, proxyType, null)
        {
        }


        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        /// <param name="proxyEndPoint">Host endpoint of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(IPEndPoint remoteEndPoint, IPEndPoint proxyEndPoint, string proxyUsername, string proxyPassword, ProxyType proxyType, WebProxy webProxy)
            : this(remoteEndPoint.Address.ToString(), remoteEndPoint.Port, proxyEndPoint.Address.ToString(), proxyEndPoint.Port, proxyUsername, proxyPassword, proxyType, webProxy)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="localEndPoint">Local endpoint you want to bind to.</param>
        /// <param name="remoteEndPoint">Host endpoint of messenger server. Standard is msnmsgr.escargot.chat:1863</param>
        /// <param name="proxyEndPoint">Host endpoint of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, IPEndPoint proxyEndPoint, string proxyUsername, string proxyPassword, ProxyType proxyType, WebProxy webProxy)
            : this(localEndPoint.Address.ToString(), localEndPoint.Port, remoteEndPoint.Address.ToString(), remoteEndPoint.Port, proxyEndPoint.Address.ToString(), proxyEndPoint.Port, proxyUsername, proxyPassword, proxyType, webProxy)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        /// <param name="proxyHost">Host of the proxy server</param>
        /// <param name="proxyPort">Port of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(string host, int port, string proxyHost, int proxyPort, string proxyUsername, string proxyPassword, ProxyType proxyType, WebProxy webProxy)
            :this(string.Empty, 0, host, port, proxyHost, proxyPort, proxyUsername, proxyPassword, proxyType, webProxy)
        {
        }

        /// <summary>
        /// Constructs a ConnectivitySettings which uses a socks proxy in all direct TCP communications with the messenger servers. HTTP resources are accessed via the supplied WebProxy.
        /// </summary>
        /// <param name="localHost">The local address you want to bind to.</param>
        /// <param name="localPort">The local port you want to bind to.</param>
        /// <param name="host">Host of messenger server. Standard is msnmsgr.escargot.chat</param>
        /// <param name="port">Port of messenger server. Standard is 1863</param>
        /// <param name="proxyHost">Host of the proxy server</param>
        /// <param name="proxyPort">Port of the proxy server</param>
        /// <param name="proxyUsername">Username, if any, used when accessing the proxyserver</param>
        /// <param name="proxyPassword">Password, if any, used when accessing the proxyserver</param>
        /// <param name="proxyType">The proxy version, Socks4 or Socks5</param>
        /// <param name="webProxy">Webproxy to be used when accessing HTTP resources</param>
        public ConnectivitySettings(string localHost, int localPort, string host, int port, string proxyHost, int proxyPort, string proxyUsername, string proxyPassword, ProxyType proxyType, WebProxy webProxy)
        {
            this.localHost = localHost;
            this.localPort = localPort;
            this.host = host;
            this.port = port;
            this.proxyHost = proxyHost;
            this.proxyPort = proxyPort;
            this.proxyUsername = proxyUsername;
            this.proxyPassword = proxyPassword;
            this.proxyType = proxyType;
            this.webProxy = webProxy;
        }

        #endregion

        #region Private members

        private string host = "msnmsgr.escargot.chat";
        private int port = 1863;
        private string localHost = string.Empty;
        private int localPort = 0;
        private string proxyHost = string.Empty;
        private int proxyPort = 0;
        private string proxyUsername = string.Empty;
        private string proxyPassword = string.Empty;
        private ProxyType proxyType = ProxyType.None;
        private WebProxy webProxy = null;

        #endregion

        #region Public properties

        /// <summary>
        /// Hostname of the MSN server. Defaults is msnmsgr.escargot.chat		
        /// </summary>
        public string Host
        {
            get
            {
                return host;
            }
            set
            {
                host = value;
            }
        }

        /// <summary>
        /// Port of the MSN server. Default is 1863.
        /// </summary>
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }

        /// <summary>
        /// The local ip you want to bind.
        /// </summary>
        public string LocalHost
        {
            get 
            { 
                return localHost; 
            }
            set 
            { 
                localHost = value; 
            }
        }

        /// <summary>
        /// The local port you want to bind, default is 0 (which means the system will assign it automatically).
        /// </summary>
        public int LocalPort
        {
            get 
            { 
                return localPort; 
            }
            set 
            { 
                localPort = value; 
            }
        }

        /// <summary>
        /// The host of the proxy. This must be filled in when ProxyType is set to something else than ProxyType.None.
        /// </summary>
        public string ProxyHost
        {
            get
            {
                return proxyHost;
            }
            set
            {
                proxyHost = value;
            }
        }

        /// <summary>
        /// The port used to access the proxy. This must be filled in when ProxyType is set to something else than ProxyType.None.
        /// </summary>
        public int ProxyPort
        {
            get
            {
                return proxyPort;
            }
            set
            {
                proxyPort = value;
            }
        }

        /// <summary>
        /// The username used when accessing the proxy. This must be filled in when ProxyType is set to something else than ProxyType.None.
        /// </summary>
        public string ProxyUsername
        {
            get
            {
                return proxyUsername;
            }
            set
            {
                proxyUsername = value;
            }
        }

        /// <summary>
        /// The username used when accessing the proxy. This must be filled in when ProxyType is set to something else than ProxyType.None.
        /// </summary>
        public string ProxyPassword
        {
            get
            {
                return proxyPassword;
            }
            set
            {
                proxyPassword = value;
            }
        }

        /// <summary>
        /// The ProxyType used. If ProxyType is set to something else than ProxyType.None a proxy server is used, using Socks4 or Socks 5 depending on the type. Read-only.
        /// </summary>
        public ProxyType ProxyType
        {
            get
            {
                return proxyType;
            }
            set
            {
                proxyType = value;
            }
        }

        /// <summary>
        /// If this is not null a webproxy is used in all HTTP request in the library. An important HTTP request is the authentication with Passport.com.
        /// </summary>
        public WebProxy WebProxy
        {
            get
            {
                return webProxy;
            }
            set
            {
                webProxy = value;
            }
        }

        /// <summary>
        /// A string that shows the current host and port.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "{Host=" + Host + ", Port=" + Port + "}";
        }
        #endregion
    }
};
