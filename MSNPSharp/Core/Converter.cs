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
using System.Xml;
using System.Security.Cryptography;

namespace MSNPSharp.Core
{
    using MSNPSharp.DataTransfer;

    internal enum EscapeType
    {
        EscapeAll,
        EscapeExceptPlus
    }

    /// <summary>
    /// Provides methods for encoding and decoding URLs when processing Web requests. This class cannot be inherited. 
    /// </summary>
    public static class MSNHttpUtility
    {
        private enum UnSafe
        {
            /// <summary>
            /// For url encode
            /// </summary>
            UrlEscape = 0x1,
            /// <summary>
            /// For XML encode
            /// </summary>
            XMLEscape = 0x2,
            /// <summary>
            /// For HTML encode
            /// </summary>
            HTMLEscape = 0x4
        }

        private static uint[] ASCII_CLASS;
        private static string strUrlUnsafe = " \"#%&+,/:;<=>?@[\\]^`{|}";
        private static string strXmlUnsafe = "&:;<=>?[]\\^{|}";
        private static string strHtmlUnsafe = "&'<> ;\"";

        static MSNHttpUtility()
        {
            ASCII_CLASS = new uint[256];
            int c = 0;
            for (c = 0; c < ASCII_CLASS.Length; c++)
            {
                if ((c >= 0 && c <= 32) || c >= 126)
                {
                    ASCII_CLASS[c] |= (uint)UnSafe.UrlEscape;
                }

                if (c >= 0 && c <= 32)
                {
                    ASCII_CLASS[c] |= (uint)UnSafe.XMLEscape;
                }

                if (strUrlUnsafe.IndexOf((char)c) != -1)
                {
                    ASCII_CLASS[c] |= (uint)UnSafe.UrlEscape;
                }
                if (strXmlUnsafe.IndexOf((char)c) != -1)
                {
                    ASCII_CLASS[c] |= (uint)UnSafe.XMLEscape;
                }
                if (strHtmlUnsafe.IndexOf((char)c) != -1)
                {
                    ASCII_CLASS[c] |= (uint)UnSafe.HTMLEscape;
                }
            }

        }

        /// <summary>
        /// Encodes a MSNObject description using UTF-8 encoding by default.
        /// </summary>
        /// <param name="str">The MSNObject description to encode.</param>
        /// <returns>An encoded string.</returns>
        public static string MSNObjectUrlEncode(string str)
        {
            return UrlEncode(str, Encoding.UTF8, EscapeType.EscapeExceptPlus);
        }

        /// <summary>
        /// Replace space into %20.
        /// </summary>
        /// <param name="str">The string to encode.</param>
        /// <returns>An encoded string.</returns>
        public static string NSEncode(string str)
        {
            return str.Replace(" ", "%20");
        }

        /// <summary>
        /// Encodes a URL string using UTF-8 encoding by default.
        /// </summary>
        /// <param name="str">The text to encode.</param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string str)
        {
            return UrlEncode(str, Encoding.UTF8, EscapeType.EscapeAll);
        }

        /// <summary>
        /// Encodes a URL string using the specified encoding object.
        /// </summary>
        /// <param name="str">The text to encode.</param>
        /// <param name="e">The <see cref="Encoding"/> object that specifies the encoding scheme. </param>
        /// <returns>An encoded string.</returns>
        public static string UrlEncode(string str, Encoding e)
        {
            return UrlEncode(str, e, EscapeType.EscapeAll);
        }

