#region
/*
Copyright (c) 2002-2011, Bas Geertsema, Xih Solutions
(http://www.xihsolutions.net), Thiago.Sayao, Pang Wu, Ethem Evlice, Andy Phan.
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
using MSNPSharp.MSNWS.MSNABSharingService;

namespace MSNPSharp
{
    /// <summary>
    /// The <see cref="Contact"/> who send a join contact invitation.
    /// </summary>
    [Obsolete("Inviter is no more supported by Microsoft.")]
    [Serializable()]
    public class CircleInviter : Contact
    {
        private string message = string.Empty;

        /// <summary>
        /// Invitation message send via the email.
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
        }


        internal CircleInviter(ContactType inviter, string inviterMessage)
            : base(WebServiceConstants.MessengerIndividualAddressBookId, inviter.contactInfo.passportName, ClientType.PassportMember, inviter.contactInfo.CID, null)
        {
            if (inviterMessage != null)
                message = inviterMessage;
        }

        internal CircleInviter(string inviterEmail, string inviterName, string inviterMessage)
            : base(WebServiceConstants.MessengerIndividualAddressBookId, inviterEmail, ClientType.PassportMember, 0, null)
        {
            if (inviterMessage != null)
                message = inviterMessage;
        }
    }
}
