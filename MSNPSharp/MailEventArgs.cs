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
using System.Net;
using System.Text.RegularExpressions;
using System.Text;
using System.Diagnostics;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    /// <summary>
    /// Send as event argument when the server has send a (initial) mailbox status.
    /// </summary>
    [Serializable()]
    public class MailboxStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Number of mails in the inbox which are unread
        /// </summary>
        public int InboxUnread
        {
            get
            {
                return inboxUnread;
            }
            set
            {
                inboxUnread = value;
            }
        }

        /// <summary>
        /// </summary>
        private int inboxUnread;

        /// <summary>
        /// Number of folders which are unread
        /// </summary>
        public int FoldersUnread
        {
            get
            {
                return foldersUnread;
            }
            set
            {
                foldersUnread = value;
            }
        }

        /// <summary>
        /// </summary>
        private int foldersUnread;

        /// <summary>
        /// The URL to go directly to the inbox of the contactlist owner
        /// </summary>
        public string InboxURL
        {
            get
            {
                return inboxURL;
            }
            set
            {
                inboxURL = value;
            }
        }

        /// <summary>
        /// </summary>
        private string inboxURL;

        /// <summary>
        /// The URL to go directly to the folders of the contactlist owner
        /// </summary>
        public string FoldersURL
        {
            get
            {
                return foldersURL;
            }
            set
            {
                foldersURL = value;
            }
        }

        /// <summary>
        /// </summary>
        private string foldersURL;

        /// <summary>
        /// The URL to go directly to the webpage to compose a new mail
        /// </summary>
        public string PostURL
        {
            get
            {
                return postURL;
            }
            set
            {
                postURL = value;
            }
        }

        /// <summary>
        /// </summary>
        private string postURL;

        /// <summary>
        /// Constructory.
        /// </summary>
        /// <param name="inboxUnread"></param>
        /// <param name="foldersUnread"></param>
        /// <param name="inboxURL"></param>
        /// <param name="foldersURL"></param>
        /// <param name="postURL"></param>
        public MailboxStatusEventArgs(int inboxUnread, int foldersUnread, string inboxURL, string foldersURL, string postURL)
        {
            InboxUnread = inboxUnread;
            FoldersUnread = foldersUnread;
            InboxURL = inboxURL;
            FoldersURL = foldersURL;
            PostURL = postURL;
        }
    }

    /// <summary>
    /// Send as event argument when unread mail has been read or the owner moves mail.
    /// </summary>
    [Serializable()]
    public class MailChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The source folder the mail(s) are moved from
        /// </summary>
        public string SourceFolder
        {
            get
            {
                return sourceFolder;
            }
            set
            {
                sourceFolder = value;
            }
        }

        /// <summary>
        /// </summary>
        private string sourceFolder;

        /// <summary>
        /// The destination folder the mail(s) are moved to
        /// </summary>
        public string DestinationFolder
        {
            get
            {
                return destinationFolder;
            }
            set
            {
                destinationFolder = value;
            }
        }

        /// <summary>
        /// </summary>
        private string destinationFolder;

        /// <summary>
        /// The number of mails moved
        /// </summary>
        public int Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
            }
        }

        /// <summary>
        /// </summary>
        private int count;

        /// <summary>
        /// Indicates whether mails are moved between folders or if unread mails are read.
        /// When sourcefolder and destination folder are the same this means the mails are not moved but read, and MailsRead will return true. Otherwise it will return false.
        /// </summary>
        public bool MailsAreRead
        {
            get
            {
                return SourceFolder == DestinationFolder;
            }
        }

        /// <summary>
        /// Constructor, mainly used internal by the library.
        /// </summary>
        /// <param name="sourceFolder"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="count"></param>
        public MailChangedEventArgs(string sourceFolder, string destinationFolder, int count)
        {
            SourceFolder = sourceFolder;
            DestinationFolder = destinationFolder;
            Count = count;
        }
    }

    /// <summary>
    /// Send as event argument when the server notifies us of a new e-mail waiting.
    /// </summary>
    [Serializable()]
    public class NewMailEventArgs : EventArgs
    {
        /// <summary>
        /// The person's name who sended the e-mail
        /// </summary>
        public string From
        {
            get
            {
                return from;
            }
            set
            {
                try
                {
                    Regex senderReg = new Regex("=\\u003F(?<encoding>.+)\\u003F(?<decoder>.)\\u003F(?<from>.+)\\u003F=");
                    if (senderReg.Match(value).Success)
                    {
                        string strencoding = senderReg.Match(value).Groups["encoding"].Value;
                        string strdecode = senderReg.Match(value).Groups["decoder"].Value;
                        string encodedfrom = senderReg.Match(value).Groups["from"].Value;
                        Encoding encode = Encoding.GetEncoding(strencoding);
                        byte[] bytfrom = null;
                        if (strdecode.ToLowerInvariant() == "b")
                        {
                            bytfrom = Convert.FromBase64String(encodedfrom);
                            from = encode.GetString(bytfrom);
                            return;
                        }
                        if (strdecode.ToLowerInvariant() == "q")
                        {
                            //I GUSS this can work.
                            from = MSNHttpUtility.QPDecode(encodedfrom, encode);
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                }
                from = value;
            }
        }

        /// <summary>
        /// </summary>
        private string from;

        /// <summary>
        /// The url to directly view the message in Hotmail
        /// </summary>
        public string MessageUrl
        {
            get
            {
                return messageUrl;
            }
            set
            {
                messageUrl = value;
            }
        }

        /// <summary>
        /// </summary>
        private string messageUrl;

        /// <summary>
        /// The post url used for automatic hotmail login
        /// </summary>
        public string PostUrl
        {
            get
            {
                return postUrl;
            }
            set
            {
                postUrl = value;
            }
        }

        /// <summary>
        /// </summary>
        private string postUrl;

        /// <summary>
        /// The subject of the e-mail
        /// </summary>
        public string Subject
        {
            get
            {
                return subject;
            }
            set
            {
                subject = value;
            }
        }

        /// <summary>
        /// </summary>
        private string subject;

        /// <summary>
        /// The folder the mail is redirected to
        /// </summary>
        public string DestinationFolder
        {
            get
            {
                return destinationFolder;
            }
            set
            {
                destinationFolder = value;
            }
        }

        /// <summary>
        /// </summary>
        private string destinationFolder;

        /// <summary>
        /// The e-mail adress of the person who sended the e-mail
        /// </summary>
        public string FromEmail
        {
            get
            {
                return fromEmail;
            }
            set
            {
                fromEmail = value;
            }
        }

        /// <summary>
        /// </summary>
        private string fromEmail;

        /// <summary>
        /// ID of the message, used for hotmail login
        /// </summary>
        public int Id
        {
            get
            {
                return id;
            }
            set
            {
                id = value;
            }
        }

        /// <summary>
        /// </summary>
        private int id;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="messageUrl"></param>
        /// <param name="postUrl"></param>
        /// <param name="subject"></param>
        /// <param name="destinationFolder"></param>
        /// <param name="fromEmail"></param>
        /// <param name="id"></param>
        public NewMailEventArgs(string from, string messageUrl, string postUrl, string subject, string destinationFolder, string fromEmail, int id)
        {
            From = from;
            PostUrl = postUrl;
            MessageUrl = messageUrl;
            Subject = subject;
            DestinationFolder = destinationFolder;
            FromEmail = fromEmail;
            Id = id;
        }
    }
};
