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
using System.Web;
using System.Xml;
using System.Text;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    [Serializable()]
    public class PersonalMessage
    {
        private string personalMessage;
        private Guid machineGuid;

        private string signatureSound;

        private string appName;
        private string format;
        private MediaType mediaType;

        [NonSerialized]
        private NSMessage nsMessage;

        private string[] content;

        public PersonalMessage(string personalmsg, MediaType mediatype, string[] currentmediacontent, Guid machineguid)
        {
            Message = personalmsg;
            mediaType = mediatype;
            content = currentmediacontent;
            machineGuid = machineguid;
            Format = "{0}";
        }

        public PersonalMessage(string personalmsg, MediaType mediatype, string[] currentmediacontent, string contentformat, Guid machineguid)
        {
            Message = personalmsg;
            mediaType = mediatype;
            content = currentmediacontent;
            machineGuid = machineguid;
            Format = contentformat;
        }

        internal PersonalMessage(NSMessage message)
        {
            nsMessage = message;
            mediaType = MediaType.None;
            machineGuid = Guid.Empty;

            try
            {
                Handle();
            }
            catch (Exception exception)
            {
                System.Diagnostics.Trace.WriteLineIf(Settings.TraceSwitch.TraceError, exception.Message, GetType().Name);
            }
        }

        public NSMessage NSMessage
        {
            get
            {
                return nsMessage;
            }
        }

        public string Payload
        {
            get
            {
                string currentmedia = String.Empty;

                if (mediaType != MediaType.None)
                {
                    foreach (string media in content)
                    {
                        currentmedia = currentmedia + media + @"\0";
                    }

                    if (Format == null || Format == "" || Format == String.Empty)
                        Format = "{0}";

                    currentmedia = @"\0" + mediaType.ToString() + @"\01\0" + Format + @"\0" + currentmedia;
                }

                string pload = String.Format(
                    "<Data><PSM>{0}</PSM><CurrentMedia>{1}</CurrentMedia>" +
                    "<MachineGuid>{2}</MachineGuid>" +
                    "<SignatureSound>{3}</SignatureSound>" +
                    "</Data>",
                    MSNHttpUtility.XmlEncode(personalMessage),
                    MSNHttpUtility.XmlEncode(currentmedia),
                    MSNHttpUtility.XmlEncode(machineGuid.ToString("B")),
                    MSNHttpUtility.XmlEncode(signatureSound));

                return pload;
            }
        }

        public string Message
        {
            get
            {
                return personalMessage;
            }
            set
            {
                personalMessage = value;
            }
        }

        public Guid MachineGuid
        {
            get
            {
                return machineGuid;
            }
        }

        public string SignatureSound
        {
            get
            {
                return signatureSound;
            }
        }

        public MediaType MediaType
        {
            get
            {
                return mediaType;
            }
        }

        public string AppName
        {
            get
            {
                return appName;
            }
        }

        public string Format
        {
            get
            {
                return format;
            }
            set
            {
                format = value;
            }
        }

        //This is used in conjunction with format
        public string[] CurrentMediaContent
        {
            get
            {
                return content;
            }
            set
            {
                content = value;
            }
        }

        public string ToDebugString()
        {
            return System.Text.UTF8Encoding.UTF8.GetString(nsMessage.InnerBody);
        }

        public override string ToString()
        {
            return personalMessage;
        }

        private void Handle()
        {
            if (nsMessage.InnerBody == null)
                return;

            XmlDocument xmlDoc = new XmlDocument();
            MemoryStream ms = new MemoryStream(nsMessage.InnerBody);
            TextReader reader = new StreamReader(ms, new System.Text.UTF8Encoding(false));

            xmlDoc.Load(reader);

            XmlNode node = xmlDoc.SelectSingleNode("//Data/PSM");
            if (node != null)
            {
                personalMessage = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);
            }

            node = xmlDoc.SelectSingleNode("//Data/CurrentMedia");
            if (node != null)
            {
                string cmedia = System.Web.HttpUtility.UrlDecode(node.InnerText, System.Text.Encoding.UTF8);

                if (cmedia.Length > 0)
                {
                    string[] vals = cmedia.Split(new string[] { @"\0" }, StringSplitOptions.None);

                    if (vals[0] != "")
                        appName = vals[0];

                    switch (vals[1])
                    {
                        case "Music":
                            mediaType = MediaType.Music;
                            break;
                        case "Games":
                            mediaType = MediaType.Games;
                            break;
                        case "Office":
                            mediaType = MediaType.Office;
                            break;
                    }

                    /*
                    0 
                    1 Music
                    2 1
                    3 {0} - {1}
                    4 Evanescence
                    5 Call Me When You're Sober
                    6 Album
                    7 WMContentID
                    8 
                    */
                    //vals[2] = Enabled/Disabled

                    format = vals[3];

                    int size = vals.Length - 4;

                    content = new String[size];

                    for (int i = 0; i < size; i++)
                        content[i] = vals[i + 4];
                }
            }

            node = xmlDoc.SelectSingleNode("//Data/MachineGuid");
            if (node != null && node.InnerText != String.Empty)
                machineGuid = new Guid(node.InnerText);

            node = xmlDoc.SelectSingleNode("//Data/SignatureSound");
            if (node != null)
                signatureSound = node.InnerText;

        }
    }
};