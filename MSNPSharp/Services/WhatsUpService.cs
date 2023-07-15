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
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;

    public class GetWhatsUpCompletedEventArgs : EventArgs
    {
        private Exception error = null;
        private GetContactsRecentActivityResultType response = null;

        /// <summary>
        /// InnerException
        /// </summary>
        public Exception Error
        {
            get
            {
                return error;
            }
        }

        public GetContactsRecentActivityResultType Response
        {
            get
            {
                return response;
            }
        }

        protected GetWhatsUpCompletedEventArgs()
        {
        }

        public GetWhatsUpCompletedEventArgs(Exception err, GetContactsRecentActivityResultType resp)
        {
            error = err;
            response = resp;
        }
    }


    public class WhatsUpService : MSNService
    {

        private string feedUrl = string.Empty;

        /// <summary>
        /// RSS feed url for what's up service.
        /// </summary>
        public string FeedUrl
        {
            get
            {
                return feedUrl;
            }
        }

        public event EventHandler<GetWhatsUpCompletedEventArgs> GetWhatsUpCompleted;


        public WhatsUpService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
        }

        public void GetWhatsUp()
        {
            GetWhatsUp(50);
        }

        /// <summary>
        /// Get the recent activities of your contacts.
        /// </summary>
        /// <param name="count">Max activity count, must be larger than zero and less than 200.</param>
        public void GetWhatsUp(int count)
        {
            if (count > 200)
            {
                count = 200;
            }
            else if (count < 0)
            {
                count = 50;
            }

            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                MsnServiceState getContactsRecentActivityObject = new MsnServiceState(PartnerScenario.None, "GetContactsRecentActivity", true);
                WhatsUpServiceBinding wuService = (WhatsUpServiceBinding)CreateService(MsnServiceType.WhatsUp, getContactsRecentActivityObject);
                wuService.GetContactsRecentActivityCompleted += delegate(object sender, GetContactsRecentActivityCompletedEventArgs e)
                {
                    OnAfterCompleted(new ServiceOperationEventArgs(wuService, MsnServiceType.WhatsUp, e));

                    if (NSMessageHandler.MSNTicket == MSNTicket.Empty)
                        return;

                    if (e.Cancelled)
                        return;

                    if (e.Error != null)
                    {
                        OnGetWhatsUpCompleted(this, new GetWhatsUpCompletedEventArgs(e.Error, null));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, e.Error.Message, GetType().Name);
                        return;
                    }

                    if (e.Result.GetContactsRecentActivityResult != null)
                    {
                        feedUrl = e.Result.GetContactsRecentActivityResult.FeedUrl;
                        OnGetWhatsUpCompleted(this, new GetWhatsUpCompletedEventArgs(null, e.Result.GetContactsRecentActivityResult));
                    }
                    else
                    {
                        OnGetWhatsUpCompleted(this, new GetWhatsUpCompletedEventArgs(null, null));
                    }
                };

                GetContactsRecentActivityRequestType request = new GetContactsRecentActivityRequestType();
                request.entityHandle = new entityHandle();
                request.entityHandle.Cid = Convert.ToInt64(NSMessageHandler.ContactList.Owner.CID);
                request.locales = new string[] { System.Globalization.CultureInfo.CurrentCulture.Name };
                request.count = count;

                RunAsyncMethod(new BeforeRunAsyncMethodEventArgs(wuService, MsnServiceType.WhatsUp, getContactsRecentActivityObject, request));
            }
        }

        protected virtual void OnGetWhatsUpCompleted(object sender, GetWhatsUpCompletedEventArgs e)
        {
            if (GetWhatsUpCompleted != null)
            {
                GetWhatsUpCompleted(sender, e);
            }
        }
    }
};