        private static string UrlEncode(string str, Encoding e, EscapeType type)
        {
            if (str == null)
                return string.Empty;
            byte[] byt = e.GetBytes(str);
            StringBuilder result = new StringBuilder(256);
            for (int c = 0; c < byt.Length; c++)
            {
                byte chr = byt[c];
                if ((ASCII_CLASS[chr] & (uint)UnSafe.UrlEscape) != 0)
                {
                    switch (chr)
                    {
                        case (byte)'+':
                            if (type == EscapeType.EscapeAll)
                            {
                                result.Append("%20");
                            }
                            else if (type == EscapeType.EscapeExceptPlus)
                            {
                                result.Append("%2B");
                            }

                            break;
                        default:
                            result.Append("%" + ((int)chr).ToString("X2"));
                            break;

                    }
                }
                else
                {
                    result.Append((char)chr);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Decode an encoded <see cref="MSNObject"/>.
        /// </summary>
        /// <param name="str">The encoded MSNObject description.</param>
        /// <returns>A decoded string.</returns>
        public static string MSNObjectUrlDecode(string str)
        {
            return UrlDecode(str, Encoding.UTF8, EscapeType.EscapeExceptPlus);
        }

        /// <summary>
        /// Converts a string that has been encoded for transmission in a URL into a decoded string using UTF-8 encoding by default.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string str)
        {
            return UrlDecode(str, Encoding.UTF8, EscapeType.EscapeAll);
        }

        /// <summary>
        /// Converts a URL-encoded string into a decoded string, using the specified encoding object.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <param name="e">The <see cref="Encoding"/> that specifies the decoding scheme.</param>
        /// <returns>A decoded string.</returns>
        public static string UrlDecode(string str, Encoding e)
        {
            return UrlDecode(str, e, EscapeType.EscapeAll);
        }

        /// <summary>
        /// Replace "%20" into space.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string NSDecode(string str)
        {
            return str.Replace("%20", " ");
        }

        private static string UrlDecode(string str, Encoding e, EscapeType type)
        {
            if (type == EscapeType.EscapeExceptPlus)
            {
                return HttpUtility.UrlDecode(str.Replace("%2b", "+").Replace("%2B", "+"), e);
            }

            return HttpUtility.UrlDecode(str.Replace("%20", "+"), e);
        }

        /// <summary>
        /// Encodes a Xml string.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string XmlEncode(string str)
        {
            if (str == null)
                return string.Empty;

            char[] chrArr = str.ToCharArray();
            char chr;
            StringBuilder result = new StringBuilder(256);
            for (int c = 0; c < chrArr.Length; c++)
            {
                chr = chrArr[c];

                if (chr < 128)
                {
                    if ((ASCII_CLASS[chr] & (uint)UnSafe.XMLEscape) != 0)
                    {
                        result.Append("&#x" + ((int)chr).ToString("X2") + ";");
                        continue;
                    }
                }

                result.Append(chr);
            }

            return result.ToString();
        }


        /// <summary>
        /// Decode the QP encoded string.
        /// </summary>
        /// <param name="str">The string to decode.</param>
        /// <returns>A decoded string.</returns>
        public static string QPDecode(string str)
        {
            return QPDecode(str, Encoding.Default);
        }

        /// <summary>
        /// Decode the QP encoded string using an encoding
        /// </summary>
        /// <param name="value">The string to decode.</param>
        /// <param name="encode">The <see cref="Encoding"/> that specifies the decoding scheme.</param>
        /// <returns>A decoded string.</returns>
        public static string QPDecode(string value, Encoding encode)
        {
            string inputString = value;
            StringBuilder builder1 = new StringBuilder();
            inputString = inputString.Replace("=\r\n", "");
            for (int num1 = 0; num1 < inputString.Length; num1++)
            {
                if (inputString[num1] == '=')
                {
                    try
                    {
                        if (HexToDec(inputString.Substring(num1 + 1, 2)) < 0x80)
                        {
                            if (HexToDec(inputString.Substring(num1 + 1, 2)) >= 0)
                            {
                                byte[] buffer1 = new byte[1] { (byte)HexToDec(inputString.Substring(num1 + 1, 2)) };
                                builder1.Append(encode.GetString(buffer1));
                                num1 += 2;
                            }
                        }
                        else if (inputString[num1 + 1] != '=')
                        {
                            byte[] buffer2 = new byte[2] { (byte)HexToDec(inputString.Substring(num1 + 1, 2)), (byte)HexToDec(inputString.Substring(num1 + 4, 2)) };
                            builder1.Append(encode.GetString(buffer2));
                            num1 += 5;
                        }
                    }
                    catch
                    {
                        builder1.Append(inputString.Substring(num1, 1));
                    }
                }
                else
                {
                    builder1.Append(inputString.Substring(num1, 1));
                }
            }
            return builder1.ToString();
        }

        private static int HexToDec(string hex)
        {
            int num1 = 0;
            string text1 = "0123456789ABCDEF";
            for (int num2 = 0; num2 < hex.Length; num2++)
            {
                if (text1.IndexOf(hex[num2]) == -1)
                {
                    return -1;
                }
                num1 = (num1 * 0x10) + text1.IndexOf(hex[num2]);
            }
            return num1;
        }


        public static int IndexOf(byte[] input, byte[] pattern)
        {
            if (pattern.Length > input.Length)
                return -1;

            for (int i = 0; i <= input.Length - pattern.Length; i++)
            {
                bool found = true;

                for (int j = 0; j < pattern.Length; j++)
                {
                    if (input[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                    return i;
            }

            return -1;
        }

        public static int IndexOf(byte[] input, string pattern)
        {
            return IndexOf(input, Encoding.UTF8.GetBytes(pattern));
        }
    }

    public static class WebServiceDateTimeConverter
    {
        /// <summary>
        /// Convert the XML time to .net <see cref="DateTime"/> instance.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static DateTime ConvertToDateTime(string dateTime)
        {
            if (dateTime == null || dateTime == string.Empty)
                dateTime = WebServiceConstants.ZeroTime;

            return XmlConvert.ToDateTime(dateTime, WebServiceConstants.XmlDateTimeFormats);
        }
    }


    public static class HashedNonceGenerator
    {
        /// <summary>
        /// Creates handshake guid using SHA1 hash algorithm. 
        /// </summary>
        /// <param name="nonce"></param>
        /// <returns>The output packed to handshake for direct connect</returns>
        public static Guid HashNonce(Guid nonce)
        {
            // http://forums.fanatic.net.nz/index.php?showtopic=19372&view=findpost&p=108868
            // {2B95F56D-9CA0-9A64-82CE-ADC1F3C55845} <-> [0x37,0x29,0x2d,0x12,0x86,0x5c,0x7b,0x4c,0x81,0xf5,0xe,0x5,0x1,0x78,0x80,0xc2]
            // OUTPUT: 2b95f56d-9ca0-9a64-82ce-adc1f3c55845
            // INPUT: 122d2937-5c86-4c7b-81f5-0e05017880c2

            Guid handshakeNonce = Guid.Empty;

            using (SHA1Managed sha1 = new SHA1Managed())
            {
                // Returns 20 bytes
                byte[] hash = sha1.ComputeHash(nonce.ToByteArray());

                handshakeNonce = CreateGuidFromData(P2PVersion.P2PV2, hash);
            }

            return handshakeNonce;
        }


        public static Guid CreateGuidFromData(P2PVersion ver, byte[] data)
        {
            Guid ret = Guid.Empty;

            if (ver == P2PVersion.P2PV1)
            {
                P2PMessage message = new P2PMessage(ver);
                message.ParseBytes(data);

                ret = new Guid(
                    (int)message.V1Header.AckSessionId,

                    (short)(message.Header.AckIdentifier & 0x0000FFFF),
                    (short)((message.Header.AckIdentifier & 0xFFFF0000) >> 16),

                    (byte)((message.V1Header.AckTotalSize & 0x00000000000000FF)),
                    (byte)((message.V1Header.AckTotalSize & 0x000000000000FF00) >> 8),
                    (byte)((message.V1Header.AckTotalSize & 0x0000000000FF0000) >> 16),
                    (byte)((message.V1Header.AckTotalSize & 0x00000000FF000000) >> 24),
                    (byte)((message.V1Header.AckTotalSize & 0x000000FF00000000) >> 32),
                    (byte)((message.V1Header.AckTotalSize & 0x0000FF0000000000) >> 40),
                    (byte)((message.V1Header.AckTotalSize & 0x00FF000000000000) >> 48),
                    (byte)((message.V1Header.AckTotalSize & 0xFF00000000000000) >> 56)
                );
            }
            else if (ver == P2PVersion.P2PV2)
            {
                Int32 a = BitUtility.ToInt32(data, 0, BitConverter.IsLittleEndian);
                Int16 b = BitUtility.ToInt16(data, 4, BitConverter.IsLittleEndian);
                Int16 c = BitUtility.ToInt16(data, 6, BitConverter.IsLittleEndian);
                byte d = data[8], e = data[9], f = data[10], g = data[11];
                byte h = data[12], i = data[13], j = data[14], k = data[15];

                ret = new Guid(a, b, c, d, e, f, g, h, i, j, k);
            }

            return ret;
        }
    }
};