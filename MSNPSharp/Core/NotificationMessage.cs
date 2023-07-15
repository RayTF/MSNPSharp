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
using System.Xml;
using System.Text;
using System.Diagnostics;
using System.Globalization;

namespace MSNPSharp.Core
{
    /// <summary>
    /// Represents a single NOT or IPG message.
    /// </summary>
    /// <remarks>
    /// These messages are receid from, and send to, a nameserver. NOT messages are rececived for MSN-Calendar or MSN-Alert notifications.
    /// IPG commands are received/send to exchange pager (sms) messages.
    /// </remarks>
    [Serializable()]
    public class NotificationMessage : MSNMessage
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationMessage()
        {
        }

        /// <summary>
        /// Constructs a NotificationMessage from the inner body contents of the specified message object.
        /// This will also set the InnerMessage property of the message object to the newly created NotificationMessage.
        /// </summary>
        public NotificationMessage(NetworkMessage message)
        {
            ParseBytes(message.InnerBody);
            message.InnerMessage = this;
        }
        /* Example notification
         * <NOTIFICATION ver="1" siteid="111100200" siteurl="http://calendar.msn.com" id="1">\r\n
          <TO pid="0x00060000:0x81ee5a43" name="example@passport.com" />\r\n
          <MSG pri="" id="1">\r\n
            <ACTION url="/calendar/isapi.dll?request=action&operation=modify&objectID=1&uicode1=modifyreminder&locale=2052"/>\r\n
            <SUBSCR url="/calendar/isapi.dll?request=action&operation=modify&objectID=1&uicode1=modifyreminder&locale=2052"/><CAT id="111100201" />\r\n
            <BODY lang="2052" icon="/En/img/calendar.png">\r\n
              <TEXT>goto club 7. 2002 21:15 - 22:15 </TEXT>\r\n
            </BODY>\r\n
          </MSG>\r\n
        </NOTIFICATION>\r\n
        */

        #region Private

        private bool notificationTypeSpecified = false;

        public bool NotificationTypeSpecified
        {
            get { return notificationTypeSpecified; }
            set { notificationTypeSpecified = value; }
        }

        /// <summary>
        /// </summary>
        NotificationType notificationType = NotificationType.Alert;

        public NotificationType NotificationType
        {
            get { return notificationType; }
            set { notificationType = value; }
        }

        private int id = 0;

        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        private int siteId = 0;

        public int SiteId
        {
            get { return siteId; }
            set { siteId = value; }
        }
        private string siteUrl = string.Empty;

        public string SiteUrl
        {
            get { return siteUrl; }
            set { siteUrl = value; }
        }

        private string receiverAccount = string.Empty;

        public string ReceiverAccount
        {
            get { return receiverAccount; }
            set { receiverAccount = value; }
        }
        private string receiverOfflineMail = string.Empty;

        public string ReceiverOfflineMail
        {
            get { return receiverOfflineMail; }
            set { receiverOfflineMail = value; }
        }
        private string receiverMemberIdLow = string.Empty;

        public string ReceiverMemberIdLow
        {
            get { return receiverMemberIdLow; }
            set { receiverMemberIdLow = value; }
        }
        private string receiverMemberIdHigh = string.Empty;

        public string ReceiverMemberIdHigh
        {
            get { return receiverMemberIdHigh; }
            set { receiverMemberIdHigh = value; }
        }

        private string senderAccount = string.Empty;

        public string SenderAccount
        {
            get { return senderAccount; }
            set { senderAccount = value; }
        }
        private string senderMemberIdLow = string.Empty;

        public string SenderMemberIdLow
        {
            get { return senderMemberIdLow; }
            set { senderMemberIdLow = value; }
        }
        private string senderMemberIdHigh = string.Empty;

        public string SenderMemberIdHigh
        {
            get { return senderMemberIdHigh; }
            set { senderMemberIdHigh = value; }
        }

        private string sendDevice = string.Empty;

        public string SendDevice
        {
            get { return sendDevice; }
            set { sendDevice = value; }
        }

        private int messageId = 0;

        public int MessageId
        {
            get { return messageId; }
            set { messageId = value; }
        }

        private string pri = string.Empty;

        public string Pri
        {
            get { return pri; }
            set { pri = value; }
        }

        private string actionUrl = string.Empty;

        public string ActionUrl
        {
            get { return actionUrl; }
            set { actionUrl = value; }
        }
        private string subcriptionUrl = string.Empty;

        public string SubcriptionUrl
        {
            get { return subcriptionUrl; }
            set { subcriptionUrl = value; }
        }

        private string catId = "110110001";

        public string CatId
        {
            get { return catId; }
            set { catId = value; }
        }

