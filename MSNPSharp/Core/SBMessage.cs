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
using System.IO;
using System.Text;
using System.Collections;

namespace MSNPSharp.Core
{
    using MSNPSharp;
    using System.Globalization;

    [Serializable]
    public class SBMessage : MSNMessage
    {
        private string acknowledgement = "N";
        protected bool hasAckField = false;

        public string Acknowledgement
        {
            get
            {
                return acknowledgement;
            }
            set
            {
                if (Command == "MSG")
                    hasAckField = true;
                else
                    return;

                acknowledgement = value;
            }
        }

        public SBMessage()
        {
        }

        public SBMessage(string command, string[] commandValues)
            : base(command, new ArrayList(commandValues))
        {
        }

        public SBMessage(string command, ArrayList commandValues)
            : base(command, commandValues)
        {
        }

        public override byte[] GetBytes()
        {
            StringBuilder builder = new StringBuilder(128);
            builder.Append(Command);

            if (Command != "OUT")
            {
                if (TransactionID != -1)
                {
                    builder.Append(' ');
                    builder.Append(TransactionID.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (Command == "MSG" && hasAckField)
            {
                builder.Append(' ');
                builder.Append(Acknowledgement);
            }
            else
            {

                foreach (string val in CommandValues)
                {
                    builder.Append(' ');
                    builder.Append(val);
                }
            }

            if (InnerMessage != null && InnerBody == null)  //This is a message created locally.
            {
                builder.Append(' ');
                builder.Append(InnerMessage.GetBytes().Length.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append("\r\n");

            if (InnerMessage != null)
                return AppendArray(System.Text.Encoding.UTF8.GetBytes(builder.ToString()), InnerMessage.GetBytes());
            else
                return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(128);
            builder.Append(Command);

            if (Command != "OUT")
            {
                if (TransactionID != -1)
                {
                    builder.Append(' ');
                    builder.Append(TransactionID.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (Command == "MSG" && hasAckField)
            {
                builder.Append(' ');
                builder.Append(Acknowledgement);

                foreach (string val in CommandValues)
                {
                    builder.Append(' ');
                    builder.Append(val);
                }
            }
            else
            {

                foreach (string val in CommandValues)
                {
                    builder.Append(' ');
                    builder.Append(val);
                }
            }

            if (InnerMessage != null && InnerBody == null)
            {
                builder.Append(' ');
                builder.Append(InnerMessage.GetBytes().Length.ToString(CultureInfo.InvariantCulture));
            }

            builder.Append("\r\n");

            return builder.ToString();
        }
    }
};
