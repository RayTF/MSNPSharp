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
using System.Collections;
using System.Threading;

namespace MSNPSharp
{
    [Serializable()]
    public class CircleList : IEnumerable
    {
        private Dictionary<string, Circle> list = new Dictionary<string, Circle>();

        [NonSerialized]
        private NSMessageHandler nsMessageHandler = null;

        [NonSerialized]
        private object syncRoot;

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

        internal bool AddCircle(Circle circle)
        {
            if (this[circle.Mail] == null)
            {
                lock (SyncRoot)
                    list.Add(circle.Mail, circle);
                return true;
            }
            else
            {
                lock (SyncRoot)
                {
                    circle.Lists = list[circle.Mail].Lists;
                    list[circle.Mail] = circle;
                }
            }

            return false;
        }

        /// <summary>
        ///  Remove a circle from circle list.
        /// </summary>
        /// <param name="circle"></param>
        internal void RemoveCircle(Circle circle)
        {
            lock (SyncRoot)
            {
                list.Remove(circle.Mail);
            }
        }

        /// <summary>
        /// Remove a circle from circle list, by providing addressbook Id and Host Domain.
        /// </summary>
        /// <param name="abId">The addressbook Id for circle's addressbook page.</param>
        /// <param name="hostDomain">The host domain of a circle.</param>
        internal void RemoveCircle(Guid abId, string hostDomain)
        {
            lock (SyncRoot)
            {
                string identifier = abId.ToString().ToLowerInvariant() + "@" + hostDomain.ToLowerInvariant();
                list.Remove(identifier);
            }
        }

        internal CircleList(NSMessageHandler handler)
        {
            nsMessageHandler = handler;
        }

        
        /// <summary>
        /// Find <see cref="Circle"/> by circle Id and host domain if circle not found, return null.
        /// </summary>
        /// <param name="abId">Circle id</param>
        /// <param name="hostDomain">Circle host domain.</param>
        /// <returns></returns>
        public Circle this[Guid abId, string hostDomain]
        {
            get
            {
                string identifier = abId.ToString().ToLowerInvariant() + "@" + hostDomain.ToLowerInvariant();
                if (list.ContainsKey(identifier))
                    return list[identifier];

                return null;
            }
        }

        /// <summary>
        /// Find <see cref="Circle"/> by circle Id and host domain, if circle not found, return null.
        /// </summary>
        /// <param name="idAndHostDomain">Via Id: guid@hostdomain</param>
        /// <returns></returns>
        public Circle this[string idAndHostDomain]
        {
            get
            {
                string identifier = idAndHostDomain.ToLowerInvariant();
                if (list.ContainsKey(identifier))
                    return list[identifier];

                return null;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return list.Values.GetEnumerator();
        }

        public void Clear()
        {
            lock (SyncRoot)
                list.Clear();
        }

        public int Count
        {
            get { return list.Count; }
        }
    }
}