        private string language = string.Empty;

        public string Language
        {
            get { return language; }
            set { language = value; }
        }

        private string iconUrl = string.Empty;

        public string IconUrl
        {
            get { return iconUrl; }
            set { iconUrl = value; }
        }

        private string text = string.Empty;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        private string offlineText = string.Empty;

        public string OfflineText
        {
            get { return offlineText; }
            set { offlineText = value; }
        }

        private string bodyPayload = string.Empty;

        public string BodyPayload
        {
            get { return bodyPayload; }
            set { bodyPayload = value; }
        }


        #endregion

        #region Public

        /// <summary>
        /// Creates a xml message based on the data in the object. It is used before the message is send to the server.
        /// </summary>
        protected virtual XmlDocument CreateXmlMessage()
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("NOTIFICATION");

            if (NotificationTypeSpecified)
                root.Attributes.Append(doc.CreateAttribute("ver")).Value = ((int)NotificationType).ToString();

            root.Attributes.Append(doc.CreateAttribute("id")).Value = Id.ToString();
            if (siteId > 0)
                root.Attributes.Append(doc.CreateAttribute("siteid")).Value = SiteId.ToString();
            if (siteUrl.Length > 0)
                root.Attributes.Append(doc.CreateAttribute("siteurl")).Value = SiteUrl;

            XmlElement to = doc.CreateElement("TO");
            if (ReceiverMemberIdLow.Length > 0 && ReceiverMemberIdHigh.Length > 0)
                to.Attributes.Append(doc.CreateAttribute("pid")).Value = ReceiverMemberIdLow.ToString() + ":" + ReceiverMemberIdHigh.ToString();
            if (ReceiverAccount.Length > 0)
                to.Attributes.Append(doc.CreateAttribute("name")).Value = ReceiverAccount;
            if (ReceiverOfflineMail.Length > 0)
                to.Attributes.Append(doc.CreateAttribute("email")).Value = ReceiverOfflineMail;
            if (SendDevice.Length > 0)
            {
                XmlElement via = doc.CreateElement("VIA");
                via.Attributes.Append(doc.CreateAttribute("agent")).Value = SendDevice;
                to.AppendChild(via);
            }
            root.AppendChild(to);

            XmlElement from = doc.CreateElement("FROM");
            if (SenderMemberIdLow.Length > 0 && SenderMemberIdHigh.Length > 0)
                from.Attributes.Append(doc.CreateAttribute("pid")).Value = SenderMemberIdLow.ToString() + ":" + SenderMemberIdHigh.ToString();
            if (SenderAccount.Length > 0)
                from.Attributes.Append(doc.CreateAttribute("name")).Value = SenderAccount;
            root.AppendChild(from);

            XmlElement msg = doc.CreateElement("MSG");
            if (Pri.Length > 0)
                msg.Attributes.Append(doc.CreateAttribute("pri")).Value = Pri.ToString();

            msg.Attributes.Append(doc.CreateAttribute("id")).Value = MessageId.ToString();

            if (ActionUrl.Length > 0)
            {
                XmlElement action = doc.CreateElement("ACTION");
                action.Attributes.Append(doc.CreateAttribute("url")).Value = ActionUrl;
                msg.AppendChild(action);
            }
            if (SubcriptionUrl.Length > 0)
            {
                XmlElement subscr = doc.CreateElement("SUBSCR");
                subscr.Attributes.Append(doc.CreateAttribute("url")).Value = SubcriptionUrl;
                msg.AppendChild(subscr);
            }
            if (CatId.Length > 0)
            {
                XmlElement cat = doc.CreateElement("CAT");
                cat.Attributes.Append(doc.CreateAttribute("id")).Value = CatId.ToString();
                msg.AppendChild(cat);
            }

            XmlElement body = doc.CreateElement("BODY");
            if (Language.Length > 0)
                body.Attributes.Append(doc.CreateAttribute("id")).Value = Language;
            if (IconUrl.Length > 0)
                body.Attributes.Append(doc.CreateAttribute("icon")).Value = IconUrl;
            if (Text.Length > 0)
            {
                XmlElement textEl = doc.CreateElement("TEXT");
                textEl.AppendChild(doc.CreateTextNode(Text));
                body.AppendChild(textEl);
            }

            if (OfflineText.Length > 0)
            {
                XmlElement emailTextEl = doc.CreateElement("EMAILTEXT");
                emailTextEl.AppendChild(doc.CreateTextNode(OfflineText));
                body.AppendChild(emailTextEl);
            }
            msg.AppendChild(body);

            root.AppendChild(msg);

            doc.AppendChild(root);

