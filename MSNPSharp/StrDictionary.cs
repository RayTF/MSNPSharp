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
using System.Collections.Generic;

namespace MSNPSharp
{
    /*
     * Those classes are needed because Dictionary/Hashtable doesn't guarantee
     * that the Add order will be the same loop order (at least on mono)
     */

    public class StrKeyValuePair
    {
        string key;
        string val;

        public StrKeyValuePair(string key, string val)
        {
            this.key = key;
            this.val = val;
        }

        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                key = value;
            }
        }

        public string Value
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
            }
        }
    }

    public class StrDictionary : IEnumerable
    {
        List<StrKeyValuePair> content;

        public StrDictionary()
        {
            content = new List<StrKeyValuePair>();
        }

        /*
         * I know, foreach sux, but for this use case it's ok since
         * we will only have no more than 10 items
         */
        public string this[string key]
        {
            get
            {
                foreach (StrKeyValuePair kvp in content)
                    if (kvp.Key == key)
                        return kvp.Value;

                return null;
            }
            set
            {
                bool found = false;

                foreach (StrKeyValuePair kvp in content)
                {
                    if (kvp.Key == key)
                    {
                        kvp.Value = value;
                        found = true;
                    }
                }

                if (!found)
                    Add(key, value);
            }
        }

        public void Add(string key, string val)
        {
            StrKeyValuePair kvp = new StrKeyValuePair(key, val);

            content.Add(kvp);
        }

        public bool ContainsKey(string key)
        {
            foreach (StrKeyValuePair kvp in content)
                if (kvp.Key == key)
                    return true;

            return false;
        }

        public bool Remove(string key)
        {
            bool found = false;

            List<StrKeyValuePair> contentClone = new List<StrKeyValuePair>(content);
            foreach (StrKeyValuePair kvp in contentClone)
            {
                if (kvp.Key == key)
                {
                    content.Remove(kvp);
                    found = true;
                }
            }

            return found;
        }

        public void Clear()
        {
            content.Clear();
        }

        public IEnumerator GetEnumerator()
        {
            return content.GetEnumerator();
        }
    }
};
