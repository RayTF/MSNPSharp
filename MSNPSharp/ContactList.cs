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
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    [Serializable()]
    public class ContactList : Dictionary<int, Contact>
    {
        private static ClientType[] clientTypes = (ClientType[])Enum.GetValues(typeof(ClientType));

        [NonSerialized]
        private NSMessageHandler nsMessageHandler;
        [NonSerialized]
        private object syncRoot;

        private Guid addressBookId = Guid.Empty;
        private Owner owner = null;

        private ContactList()
        {
        }

        private void Initialize(NSMessageHandler handler, Owner owner)
        {
            nsMessageHandler = handler;
            this.owner = owner;
        }

        public ContactList(NSMessageHandler handler)
        {
            addressBookId = new Guid(WebServiceConstants.MessengerIndividualAddressBookId);
            nsMessageHandler = handler;
        }

        public ContactList(string abId, Owner owner, NSMessageHandler handler)
        {
            addressBookId = new Guid(abId);
            Initialize(handler, owner);
        }

        public ContactList(Guid abId, Owner owner, NSMessageHandler handler)
        {
            addressBookId = abId;
            Initialize(handler, owner);
        }

        #region ListEnumerators

        public class ListEnumerator : IEnumerator<Contact>
        {
            private Enumerator baseEnum;
            private MSNLists listFilter;

            public ListEnumerator(Enumerator listEnum, MSNLists filter)
            {
                baseEnum = listEnum;
                listFilter = filter;
            }

            public virtual bool MoveNext()
            {
                if (listFilter == MSNLists.None)
                {
                    return baseEnum.MoveNext();
                }
                else
                {
                    while (baseEnum.MoveNext())
                    {
                        if (Current.HasLists(listFilter))
                            return true;
                    }
                    return false;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return baseEnum.Current;
                }
            }

            public Contact Current
            {
                get
                {
                    return baseEnum.Current.Value;
                }
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
                baseEnum.Dispose();
            }

            public IEnumerator<Contact> GetEnumerator()
            {
                return this;
            }
        }

        public class EmailListEnumerator : ContactList.ListEnumerator
        {
            public EmailListEnumerator(Enumerator listEnum)
                : base(listEnum, MSNLists.None)
            {
            }

            public override bool MoveNext()
            {
                while (base.MoveNext())
                {
                    if (Current.Guid != Guid.Empty && Current.IsMessengerUser == false)
                        return true;
                }
                return false;
            }
        }

        #endregion

        #region Lists

        public ContactList.ListEnumerator Forward
        {
            get
            {
                return new ContactList.ListEnumerator(GetEnumerator(), MSNLists.ForwardList);
            }
        }

        public ContactList.ListEnumerator Allowed
        {
            get
            {
                return new ContactList.ListEnumerator(GetEnumerator(), MSNLists.AllowedList);
            }
        }

        public ContactList.ListEnumerator BlockedList
        {
            get
            {
                return new ContactList.ListEnumerator(GetEnumerator(), MSNLists.BlockedList);
            }
        }

        public ContactList.ListEnumerator Reverse
        {
            get
            {
                return new ContactList.ListEnumerator(GetEnumerator(), MSNLists.ReverseList);
            }
        }

        public ContactList.ListEnumerator Pending
        {
            get
            {
                return new ContactList.ListEnumerator(GetEnumerator(), MSNLists.PendingList);
            }
        }

        public ContactList.ListEnumerator All
        {
            get
            {
                return new ContactList.ListEnumerator(GetEnumerator(), MSNLists.None);
            }
        }

        public ContactList.ListEnumerator Email
        {
            get
            {
                return new ContactList.EmailListEnumerator(GetEnumerator());
            }
        }

        #endregion

        public object SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    object newobj = new object();
                    Interlocked.CompareExchange(ref syncRoot, newobj, null);
                }
                return syncRoot;
            }
        }

        /// <summary>
        /// The addressbook identifier of this addressbook.
        /// </summary>
        public Guid AddressBookId
        {
            get
            {
                return addressBookId;
            }
        }

        /// <summary>
        /// The owner of the contactlist. This is the identity that logged into the messenger network.
        /// </summary>
        public Owner Owner
        {
            get
            {
                return owner;
            }
        }

        /// <summary>
        /// Get the specified contact.
        /// <remarks>If the contact does not exist, return null</remarks>
        /// </summary>
        /// <param name="account"></param>
        /// <returns>
        /// If the contact does not exist, return null.
        /// If the specified account has multi-clienttype, the contact with type
        /// <see cref="ClientType.PassportMember"/> will be returned first.
        /// If there's no PassportMember with the specified account, the contact with type 
        /// <see cref="ClientType.EmailMember"/> will be returned. Then the next is <see cref="ClientType.PhoneMember"/>
        /// ,<see cref="ClientType.LCS"/> and so on...
        /// </returns>
        public Contact GetContact(string account)
        {
            if (HasContact(account, ClientType.PassportMember))
                return GetContact(account, ClientType.PassportMember);

            if (HasContact(account, ClientType.EmailMember))
                return GetContact(account, ClientType.EmailMember);

            if (HasContact(account, ClientType.PhoneMember))
                return GetContact(account, ClientType.PhoneMember);

            if (HasContact(account, ClientType.LCS))
                return GetContact(account, ClientType.LCS);

            return null;
        }

        /// <summary>
        /// Get the specified contact.
        /// <para>This overload will set the contact name to a specified value (if the contact exists.).</para>
        /// <remarks>If the contact does not exist, return null</remarks>
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name"></param>
        /// <returns>
        /// If the contact does not exist, return null.
        /// If the specified account has multi-clienttype, the contact with type
        /// <see cref="ClientType.PassportMember"/> will be returned first.
        /// If there's no PassportMember with the specified account, the contact with type 
        /// <see cref="ClientType.EmailMember"/> will be returned.Then the next is <see cref="ClientType.PhoneMember"/>
        /// ,<see cref="ClientType.LCS"/> and so on...
        /// </returns>
        internal Contact GetContact(string account, string name)
        {
            Contact contact = GetContact(account);
            if (contact != null)
                lock (SyncRoot)
                    contact.SetName(name);

            return contact;
        }

        /// <summary>
        /// Get a contact with specified account and client type, if the contact does not exist, create it.
        /// <para>This overload will set the contact name to a specified value.</para>
        /// </summary>
        /// <param name="account"></param>
        /// <param name="name">The new name you want to set.</param>
        /// <param name="type"></param>
        /// <returns>
        /// A <see cref="Contact"/> object.
        /// If the contact does not exist, create it.
        /// </returns>
        internal Contact GetContact(string account, string name, ClientType type)
        {
            Contact contact = GetContact(account, type);

            lock (SyncRoot)
                contact.SetName(name);

            return contact;
        }

        /// <summary>
        /// Get a contact with specified account and client type, if the contact does not exist, create it.
        /// </summary>
        /// <param name="account">Account (Mail) of a contact</param>
        /// <param name="type">Contact type.</param>
        /// <returns>
        /// A <see cref="Contact"/> object.
        /// If the contact does not exist, create it.
        /// </returns>
        internal Contact GetContact(string account, ClientType type)
        {
            int hash = Contact.MakeHash(account, type, AddressBookId.ToString("D")).GetHashCode();
            
            lock (SyncRoot)
            {
                if (ContainsKey(hash))
                {
                    return this[hash];
                }
    
                Contact tmpContact = new Contact(AddressBookId, account, type, 0, nsMessageHandler);
    
                
                Add(hash, tmpContact);
            }

            return GetContact(account, type);
        }

        public Contact GetContactByGuid(Guid guid)
        {
            if (guid != Guid.Empty)
            {
                lock (SyncRoot)
                {
                    foreach (Contact contact in Values)
                    {
                        if (contact.Guid == guid)
                            return contact;
                    }
                }
            }
            return null;
        }

        public Contact GetContactByCID(long cid)
        {
            if (cid != 0)
            {
                lock (SyncRoot)
                {
                    foreach (Contact contact in Values)
                    {
                        if (contact.CID == cid)
                            return contact;
                    }
                }                
            }
            return null;
        }

        public Contact this[string account]
        {
            get
            {
                return GetContact(account);
            }
            set
            {
                this[account, value.ClientType] = value;
            }
        }

        public Contact this[string account, ClientType type]
        {
            get
            {
                return GetContact(account, type);
            }
            set
            {
                this[account, type] = value;
            }
        }

        /// <summary>
        /// Check whether the specified account is in the contact list.
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public bool HasContact(string account)
        {
            foreach (ClientType ct in clientTypes)
            {
                if (HasContact(account, ct))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Check whether the account with specified client type is in the contact list.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool HasContact(string account, ClientType type)
        {
            return ContainsKey(Contact.MakeHash(account, type, AddressBookId.ToString("D")).GetHashCode());
        }

        public void CopyTo(Contact[] array, int index)
        {
            lock (SyncRoot)
                Values.CopyTo(array, index);
        }

        /// <summary>
        /// Copy the whole contact list out.
        /// </summary>
        /// <returns></returns>
        public Contact[] ToArray()
        {
            lock (SyncRoot)
            {
                Contact[] array = new Contact[Values.Count];
                CopyTo(array, 0);
                return array;
            }
        }

        /// <summary>
        /// Remove all the contacts with the specified account.
        /// </summary>
        /// <param name="account"></param>
        internal void Remove(string account)
        {
            foreach (ClientType ct in clientTypes)
            {
                if (HasContact(account, ct))
                    Remove(account, ct);
            }
        }

        /// <summary>
        /// Remove a contact with specified account and client type.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="type"></param>
        internal void Remove(string account, ClientType type)
        {
            lock (SyncRoot)
            {
                Remove(Contact.MakeHash(account, type, AddressBookId.ToString("D")).GetHashCode());
            }
        }

        /// <summary>
        /// Set the owner for default addressbook. This funcation can be only called once.
        /// </summary>
        /// <param name="owner"></param>
        internal void SetOwner(Owner owner)
        {
            if (AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                throw new InvalidOperationException("Only default addressbook can call this function.");
            }

            if (Owner != null)
            {
                throw new InvalidOperationException("Owner already set.");
            }

            if (owner.AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                throw new InvalidOperationException("Invalid owner: This is not the owner for default addressbook.");
            }

            this.owner = owner;
        }

        public bool HasMultiType(string account)
        {
            int typecount = 0;
            foreach (ClientType ct in clientTypes)
            {
                if (HasContact(account, ct) && ++typecount > 1)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Reset the contact list and clear the owner.
        /// </summary>
        public void Reset()
        {
            if (Owner != null)
            {
                Owner.Emoticons.Clear();
                Owner.EndPointData.Clear();
                Owner.LocalEndPointClientCapacities = ClientCapacities.None;
                Owner.LocalEndPointClientCapacitiesEx = ClientCapacitiesEx.None;
            }

            Clear();
            owner = null;
        }
    }
};