            return doc;

        }

        /// <summary>
        /// Returns the command message as a byte array. This can be directly send over a networkconnection.
        /// </summary>
        /// <remarks>
        /// Remember to set the transaction ID before calling this method.
        /// Uses UTF8 Encoding.
        /// </remarks>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            return new byte[] { 0x00 };
            //throw new MSNPSharpException("You can't send notification messages yourself. It is only possible to retrieve them.");
        }

        /// <summary>
        /// Parses incoming byte data send from the network.
        /// </summary>
        /// <param name="data">The raw message as received from the server</param>
        public override void ParseBytes(byte[] data)
        {
            if (data != null)
            {
                // retrieve the innerbody
                XmlDocument xmlDoc = new XmlDocument();

                TextReader reader = new StreamReader(new MemoryStream(data), new System.Text.UTF8Encoding(false));

                Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, reader.ReadToEnd(), GetType().Name);

                reader = new StreamReader(new MemoryStream(data), new System.Text.UTF8Encoding(false));
                xmlDoc.Load(reader);

                // Root node: NOTIFICATION
                XmlNode node = xmlDoc.SelectSingleNode("//NOTIFICATION");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("ver") != null)
                    {
                        NotificationType = (NotificationType)int.Parse(node.Attributes.GetNamedItem("ver").Value);
                        NotificationTypeSpecified = true;
                    }

                    if (node.Attributes.GetNamedItem("id") != null)
                        Id = int.Parse(node.Attributes.GetNamedItem("id").Value);
                    if (node.Attributes.GetNamedItem("siteid") != null)
                        SiteId = int.Parse(node.Attributes.GetNamedItem("siteid").Value);
                    if (node.Attributes.GetNamedItem("siteurl") != null)
                        SiteUrl = node.Attributes.GetNamedItem("siteurl").Value;
                }

                // TO element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/TO");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("pid") != null)
                    {
                        string[] values = node.Attributes.GetNamedItem("pid").Value.Split(':');
                        ReceiverMemberIdLow = values[0];
                        ReceiverMemberIdHigh = values[1];
                    }
                    if (node.Attributes.GetNamedItem("name") != null)
                        ReceiverAccount = node.Attributes.GetNamedItem("name").Value;
                    if (node.Attributes.GetNamedItem("email") != null)
                        ReceiverOfflineMail = node.Attributes.GetNamedItem("email").Value;
                }

                // VIA element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/TO/VIA");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("agent") != null)
                        SendDevice = node.Attributes.GetNamedItem("agent").Value;
                }

                // FROM element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/FROM");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("pid") != null)
                    {
                        string[] values = node.Attributes.GetNamedItem("pid").Value.Split(':');
                        SenderMemberIdLow = values[0];
                        SenderMemberIdHigh = values[1];
                    }
                    if (node.Attributes.GetNamedItem("name") != null)
                        SenderAccount = node.Attributes.GetNamedItem("name").Value;
                }

                // MSG element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("pri") != null)
                        Pri = node.Attributes.GetNamedItem("pri").Value;
                    if (node.Attributes.GetNamedItem("id") != null)
                        MessageId = int.Parse(node.Attributes.GetNamedItem("id").Value);

                }

                // ACTION element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/ACTION");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("url") != null)
                        ActionUrl = node.Attributes.GetNamedItem("url").Value;
                }

                // SUBSCR element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/SUBSCR");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("url") != null)
                        SubcriptionUrl = node.Attributes.GetNamedItem("url").Value;
                }

                // CAT element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/CAT");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("id") != null)
                        CatId = node.Attributes.GetNamedItem("id").Value;
                }

                // BODY element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/BODY");
                if (node != null)
                {
                    if (node.Attributes.GetNamedItem("lang") != null)
                        Language = node.Attributes.GetNamedItem("lang").Value;
                    if (node.Attributes.GetNamedItem("icon") != null)
                        IconUrl = node.Attributes.GetNamedItem("icon").Value;
                }

                if (!NotificationTypeSpecified && Id == 0 && node != null)
                {
                    bodyPayload = node.InnerText;
                }

                // TEXT element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/BODY/TEXT");
                if (node != null)
                {
                    Text = node.Value;
                }

                // EMAILTEXT element
                node = xmlDoc.SelectSingleNode("/NOTIFICATION/MSG/BODY/EMAILTEXT");
                if (node != null)
                {
                    OfflineText = node.Value;
                }


            }
            else
                throw new MSNPSharpException("NotificationMessage expected payload data, but not InnerBody is present.");
        }




        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return System.Text.UTF8Encoding.UTF8.GetString(this.GetBytes());
        }

        #endregion
    }
};
