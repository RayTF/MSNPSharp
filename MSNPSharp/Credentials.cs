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

namespace MSNPSharp
{
    using MSNPSharp.Core;

    /// <summary>
    /// Specifies the user credentials. These settings are used when authentication
    /// is required on the network.
    /// </summary>
    /// <remarks>
    /// The client identifier, together with the client code, represents
    /// a unique way of identifying the client connected to the network.
    /// 
    /// Third party softwarehouses can request their own identifier/code combination
    /// for their software. These values have to be stored in the properties before connecting
    /// to the network.
    /// When you want to emulate the Microsoft MSN Messenger client, you can use any of the following
    /// values:
    /// <c>
    /// ClientID			ClientCode          Client Version              Acknowledgement
    /// msmsgs@msnmsgr.com	Q1P7W2E4J9R8U3S5 
    /// PROD0038W!61ZTF9	VT6PX?UQTM4WM%YR 
    /// PROD0058#7IL2{QD	QHDCY@7R1TB6W?5B 
    /// PROD0061VRRZH@4F	JXQ6J@TUOGYV@N0M
    /// PROD0119GSJUC$18    ILTXC!4IXB5FB*PX
    /// PROD0120PW!CCV9@    C1BX{V4W}Q3*10SM    WLM 2009 v14.0.8050.1202   http://twitter.com/mynetx
    /// </c>
    /// 
    /// Note that officially you must use an obtained license (client id and client code) from Microsoft in order to access the network legally!
    /// After you have received your own license you can set the client id and client code in this class.
    /// </remarks>
    [Serializable]
    public class Credentials
    {
        private ClientInfo clientInfo = new ClientInfo();
        private string password;
        private string account;

        public ClientInfo ClientInfo
        {
            get
            {
                return clientInfo;
            }
        }

        /// <summary>
        /// Msn protocol
        /// </summary>
        public MsnProtocol MsnProtocol
        {
            get
            {
                return clientInfo.MsnProtocol;
            }
        }

        /// <summary>
        /// The client identifier used to identify the clientsoftware.
        /// </summary>
        public string ClientID
        {
            get
            {
                return clientInfo.ProductID;
            }
        }

        /// <summary>
        /// The client code used to identify the clientsoftware.
        /// </summary>
        public string ClientCode
        {
            get
            {
                return clientInfo.ProductKey;
            }
        }

        /// <summary>
        /// Password for the account. Used when logging into the network.
        /// </summary>
        public string Password
        {
            get
            {
                return password;
            }
            set
            {
                password = value;
            }
        }

        /// <summary>
        /// The account the identity uses. A typical messenger account is specified as name@hotmail.com.
        /// </summary>
        public string Account
        {
            get
            {
                return account;
            }
            set
            {
                account = value;
            }
        }

        /// <summary>
        /// Constructor to instantiate a Credentials object.
        /// </summary>
        protected Credentials()
        {
        }

        public Credentials(MsnProtocol msnp)
            : this(string.Empty, string.Empty)
        {
        }

        public Credentials(string account, string password)
            : this(account, password, MsnProtocol.MSNP18)
        {
        }

        public Credentials(string account, string password, MsnProtocol msnp)
        {
            this.account = account;
            this.password = password;
            this.clientInfo = (ClientInfo)DefaultCredentials[msnp].Clone();
        }

        /// <summary>
        /// Constructor to instantiate a Credentials object with the specified values.
        /// </summary>
        public Credentials(string account, string password, string clientID, string clientCode)
            : this(account, password, clientID, clientCode, MsnProtocol.MSNP18)
        {
        }

        /// <summary>
        /// Constructor to instantiate a Credentials object with the specified values and msn protocol speaking.
        /// </summary>
        public Credentials(string account, string password, string clientID, string clientCode, MsnProtocol msnp)
            : this(account, password, msnp)
        {
            clientInfo.ProductID = clientID;
            clientInfo.ProductKey = clientCode;
        }

        static readonly Dictionary<MsnProtocol, ClientInfo> DefaultCredentials = new Dictionary<MsnProtocol, ClientInfo>();
        static Credentials()
        {
            // MSNP18
            ClientInfo msnp18 = new ClientInfo();
            msnp18.MsnProtocol = MsnProtocol.MSNP18;
            msnp18.ProductID = "PROD0120PW!CCV9@";
            msnp18.ProductKey = "C1BX{V4W}Q3*10SM";
            msnp18.MessengerClientName = "MSNMSGR";
            msnp18.MessengerClientBuildVer = "14.0.8117.0416";
            msnp18.ApplicationId = "AAD9B99B-58E6-4F23-B975-D9EC1F9EC24A";
            msnp18.MessengerClientBrand = "msmsgs";
            DefaultCredentials[msnp18.MsnProtocol] = msnp18;

        }
    }

    [Serializable]
    public struct ClientInfo : ICloneable
    {
        public MsnProtocol MsnProtocol;
        public string ApplicationId;
        public string MessengerClientBuildVer;
        public string MessengerClientName;
        public string ProductID;
        public string ProductKey;
        public string MessengerClientBrand;

        public object Clone()
        {
            ClientInfo ci = new ClientInfo();
            ci.MsnProtocol = MsnProtocol;
            ci.ApplicationId = String.Copy(ApplicationId);
            ci.MessengerClientBuildVer = String.Copy(MessengerClientBuildVer);
            ci.MessengerClientName = String.Copy(MessengerClientName);
            ci.ProductID = String.Copy(ProductID);
            ci.ProductKey = String.Copy(ProductKey);
            ci.MessengerClientBrand = MessengerClientBrand;

            return ci;
        }

    }
};
