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
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.Core;
    using System.IO;


    [Serializable]
    public class Owner : Contact
    {
        /// <summary>
        /// Fired when owner places changed.
        /// </summary>
        public event EventHandler<PlaceChangedEventArgs> PlacesChanged;


        private string epName = Environment.MachineName;

        private bool passportVerified;
        private PrivacyMode privacy = PrivacyMode.Unknown;
        private NotifyPrivacy notifyPrivacy = NotifyPrivacy.Unknown;
        private RoamLiveProperty roamLiveProperty = RoamLiveProperty.Unspecified;

        public Owner(string abId, string account, long cid, NSMessageHandler handler)
            : base(abId, account, ClientType.PassportMember, cid, handler)
        {
        }

        public Owner(Guid abId, string account, long cid, NSMessageHandler handler)
            : base(abId, account, ClientType.PassportMember, cid, handler)
        {
        }

        protected override void Initialized(Guid abId, string account, ClientType cliType, long cid, NSMessageHandler handler)
        {
            base.Initialized(abId, account, cliType, cid, handler);

            EndPointData.Clear();
            EndPointData.Add(Guid.Empty, new PrivateEndPointData(account, Guid.Empty));
            EndPointData.Add(NSMessageHandler.MachineGuid, new PrivateEndPointData(account, NSMessageHandler.MachineGuid));
        }

        /// <summary>
        /// Called when the End Points changed.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPlacesChanged(PlaceChangedEventArgs e)
        {
            if (PlacesChanged != null)
                PlacesChanged(this, e);
        }


        /// <summary>
        /// Fired when owner profile received.
        /// </summary>
        public event EventHandler<EventArgs> ProfileReceived;

        internal void CreateDefaultDisplayImage(SerializableMemoryStream sms)
        {
            if (sms == null)
            {
                sms = new SerializableMemoryStream();
                Image msnpsharpDefaultImage = Properties.Resources.MSNPSharp_logo.Clone() as Image;
                msnpsharpDefaultImage.Save(sms, msnpsharpDefaultImage.RawFormat);
            }

            DisplayImage displayImage = new DisplayImage(Mail.ToLowerInvariant(), sms);

            this.DisplayImage = displayImage;
        }

        internal void SetPrivacy(PrivacyMode mode)
        {
            privacy = mode;
        }

        internal void SetNotifyPrivacy(NotifyPrivacy mode)
        {
            notifyPrivacy = mode;
        }

        internal void SetRoamLiveProperty(RoamLiveProperty mode)
        {
            roamLiveProperty = mode;
        }

        internal void SetChangedPlace(Guid epId, string placeName, PlaceChangedReason action)
        {
            bool triggerEvent = false;
            lock (SyncObject)
            {
                switch (action)
                {
                    case PlaceChangedReason.SignedIn:
                        if (!EndPointData.ContainsKey(epId))
                        {
                            PrivateEndPointData newEndPoint = new PrivateEndPointData(Mail, epId);
                            newEndPoint.Name = placeName;
                            EndPointData[epId] = newEndPoint;
                            triggerEvent = true;
                        }
                        break;

                    case PlaceChangedReason.SignedOut:
                        if (EndPointData.ContainsKey(epId))
                        {
                            EndPointData.Remove(epId);
                            triggerEvent = true;
                        }
                        break;
                }

            }

            if (triggerEvent)
            {
                OnPlacesChanged(new PlaceChangedEventArgs(epId, placeName, action));
            }
        }

        /// <summary>
        /// This place's name
        /// </summary>
        public string EpName
        {
            get
            {
                return epName;
            }
            set
            {
                epName = value;

                if (NSMessageHandler != null && NSMessageHandler.IsSignedIn && Status != PresenceStatus.Offline)
                {
                    NSMessageHandler.SetPresenceStatusUUX(Status);
                }
            }
        }

        /// <summary>
        /// Get or set the <see cref="ClientCapacities"/> of local end point.
        /// </summary>
        public ClientCapacities LocalEndPointClientCapacities
        {
            get
            {
                if (EndPointData.ContainsKey(NSMessageHandler.MachineGuid))
                    return EndPointData[NSMessageHandler.MachineGuid].ClientCapacities;

                return ClientCapacities.None;
            }

            set
            {
                if (value != LocalEndPointClientCapacities)
                {
                    EndPointData[NSMessageHandler.MachineGuid].ClientCapacities = value;
                    BroadcastDisplayImage();
                }
            }
        }

        /// <summary>
        /// Get or set the <see cref="ClientCapacitiesEx"/> of local end point.
        /// </summary>
        public ClientCapacitiesEx LocalEndPointClientCapacitiesEx
        {
            get
            {
                if (EndPointData.ContainsKey(NSMessageHandler.MachineGuid))
                    return EndPointData[NSMessageHandler.MachineGuid].ClientCapacitiesEx;

                return ClientCapacitiesEx.None;
            }

            set
            {
                if (value != LocalEndPointClientCapacitiesEx)
                {
                    EndPointData[NSMessageHandler.MachineGuid].ClientCapacitiesEx = value;
                    BroadcastDisplayImage();
                }
            }
        }

        /// <summary>
        /// Sign the owner out from every place.
        /// </summary>
        public void SignoutFromEverywhere()
        {
            Status = PresenceStatus.Hidden;
            NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("UUN", new string[] { Mail, "8" }, "gtfo"));
            Status = PresenceStatus.Offline;
        }

        /// <summary>
        /// Sign the owner out from the specificed place.
        /// </summary>
        /// <param name="endPointID">The EndPoint guid to be signed out</param>
        public void SignoutFrom(Guid endPointID)
        {
            if (endPointID == Guid.Empty)
            {
                SignoutFromEverywhere();
                return;
            }

            if (EndPointData.ContainsKey(endPointID))
            {
                NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("UUN",
                    new string[] { Mail + ";" + endPointID.ToString("B").ToLowerInvariant(), "4" }, "goawyplzthxbye" + (MPOPMode == MPOP.AutoLogoff ? "-nomorempop" : String.Empty)));
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Invalid place (signed out already): " + endPointID.ToString("B"), GetType().Name);
            }
        }

        /// <summary>
        /// Owner display image. The image is broadcasted automatically.
        /// </summary>
        public override DisplayImage DisplayImage
        {
            get
            {
                return base.DisplayImage;
            }

            internal set
            {
                if (value != null)
                {
                    if (base.DisplayImage != null)
                    {
                        if (value.Sha == base.DisplayImage.Sha)
                        {
                            return;
                        }

                        MSNObjectCatalog.GetInstance().Remove(base.DisplayImage);
                    }

                    SetDisplayImageAndFireDisplayImageChangedEvent(value);
                    value.Creator = Mail;

                    MSNObjectCatalog.GetInstance().Add(base.DisplayImage);

                    BroadcastDisplayImage();
                }
            }
        }

        /// <summary>
        /// Personel message
        /// </summary>
        public new PersonalMessage PersonalMessage
        {
            get
            {
                return base.PersonalMessage;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPersonalMessage(value);
                }

                if (value != null)
                    base.SetPersonalMessage(value);
            }
        }


        internal void BroadcastDisplayImage()
        {
            if (NSMessageHandler != null && NSMessageHandler.IsSignedIn && Status != PresenceStatus.Offline && Status != PresenceStatus.Unknown)
            {
                // Resend the user status so other client can see the new msn object

                string capacities = ((long)LocalEndPointClientCapacities).ToString() + ":" + ((long)LocalEndPointClientCapacitiesEx).ToString();

                string context = "0";

                if (DisplayImage != null && DisplayImage.Image != null)
                    context = DisplayImage.Context;

                NSMessageHandler.MessageProcessor.SendMessage(new NSMessage("CHG", new string[] { NSMessageHandler.ParseStatus(Status), capacities, context }));
            }
        }

        public new string MobilePhone
        {
            get
            {
                return base.MobilePhone;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPhoneNumberMobile(value);
                }
            }
        }

        public new string WorkPhone
        {
            get
            {
                return base.WorkPhone;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPhoneNumberWork(value);
                }
            }
        }

        public new string HomePhone
        {
            get
            {
                return base.HomePhone;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPhoneNumberHome(value);
                }
            }
        }

        public new bool MobileDevice
        {
            get
            {
                return base.MobileDevice;
            }
            // it seems the server does not like it when we want to set mobile device ourselves!
            /*set 
            {
                if(nsMessageHandler != null)
                {
                    nsMessageHandler.SetMobileDevice(value);
                }
            }*/
        }

        public new bool MobileAccess
        {
            get
            {
                return base.MobileAccess;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetMobileAccess(value);
                }
            }
        }

        /// <summary>
        /// Whether this account is verified by email. If an account is not verified, "(email not verified)" will be displayed after a contact's displayname.
        /// </summary>
        public bool PassportVerified
        {
            get
            {
                return passportVerified;
            }
            internal set
            {
                passportVerified = value;
            }
        }

        public PrivacyMode Privacy
        {
            get
            {
                return privacy;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPrivacyMode(value);
                }
            }
        }

        public NotifyPrivacy NotifyPrivacy
        {
            get
            {
                return notifyPrivacy;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetNotifyPrivacyMode(value);
                }
            }
        }

        public RoamLiveProperty RoamLiveProperty
        {
            get
            {
                return roamLiveProperty;
            }
            set
            {
                roamLiveProperty = value;
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.ContactService.UpdateMe();
                }
            }
        }

        bool mpopEnabled;
        MPOP mpopMode = MPOP.Unspecified;
        string _routeInfo = string.Empty;

        /// <summary>
        /// Route address, used for PNRP??
        /// </summary>
        public string RouteInfo
        {
            get
            {
                return _routeInfo;
            }
            internal set
            {
                _routeInfo = value;
            }
        }

        /// <summary>
        /// Whether the contact list owner has Multiple Points of Presence Support (MPOP) that is owner connect from multiple places.
        /// </summary>
        public bool MPOPEnable
        {
            get
            {
                return mpopEnabled;
            }
            internal set
            {
                mpopEnabled = value;
            }
        }


        internal void SetMPOP(MPOP mpop)
        {
            MPOPMode = mpop;
        }

        /// <summary>
        /// Reaction when sign in at another place.
        /// </summary>
        public MPOP MPOPMode
        {
            get
            {
                if (mpopMode == MPOP.Unspecified && mpopEnabled)  //If unspecified, we get it from profile.
                {
                    if (NSMessageHandler != null)
                    {
                        if (NSMessageHandler.ContactService.AddressBook != null)
                        {
                            if (NSMessageHandler.ContactService.AddressBook.MyProperties.ContainsKey(AnnotationNames.MSN_IM_MPOP))
                                mpopMode = NSMessageHandler.ContactService.AddressBook.MyProperties[AnnotationNames.MSN_IM_MPOP] == "1" ? MPOP.KeepOnline : MPOP.AutoLogoff;

                        }
                    }
                }
                return mpopMode;
            }
            set
            {
                if (NSMessageHandler != null && MPOPEnable)
                {
                    mpopMode = value;
                    NSMessageHandler.ContactService.UpdateMe();
                }
            }
        }

        public override PresenceStatus Status
        {
            get
            {
                return base.Status;
            }

            set
            {
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetPresenceStatus(value);
                }

                if (PersonalMessage != null)
                {
                    (EndPointData[MachineGuid] as PrivateEndPointData).State = base.Status;
                }
                else
                {
                    (EndPointData[NSMessageHandler.MachineGuid] as PrivateEndPointData).State = base.Status;
                }
            }
        }

        public override string Name
        {
            get
            {
                return string.IsNullOrEmpty(base.Name) ? NickName : base.Name;
            }

            set
            {
                if (Name == value) return;
                if (NSMessageHandler != null)
                {
                    NSMessageHandler.SetScreenName(value);
                }
            }
        }


        #region Profile datafields
        bool validProfile;
        string loginTime;
        bool emailEnabled;
        string memberIdHigh;
        string memberIdLowd;
        string preferredLanguage;
        string preferredMail;
        string country;
        string postalCode;
        string gender;
        string kid;
        string age;
        string birthday;
        string wallet;
        string sid;
        string kV;
        string mSPAuth;
        IPAddress clientIP;
        int clientPort;

        public bool ValidProfile
        {
            get
            {
                return validProfile;
            }
            internal set
            {
                validProfile = value;
            }
        }

        public string LoginTime
        {
            get
            {
                return loginTime;
            }
            set
            {
                loginTime = value;
            }
        }

        public bool EmailEnabled
        {
            get
            {
                return emailEnabled;
            }
            set
            {
                emailEnabled = value;
            }
        }

        public string MemberIdHigh
        {
            get
            {
                return memberIdHigh;
            }
            set
            {
                memberIdHigh = value;
            }
        }

        public string MemberIdLowd
        {
            get
            {
                return memberIdLowd;
            }
            set
            {
                memberIdLowd = value;
            }
        }

        public string PreferredLanguage
        {
            get
            {
                return preferredLanguage;
            }
            set
            {
                preferredLanguage = value;
            }
        }

        public string PreferredMail
        {
            get
            {
                return preferredMail;
            }
            set
            {
                preferredMail = value;
            }
        }

        public string Country
        {
            get
            {
                return country;
            }
            set
            {
                country = value;
            }
        }

        public string PostalCode
        {
            get
            {
                return postalCode;
            }
            set
            {
                postalCode = value;
            }
        }

        public string Gender
        {
            get
            {
                return gender;
            }
            set
            {
                gender = value;
            }
        }

        public string Kid
        {
            get
            {
                return kid;
            }
            set
            {
                kid = value;
            }
        }

        public string Age
        {
            get
            {
                return age;
            }
            set
            {
                age = value;
            }
        }

        public string Birthday
        {
            get
            {
                return birthday;
            }
            set
            {
                birthday = value;
            }
        }


        public string Wallet
        {
            get
            {
                return wallet;
            }
            set
            {
                wallet = value;
            }
        }

        public string Sid
        {
            get
            {
                return sid;
            }
            set
            {
                sid = value;
            }
        }

        public string KV
        {
            get
            {
                return kV;
            }
            set
            {
                kV = value;
            }
        }

        public string MSPAuth
        {
            get
            {
                return mSPAuth;
            }
            set
            {
                mSPAuth = value;
            }
        }

        public IPAddress ClientIP
        {
            get
            {
                return clientIP;
            }
            set
            {
                clientIP = value;
            }
        }

        public int ClientPort
        {
            get
            {
                return clientPort;
            }
            set
            {
                clientPort = value;
            }
        }

        #endregion

        /// <summary>
        /// This will update the profile of the Owner object. 
        /// </summary>
        /// <remarks>This method fires the <see cref="ProfileReceived"/> event.</remarks>
        /// <param name="loginTime"></param>
        /// <param name="emailEnabled"></param>
        /// <param name="memberIdHigh"></param>
        /// <param name="memberIdLowd"></param>
        /// <param name="preferredLanguage"></param>
        /// <param name="preferredMail"></param>
        /// <param name="country"></param>
        /// <param name="postalCode"></param>
        /// <param name="gender"></param>
        /// <param name="kid"></param>
        /// <param name="age"></param>
        /// <param name="birthday"></param>
        /// <param name="wallet"></param>
        /// <param name="sid"></param>
        /// <param name="kv"></param>
        /// <param name="mspAuth"></param>
        /// <param name="clientIP"></param>
        /// <param name="clientPort"></param>
        /// <param name="nick"></param>
        /// <param name="mpop"></param>
        /// <param name="routeInfo"></param>
        internal void UpdateProfile(
            string loginTime, bool emailEnabled, string memberIdHigh,
            string memberIdLowd, string preferredLanguage, string preferredMail,
            string country, string postalCode, string gender,
            string kid, string age, string birthday,
            string wallet, string sid, string kv,
            string mspAuth, IPAddress clientIP, int clientPort,
            string nick,
            bool mpop,
            string routeInfo)
        {
            LoginTime = loginTime;
            EmailEnabled = emailEnabled;
            MemberIdHigh = memberIdHigh;
            MemberIdLowd = memberIdLowd;
            PreferredLanguage = preferredLanguage;
            PreferredMail = preferredMail;
            Country = country;
            PostalCode = postalCode;
            Gender = gender;
            Kid = kid;
            Age = age;
            Birthday = birthday;
            Wallet = wallet;
            Sid = sid;
            KV = kv;
            MSPAuth = mspAuth;
            ClientIP = clientIP;
            ClientPort = clientPort;
            MPOPEnable = mpop;
            RouteInfo = routeInfo;
            SetNickName(nick);
            ValidProfile = true;

            OnProfileReceived(EventArgs.Empty);
        }

        /// <summary>
        /// Called when the server has send a profile description.
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnProfileReceived(EventArgs e)
        {
            if (ProfileReceived != null)
                ProfileReceived(this, e);
        }

    }
};
