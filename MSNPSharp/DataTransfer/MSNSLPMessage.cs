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
using System.IO;
using System.Collections;
using System.Globalization;

namespace MSNPSharp.DataTransfer
{
    using MSNPSharp;
    using MSNPSharp.Core;

    /// <summary>
    /// Base SLP message for SLPStatusMessage and SLPRequestMessage.
    /// Usually this message is contained in a P2P Message.
    /// </summary>
    [Serializable]
    public abstract class SLPMessage : NetworkMessage
    {
        private Encoding encoding = Encoding.UTF8;
        private MimeDictionary mimeHeaders = new MimeDictionary();
        private MimeDictionary mimeBodies = new MimeDictionary();

        private Guid GetEndPointIDFromMailEPIDString(string mailEPID)
        {
            if (mailEPID.Contains(";"))
            {
                return new Guid(mailEPID.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)[1]);
            }

            return Guid.Empty;
        }


        protected SLPMessage()
        {
            Via = "MSNSLP/1.0/TLP ";
            Branch = Guid.NewGuid().ToString("B").ToUpperInvariant();
            CSeq = 0;
            CallId = Guid.NewGuid();
            MaxForwards = 0;
            ContentType = "text/unknown";
            mimeHeaders[MimeHeaderStrings.Content_Length] = "0";
        }

        protected SLPMessage(byte[] data)
        {
            ParseBytes(data);
        }

        protected abstract string StartLine
        {
            get;
            set;
        }

        /// <summary>
        /// Defaults to UTF8
        /// </summary>
        public Encoding Encoding
        {
            get
            {
                return encoding;
            }
            set
            {
                encoding = value;
            }
        }

        public int MaxForwards
        {
            get
            {
                return int.Parse(mimeHeaders[MimeHeaderStrings.Max_Forwards], System.Globalization.CultureInfo.InvariantCulture);
            }
            set
            {
                mimeHeaders[MimeHeaderStrings.Max_Forwards] = value.ToString();
            }
        }

        public string To
        {
            get
            {
                return mimeHeaders[MimeHeaderStrings.To];
            }
        }

        public string From
        {
            get
            {
                return mimeHeaders[MimeHeaderStrings.From];
            }
        }

        public Guid FromEndPoint
        {
            get
            {
                string mailEpID = From.Replace("<msnmsgr:", String.Empty).Replace(">", String.Empty);
                return GetEndPointIDFromMailEPIDString(mailEpID);
            }
        }

        public Guid ToEndPoint
        {
            get
            {
                string mailEpID = To.Replace("<msnmsgr:", String.Empty).Replace(">", String.Empty);
                return GetEndPointIDFromMailEPIDString(mailEpID);
            }
        }

        /// <summary>
        /// The contact that send the message.
        /// </summary>
        public string FromMail
        {
            get
            {
                return From.Replace("<msnmsgr:", String.Empty).Replace(">", String.Empty);
            }
            internal set
            {
                mimeHeaders[MimeHeaderStrings.From] = String.Format("<msnmsgr:{0}>", value);
            }
        }

        /// <summary>
        /// The contact that receives the message.
        /// </summary>
        public string ToMail
        {
            get
            {
                return To.Replace("<msnmsgr:", String.Empty).Replace(">", String.Empty);
            }

            internal set
            {
                mimeHeaders[MimeHeaderStrings.To] = String.Format("<msnmsgr:{0}>", value);
            }
        }

        public string Via
        {
            get
            {
                return mimeHeaders["Via"];
            }
            set
            {
                mimeHeaders["Via"] = value;
            }
        }

        /// <summary>
        /// The current branch this message applies to.
        /// </summary>
        public string Branch
        {
            get
            {
                return mimeHeaders["Via"]["branch"];
            }
            set
            {
                mimeHeaders["Via"]["branch"] = value;
            }
        }

        /// <summary>
        /// The sequence count of this message.
        /// </summary>
        public int CSeq
        {
            get
            {
                return int.Parse(mimeHeaders["CSeq"], System.Globalization.CultureInfo.InvariantCulture);
            }
            set
            {
                mimeHeaders["CSeq"] = value.ToString();
            }
        }

