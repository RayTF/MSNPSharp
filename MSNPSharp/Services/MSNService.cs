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
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Web.Services.Protocols;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.MSNWS.MSNStorageService;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.MSNWS.MSNRSIService;
    using MSNPSharp.MSNWS.MSNOIMStoreService;
    using MSNPSharp.Services;

    #region MsnServiceState

    public class MsnServiceState
    {
        private PartnerScenario partnerScenario;
        private string methodName;
        private bool addToAsyncList;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="scenario">Partner scenario</param>
        /// <param name="method">Method name</param>
        /// <param name="async"></param>
        public MsnServiceState(PartnerScenario scenario, string method, bool async)
        {
            partnerScenario = scenario;
            methodName = method;
            addToAsyncList = async;
        }

        public PartnerScenario PartnerScenario
        {
            get
            {
                return partnerScenario;
            }
        }

        public string MethodName
        {
            get
            {
                return methodName;
            }
        }

        public bool AddToAsyncList
        {
            get
            {
                return addToAsyncList;
            }
        }
    }
    #endregion

    #region ServiceOperationEventArgs
    public class ServiceOperationEventArgs : EventArgs
    {
        private SoapHttpClientProtocol webService;
        private MsnServiceType serviceType;
        private AsyncCompletedEventArgs asyncCompletedEventArgs;

        public MsnServiceState MsnServiceState
        {
            get
            {
                return (MsnServiceState)asyncCompletedEventArgs.UserState;
            }
        }

        public SoapHttpClientProtocol WebService
        {
            get
            {
                return webService;
            }
        }

        public MsnServiceType ServiceType
        {
            get
            {
                return serviceType;
            }
        }

        public AsyncCompletedEventArgs AsyncCompletedEventArgs
        {
            get
            {
                return asyncCompletedEventArgs;
            }
        }

        public ServiceOperationEventArgs(SoapHttpClientProtocol ws, MsnServiceType st, AsyncCompletedEventArgs e)
        {
            webService = ws;
            serviceType = st;
            asyncCompletedEventArgs = e;
        }
    }

    #endregion

    #region BeforeServiceMethodEventArgs

    /// <summary>
    /// An object contains the calling information for a MSN async webservice method.
    /// </summary>
    public class BeforeRunAsyncMethodEventArgs : EventArgs
    {
        private SoapHttpClientProtocol webService;
        private MsnServiceType serviceType;
        private MsnServiceState serviceState;
        private object request;

        public SoapHttpClientProtocol WebService
        {
            get
            {
                return webService;
            }
        }

        public MsnServiceType ServiceType
        {
            get
            {
                return serviceType;
            }
        }

        public MsnServiceState ServiceState
        {
            get
            {
                return serviceState;
            }
        }

        public object Request
        {
            get
            {
                return request;
            }
        }

        /// <summary>
        /// Construct a <see cref="BeforeRunAsyncMethodEventArgs"/> object.
        /// </summary>
        /// <param name="ws">Webservice binding to call.</param>
        /// <param name="st">Service type.</param>
        /// <param name="ss">Service state object.</param>
        /// <param name="r">Webservice requst parameter.</param>
        public BeforeRunAsyncMethodEventArgs(SoapHttpClientProtocol ws, MsnServiceType st, MsnServiceState ss, object r)
        {
            webService = ws;
            serviceType = st;
            serviceState = ss;
            request = r;
        }
    }

    #endregion

    #region ServiceOperationFailedEventArgs

    public class ServiceOperationFailedEventArgs : EventArgs
    {
        private string method;
        private Exception exc;

        public ServiceOperationFailedEventArgs(string methodname, Exception ex)
        {
            method = methodname;
            exc = ex;
        }

        public string Method
        {
            get
            {
                return method;
            }
        }
        public Exception Exception
        {
            get
            {
                return exc;
            }
        }
    }

    #endregion

    /// <summary>
    /// Base class of webservice-related classes
    /// </summary>
    public abstract class MSNService
    {
        /// <summary>
        /// Redirection host for service on *.msnmsgr.escargot.chat
        /// </summary>
        public const string ContactServiceRedirectionHost = @"msnmsgr.escargot.chat";

        /// <summary>
        /// Redirection host for service on *.storage.msn.com
        /// </summary>
        public const string StorageServiceRedirectionHost = @"msnmsgr.escargot.chat";


        private WebProxy webProxy;
        private NSMessageHandler nsMessageHandler;
        private Dictionary<SoapHttpClientProtocol, MsnServiceState> asyncStates =
            new Dictionary<SoapHttpClientProtocol, MsnServiceState>(0);
        
        private Dictionary<MsnServiceState, object> asyncRequests =
            new Dictionary<MsnServiceState, object>(0);

        private MSNService()
        {
        }

        protected MSNService(NSMessageHandler nsHandler)
        {
            nsMessageHandler = nsHandler;
        }

        #region Properties

        internal NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }
        }

        public WebProxy WebProxy
        {
            get
            {
                if (NSMessageHandler.ConnectivitySettings != null && NSMessageHandler.ConnectivitySettings.WebProxy != null)
                {
                    webProxy = NSMessageHandler.ConnectivitySettings.WebProxy;
                }

                return webProxy;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Fired when request to an async webservice method failed.
        /// </summary>
        public event EventHandler<ServiceOperationFailedEventArgs> ServiceOperationFailed;

        /// <summary>
        /// Fired after asyc web service method completed.
        /// </summary>
        public event EventHandler<ServiceOperationEventArgs> AfterCompleted;

        /// <summary>
        /// Fired before asyc web service method.
        /// </summary>
        public event EventHandler<BeforeRunAsyncMethodEventArgs> BeforeRunAsyncMethod;

        #endregion

        #region CreateService

        protected internal SoapHttpClientProtocol CreateService(MsnServiceType serviceType, MsnServiceState asyncObject)
        {
            SoapHttpClientProtocol service = null;
            IPEndPoint localEndPoint = new IPEndPoint(String.IsNullOrEmpty(NSMessageHandler.ConnectivitySettings.LocalHost) ? IPAddress.Any : IPAddress.Parse(NSMessageHandler.ConnectivitySettings.LocalHost), NSMessageHandler.ConnectivitySettings.LocalPort);

            switch (serviceType)
            {
                case MsnServiceType.AB:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Contact);
                    ABServiceBinding abService = new ABServiceBindingWrapper(localEndPoint);
                    abService.Proxy = WebProxy;
                    abService.Timeout = Int32.MaxValue;
                    abService.UserAgent = Properties.Resources.WebServiceUserAgent;
                    abService.ABApplicationHeaderValue = new ABApplicationHeader();
                    abService.ABApplicationHeaderValue.ApplicationId = NSMessageHandler.Credentials.ClientInfo.ApplicationId;
                    abService.ABApplicationHeaderValue.IsMigration = false;
                    abService.ABApplicationHeaderValue.PartnerScenario = Convert.ToString(asyncObject.PartnerScenario);
                    abService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
                    abService.ABAuthHeaderValue = new ABAuthHeader();
                    abService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
                    abService.ABAuthHeaderValue.ManagedGroupRequest = false;

                    service = abService;
                    break;

                case MsnServiceType.Sharing:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Contact);

                    SharingServiceBinding sharingService = new SharingServiceBindingWrapper(localEndPoint);
                    sharingService.Proxy = WebProxy;
                    sharingService.Timeout = Int32.MaxValue;
                    sharingService.UserAgent = Properties.Resources.WebServiceUserAgent;
                    sharingService.ABApplicationHeaderValue = new ABApplicationHeader();
                    sharingService.ABApplicationHeaderValue.ApplicationId = NSMessageHandler.Credentials.ClientInfo.ApplicationId;
                    sharingService.ABApplicationHeaderValue.IsMigration = false;
                    sharingService.ABApplicationHeaderValue.PartnerScenario = Convert.ToString(asyncObject.PartnerScenario);
                    sharingService.ABApplicationHeaderValue.BrandId = NSMessageHandler.MSNTicket.MainBrandID;
                    sharingService.ABApplicationHeaderValue.CacheKey = NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey];
                    sharingService.ABAuthHeaderValue = new ABAuthHeader();
                    sharingService.ABAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Contact].Ticket;
                    sharingService.ABAuthHeaderValue.ManagedGroupRequest = false;

                    service = sharingService;
                    break;

                case MsnServiceType.Storage:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Storage);

                    StorageService storageService = new StorageServiceWrapper(localEndPoint);
                    storageService.Proxy = WebProxy;
                    storageService.StorageApplicationHeaderValue = new StorageApplicationHeader();
                    storageService.StorageApplicationHeaderValue.ApplicationID = Properties.Resources.ApplicationStrId;


                    storageService.StorageApplicationHeaderValue.Scenario = Convert.ToString(asyncObject.PartnerScenario);
                    storageService.StorageUserHeaderValue = new StorageUserHeader();
                    storageService.StorageUserHeaderValue.Puid = 0;
                    storageService.StorageUserHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Storage].Ticket;
                    storageService.AffinityCacheHeaderValue = new AffinityCacheHeader();
                    storageService.AffinityCacheHeaderValue.CacheKey = NSMessageHandler.ContactService.Deltas.CacheKeys.ContainsKey(CacheKeyType.StorageServiceCacheKey)
                        ? NSMessageHandler.ContactService.Deltas.CacheKeys[CacheKeyType.StorageServiceCacheKey] : String.Empty;

                    service = storageService;
                    break;

                case MsnServiceType.RSI:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.Web);

                    string[] TandP = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Web].Ticket.Split(new string[] { "t=", "&p=" }, StringSplitOptions.None);

                    RSIService rsiService = new RSIServiceWrapper(localEndPoint);
                    rsiService.Proxy = WebProxy;
                    rsiService.Timeout = Int32.MaxValue;
                    rsiService.PassportCookieValue = new PassportCookie();
                    rsiService.PassportCookieValue.t = TandP[1];
                    rsiService.PassportCookieValue.p = TandP[2];

                    service = rsiService;
                    break;

                case MsnServiceType.OIMStore:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.OIM);

                    OIMStoreService oimService = new OIMStoreServiceWrapper(localEndPoint);
                    oimService.Proxy = WebProxy;
                    oimService.TicketValue = new Ticket();
                    oimService.TicketValue.passport = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.OIM].Ticket;
                    oimService.TicketValue.lockkey = NSMessageHandler.MSNTicket.OIMLockKey;
                    oimService.TicketValue.appid = NSMessageHandler.Credentials.ClientID;

                    service = oimService;
                    break;

                case MsnServiceType.WhatsUp:

                    SingleSignOnManager.RenewIfExpired(NSMessageHandler, SSOTicketType.WhatsUp);

                    WhatsUpServiceBinding wuService = new WhatsUpServiceBindingWrapper(localEndPoint);
                    wuService.Proxy = WebProxy;
                    wuService.Timeout = 60000;
                    wuService.UserAgent = Properties.Resources.WebServiceUserAgent;
                    wuService.Url = "http://msnmsgr.escargot.chat/whatsnew/whatsnewservice.asmx";
                    wuService.WNApplicationHeaderValue = new WNApplicationHeader();
                    wuService.WNApplicationHeaderValue.ApplicationId = Properties.Resources.WhatsupServiceAppID;
                    wuService.WNAuthHeaderValue = new WNAuthHeader();
                    wuService.WNAuthHeaderValue.TicketToken = NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.WhatsUp].Ticket;

                    service = wuService;
                    break;
            }

            if (service != null)
            {
                service.EnableDecompression = Settings.EnableGzipCompressionForWebServices;

                if (asyncObject != null && asyncObject.AddToAsyncList)
                {
                    lock (asyncStates)
                        asyncStates[service] = asyncObject;
                }
            }

            return service;
        }

        #endregion

        /// <summary>
        /// Call an async webservice method by using the specific info.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void RunAsyncMethod(BeforeRunAsyncMethodEventArgs e)
        {
            if (e.ServiceState.AddToAsyncList)
            {
                if (BeforeRunAsyncMethod != null)
                {
                    BeforeRunAsyncMethod(this, e);
                }

                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(e.WebService, e.ServiceType, e.ServiceState, e.Request);

                lock (asyncRequests)
                {
                    asyncRequests[e.ServiceState] = e.Request;
                }

                
                // Run async method now
                e.WebService.GetType().InvokeMember(
                    e.ServiceState.MethodName + "Async",
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    e.WebService,
                    new object[] { e.Request, e.ServiceState }
                );
            }
        }

        protected void ChangeCacheKeyAndPreferredHostForSpecifiedMethod(SoapHttpClientProtocol ws, MsnServiceType st, MsnServiceState ss, object request)
        {
            if (st == MsnServiceType.AB ||
                st == MsnServiceType.Sharing ||
                st == MsnServiceType.Storage)
            {
                DeltasList deltas = NSMessageHandler.ContactService.Deltas;
                if (deltas == null)
                {
                    throw new MSNPSharpException("Deltas is null.");
                }

                string methodName = ss.MethodName;
                string preferredHostKey = ws.ToString() + "." + methodName;
                CacheKeyType keyType = (st == MsnServiceType.Storage) ? CacheKeyType.StorageServiceCacheKey : CacheKeyType.OmegaContactServiceCacheKey;

                string originalUrl = ws.Url;
                string originalHost = FetchHost(ws.Url);
                bool needRequest = false;

                lock (deltas.SyncObject)
                {
                    needRequest = (deltas.CacheKeys.ContainsKey(keyType) == false ||
                                   deltas.CacheKeys[keyType] == string.Empty ||
                                   (deltas.CacheKeys[keyType] != string.Empty &&
                                   (deltas.PreferredHosts.ContainsKey(preferredHostKey) == false ||
                                    deltas.PreferredHosts[preferredHostKey] == String.Empty)));
                }

                if(needRequest)
                {

                    try
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, ws.GetType().ToString() + " is requesting a cachekey and preferred host for calling " + methodName);

                        switch (keyType)
                        {
                            case CacheKeyType.OmegaContactServiceCacheKey:
                                ws.Url = ws.Url.Replace(originalHost, MSNService.ContactServiceRedirectionHost);
                                break;
                            case CacheKeyType.StorageServiceCacheKey:
                                ws.Url = ws.Url.Replace(originalHost, MSNService.StorageServiceRedirectionHost);
                                break;
                        }

                        ws.GetType().InvokeMember(methodName, System.Reflection.BindingFlags.InvokeMethod,
                            null, ws,
                            new object[] { request });
                    }
                    catch (Exception ex)
                    {
                        bool getHost = false;
                        if (ex.InnerException is WebException && ex.InnerException != null)
                        {
                            WebException webException = ex.InnerException as WebException;
                            HttpWebResponse webResponse = webException.Response as HttpWebResponse;

                            if (webResponse != null)
                            {
                                if (webResponse.StatusCode == HttpStatusCode.Moved ||
                                    webResponse.StatusCode == HttpStatusCode.MovedPermanently ||
                                    webResponse.StatusCode == HttpStatusCode.Redirect ||
                                    webResponse.StatusCode == HttpStatusCode.RedirectKeepVerb)
                                {
                                    string redirectUrl = webResponse.Headers[HttpResponseHeader.Location];
                                    if (!string.IsNullOrEmpty(redirectUrl))
                                    {
                                        getHost = true;

                                        lock (deltas.SyncObject)
                                        {
                                            deltas.PreferredHosts[preferredHostKey] = FetchHost(redirectUrl);
                                            deltas.Save();
                                        }
                                        
                                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Get redirect URL by HTTP error succeed, method " + methodName + ":\r\n " +
                                            "Original: " + FetchHost(ws.Url) + "\r\n " +
                                            "Redirect: " + FetchHost(redirectUrl) + "\r\n");
                                    }

                                    #region Fetch CacheKey

                                    try
                                    {
                                        XmlDocument errdoc = new XmlDocument();
                                        string errorMessage = ex.InnerException.Message;
                                        string xmlstr = errorMessage.Substring(errorMessage.IndexOf("<?xml"));
                                        xmlstr = xmlstr.Substring(0, xmlstr.IndexOf("</soap:envelope>", StringComparison.InvariantCultureIgnoreCase) + "</soap:envelope>".Length);

                                        //I think the xml parser microsoft used internally is just a super parser, it can ignore everything.
                                        xmlstr = xmlstr.Replace("&amp;", "&");
                                        xmlstr = xmlstr.Replace("&", "&amp;");

                                        errdoc.LoadXml(xmlstr);

                                        XmlNodeList findnodelist = errdoc.GetElementsByTagName("CacheKey");
                                        if (findnodelist.Count > 0)
                                        {
                                            deltas.CacheKeys[keyType] = findnodelist[0].InnerText;
                                        }
                                    }
                                    catch (Exception exc)
                                    {
                                        Trace.WriteLineIf(
                                            Settings.TraceSwitch.TraceError,
                                            "An error occured while getting CacheKey:\r\n" +
                                            "Service:    " + ws.GetType().ToString() + "\r\n" +
                                            "MethodName: " + methodName + "\r\n" +
                                            "Message:    " + exc.Message);

                                    }

                                    #endregion
                                }
                            }
                        }

                        if (!getHost)
                        {
                            Trace.WriteLineIf(
                                Settings.TraceSwitch.TraceError,
                                "An error occured while getting CacheKey and Preferred host:\r\n" +
                                "Service:    " + ws.GetType().ToString() + "\r\n" +
                                "MethodName: " + methodName + "\r\n" +
                                "Message:    " + ex.Message);
                            lock (deltas.SyncObject)
                                deltas.PreferredHosts[preferredHostKey] = originalHost; //If there's an error, we must set the host back to its original value.
                        }

                    }
                    deltas.Save();
                }

                lock (deltas.SyncObject)
                {
                    if (originalHost != null && originalHost != String.Empty)
                    {
                        if (deltas.PreferredHosts.ContainsKey(preferredHostKey))
                        {
                            ws.Url = originalUrl.Replace(originalHost, FetchHost(deltas.PreferredHosts[preferredHostKey]));
                        }
                        else
                        {
                            //This means the redirection URL returns respond content.
                            lock (deltas.SyncObject)
                            {
                                deltas.PreferredHosts[preferredHostKey] = ws.Url;
                                deltas.Save();
                            }

                            Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "The redirect URL returns correct result, use " + ws.Url + " for " + preferredHostKey);
                        }
                    }

                    // Set cache key
                    if (st == MsnServiceType.AB)
                    {
                        ((ABServiceBinding)ws).ABApplicationHeaderValue.CacheKey = deltas.CacheKeys[keyType];
                    }
                    else if (st == MsnServiceType.Sharing)
                    {
                        ((SharingServiceBinding)ws).ABApplicationHeaderValue.CacheKey = deltas.CacheKeys[keyType];
                    }
                    else if (st == MsnServiceType.Storage)
                    {
                        ((StorageService)ws).AffinityCacheHeaderValue.CacheKey = deltas.CacheKeys[keyType];
                    }
                }
            }
        }

        private string FetchHost(string fullUrl)
        {
            string httpsHeader = "https://";
            string httpHeader = "http://";
            string sp = "/";
            string host = fullUrl;

            if (fullUrl.StartsWith(httpsHeader))
            {
                host = fullUrl.Substring(httpsHeader.Length);
            }

            if (fullUrl.StartsWith(httpHeader))
            {
                host = fullUrl.Substring(httpHeader.Length);
            }

            int spIndex = host.IndexOf(sp);

            if (spIndex > 0)
                host = host.Substring(0, spIndex);

            return host;
        }

        private void HandleServiceHeader(SoapHttpClientProtocol ws, MsnServiceType st, MsnServiceState ss)
        {
            ServiceHeader sh = (st == MsnServiceType.AB) ?
                                ((ABServiceBinding)ws).ServiceHeaderValue :
                                ((SharingServiceBinding)ws).ServiceHeaderValue;

            if (null != sh &&
                NSMessageHandler.ContactService != null &&
                NSMessageHandler.ContactService.Deltas != null)
            {
                if (sh.CacheKeyChanged)
                {
                    NSMessageHandler.MSNTicket.CacheKeys[CacheKeyType.OmegaContactServiceCacheKey] = sh.CacheKey;
                }

                lock (NSMessageHandler.ContactService.Deltas.SyncObject)
                {
                    /*
                    if (!String.IsNullOrEmpty(sh.PreferredHostName))
                    {
                        string methodKey = ws.ToString() + "." + ss.MethodName;
                        string preferredHost = FetchHost(sh.PreferredHostName);
                        if (NSMessageHandler.ContactService.Deltas.PreferredHosts[methodKey] == preferredHost)
                            return;

                        Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "Update redirect URL by response succeed, method " + ss.MethodName + ":\r\n " +
                                        "Original: " + FetchHost(ws.Url) + "\r\n " +
                                        "Redirect: " + preferredHost + "\r\n");

                        NSMessageHandler.ContactService.Deltas.PreferredHosts[methodKey] = preferredHost;
                    }*/

                    NSMessageHandler.ContactService.Deltas.Save();
                }
            }
        }

        protected virtual void OnAfterCompleted(ServiceOperationEventArgs e)
        {
            object request = null;
            lock (asyncRequests)
            {
                if (asyncRequests.ContainsKey(e.MsnServiceState))
                {
                    request = asyncRequests[e.MsnServiceState];
                    asyncRequests.Remove(e.MsnServiceState);
                }
            }

            
            if (e.MsnServiceState.AddToAsyncList)
            {
                lock (asyncStates)
                    asyncStates.Remove(e.WebService);
            }

            if (e.AsyncCompletedEventArgs.Cancelled)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo,
                    "Async method cancelled:\r\n" +
                    "Service:         " + e.WebService.ToString() + "\r\n" +
                    "MethodName:      " + e.MsnServiceState.MethodName + "\r\n" +
                    "PartnerScenario: " + e.MsnServiceState.PartnerScenario);
            }
            else if (e.AsyncCompletedEventArgs.Error != null)
            {
                BeforeRunAsyncMethodEventArgs reinvokeArgs = null;
                if (e.AsyncCompletedEventArgs.Error is WebException)
                {
                    WebException webException = e.AsyncCompletedEventArgs.Error as WebException;
                    HttpWebResponse webResponse = webException.Response as HttpWebResponse;

                    if (webResponse != null && request != null)
                    {
                        if (webResponse.StatusCode == HttpStatusCode.MovedPermanently)
                        {
                            DeltasList deltas = NSMessageHandler.ContactService.Deltas;
                            if (deltas == null)
                            {
                                throw new MSNPSharpException("Deltas is null.");
                            }

                            string redirctURL = webResponse.Headers[HttpResponseHeader.Location];
                            string preferredHostKey = e.WebService.ToString() + "." + e.MsnServiceState.MethodName;

                            lock (deltas.SyncObject)
                            {
                                deltas.PreferredHosts[preferredHostKey] = FetchHost(redirctURL);
                                deltas.Save();
                            }

                            e.WebService.Url = redirctURL;

                            reinvokeArgs = new BeforeRunAsyncMethodEventArgs(e.WebService, e.ServiceType, e.MsnServiceState, request);
                        }
                    }
                }

                if (reinvokeArgs == null)
                {
                    OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs(e.MsnServiceState.MethodName, e.AsyncCompletedEventArgs.Error));
                }
                else
                {
                    RunAsyncMethod(reinvokeArgs);
                }
   
            }
            else
            {
                // HandleServiceHeader
                if (NSMessageHandler.MSNTicket != MSNTicket.Empty &&
                    (e.ServiceType == MsnServiceType.AB || e.ServiceType == MsnServiceType.Sharing))
                {
                    HandleServiceHeader(e.WebService, e.ServiceType, e.MsnServiceState);
                }

                // Fire event
                if (AfterCompleted != null)
                {
                    AfterCompleted(this, e);
                }
            }
        }

        /// <summary>
        /// Fires ServiceOperationFailed event.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnServiceOperationFailed(object sender, ServiceOperationFailedEventArgs e)
        {
            if (ServiceOperationFailed != null)
                ServiceOperationFailed(sender, e);
        }


        private void CancelAndDisposeAysncMethods()
        {
            if (asyncStates.Count > 0)
            {
                lock (this)
                {
                    if (asyncStates.Count > 0)
                    {
                        Dictionary<SoapHttpClientProtocol, MsnServiceState> copyStates = new Dictionary<SoapHttpClientProtocol, MsnServiceState>(asyncStates);
                        asyncStates = new Dictionary<SoapHttpClientProtocol, MsnServiceState>();
                        asyncRequests = new Dictionary<MsnServiceState, object>();

                        foreach (KeyValuePair<SoapHttpClientProtocol, MsnServiceState> state in copyStates)
                        {
                            try
                            {
                                state.Key.GetType().InvokeMember("CancelAsync",
                                    System.Reflection.BindingFlags.InvokeMethod,
                                    null, state.Key,
                                    new object[] { state.Value });
                            }
                            catch (Exception error)
                            {
                                System.Diagnostics.Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                                        "An error occured while canceling :\r\n" +
                                        "Service: " + state.Key.ToString() + "\r\n" +
                                        "State:   " + state.Value.MethodName + "(" + state.Value.GetHashCode() + ")r\n" +
                                        "Message: " + error.Message);
                            }
                            finally
                            {
                                state.Key.Dispose();
                            }
                        }
                        copyStates.Clear();
                    }
                }
            }
        }

        public virtual void Clear()
        {
            CancelAndDisposeAysncMethods();
        }
    }
};
