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
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Net.Cache;
using System.Web.Services;
using System.Diagnostics;
using System.Security.Permissions;
using System.Web.Services.Protocols;

namespace MSNPSharp.Services
{
    using MSNPSharp.MSNWS.MSNSecurityTokenService;
    using MSNPSharp.Framework;


    [System.Web.Services.WebServiceBindingAttribute(Name = "SecurityTokenServicePortBinding", Namespace = "http://schemas.microsoft.com/Passport/SoapServices/PPCRL")]
    public sealed class SecurityTokenServiceWrapper : SecurityTokenService
    {
        private IPEndPoint localEndPoint = null;

        public SecurityTokenServiceWrapper()
            : base()
        {
        }

        public SecurityTokenServiceWrapper(IPEndPoint localEndPoint)
            : base()
        {
            this.localEndPoint = localEndPoint;
        }

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request = base.GetWebRequest(uri);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint((new IPEndPointCallback(localEndPoint)).BindIPEndPointCallback);
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            request.ContentType = ContentType.ApplicationSoap;
            WebResponse response = base.GetWebResponse(request);
            if (!ContentType.IsSoap(response.ContentType))
                response.Headers[HttpResponseHeader.ContentType] = response.ContentType.Replace(ContentType.TextHtml, ContentType.ApplicationSoap);
            return response;
        }

        protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
        {
            request.ContentType = ContentType.ApplicationSoap;

            WebResponse response = base.GetWebResponse(request, result);
            if (!ContentType.IsSoap(response.ContentType))
                response.Headers[HttpResponseHeader.ContentType] = response.ContentType.Replace(ContentType.TextHtml, ContentType.ApplicationSoap);
            return response;
        }

        [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
        protected override XmlReader GetReaderForMessage(SoapClientMessage message, int bufferSize)
        {
            string xmlMatchSchema = "<?xml";
            string xmlSchema = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>";
            int schemaLength = Encoding.UTF8.GetByteCount(xmlMatchSchema);
            Stream messageStream = message.Stream;
            byte[] schemaArray = new byte[schemaLength];

            if (messageStream.CanSeek)
            {
                long originalPosition = messageStream.Position;

                messageStream.Seek(0, SeekOrigin.Begin);
                int bytesRead = messageStream.Read(schemaArray, 0, schemaArray.Length);

                string readSchema = Encoding.UTF8.GetString(schemaArray);
                if (readSchema.ToLowerInvariant() != xmlMatchSchema.ToLowerInvariant())
                {
                    messageStream.Seek(0, SeekOrigin.Begin);
                    byte[] content = new byte[messageStream.Length];
                    messageStream.Read(content, 0, content.Length);
                    messageStream.Seek(0, SeekOrigin.Begin);

                    string strContent = Encoding.UTF8.GetString(content);

                    MemoryStream newMemStream = new MemoryStream();
                    newMemStream.Seek(0, SeekOrigin.Begin);
                    newMemStream.Write(Encoding.UTF8.GetBytes(xmlSchema), 0, Encoding.UTF8.GetByteCount(xmlSchema));
                    newMemStream.Write(content, 0, content.Length);
                    newMemStream.Seek(0, SeekOrigin.Begin);

                    XmlTextReader reader = null;
                    Encoding encoding = (message.SoapVersion == SoapProtocolVersion.Soap12) ? RequestResponseUtils.GetEncoding2(message.ContentType) : RequestResponseUtils.GetEncoding(message.ContentType);
                    if (bufferSize < 0x200)
                    {
                        bufferSize = 0x200;
                    }

                    if (encoding != null)
                    {
                        reader = new XmlTextReader(new StreamReader(message.Stream, encoding, true, bufferSize));
                    }
                    else
                    {
                        reader = new XmlTextReader(message.Stream);
                    }
                    reader.ProhibitDtd = true;
                    reader.Normalization = true;
                    reader.XmlResolver = null;
                    return reader;
                }
                else
                {
                    messageStream.Seek(originalPosition, SeekOrigin.Begin);
                    return base.GetReaderForMessage(message, bufferSize);
                }
            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceWarning, "Unseekable stream returned with message, maybe the connection has terminated. Stream type: " + message.Stream.GetType().ToString());
                return base.GetReaderForMessage(message, bufferSize);
            }
        }

        [PermissionSet(SecurityAction.InheritanceDemand, Name = "FullTrust")]
        protected override XmlWriter GetWriterForMessage(SoapClientMessage message, int bufferSize)
        {
            if (bufferSize < 0x200)
            {
                bufferSize = 0x200;
            }

            return new XmlSpecialNSPrefixTextWriter(new StreamWriter(message.Stream, (base.RequestEncoding != null) ? base.RequestEncoding : new UTF8Encoding(false), bufferSize));
        }
    }


}
