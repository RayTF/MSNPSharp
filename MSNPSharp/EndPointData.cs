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

namespace MSNPSharp
{
    /// <summary>
    /// Represent the information of a certain endpoint.
    /// </summary>
    [Serializable]
    public class EndPointData
    {
        #region Fields and Properties

        private Guid id = Guid.Empty;

        /// <summary>
        /// The Id of endpoint data.
        /// </summary>
        public Guid Id
        {
            get
            {
                return id;
            }
        }

        private ClientCapacities clientCapacities = ClientCapacities.None;

        /// <summary>
        /// The capacities of the client at this enpoint.
        /// </summary>
        public ClientCapacities ClientCapacities
        {
            get
            {
                return clientCapacities;
            }

            internal set
            {
                clientCapacities = value;
            }
        }

        private ClientCapacitiesEx clientCapacitiesEx = ClientCapacitiesEx.None;

        /// <summary>
        /// The new capacities of the client at this enpoint.
        /// </summary>
        public ClientCapacitiesEx ClientCapacitiesEx
        {
            get
            {
                return clientCapacitiesEx;
            }
            internal set
            {
                clientCapacitiesEx = value;
            }
        }

        private string account = string.Empty;

        /// <summary>
        /// The account of this endpoint, different endpoints might share the same account.
        /// </summary>
        public string Account
        {
            get
            {
                return account;
            }
        }

        #endregion

        protected EndPointData()
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="epId">The endpoint Id.</param>
        public EndPointData(string account, Guid epId)
        {
            this.id = epId;
            this.account = account.ToLowerInvariant();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="uniqueEPIDString">The string represents the endpoint with the format [account];[EndPoingGUID]</param>
        public EndPointData(string uniqueEPIDString)
        {
            account = GetAccountFromUniqueEPIDString(uniqueEPIDString);
            id = GetEndPointIDFromUniqueEPIDString(uniqueEPIDString);
        }

        public static string GetAccountFromUniqueEPIDString(string uniqueEPIDString)
        {
            string[] accountGuid = uniqueEPIDString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return accountGuid[0].ToLowerInvariant();
        }

        public static Guid GetEndPointIDFromUniqueEPIDString(string uniqueEPIDString)
        {
            string[] accountGuid = uniqueEPIDString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                if (accountGuid.Length > 0)
                {
                    return new Guid(accountGuid[1]);
                }
                else
                {
                    return Guid.Empty;
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "GetEndPointIDFromUniqueEPIDString error, empty GUID returned. StackTrace: " + ex.StackTrace);
                return Guid.Empty;
            }
        }

        public override string ToString()
        {
            return Account + ";" + Id.ToString("B") + " " + ClientCapacities + ":" + ClientCapacitiesEx;
        }
    }

    [Serializable]
    public class PrivateEndPointData : EndPointData
    {
        private string name = string.Empty;
        public const string EveryWherePlace = "Everywhere";

        /// <summary>
        /// The EpName xml node of UBX command payload.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        private string clientType = string.Empty;

        /// <summary>
        /// The ClientType xml node of UBX command payload.
        /// </summary>
        public string ClientType
        {
            get
            {
                return clientType;
            }
            set
            {
                clientType = value;
            }
        }

        private bool idle = false;

        /// <summary>
        /// The Idle xml node of UBX command payload.
        /// </summary>
        public bool Idle
        {
            get
            {
                return idle;
            }
            set
            {
                idle = value;
            }
        }

        private PresenceStatus state = PresenceStatus.Unknown;

        public PresenceStatus State
        {
            get
            {
                return state;
            }
            set
            {
                state = value;
            }
        }

        public PrivateEndPointData(string account, Guid epId)
            : base(account, epId)
        {
            if (epId == NSMessageHandler.MachineGuid)
            {
                Name = Environment.MachineName;
            }

            if (epId == Guid.Empty)
            {
                Name = EveryWherePlace;
            }
        }

        public PrivateEndPointData(string uniqueEPIDString)
            : base(uniqueEPIDString)
        {
        }
    }
};
