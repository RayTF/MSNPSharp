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
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

namespace MSNPSharp
{
    [Serializable()]
    public class ContactGroupList : IEnumerable
    {
        List<ContactGroup> list = new List<ContactGroup>();

        [NonSerialized]
        NSMessageHandler nsMessageHandler = null;

        internal void AddGroup(ContactGroup group)
        {
            if (this[group.Guid] == null)
                list.Add(group);
            else
                this[group.Guid].SetName(group.Name);
        }
        internal void RemoveGroup(ContactGroup group)
        {
            list.Remove(group);
        }

        internal ContactGroupList(NSMessageHandler handler)
        {
            nsMessageHandler = handler;
        }

        public virtual void Add(string name)
        {
            if (nsMessageHandler == null)
                throw new MSNPSharpException("No nameserver handler defined");

            nsMessageHandler.ContactService.AddContactGroup(name);
        }

        public virtual void Remove(ContactGroup group)
        {
            if (nsMessageHandler == null)
                throw new MSNPSharpException("No nameserver handler defined");

            if (this[group.Guid] != null)
                nsMessageHandler.ContactService.RemoveContactGroup(group);
            else
                throw new MSNPSharpException("Contactgroup not defined in this list");
        }

        public ContactGroup GetByName(string name)
        {
            foreach (ContactGroup group in list)
            {
                if (group.Name == name)
                    return group;
            }

            return null;
        }

        public ContactGroup this[string guid]
        {
            get
            {
                foreach (ContactGroup group in list)
                {
                    if (group.Guid.ToLower(CultureInfo.InvariantCulture) == guid.ToLower(CultureInfo.InvariantCulture))
                        return group;
                }
                return null;
            }
        }

        public ContactGroup FavoriteGroup
        {
            get
            {
                foreach (ContactGroup group in list)
                {
                    if (group.IsFavorite)
                        return group;
                }
                return null;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public void Clear()
        {
            list.Clear();
        }
    }
};
