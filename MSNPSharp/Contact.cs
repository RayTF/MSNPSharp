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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;
    using MSNPSharp.MSNWS.MSNABSharingService;


    /// <summary>
    /// User in roster list.
    /// </summary>
    [Serializable()]
    public class Contact
    {
        #region Fields

        protected Guid guid = Guid.Empty;
        protected Guid addressBookId = Guid.Empty;
        private long cId = 0;
        private string mail = string.Empty;
        private string name = string.Empty;
        private string nickName = string.Empty;

        private Dictionary<string, string> phoneNumbers = new Dictionary<string, string>();
        private string contactType = string.Empty;
        private string comment = string.Empty;
        private string siblingString = string.Empty;
        private string hash = string.Empty;

        private bool hasSpace = false;
        private bool mobileDevice = false;
        private bool mobileAccess = false;
        private bool isMessengerUser = false;
        private bool hasInitialized = false;

        private PresenceStatus status = PresenceStatus.Offline;
        private ClientType clientType = ClientType.PassportMember;
        private CirclePersonalMembershipRole circleRole = CirclePersonalMembershipRole.None;

        private List<ContactGroup> contactGroups = new List<ContactGroup>(0);
        private MSNLists lists = MSNLists.None;

        private DisplayImage displayImage = null;
        private PersonalMessage personalMessage = null;


        private Dictionary<string, Emoticon> emoticons = new Dictionary<string, Emoticon>(0);
        private Dictionary<string, Contact> siblings = new Dictionary<string, Contact>(0);
        protected Dictionary<Guid, EndPointData> endPointData = new Dictionary<Guid, EndPointData>(0);

        private ulong oimCount = 1;
        private int adlCount = 1;
        private object clientData = null;

        private List<ActivityDetailsType> activities = new List<ActivityDetailsType>(0);
        private Uri userTile = null;
        private string userTileLocation = string.Empty;

        private object syncObject = new object();

        private NSMessageHandler nsMessageHandler = null;

        #endregion

        private Contact()
        {
        }

        protected internal Contact(string abId, string account, ClientType cliType, long cid, NSMessageHandler handler)
        {
            Initialized(new Guid(abId), account, cliType, cid, handler);
        }

        protected internal Contact(Guid abId, string account, ClientType cliType, long cid, NSMessageHandler handler)
        {
            Initialized(abId, account, cliType, cid, handler);
        }

        protected virtual void Initialized(Guid abId, string account, ClientType cliType, long cid, NSMessageHandler handler)
        {
            if (hasInitialized)
                return;

            NSMessageHandler = handler;
            addressBookId = abId;
            mail = account.ToLowerInvariant();
            clientType = cliType;
            cId = cid;

            SetName(account);
            siblingString = ClientType.ToString() + ":" + account.ToLowerInvariant();
            hash = MakeHash(Mail, ClientType, AddressBookId);
            EndPointData[Guid.Empty] = new EndPointData(account, Guid.Empty);

            if (NSMessageHandler != null)
            {
                NSMessageHandler.Manager.Add(this);
            }

            displayImage = DisplayImage.CreateDefaultImage(Mail);

            hasInitialized = true;
        }

        #region Events
        /// <summary>
        /// Fired when contact's display name changed.
        /// </summary>
        public event EventHandler<EventArgs> ScreenNameChanged;

        public event EventHandler<EventArgs> PersonalMessageChanged;

        /// <summary>
        /// Fired after contact's display image has been changed.
        /// </summary>
        public event EventHandler<DisplayImageChangedEventArgs> DisplayImageChanged;

        /// <summary>
        /// Fired after received contact's display image changed notification.
        /// </summary>
        public event EventHandler<DisplayImageChangedEventArgs> DisplayImageContextChanged;
        public event EventHandler<ContactGroupEventArgs> ContactGroupAdded;
        public event EventHandler<ContactGroupEventArgs> ContactGroupRemoved;
        public event EventHandler<EventArgs> ContactBlocked;
        public event EventHandler<EventArgs> ContactUnBlocked;
        public event EventHandler<StatusChangedEventArgs> ContactOnline;
        public event EventHandler<StatusChangedEventArgs> ContactOffline;
        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        #endregion

        #region Contact Properties

        internal object SyncObject
        {
            get 
            { 
                return syncObject; 
            }
        }

        internal string SiblingString
        {
            get
            {
                return siblingString;
            }
        }

        internal NSMessageHandler NSMessageHandler
        {
            get
            {
                return nsMessageHandler;
            }

            set
            {
                nsMessageHandler = value;
            }
        }

        /// <summary>
        /// The display image url from the webside.
        /// </summary>
        public Uri UserTileURL
        {
            get
            {
                return userTile;
            }

            internal set
            {
                userTile = value;
            }
        }

        /// <summary>
        /// The displayimage context.
        /// </summary>
        public string UserTileLocation
        {
            //I create this property because I don't want to play tricks with display image's OriginalContext and Context any more.

            get
            {
                return userTileLocation;
            }

            internal set
            {
                userTileLocation = MSNObject.GetDecodeString(value);
            }
        }

        /// <summary>
        /// Get the Guid of contact, NOT CID.
        /// </summary>
        public Guid Guid
        {
            get
            {
                return guid;
            }

            internal set
            {
                guid = value;
            }
        }

        /// <summary>
        /// The identifier of addressbook this contact belongs to.
        /// </summary>
        public Guid AddressBookId
        {
            get
            {
                return addressBookId;
            }
        }

        /// <summary>
        /// Machine ID, this may be different from the endpoint id.
        /// </summary>
        public Guid MachineGuid
        {
            get
            {
                if (PersonalMessage != null)
                {
                    if (PersonalMessage.MachineGuid != null)
                    {
                        if (PersonalMessage.MachineGuid != Guid.Empty)
                            return PersonalMessage.MachineGuid;
                    }
                }
                return Guid.Empty;
            }
        }

        /// <summary>
        /// The contact id of contact, only PassportMembers have CID.
        /// </summary>
        public long CID
        {
            get
            {
                return cId;
            }
            internal set
            {
                cId = value;
            }
        }

        /// <summary>
        /// The email account of contact.
        /// </summary>
        public string Mail
        {
            get
            {
                return mail;
            }
        }

        /// <summary>
        /// The display name of contact.
        /// </summary>
        public virtual string Name
        {
            get
            {
                return name;
            }

            set
            {
                throw new NotImplementedException("Must be override in subclass.");
            }
        }

        public string HomePhone
        {
            get
            {
                return phoneNumbers.ContainsKey(ContactPhoneTypes.ContactPhonePersonal) ?
                    phoneNumbers[ContactPhoneTypes.ContactPhonePersonal] : string.Empty;
            }
        }

        public string WorkPhone
        {
            get
            {
                return phoneNumbers.ContainsKey(ContactPhoneTypes.ContactPhoneBusiness) ?
                    phoneNumbers[ContactPhoneTypes.ContactPhoneBusiness] : string.Empty;
            }
        }

        public string MobilePhone
        {
            get
            {
                return phoneNumbers.ContainsKey(ContactPhoneTypes.ContactPhoneMobile) ?
                    phoneNumbers[ContactPhoneTypes.ContactPhoneMobile] : string.Empty;
            }
        }

        public Dictionary<string, string> PhoneNumbers
        {
            get
            {
                return phoneNumbers;
            }
        }

        public bool MobileDevice
        {
            get
            {
                return mobileDevice;
            }
        }

        public bool MobileAccess
        {
            get
            {
                return mobileAccess;
            }
        }

        /// <summary>
        /// Indicates whether this contact has MSN Space.
        /// </summary>
        public bool HasSpace
        {
            get
            {
                return hasSpace;
            }

            internal set
            {
                hasSpace = value;
                NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
            }
        }

        public Dictionary<Guid, EndPointData> EndPointData
        {
            get
            {
                return endPointData;
            }
        }

        public bool HasSignedInWithMultipleEndPoints
        {
            get
            {
                //One for Guid.Empty added when calling the constructor, another for contact's own end point.
                return EndPointData.Count > 2;
            }
        }

        public int PlaceCount
        {
            get
            {
                return HasSignedInWithMultipleEndPoints ? EndPointData.Count - 1 : 1;
            }
        }

        /// <summary>
        /// The online status of contact.
        /// </summary>
        public virtual PresenceStatus Status
        {
            get
            {
                return status;
            }

            set
            {
                throw new NotImplementedException("This property is real-only for base class. Must be override in subclass.");
            }
        }

        /// <summary>
        /// Indicates whether the contact is online.
        /// </summary>
        public bool Online
        {
            get
            {
                return status != PresenceStatus.Offline;
            }
        }

        /// <summary>
        /// The type of contact's email account.
        /// </summary>
        public ClientType ClientType
        {
            get
            {
                return clientType;
            }
        }

        /// <summary>
        /// The role of contact in the addressbook.
        /// </summary>
        public string ContactType
        {
            get
            {
                return contactType;
            }
            internal set
            {
                contactType = value;
            }
        }

        public List<ContactGroup> ContactGroups
        {
            get
            {
                return contactGroups;
            }
        }

        public Dictionary<string, Contact> Siblings
        {
            get 
            { 
                return siblings; 
            }
        }

        public virtual DisplayImage DisplayImage
        {
            get
            {

                LoadDisplayImageFromDeltas();
                return displayImage;
            }

            //Calling this will not fire DisplayImageChanged event.
            internal set
            {
                if (displayImage != value)
                {
                    displayImage = value;
                    SaveDisplayImage(displayImage);
                }
            }
        }

        public PersonalMessage PersonalMessage
        {
            get
            {
                return personalMessage;
            }
        }

        /// <summary>
        /// Emoticons[sha]
        /// </summary>
        public Dictionary<string, Emoticon> Emoticons
        {
            get
            {
                return emoticons;
            }
        }

        public List<ActivityDetailsType> Activities
        {
            get
            {
                return activities;
            }
        }

        /// <summary>
        /// The string representation info of contact.
        /// </summary>
        public virtual string Hash
        {
            get
            {
                return hash;
            }
        }

        public object ClientData
        {
            get
            {
                return clientData;
            }
            set
            {
                clientData = value;
            }
        }

        /// <summary>
        /// Receive updated contact information automatically.
        /// <remarks>Contact details like address and phone numbers are automatically downloaded to your Address Book.</remarks>
        /// </summary>
        public bool AutoSubscribeToUpdates
        {
            get
            {
                return (contactType == MessengerContactType.Live || contactType == MessengerContactType.LivePending);
            }
            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && ClientType == ClientType.PassportMember)
                {
                    if (value)
                    {
                        if (!AutoSubscribeToUpdates)
                        {
                            contactType = MessengerContactType.LivePending;
                            NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                        }
                    }
                    else
                    {
                        if (contactType != MessengerContactType.Regular)
                        {
                            contactType = MessengerContactType.Regular;
                            NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the contact can receive MSN message.
        /// </summary>
        public bool IsMessengerUser
        {
            get
            {
                return isMessengerUser;
            }


            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && IsMessengerUser != value)
                {
                    isMessengerUser = value;
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, 
                        delegate  //If you don't add this, you can't see the contact online until your next login
                        {
                            Dictionary<string, MSNLists> hashlist = new Dictionary<string, MSNLists>(2);
                            hashlist.Add(Hash, Lists ^ MSNLists.ReverseList);
                            string payload = ContactService.ConstructLists(hashlist, false)[0];
                            NSMessageHandler.MessageProcessor.SendMessage(new NSPayLoadMessage("ADL", payload));
                        });
                }

                NotifyManager();
            }
        }


        public string Comment
        {
            get
            {
                return comment;
            }
            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && Comment != value)
                {
                    comment = value;
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                }
            }
        }

        /// <summary>
        /// The name provide by the owner.
        /// </summary>
        public string NickName
        {
            get
            {
                return nickName;
            }
            set
            {
                if (NSMessageHandler != null && Guid != Guid.Empty && NickName != value)
                {
                    nickName = value;
                    NSMessageHandler.ContactService.UpdateContact(this, AddressBookId, null);
                }
            }
        }


        /// <summary>
        /// The amount of OIMs sent in a session.
        /// </summary>
        internal ulong OIMCount
        {
            get
            {
                return oimCount;
            }
            set
            {
                if (value < 1)
                {
                    value = 1;
                }
                oimCount = value;
            }
        }

        /// <summary>
        /// The amount of ADL commands send for this contact.
        /// </summary>
        internal int ADLCount
        {
            get { return adlCount; }
            set 
            {
                if (value < 0)
                {
                    value = 0;
                }

                adlCount = value; 
            }
        }

        internal string LocalContactString
        {
            get
            {
                return GetLocalContactString();

            }
        }

        /// <summary>
        /// The role of a contact in the addressbook.
        /// </summary>
        public CirclePersonalMembershipRole CircleRole
        {
            get 
            { 
                return circleRole; 
            }

            internal set 
            { 
                circleRole = value; 
            }
        }

        #endregion

        #region List Properties

        public bool OnForwardList
        {
            get
            {
                return ((lists & MSNLists.ForwardList) == MSNLists.ForwardList);
            }
            set
            {
                if (value != OnForwardList)
                {
                    if (value)
                    {
                        NSMessageHandler.ContactService.AddContactToList(this, MSNLists.ForwardList, null);
                    }
                    else
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.ForwardList, null);
                    }
                }
            }
        }

        /// <summary>
        /// Blocks/unblocks this contact. If blocked, will be placed in your BL and removed
        /// from your AL; otherwise, will be removed from your BL and placed in your AL.
        /// If this contact is not in ReverseList and you want to delete forever,
        /// set the <see cref="OnAllowedList"/> or <see cref="OnBlockedList"/> to false.
        /// </summary>
        public bool Blocked
        {
            get
            {
                return OnBlockedList;
            }
            set
            {
                if (NSMessageHandler != null)
                {
                    if (value)
                        NSMessageHandler.ContactService.BlockContact(this);
                    else
                        NSMessageHandler.ContactService.UnBlockContact(this);
                }
            }
        }

        /// <summary>
        /// Adds or removes this contact into/from your AL.
        /// If this contact is not in ReverseList and you want to delete forever,
        /// set this property to false.
        /// </summary>
        public bool OnAllowedList
        {
            get
            {
                return ((lists & MSNLists.AllowedList) == MSNLists.AllowedList);
            }
            set
            {
                if (value != OnAllowedList)
                {
                    if (value)
                    {
                        Blocked = false;
                    }
                    else if (!OnReverseList)
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.AllowedList, null);
                    }
                }
            }
        }

        /// <summary>
        /// Adds or removes this contact into/from your BL.
        /// If this contact is not in ReverseList and you want to delete forever,
        /// set this property to false.
        /// </summary>
        public bool OnBlockedList
        {
            get
            {
                return ((lists & MSNLists.BlockedList) == MSNLists.BlockedList);
            }
            set
            {
                if (value != OnBlockedList)
                {
                    if (value)
                    {
                        Blocked = true;
                    }
                    else if (!OnReverseList)
                    {
                        NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.BlockedList, null);
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the contact have you on their contact list. 
        /// </summary>
        public bool OnReverseList
        {
            get
            {
                return ((lists & MSNLists.ReverseList) == MSNLists.ReverseList);
            }
        }

        /// <summary>
        /// Indicates whether the contact have you on their contact list and pending your approval. 
        /// </summary>
        public bool OnPendingList
        {
            get
            {
                return ((lists & MSNLists.PendingList) == MSNLists.PendingList);
            }
            set
            {
                if (value != OnPendingList && value == false)
                {
                    NSMessageHandler.ContactService.RemoveContactFromList(this, MSNLists.PendingList, null);
                }
            }
        }

        /// <summary>
        /// The msn lists this contact has.
        /// </summary>
        public MSNLists Lists
        {
            get
            {
                return lists;
            }

            protected internal set
            {
                lists = value;
                NotifyManager();
            }
        }

        #endregion

        #region Internal setters

        internal void SetComment(string note)
        {
            comment = note;
        }

        internal void SetIsMessengerUser(bool isMessengerEnabled)
        {
            isMessengerUser = isMessengerEnabled;
            NotifyManager();
        }

        internal void SetList(MSNLists msnLists)
        {
            lists = msnLists;
            NotifyManager();
        }

        internal void SetMobileAccess(bool enabled)
        {
            mobileAccess = enabled;
        }

        internal void SetMobileDevice(bool enabled)
        {
            mobileDevice = enabled;
        }

        internal void SetName(string newName)
        {
            if (name != newName)
            {
                string oldName = name;
                name = newName;

                // notify all of our buddies we changed our name
                OnScreenNameChanged(oldName);
            }
        }

        

        internal void SetHasSpace(bool hasSpaceValue)
        {
            hasSpace = hasSpaceValue;
        }

        internal void SetNickName(string newNick)
        {
            nickName = newNick;
        }

        internal void SetPersonalMessage(PersonalMessage newpmessage)
        {
            if (personalMessage != newpmessage)
            {
                personalMessage = newpmessage;
                // notify the user we changed our display message
                OnPersonalMessageChanged(newpmessage);
            }
        }

        internal void SetStatus(PresenceStatus newStatus)
        {
            //Becareful deadlock!

            PresenceStatus currentStatus = PresenceStatus.Unknown;

            lock (syncObject)
            {
                currentStatus = status;
            }

            if (currentStatus != newStatus)
            {
                PresenceStatus oldStatus = currentStatus;

                lock (syncObject)
                {
                    
                    status = newStatus;
                }

                // raise an event									
                OnStatusChanged(oldStatus);

                // raise the online/offline events
                if (oldStatus == PresenceStatus.Offline)
                    OnContactOnline(oldStatus);

                if (newStatus == PresenceStatus.Offline)
                    OnContactOffline(oldStatus);
            }

        }

        /// <summary>
        /// This method will lead to fire <see cref="Contact.DisplayImageContextChanged"/> event if the DisplayImage.Sha has been changed.
        /// </summary>
        /// <param name="updatedImageContext"></param>
        /// <returns>
        /// false: No event was fired.<br/>
        /// true: The <see cref="Contact.DisplayImageContextChanged"/> was fired.
        /// </returns>
        internal bool FireDisplayImageContextChangedEvent(string updatedImageContext)
        {
            if (DisplayImage == updatedImageContext)
                return false;

            OnDisplayImageContextChanged(new DisplayImageChangedEventArgs(null, DisplayImageChangedType.UpdateTransmissionRequired));
            return true;
        }

        /// <summary>
        /// This method will lead to fire <see cref="Contact.DisplayImageChanged"/> event if the DisplayImage.Image has been changed.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>
        /// false: No event was fired.<br/>
        /// true: The <see cref="Contact.DisplayImageChanged"/> event was fired.
        /// </returns>
        internal bool SetDisplayImageAndFireDisplayImageChangedEvent(DisplayImage image)
        {
            if (image == null) return false;


            DisplayImageChangedEventArgs displayImageChangedArg = null;
            //if ((displayImage != null && displayImage.Sha != image.Sha && displayImage.IsDefaultImage && image.Image != null) ||     //Transmission completed. default Image -> new Image
            //    (displayImage != null && displayImage.Sha != image.Sha && !displayImage.IsDefaultImage && image.Image != null) ||     //Transmission completed. old Image -> new Image.
            //    (displayImage != null && object.ReferenceEquals(displayImage, image) && displayImage.Image != null) ||              //Transmission completed. old Image -> updated old Image.
            //    (displayImage == null))
            {

                displayImageChangedArg = new DisplayImageChangedEventArgs(image, DisplayImageChangedType.TransmissionCompleted, false);
            }

            if (!object.ReferenceEquals(displayImage, image))
            {
                displayImage = image;
            }

            SaveOriginalDisplayImageAndFireDisplayImageChangedEvent(displayImageChangedArg);

            return true;
        }

        internal void SaveOriginalDisplayImageAndFireDisplayImageChangedEvent(DisplayImageChangedEventArgs arg)
        {
            SaveDisplayImage(displayImage);
            OnDisplayImageChanged(arg);
        }

        internal void NotifyManager()
        {
            if (AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
                return;

            if (NSMessageHandler == null)
                return;

            NSMessageHandler.Manager.SyncProperties(this);
        }

        #region Protected

        protected virtual string GetLocalContactString()
        {
            if (MachineGuid == Guid.Empty)
                return Mail.ToLowerInvariant();

            return Mail.ToLowerInvariant() + ";" + MachineGuid.ToString("B");
        }

        protected virtual void OnScreenNameChanged(string oldName)
        {
            if (ScreenNameChanged != null)
            {
                ScreenNameChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnPersonalMessageChanged(PersonalMessage newmessage)
        {
            if (PersonalMessageChanged != null)
            {
                PersonalMessageChanged(this, EventArgs.Empty);
            }
        }

        protected virtual void OnStatusChanged(PresenceStatus oldStatus)
        {
            if (StatusChanged != null)
                StatusChanged(this, new StatusChangedEventArgs(oldStatus));
        }

        protected virtual void OnContactOnline(PresenceStatus oldStatus)
        {
            if (ContactOnline != null)
                ContactOnline(this, new StatusChangedEventArgs(oldStatus));
        }

        protected virtual void OnContactOffline(PresenceStatus oldStatus)
        {
            if (ContactOffline != null)
            {
                ContactOffline(this, new StatusChangedEventArgs(oldStatus));
            }
        }

        protected virtual void OnDisplayImageChanged(DisplayImageChangedEventArgs arg)
        {
            if (DisplayImageChanged != null)
            {
                DisplayImageChanged(this, arg);
            }
        }

        protected virtual void OnDisplayImageContextChanged(DisplayImageChangedEventArgs arg)
        {
            if (DisplayImageContextChanged != null)
            {
                DisplayImageContextChanged(this, arg);
            }
        }

        protected virtual void LoadDisplayImageFromDeltas()
        {
            if (NSMessageHandler.ContactService.Deltas == null)
                return;

            if (displayImage != null && !displayImage.IsDefaultImage) //Not default, no need to restore.
                return;

            string Sha = string.Empty;
            byte[] rawImageData = NSMessageHandler.ContactService.Deltas.GetRawImageDataBySiblingString(SiblingString, out Sha);
            if (rawImageData != null)
            {
                displayImage = new DisplayImage(Mail, new MemoryStream(rawImageData));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "User " + ToString() + "'s displayimage restored.\r\n " +
                    "Old SHA:     " + Sha + "\r\n " +
                    "Current SHA: " + displayImage.Sha + "\r\n");
            }
        }

        protected virtual void SaveDisplayImage(DisplayImage dispImage)
        {
            if (NSMessageHandler.ContactService.Deltas == null || dispImage == null)
                return;

            if (dispImage.Image == null || string.IsNullOrEmpty(dispImage.Sha))
                return;

            if (NSMessageHandler.ContactService.Deltas.SaveImageAndRelationship(SiblingString, dispImage.Sha, dispImage.GetRawData()))
            {
                NSMessageHandler.ContactService.Deltas.Save(true);
            }
        }

        #endregion

        #endregion

        #region Internal contact operations

        protected virtual void OnContactGroupAdded(ContactGroup group)
        {
            if (ContactGroupAdded != null)
                ContactGroupAdded(this, new ContactGroupEventArgs(group));
        }

        protected virtual void OnContactGroupRemoved(ContactGroup group)
        {
            if (ContactGroupRemoved != null)
                ContactGroupRemoved(this, new ContactGroupEventArgs(group));
        }

        protected virtual void OnContactBlocked()
        {
            if (ContactBlocked != null)
                ContactBlocked(this, new EventArgs());
        }

        protected virtual void OnContactUnBlocked()
        {
            if (ContactUnBlocked != null)
                ContactUnBlocked(this, new EventArgs());
        }


        internal void AddContactToGroup(ContactGroup group)
        {
            if (!contactGroups.Contains(group))
            {
                contactGroups.Add(group);

                OnContactGroupAdded(group);
            }
        }

        internal void RemoveContactFromGroup(ContactGroup group)
        {
            if (contactGroups.Contains(group))
            {
                contactGroups.Remove(group);

                OnContactGroupRemoved(group);
            }
        }

        /// <summary>
        /// Add a membership list for this contact.
        /// </summary>
        /// <param name="list"></param>
        /// <remarks>Since AllowList and BlockList are mutally exclusive, adding a member to AllowList will lead to the remove of BlockList, revese is as the same.</remarks>
        internal void AddToList(MSNLists list)
        {
            if ((lists & list) == MSNLists.None)
            { 
                lists |= list;

                if ((list & MSNLists.BlockedList) == MSNLists.BlockedList)
                {
                    OnContactBlocked();
                }
                
                NotifyManager();
            }

        }

        internal void AddSibling(Contact contact)
        {
            lock (syncObject)
                Siblings[contact.Hash] = contact;
        }

        internal void AddSibling(Contact[] contacts)
        {
            if (contacts == null)
                return;

            lock (syncObject)
            {
                foreach (Contact sibling in contacts)
                {
                    Siblings[sibling.Hash] = sibling;
                }
            }
        }

        internal void RemoveFromList(MSNLists list)
        {
            if ((lists & list) != MSNLists.None)
            {
                lists ^= list;

                // set this contact to offline when it is neither on the allow list or on the forward list
                if (!(OnForwardList || OnAllowedList))
                {
                    status = PresenceStatus.Offline;
                    //also clear the groups, becase msn loose them when removed from the two lists
                    contactGroups.Clear();
                }

                if ((list & MSNLists.BlockedList) == MSNLists.BlockedList)
                {
                    OnContactUnBlocked();
                }
                
                NotifyManager();
            }


        }

        internal void RemoveFromList()
        {
            if (NSMessageHandler != null)
            {
                OnAllowedList = false;
                OnForwardList = false;

                NotifyManager();
            }
        }

        internal static MSNLists GetConflictLists(MSNLists currentLists, MSNLists newLists)
        {
            MSNLists conflictLists = MSNLists.None;

            if ((currentLists & MSNLists.AllowedList) != MSNLists.None && (newLists & MSNLists.BlockedList) != MSNLists.None)
            {
                conflictLists |= MSNLists.AllowedList;
            }

            if ((currentLists & MSNLists.BlockedList) != MSNLists.None && (newLists & MSNLists.AllowedList) != MSNLists.None)
            {
                conflictLists |= MSNLists.BlockedList;
            }

            return conflictLists;
        }

        internal Guid SelectRandomEPID()
        {
            foreach (Guid epId in EndPointData.Keys)
            {
                if (epId != Guid.Empty)
                    return epId;
            }

            return Guid.Empty;
        }

        internal static string MakeHash(string account, ClientType type, Guid abId)
        {
            if (account == null)
                throw new ArgumentNullException("account");

            return type.ToString() + ":" + account.ToLowerInvariant() + ";via=" + abId.ToString("D").ToLowerInvariant();
        }

        internal static string MakeHash(string account, ClientType type, string abId)
        {
            return type.ToString() + ":" + account.ToLowerInvariant() + ";via=" + abId.ToLowerInvariant();
        }

        internal bool HasLists(MSNLists msnlists)
        {
            return ((lists & msnlists) == msnlists);
        }

        internal static MSNLists GetListForADL(MSNLists currentContactList)
        {
            if ((currentContactList & MSNLists.ReverseList) == MSNLists.ReverseList)
            {
                return currentContactList ^ MSNLists.ReverseList;
            }

            return currentContactList;
        }


        #endregion

        public bool HasGroup(ContactGroup group)
        {
            return contactGroups.Contains(group);
        }

        public void UpdateScreenName()
        {
            if (NSMessageHandler == null)
                throw new MSNPSharpException("No valid message handler object");

            NSMessageHandler.RequestScreenName(this);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }

        public static bool operator ==(Contact contact1, Contact contact2)
        {
            if (((object)contact1) == null && ((object)contact2) == null)
                return true;
            if (((object)contact1) == null || ((object)contact2) == null)
                return false;
            return contact1.GetHashCode() == contact2.GetHashCode();
        }

        public static bool operator !=(Contact contact1, Contact contact2)
        {
            return !(contact1 == contact2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetHashCode() == GetHashCode();
        }

        public override string ToString()
        {
            return Hash;
        }

        /// <summary>
        /// Check whether two contacts represent the same user (Have the same passport account).
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public virtual bool IsSibling(Contact contact)
        {
            if (contact == null)
                return false;
            if (ClientType == contact.ClientType && Mail.ToLowerInvariant() == contact.Mail.ToLowerInvariant())
                return true;

            return false;
        }

    }
};
