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
using System.Text;

namespace MSNPSharp.Core
{
    public class MimeDictionary : SortedList<string, MimeValue>
    {
        public new MimeValue this[string name]
        {
            get
            {
                if (!ContainsKey(name))
                    return new MimeValue();

                return (MimeValue)base[name];
            }
            set
            {
                base[name] = value;
            }
        }

        public MimeDictionary()
        {
        }

        public MimeDictionary(byte[] data)
        {
            Parse(data);
        }

        public int Parse(byte[] data)
        {
            int end = MSNHttpUtility.IndexOf(data, "\r\n\r\n");
            int ret = end;

            if (end < 0)
                ret = end = data.Length;
            else
                ret += 4;

            byte[] mimeData = new byte[end];
            Array.Copy(data, mimeData, end);

            string mimeStr = Encoding.UTF8.GetString(mimeData);
            string[] lines = mimeStr.Trim().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            int i = 0;
            while (i < lines.Length)
            {
                string line = lines[i];

                while ((++i < lines.Length) && lines[i].StartsWith("\t"))
                    line += lines[i];

                int nameEnd = line.IndexOf(":");

                if (nameEnd < 0)
                    continue;

                string name = line.Substring(0, nameEnd).Trim();
                MimeValue val = line.Substring(nameEnd + 1).Trim();

                this[name] = val;
            }

            return ret;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            foreach (KeyValuePair<string, MimeValue> pair in this)
            {
                sb.Append(pair.Key).Append(": ").Append(pair.Value.ToString()).Append("\r\n");
            }

            return sb.ToString();
        }
    }

    public class MimeValue
    {
        string _val;
        SortedList<object, string> _attributes = new SortedList<object, string>();

        public string Value
        {
            get
            {
                return _val;
            }
        }

        MimeValue(string main, SortedList<object, string> attributes)
        {
            if (attributes == null)
                throw new ArgumentNullException("attributes");

            _val = main;
            _attributes = attributes;
        }

        public MimeValue(string main)
        {
            _val = main;
        }

        public MimeValue()
        {
            _val = string.Empty;
        }

        public string this[object attKey]
        {
            get
            {
                if (!_attributes.ContainsKey(attKey))
                    return string.Empty;

                return _attributes[attKey].ToString();
            }
            set
            {
                _attributes[attKey] = value;
            }
        }

        public static implicit operator string(MimeValue val)
        {
            return val.ToString();
        }

        public static implicit operator MimeValue(string str)
        {
            if (str == null)
                str = string.Empty;

            str = str.Trim();

            string main = str;
            SortedList<object, string> attributes = new SortedList<object, string>();

            if (main.Contains(";"))
            {
                main = main.Substring(0, main.IndexOf(";")).Trim();
                str = str.Substring(str.IndexOf(";") + 1);

                string[] parameters = str.Split(';');

                int i = 0;
                foreach (string param in parameters)
                {
                    int index = param.IndexOf('=');

                    object key = i++;
                    string val = param; //string.Empty;

                    if (index > 0)
                    {
                        key = param.Substring(0, index).Trim();
                        val = param.Substring(index + 1).Trim();
                    }
                    else
                    {

                    }

                    attributes[key] = val;
                }
            }

            return new MimeValue(main, attributes);
        }

        public void ClearAttributes()
        {
            _attributes.Clear();
        }

        public bool HasAttribute(string name)
        {
            return _attributes.ContainsKey(name);
        }

        public override string ToString()
        {
            string str = _val;

            foreach (KeyValuePair<object, string> att in _attributes)
            {
                if (!string.IsNullOrEmpty(str))
                    str += ";";

                str += (att.Key is int) ? att.Value : String.Format("{0}={1}", att.Key, att.Value);
            }

            return str.Trim();
        }
    }
};
