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
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    /// <summary>
    /// A message that defines a list of emoticons used in the next textmessage.
    /// </summary>
    [Serializable()]
    public class EmoticonMessage : NetworkMessage
    {
        private EmoticonType emoticontype = EmoticonType.StaticEmoticon;
        /// <summary>
        /// Constructor.
        /// </summary>
        public EmoticonMessage()
        {
            emoticons = new List<Emoticon>();
        }

        /// <summary>
        /// Constructor with a single emoticon supplied.
        /// </summary>
        /// <param name="emoticon"></param>
        /// <param name="type"></param>
        public EmoticonMessage(Emoticon emoticon, EmoticonType type)
        {
            if (null == emoticon)
                throw new ArgumentNullException("emoticon");

            emoticons = new List<Emoticon>();
            emoticons.Add(emoticon);
            emoticontype = type;
        }

        /// <summary>
        /// Type of emoticons.
        /// </summary>
        public EmoticonType EmoticonType
        {
            get { return emoticontype; }
        }

        /// <summary>
        /// Constructor with multiple emoticons supplied.
        /// </summary>
        /// <param name="emoticons"></param>
        /// <param name="type"></param>
        public EmoticonMessage(List<Emoticon> emoticons, EmoticonType type)
        {
            Emoticons = new List<Emoticon>(emoticons);
            emoticontype = type;
        }

        /// <summary>
        /// </summary>
        private List<Emoticon> emoticons;

        /// <summary>
        /// The emoticon that is defined in this message
        /// </summary>
        public List<Emoticon> Emoticons
        {
            get
            {
                return emoticons;
            }
            set
            {
                emoticons = value;
            }
        }

        /// <summary>
        /// Sets the Emoticon property.
        /// </summary>
        /// <param name="data"></param>
        public override void ParseBytes(byte[] data)
        {
            // set the text property for easy retrieval
            string body = System.Text.Encoding.UTF8.GetString(data).Trim();

            Emoticons = new List<Emoticon>();

            string[] values = body.Split('\t');

            for (int i = 0; i < values.Length - 1; i += 2)
            {
                Emoticon emoticon = new Emoticon();
                emoticon.Shortcut = values[i].Trim();
                emoticon.SetContext(values[i + 1].Trim());

                Emoticons.Add(emoticon);
            }
        }


        /// <summary>
        /// Gets the header with the body appended as a byte array
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < emoticons.Count; i++)
            {
                Emoticon emoticon = (Emoticon)emoticons[i];
                builder.Append(emoticon.Shortcut).Append('\t').Append(emoticon.ContextPlain).Append('\t');
            }
            return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        }


        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return System.Text.Encoding.UTF8.GetString(GetBytes());
        }

    }
};
