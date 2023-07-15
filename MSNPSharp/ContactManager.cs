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

namespace MSNPSharp
{
    internal sealed class ContactManager
    {
        #region Fields
        private NSMessageHandler nsMessageHandler = null;

        private Dictionary<string, Contact> defaultContactPage = new Dictionary<string, Contact>();
        private Dictionary<string, Contact> otherContactPage = new Dictionary<string, Contact>();

        #endregion

        #region Properties

        internal NSMessageHandler NSMessageHandler
        {
            get { return nsMessageHandler; }
        }

        #endregion

        #region Constructors

        public ContactManager(NSMessageHandler handler)
        {
            nsMessageHandler = handler;
        }

        #endregion

        #region Private functions

        private string AddToDefaultContactPage(Contact contact)
        {
            string key = GetContactKey(contact);
            if (contact.AddressBookId == new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                
                lock (defaultContactPage)
                    defaultContactPage[key] = contact;
            }

            return key;
        }

        private void DisplayImageChanged(object sender, EventArgs e)
        {
            Contact contact = sender as Contact;
            if (contact == null)
                return;

            SyncDisplayImage(contact);
        }

        private string AddToOtherContactPage(Contact contact)
        {
            string key = GetContactKey(contact);
            if (contact.AddressBookId != new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {
                
                lock (otherContactPage)
                {
                    if (!otherContactPage.ContainsKey(key))
                    {
                        otherContactPage[key] = contact;
                    }
                    else
                    {
                        otherContactPage[key].AddSibling(contact);
                    }
                }

                if (NeedSync(key))
                {
                    lock (defaultContactPage)
                    {
                        if (contact != defaultContactPage[key])
                            defaultContactPage[key].AddSibling(contact);
                    }
                }
            }

            return key;
        }

        private bool NeedSync(Contact contact)
        {
            return NeedSync(GetContactKey(contact));
        }

        private bool NeedSync(string key)
        {
            lock (defaultContactPage)
                return defaultContactPage.ContainsKey(key);
        }

        private bool Sync(string key)
        {
            lock (defaultContactPage)
            {
                if (!NeedSync(key))
                    return false;

                Contact root = defaultContactPage[key];

                if (root.Siblings.Count > 0)
                {
                    SyncProperties(root);
                }

            }

            return true;
        }

        #endregion

        #region Public functions

        public void SyncProperties(Contact root)
        {
            lock (root.SyncObject)
            {
                foreach (Contact sibling in root.Siblings.Values)
                {
                    if (sibling == root)
                        continue;

                    sibling.SetList(root.Lists);
                    sibling.SetIsMessengerUser(root.IsMessengerUser);

                }
            }
        }

        public void SyncDisplayImage(Contact initator)
        {
            if (initator.AddressBookId == new Guid(WebServiceConstants.MessengerIndividualAddressBookId))
            {

                if (initator.Siblings.Count > 0)
                {
                    lock (initator.SyncObject)
                    {
                        foreach (Contact sibling in initator.Siblings.Values)
                        {
                            if (sibling != initator)
                                sibling.DisplayImage = initator.DisplayImage;
                        }
                    }
                }
            }
            else
            {
                string key = GetContactKey(initator);

                Contact root = null;
                lock (otherContactPage)
                {
                    if (!otherContactPage.ContainsKey(key))
                        return;

                    root = otherContactPage[key];
                }

                if (root.Siblings.Count > 0)
                {
                    lock (root.SyncObject)
                    {
                        foreach (Contact sibling in root.Siblings.Values)
                        {
                            if (sibling != root)
                                sibling.DisplayImage = root.DisplayImage;
                        }
                    }
                }
            }
        }

        public void Add(Contact contact)
        {
            string key = AddToDefaultContactPage(contact);
            AddToOtherContactPage(contact);
            contact.DisplayImageChanged += new EventHandler<DisplayImageChangedEventArgs>(DisplayImageChanged);
            if (!NeedSync(key))
            {
                return;
            }

            Sync(key);

        }

        public string GetContactKey(Contact contact)
        {
            return contact.ClientType.ToString().ToLowerInvariant() + ":" + contact.Mail.ToLowerInvariant();
        }

        public void Reset()
        {
            lock (defaultContactPage)
                defaultContactPage.Clear();
            lock (otherContactPage)
                otherContactPage.Clear();
        }
        #endregion
    }
}
