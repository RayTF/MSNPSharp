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
using System.Web;
using System.Text;
using System.Drawing;
using System.Collections;
using System.Text.RegularExpressions;

namespace MSNPSharp
{
    using MSNPSharp.Core;

    /// <summary>
    /// Represents a single plain textual message send over a switchboard (conversation).
    /// </summary>
    /// <remarks>
    /// These message objects are dispatched by events. 
    /// </remarks>
    [Serializable()]
    public class TextMessage : NetworkMessage, ICloneable
    {
        /// <summary>
        /// The body of the message
        /// </summary>
        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text = value;
            }
        }

        /// <summary>
        /// </summary>
        private string text = String.Empty;

        /// <summary>
        /// The font used in the message. Default is 'Arial'
        /// </summary>
        public string Font
        {
            get
            {
                return font;
            }
            set
            {
                font = value;
            }
        }

        /// <summary>
        /// </summary>
        private string font = "Arial";

        /// <summary>
        /// The color used in the message. Default is black.
        /// </summary>
        public System.Drawing.Color Color
        {
            get
            {
                return color;
            }
            set
            {
                color = value;
            }
        }

        /// <summary>
        /// </summary>
        private System.Drawing.Color color = Color.Black;

        /// <summary>
        /// The decorations used in the message.
        /// </summary>
        /// <remarks>
        /// When there are multiple decorations used the values are bitwise OR'ed!
        /// Example to check for bold: 
        /// <code>if((Decorations &amp; MSNTextDecorations.Bold) > 0) ....</code>
        /// If you want to use multiple decorations in a new message:
        /// <code>textMessage.Decorations = TextDecorations.Underline | TextDecorations.Italic</code>
        /// </remarks>		
        public TextDecorations Decorations
        {
            get
            {
                return decorations;
            }
            set
            {
                decorations = value;
            }
        }

        /// <summary>
        /// </summary>
        private TextDecorations decorations = TextDecorations.None;

        /// <summary>
        /// The charset used in the message. Default is the ANSI charset.
        /// </summary>
        public MessageCharSet CharSet
        {
            get
            {
                return charSet;
            }
            set
            {
                charSet = value;
            }
        }

        /// <summary>
        /// </summary>
        private MessageCharSet charSet = MessageCharSet.Ansi;

        /// <summary>
        /// </summary>
        private bool rightToLeft;

        /// <summary>
        /// Text is read right-to-left
        /// </summary>
        public bool RightToLeft
        {
            get
            {
                return rightToLeft;
            }
            set
            {
                rightToLeft = value;
            }
        }

        /// <summary>
        /// The (optional) custom nickname of this message
        /// </summary>
        private string customNickname = string.Empty;

        /// <summary>
        /// The (optional) custom nickname of this message
        /// </summary>
        public string CustomNickname
        {
            get
            {
                return customNickname;
            }
            set
            {
                customNickname = value;
            }
        }

        /// <summary>
        /// Parses the header in the parent message and sets the style properties.
        /// </summary>
        /// <param name="containerMessage"></param>
        public override void CreateFromParentMessage(NetworkMessage containerMessage)
        {
            base.CreateFromParentMessage(containerMessage);

            if (ParentMessage == null)
                return;

            // we expect a MSGMessage object
            MimeMessage MSGMessage = (MimeMessage)ParentMessage;

            // parse the header from the parent message
            ParseHeader(MSGMessage.MimeHeader);
        }

        /// <summary>
        /// Sets the Text property.
        /// </summary>
        /// <param name="data"></param>
        public override void ParseBytes(byte[] data)
        {
            // set the text property for easy retrieval
            Text = System.Text.Encoding.UTF8.GetString(data);
        }


        /// <summary>
        /// Gets the style string specifying charset, font, etc. This is used in the MIME header send with a switchboard message.
        /// </summary>
        /// <returns></returns>
        internal string GetStyleString()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("FN=").Append(MSNHttpUtility.UrlEncode(Font.ToString()));
            builder.Append("; EF=");
            if (((int)Decorations & (int)TextDecorations.Italic) > 0)
                builder.Append('I');
            if (((int)Decorations & (int)TextDecorations.Bold) > 0)
                builder.Append('B');
            if (((int)Decorations & (int)TextDecorations.Underline) > 0)
                builder.Append('U');
            if (((int)Decorations & (int)TextDecorations.Strike) > 0)
                builder.Append('S');
            builder.Append("; CO=");
            builder.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:x2}", new object[] { Color.B }));
            builder.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:x2}", new object[] { Color.G }));
            builder.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:x2}", new object[] { Color.R }));
            builder.Append("; CS=").Append(((int)CharSet).ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (rightToLeft)
                builder.Append("; RL=1");
            builder.Append("; PF=22");

            return builder.ToString();
        }

        /// <summary>
        /// Gets the header with the body appended as a byte array
        /// </summary>
        /// <returns></returns>
        public override byte[] GetBytes()
        {
            return System.Text.Encoding.UTF8.GetBytes(Text);
        }

        /// <summary>
        /// Parses the raw header to set the member variables
        /// </summary>
        internal void ParseHeader(StrDictionary mimeHeader)
        {
            // example header: "X-MMS-IM-Format: FN=Microsoft%20Sans%20Serif; EF=I; CO=000000; CS=0; PF=22"
            if (mimeHeader.ContainsKey(MimeHeaderStrings.X_MMS_IM_Format))
            {
                string[] fields = mimeHeader[MimeHeaderStrings.X_MMS_IM_Format].Split(';');
                string FNValue = string.Empty;
                string EFValue = string.Empty;
                string CSValue = string.Empty;
                string COValue = string.Empty;
                string PFValue = string.Empty;

                foreach (string field in fields)
                {
                    if (field.Trim().IndexOf("FN=") == 0)
                    {
                        FNValue = field.Split('=')[1];
                    }

                    if (field.Trim().IndexOf("EF=") == 0)
                    {
                        EFValue = field.Split('=')[1];
                    }

                    if (field.Trim().IndexOf("CS=") == 0)
                    {
                        CSValue = field.Split('=')[1];
                    }

                    if (field.Trim().IndexOf("CO=") == 0)
                    {
                        COValue = field.Split('=')[1];
                    }

                    if (field.Trim().IndexOf("PF=") == 0)
                    {
                        PFValue = field.Split('=')[1];
                    }
                }

                if (FNValue != string.Empty)
                {
                    Font = HttpUtility.UrlDecode(FNValue);
                }

                if (EFValue != string.Empty)
                {
                    Decorations = TextDecorations.None;
                    string dec = EFValue;
                    if (dec.IndexOf('I') >= 0)
                        Decorations |= TextDecorations.Italic;
                    if (dec.IndexOf('B') >= 0)
                        Decorations |= TextDecorations.Bold;
                    if (dec.IndexOf('U') >= 0)
                        Decorations |= TextDecorations.Underline;
                    if (dec.IndexOf('S') >= 0)
                        Decorations |= TextDecorations.Strike;
                }

                if (COValue != string.Empty)
                {
                    string color = COValue;

                    if (color.Length < 6)
                    {
                        for (int i = 0; i < 6 - COValue.Length; i++)
                        {
                            color = "0" + color;
                        }
                    }

                    try
                    {
                        if (color.Length >= 6)
                            Color = System.Drawing.Color.FromArgb(
                                int.Parse(color.Substring(4, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture), // R
                                int.Parse(color.Substring(2, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture), // G
                                int.Parse(color.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture)  // B												
                                );
                    }
                    catch (Exception)
                    {
                    }
                }

                if (CSValue != string.Empty)
                {
                    try
                    {
                        CharSet = (MessageCharSet)int.Parse(CSValue, System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                    }
                }

                if (mimeHeader.ContainsKey(MimeHeaderStrings.P4_Context))
                    this.customNickname = mimeHeader[MimeHeaderStrings.P4_Context];
            }
        }

        /// <summary>
        /// Basic constructor.
        /// </summary>
        public TextMessage()
        {
        }

        /// <summary>
        /// Textual presentation.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text.Replace("\r", "\\r").Replace("\n", "\\n\n");
        }


        /// <summary>
        /// Creates a TextMessage with the specified text as message.
        /// </summary>
        /// <remarks>
        /// This leaves all style attributes to their default values.
        /// </remarks>
        /// <param name="message"></param>
        public TextMessage(string message)
        {
            Text = message;
        }

        #region ICloneable Members

        public object Clone()
        {
            TextMessage message = new TextMessage();
            message.charSet = this.charSet;
            message.color = this.color;
            message.customNickname = this.customNickname;
            message.decorations = this.decorations;
            message.font = this.font;
            message.InnerBody = this.InnerBody;
            message.rightToLeft = this.rightToLeft;
            message.text = this.text;

            return message;
        }

        #endregion

    }
};
