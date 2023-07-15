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
using MSNPSharp.MSNWS.MSNABSharingService;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    /// <summary>
    /// A new type of group introduces with WLM2009.
    /// </summary>
    [Serializable()]
    public partial class Circle : Contact
    {
        private ContactList contactList = null;
        private string hostDomain = CircleString.DefaultHostDomain;
        private long segmentCounter = 0;

        private ContactType meContact = null;

        private ABFindContactsPagedResultTypeAB abinfo = null;


        public string HostDomain
        {
            get { return hostDomain; }
        }

        public ContactList ContactList
        {
            get
            {
                return contactList;
            }
        }

        /// <summary>
        /// The last change time of circle's addressbook.
        /// </summary>
        public string LastChanged
        {
            get
            {
                if (abinfo == null)
                    return WebServiceConstants.ZeroTime;

                return abinfo.lastChange;
            }
        }

        /// <summary>
        /// Circle constructor
        /// </summary>
        /// <param name="me">The "Me Contact" in the addressbook.</param>
        /// <param name="circleInfo"></param>
        /// <param name="handler"></param>
        public Circle(ContactType me, CircleInverseInfoType circleInfo, NSMessageHandler handler)
            : base(circleInfo.Content.Handle.Id.ToLowerInvariant(), circleInfo.Content.Handle.Id.ToLowerInvariant() + "@" + circleInfo.Content.Info.HostedDomain.ToLowerInvariant(), ClientType.CircleMember, me.contactInfo.CID, handler)
        {
            hostDomain = circleInfo.Content.Info.HostedDomain.ToLowerInvariant();

            CircleRole = (CirclePersonalMembershipRole)Enum.Parse(typeof(CirclePersonalMembershipRole), circleInfo.PersonalInfo.MembershipInfo.CirclePersonalMembership.Role);

            SetName(circleInfo.Content.Info.DisplayName);
            SetNickName(Name);

            meContact = me;

            CID = 0;

            contactList = new ContactList(AddressBookId, new Owner(AddressBookId, me.contactInfo.passportName, me.contactInfo.CID, NSMessageHandler), NSMessageHandler);
            Initialize();
        }

        private void CheckValidation()
        {
            if (NSMessageHandler == null)
                throw new MSNPSharpException("NSMessagehandler is null");
            if (!NSMessageHandler.IsSignedIn)
                throw new InvalidOperationException("Cannot send a message without signning in to the server. Please sign in first.");

            if (NSMessageHandler.ContactList.Owner.Status == PresenceStatus.Hidden)
                throw new InvalidOperationException("Cannot send a message when you are in 'Hidden' status.");
        }

        private string ConstructSDGScheme()
        {
            string from = ((int)NSMessageHandler.ContactList.Owner.ClientType).ToString() + ":" +
                NSMessageHandler.ContactList.Owner.Mail +
                ";epid=" + NSMessageHandler.ContactList.Owner.MachineGuid.ToString("B").ToLowerInvariant();


            string to = ((int)ClientType).ToString() + ":" + Mail + ";path=IM";

            string routingInfo = CircleString.RoutingScheme.Replace(CircleString.ToReplacementTag, to);
            routingInfo = routingInfo.Replace(CircleString.FromReplacementTag, from);

            string reliabilityInfo = CircleString.ReliabilityScheme.Replace(CircleString.StreamReplacementTag, "0");
            reliabilityInfo = reliabilityInfo.Replace(CircleString.SegmentReplacementTag, IncreaseSegmentCounter().ToString());

            string putCommandString = CircleString.CircleMessageScheme;
            putCommandString = putCommandString.Replace(CircleString.RoutingSchemeReplacementTag, routingInfo);
            putCommandString = putCommandString.Replace(CircleString.ReliabilitySchemeReplacementTag, reliabilityInfo);

            return putCommandString;
        }

        /// <summary>
        /// Send nudge to all members in this circle.
        /// </summary>
        /// <exception cref="MSNPSharpException">NSMessageHandler is null</exception>
        /// <exception cref="InvalidOperationException">Not sign in to the server, or in <see cref="PresenceStatus.Hidden"/> status.</exception>
        public void SendNudge()
        {
            CheckValidation();
            string scheme = ConstructSDGScheme();

            scheme = scheme.Replace(CircleString.MessageSchemeReplacementTag, CircleString.NudgeMessageScheme);

            NSPayLoadMessage nspayload = new NSPayLoadMessage("SDG", scheme);
            NSMessageHandler.MessageProcessor.SendMessage(nspayload);
        }

        /// <summary>
        /// Send a text message to all members in this circle.
        /// </summary>
        /// <param name="textMessage"></param>
        /// <exception cref="MSNPSharpException">NSMessageHandler is null</exception>
        /// <exception cref="InvalidOperationException">Not sign in to the server, or in <see cref="PresenceStatus.Hidden"/> status.</exception>
        public void SendMessage(TextMessage textMessage)
        {
            CheckValidation();

            string scheme = ConstructSDGScheme();

            textMessage.PrepareMessage();

            string content = MimeHeaderStrings.X_MMS_IM_Format + ": " + textMessage.GetStyleString() + "\r\n\r\n" + textMessage.Text;
            string textMessageScheme = CircleString.TextMessageScheme.Replace(CircleString.TextMessageContentReplacementTag, content);
            textMessageScheme = textMessageScheme.Replace(CircleString.ContentLengthReplacementTag, textMessage.Text.Length.ToString());

            scheme = scheme.Replace(CircleString.MessageSchemeReplacementTag, textMessageScheme);

            NSPayLoadMessage nspayload = new NSPayLoadMessage("SDG", scheme);
            NSMessageHandler.MessageProcessor.SendMessage(nspayload);

        }

        /// <summary>
        /// Send a typing message indicates that you are typing to all members in this circle.
        /// </summary>
        /// <exception cref="MSNPSharpException">NSMessageHandler is null</exception>
        /// <exception cref="InvalidOperationException">Not sign in to the server, or in <see cref="PresenceStatus.Hidden"/> status.</exception>
        public void SendTypingMessage()
        {
            CheckValidation();
            string scheme = ConstructSDGScheme();

            string typingScheme = CircleString.TypingMessageScheme.Replace(CircleString.OwnerReplacementTag, NSMessageHandler.ContactList.Owner.Mail);
            scheme = scheme.Replace(CircleString.MessageSchemeReplacementTag, typingScheme);

            NSPayLoadMessage nspayload = new NSPayLoadMessage("SDG", scheme);
            NSMessageHandler.MessageProcessor.SendMessage(nspayload);
        }

        internal long IncreaseSegmentCounter()
        {
            return segmentCounter++;
        }

        /// <summary>
        /// Get a specific contact from circle's contact list by the information provided.
        /// </summary>
        /// <param name="account">The contact information</param>
        /// <param name="option">The parse option for the account parameter</param>
        /// <returns></returns>
        internal Contact GetMember(string account, AccountParseOption option)
        {
            string lowerAccount = account.ToLowerInvariant();
            try
            {
                switch (option)
                {
                    case AccountParseOption.ParseAsClientTypeAndAccount:
                        {
                            string[] typeAccount = lowerAccount.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (typeAccount.Length >= 2)
                            {
                                ClientType type = (ClientType)(int.Parse(typeAccount[0]));
                                string mail = typeAccount[1];
                                if (HasMember(mail, type))
                                {
                                    return ContactList.GetContact(mail, type);
                                }

                                return null;

                            }
                        }
                        break;
                    case AccountParseOption.ParseAsFullCircleAccount:
                        {
                            string[] sp = lowerAccount.Split(new string[] { CircleString.ViaCircleGroupSplitter }, StringSplitOptions.RemoveEmptyEntries);
                            if (sp.Length < 2)
                            {
                                return null;
                            }

                            string[] idDomain = sp[1].Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                            if (idDomain.Length < 2)
                                return null;
                            string[] typeAccount = sp[0].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (typeAccount.Length < 2)
                                return null;
                            Guid abid = new Guid(idDomain[0]);
                            if (abid != AddressBookId || idDomain[1].ToLowerInvariant() != HostDomain)  //Is it the correct circle selected?
                                return null;

                            ClientType type = (ClientType)(int.Parse(typeAccount[0]));
                            string mail = typeAccount[1];
                            if (HasMember(mail, type))
                            {
                                return ContactList.GetContact(mail, type);
                            }

                            return null;
                        }

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Get contact from circle error: account: " + account +
                    " in " + ToString() + "\r\nError Message: " + ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Remove a specific contact from circle's contact list by the information provided.
        /// </summary>
        /// <param name="account">The contact information</param>
        /// <param name="option">The parse option for the account parameter</param>
        /// <returns></returns>
        internal bool RemoveMember(string account, AccountParseOption option)
        {
            string lowerAccount = account.ToLowerInvariant();
            try
            {
                switch (option)
                {
                    case AccountParseOption.ParseAsClientTypeAndAccount:
                        {
                            string[] typeAccount = lowerAccount.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (typeAccount.Length >= 2)
                            {
                                ClientType type = (ClientType)(int.Parse(typeAccount[0]));
                                string mail = typeAccount[1];
                                if (HasMember(mail, type))
                                {
                                    ContactList.Remove(account, type);
                                    return true;
                                }

                                return false;

                            }
                        }
                        break;
                    case AccountParseOption.ParseAsFullCircleAccount:
                        {
                            string[] sp = lowerAccount.Split(new string[] { CircleString.ViaCircleGroupSplitter }, StringSplitOptions.RemoveEmptyEntries);
                            if (sp.Length < 2)
                            {
                                return false;
                            }

                            string[] idDomain = sp[1].Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                            if (idDomain.Length < 2)
                                return false;
                            string[] typeAccount = sp[0].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (typeAccount.Length < 2)
                                return false;
                            Guid abid = new Guid(idDomain[0]);
                            if (abid != AddressBookId || idDomain[1].ToLowerInvariant() != HostDomain)  //Is it the correct circle selected?
                                return false;

                            ClientType type = (ClientType)(int.Parse(typeAccount[0]));
                            string mail = typeAccount[1];
                            if (HasMember(mail, type))
                            {
                                ContactList.Remove(account, type);
                                return true;
                            }

                            return false;
                        }

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Remove contact from circle error: account: " + account +
                    " in " + ToString() + "\r\nError Message: " + ex.Message);
            }

            return false;
        }


        /// <summary>
        /// Check whether a specific contact exists in circle's contact list by the information provided.
        /// </summary>
        /// <param name="account">The contact information</param>
        /// <param name="option">The parse option for the account parameter</param>
        /// <returns></returns>
        internal bool HasMember(string account, AccountParseOption option)
        {
            string lowerAccount = account.ToLowerInvariant();
            try
            {
                switch (option)
                {
                    case AccountParseOption.ParseAsClientTypeAndAccount:
                        {
                            string[] typeAccount = lowerAccount.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (typeAccount.Length >= 2)
                            {
                                ClientType type = (ClientType)(int.Parse(typeAccount[0]));
                                string mail = typeAccount[1];
                                return HasMember(mail, type);
                            }
                        }
                        break;
                    case AccountParseOption.ParseAsFullCircleAccount:
                        {
                            string[] sp = lowerAccount.Split(new string[] { CircleString.ViaCircleGroupSplitter }, StringSplitOptions.RemoveEmptyEntries);
                            if (sp.Length < 2)
                            {
                                return false;
                            }

                            string[] idDomain = sp[1].Split(new char[] { '@' }, StringSplitOptions.RemoveEmptyEntries);
                            if (idDomain.Length < 2)
                                return false;
                            string[] typeAccount = sp[0].Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                            if (typeAccount.Length < 2)
                                return false;
                            Guid abid = new Guid(idDomain[0]);
                            if (abid != AddressBookId || idDomain[1].ToLowerInvariant() != HostDomain)  //Is it the correct circle selected?
                                return false;

                            ClientType type = (ClientType)(int.Parse(typeAccount[0]));
                            string mail = typeAccount[1];
                            return HasMember(mail, type);
                        }

                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Verifying membership error: account: " + account +
                    " in " + ToString() + "\r\nError Message: " + ex.Message);
            }

            return false;
        }


        internal bool HasMember(string account, ClientType type)
        {
            lock (ContactList)
                return ContactList.HasContact(account, type);
        }

        internal bool HasMember(Guid contactId)
        {
            lock (ContactList)
                return (ContactList.GetContactByGuid(contactId) != null);

        }

        internal bool HasMember(long CID)
        {
            lock (ContactList)
                return (ContactList.GetContactByCID(CID) != null);
        }

        internal void SetAddressBookInfo(ABFindContactsPagedResultTypeAB abInfo)
        {
            abinfo = abInfo;
        }

        #region Protected
        protected virtual void Initialize()
        {
            ContactType = MessengerContactType.Circle;
            Lists = MSNLists.AllowedList | MSNLists.ForwardList;
        }

        #endregion
    }
}