        public Guid CallId
        {
            get
            {
                return new Guid(mimeHeaders["Call-ID"]);
            }
            set
            {
                mimeHeaders["Call-ID"] = value.ToString("B").ToUpper(System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        public string ContentType
        {
            get
            {
                return mimeHeaders[MimeHeaderStrings.Content_Type];
            }
            set
            {
                mimeHeaders[MimeHeaderStrings.Content_Type] = value;
            }
        }

        /// <summary>
        /// Contains all name/value combinations of non-header fields in the message
        /// </summary>
        public MimeDictionary BodyValues
        {
            get
            {
                return mimeBodies;
            }
        }

        /// <summary>
        /// Builds the entire message and returns it as a byte array. Ready to be used in a P2P Message.
        /// This function adds the 0x00 at the end of the message.
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            return GetBytes(true);
        }

        public byte[] GetBytes(bool appendNull)
        {
            string body = mimeBodies.ToString();

            // Update the Content-Length header, +1 the additional 0x00
            // mimeBodylength + \r\n\0
            mimeHeaders[MimeHeaderStrings.Content_Length] = (body.Length + (appendNull ? 3 : 2)).ToString();

            StringBuilder builder = new StringBuilder(512);
            builder.Append(StartLine.Trim());
            builder.Append("\r\n");
            builder.Append(mimeHeaders.ToString());
            builder.Append("\r\n");
            builder.Append(body);
            builder.Append("\r\n");

            // get the bytes
            byte[] message = Encoding.GetBytes(builder.ToString());

            // add the additional 0x00
            if (appendNull)
            {
                byte[] totalMessage = new byte[message.Length + 1];
                message.CopyTo(totalMessage, 0);
                totalMessage[message.Length] = 0x00;

                return totalMessage;
            }
            else
            {
                return message;
            }
        }

        /// <summary>
        /// Parses an MSNSLP message and stores the values in the object's fields.
        /// </summary>
        /// <param name="data">The messagedata to parse</param>
        public override void ParseBytes(byte[] data)
        {
            int lineLen = MSNHttpUtility.IndexOf(data, "\r\n");
            byte[] lineData = new byte[lineLen];
            Array.Copy(data, lineData, lineLen);
            StartLine = Encoding.GetString(lineData).Trim();

            byte[] header = new byte[data.Length - lineLen - 2];
            Array.Copy(data, lineLen + 2, header, 0, header.Length);

            mimeHeaders.Clear();
            int mimeEnd = mimeHeaders.Parse(header);

            byte[] body = new byte[header.Length - mimeEnd];
            Array.Copy(header, mimeEnd, body, 0, body.Length);

            mimeBodies.Clear();
            mimeBodies.Parse(body);
        }

        /// <summary>
        /// Textual presentation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Encoding.GetString(GetBytes());
        }

        public static SLPMessage Parse(byte[] data)
        {
            int lineLen = MSNHttpUtility.IndexOf(data, "\r\n");

            if (lineLen < 0)
                return null;

            try
            {
                byte[] lineData = new byte[lineLen];
                Array.Copy(data, lineData, lineLen);
                string line = Encoding.UTF8.GetString(lineData);

                if (!line.Contains("MSNSLP"))
                    return null;

                if (line.StartsWith("MSNSLP/1.0"))
                    return new SLPStatusMessage(data);
                else
                    return new SLPRequestMessage(data);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// The MSNSLP INVITE (request to create transfer), 
    /// BYE (request to close transfer), 
    /// ACK message (request for acknowledgement).
    /// </summary>
    public class SLPRequestMessage : SLPMessage
    {
        string method = "UNKNOWN";
        string version = "MSNSLP/1.0";

        public string Method
        {
            get
            {
                return method;
            }
            set
            {
                method = value;
            }
        }

        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                version = value;
            }
        }

        protected override string StartLine
        {
            get
            {
                return String.Format("{0} {1}:{2} {3}", method, "MSNMSGR", ToMail, version);
            }
            set
            {
                string[] chunks = value.Split(new string[] { " ", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                method = chunks[0];
                version = chunks[2];
            }
        }

        public SLPRequestMessage(string to, string method)
            : base()
        {
            this.ToMail = to;
            this.method = method;
        }

        public SLPRequestMessage(byte[] data)
            : base(data)
        {
        }


        /// <summary>
        /// Creates a message which is send directly after the last data message.
        /// </summary>
        /// <returns></returns>
        public static SLPRequestMessage CreateClosingMessage(MSNSLPTransferProperties transferProperties)
        {
            SLPRequestMessage slpMessage = new SLPRequestMessage(transferProperties.RemoteContactEPIDString, MSNSLPRequestMethod.BYE);
            slpMessage.ToMail = transferProperties.RemoteContactEPIDString;
            slpMessage.FromMail = transferProperties.LocalContactEPIDString;

            slpMessage.Branch = transferProperties.LastBranch;
            slpMessage.CSeq = 0;
            slpMessage.CallId = transferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionclosebody";

            slpMessage.BodyValues["SessionID"] = transferProperties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);

            if (transferProperties.DataType == DataTransferType.Activity)
            {
                slpMessage.BodyValues["Context"] = "dAMAgQ==";

            }

            return slpMessage;
        }


    }

    /// <summary>
    /// The MSNSLP OK, Decline, Internal Error message.
    /// </summary>
    public class SLPStatusMessage : SLPMessage
    {
        string version = "MSNSLP/1.0";
        int code = 0;
        string phrase = "Unknown";

        public int Code
        {
            get
            {
                return code;
            }
            set
            {
                code = value;
            }
        }

        public string Phrase
        {
            get
            {
                return phrase;
            }
            set
            {
                phrase = value;
            }
        }

        public string Version
        {
            get
            {
                return version;
            }
            set
            {
                version = value;
            }
        }

        protected override string StartLine
        {
            get
            {
                return string.Format("{0} {1} {2}", version, code, phrase);
            }
            set
            {
                string[] chunks = value.Split(new string[] { " ", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                version = chunks[0];
                int.TryParse(chunks[1], out code);
                phrase = string.Empty;

                for (int i = 2; i < chunks.Length; i++)
                    phrase += chunks[i] + " ";

                phrase = phrase.Trim();
            }
        }

        public SLPStatusMessage(string to, int code, string phrase)
            : base()
        {
            this.ToMail = to;
            this.code = code;
            this.phrase = phrase;
        }

        public SLPStatusMessage(byte[] data)
            : base(data)
        {
        }


        /// <summary>
        /// Creates an 500 internal error message.
        /// </summary>
        /// <param name="transferProperties"></param>
        /// <returns></returns>
        public static SLPStatusMessage CreateInternalErrorMessage(MSNSLPTransferProperties transferProperties)
        {
            SLPStatusMessage slpMessage = new SLPStatusMessage(transferProperties.RemoteContactEPIDString, 500, "Internal Error");
            slpMessage.ToMail = transferProperties.RemoteContactEPIDString;
            slpMessage.FromMail = transferProperties.LocalContactEPIDString;
            slpMessage.Branch = transferProperties.LastBranch;
            slpMessage.CSeq = transferProperties.LastCSeq;
            slpMessage.CallId = transferProperties.CallId;
            slpMessage.MaxForwards = 0;
            slpMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            slpMessage.BodyValues["SessionID"] = transferProperties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            slpMessage.BodyValues["SChannelState"] = "0";
            return slpMessage;
        }

        /// <summary>
        /// Creates a 200 OK message. This is called by the handler after the client-programmer
        /// has accepted the invitation.
        /// </summary>
        /// <returns></returns>
        public static SLPStatusMessage CreateAcceptanceMessage(MSNSLPTransferProperties properties)
        {
            SLPStatusMessage newMessage = new SLPStatusMessage(properties.RemoteContactEPIDString, 200, "OK");
            newMessage.ToMail = properties.RemoteContactEPIDString;
            newMessage.FromMail = properties.LocalContactEPIDString;
            newMessage.Branch = properties.LastBranch;
            newMessage.CSeq = 1;
            newMessage.CallId = properties.CallId;
            newMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            newMessage.BodyValues["SessionID"] = properties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return newMessage;
        }

        /// <summary>
        /// Creates a 603 Decline message.
        /// </summary>
        /// <returns></returns>
        public static SLPStatusMessage CreateDeclineMessage(MSNSLPTransferProperties properties)
        {
            SLPStatusMessage newMessage = new SLPStatusMessage(properties.RemoteContactEPIDString, 603, "Decline");
            newMessage.ToMail = properties.RemoteContactEPIDString;
            newMessage.FromMail = properties.LocalContactEPIDString;
            newMessage.Branch = properties.LastBranch;
            newMessage.CSeq = 1;
            newMessage.CallId = properties.CallId;
            newMessage.ContentType = "application/x-msnmsgr-sessionreqbody";
            newMessage.BodyValues["SessionID"] = properties.SessionId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return newMessage;
        }
    }
};
