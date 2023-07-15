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
using System.Drawing;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    /// <summary>
    /// Serializable MemoryStream
    /// </summary>
    [Serializable]
    [XmlRoot("Stream")]
    public class SerializableMemoryStream : MemoryStream, IXmlSerializable
    {
        #region IXmlSerializable Members

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.IsEmptyElement)
                return;

            reader.Read();
            byte[] byt = (byte[])new XmlSerializer(typeof(byte[])).Deserialize(reader);
            reader.ReadEndElement();

            Write(byt, 0, byt.Length);
            Flush();
        }

        public void WriteXml(XmlWriter writer)
        {
            byte[] data = ToArray();
            if (data != null)
            {
                new XmlSerializer(typeof(byte[])).Serialize(writer, data);
            }
        }

        public Image ToImage()
        {
            return Image.FromStream(this);
        }

        public static SerializableMemoryStream FromImage(Image image)
        {
            SerializableMemoryStream ret = new SerializableMemoryStream();
            image.Save(ret, image.RawFormat);
            return ret;
        }

        public static explicit operator Image(SerializableMemoryStream ms)
        {
            return ms.ToImage();
        }

        public static explicit operator SerializableMemoryStream(Image image)
        {
            return SerializableMemoryStream.FromImage(image);
        }
        #endregion
    }
};
