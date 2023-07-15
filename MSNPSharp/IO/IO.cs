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
using System.Threading;
using System.Diagnostics;
using System.IO.Compression;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace MSNPSharp.IO
{
    #region MclFileStruct
    [StructLayout(LayoutKind.Sequential)]
    internal struct MclFileStruct
    {
        public byte[] content;
    }

    #endregion

    #region MclInfo

    internal class MclInfo
    {
        private MclFile file;
        private DateTime lastChange;

        public MclInfo(MclFile pfile)
        {
            file = pfile;
            if (System.IO.File.Exists(file.FileName))
            {
                lastChange = System.IO.File.GetLastWriteTime(file.FileName);
            }
        }

        /// <summary>
        /// Get whether the file was changed and refresh the <see cref="LastChange"/> property.
        /// </summary>
        /// <returns></returns>
        public bool Refresh()
        {
            if (file != null && System.IO.File.Exists(file.FileName))
            {
                bool changed = lastChange.CompareTo(System.IO.File.GetLastWriteTime(file.FileName)) != 0;
                lastChange = System.IO.File.GetLastWriteTime(file.FileName);
                return changed;
            }

            return false;
        }

        /// <summary>
        /// Inner file
        /// </summary>
        public MclFile File
        {
            get
            {
                return file;
            }
        }

        /// <summary>
        /// Last written date
        /// </summary>
        public DateTime LastChange
        {
            get
            {
                return lastChange;
            }
        }
    }

    #endregion

    /// <summary>
    /// Mcl serialization to load/save contact list files.
    /// A mcl file can be serialized as both compressed and encrypted formats.
    /// </summary>
    [Flags]
    public enum MclSerialization
    {
        /// <summary>
        /// No serialization, use plain text.
        /// </summary>
        None = 0,
        /// <summary>
        /// Use compression.
        /// </summary>
        Compression = 1,
        /// <summary>
        /// Use cryptography.
        /// </summary>
        Cryptography = 2
    }

    #region MclFile

    /// <summary>
    /// File class used to save userdata.
    /// </summary>
    public sealed class MclFile
    {
        /// <summary>
        /// Signature for compressed file - mcl.
        /// </summary>
        public static readonly byte[] MclBytes = new byte[] { (byte)'m', (byte)'c', (byte)'l' };
        /// <summary>
        /// Signature for encrypted file - mpw, Mr Pang Wu :)
        /// </summary>
        public static readonly byte[] MpwBytes = new byte[] { (byte)'m', (byte)'p', (byte)'w' };
        /// <summary>
        /// Signature for compressed+encrypted file - mcp.
        /// </summary>
        public static readonly byte[] McpBytes = new byte[] { (byte)'m', (byte)'c', (byte)'p' };

        private MclSerialization mclSerialization = MclSerialization.None;
        private string fileName = String.Empty;
        private byte[] sha256Password = new byte[32];
        private byte[] xmlData;

        public MclFile(string filename, MclSerialization st, FileAccess access, string password)
        {
            fileName = filename;
            mclSerialization = st;

            if (!String.IsNullOrEmpty(password))
            {
                using (SHA256Managed sha256 = new SHA256Managed())
                    sha256Password = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }

            if ((access & FileAccess.Read) == FileAccess.Read)
                xmlData = GetStruct().content;
        }

        /// <summary>
        /// Opens filename and fills the <see cref="Content"/> with uncompressed data if file is opened for reading.
        /// </summary>
        /// <param name="filename">Name of file</param>
        /// <param name="nocompress">Use of compression when SAVING file.</param>
        /// <param name="access">The <see cref="Content"/> is filled if the file is opened for reading</param>
        public MclFile(string filename, bool nocompress, FileAccess access)
            : this(filename, nocompress ? MclSerialization.None : MclSerialization.Compression, access, null)
        {
        }

        #region Public method
        public void Save(string filename)
        {
            WriteAllBytes(filename, FillFileStruct(xmlData));
        }

        public void Save()
        {
            Save(fileName);
        }

        /// <summary>
        /// Save the file and set its hidden attribute to true
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="saveToHiddenFile"></param>
        public void Save(string filename, bool saveToHiddenFile)
        {
            WriteAllBytes(filename, FillFileStruct(xmlData));
            if (saveToHiddenFile)
            {
                lock (this)
                {
                    File.SetAttributes(filename, FileAttributes.Hidden);
                }
            }
        }

        /// <summary>
        /// Save the file and set its hidden attribute to true
        /// </summary>
        public void SaveAndHide()
        {
            Save(fileName, true);
        }

        #endregion

        #region Properties

        /// <summary>
        /// Name of file
        /// </summary>
        public string FileName
        {
            get
            {
                return fileName;
            }
            set
            {
                fileName = value;
            }
        }

        /// <summary>
        /// XML data
        /// </summary>
        public byte[] Content
        {
            get
            {
                return xmlData;
            }
            set
            {
                xmlData = value;
            }
        }

        /// <summary>
        /// Don't use compression when SAVING.
        /// </summary>
        public bool NoCompression
        {
            get
            {
                return mclSerialization == MclSerialization.None;
            }
        }

        #endregion

        #region Private

        private void WriteAllBytes(string filename, byte[] content)
        {
            DateTime beginTime = DateTime.Now;
            fileName = filename;

            if (content != null)
            {
                lock (this)
                {
                    try
                    {
                        if (File.Exists(filename))
                            File.SetAttributes(filename, FileAttributes.Normal);

                        File.WriteAllBytes(filename, content);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "MCL File write error: " + ex.Message);
                    }
                }
            }

            TimeSpan timeConsume = DateTime.Now - beginTime;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "<" + this.GetType().ToString() + "> Write raw file time (by ticks): " + timeConsume.Ticks);
        }

        private static byte[] Compress(byte[] buffer)
        {
            DateTime beginTime = DateTime.Now;
            MemoryStream destms = new MemoryStream();
            GZipStream zipsm = new GZipStream(destms, CompressionMode.Compress, true);
            zipsm.Write(buffer, 0, buffer.Length);
            zipsm.Close();

            TimeSpan timeConsume = DateTime.Now - beginTime;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "<Compress> Compress time (by ticks): " + timeConsume.Ticks);
            return destms.ToArray();
        }

        private static byte[] Decompress(byte[] compresseddata)
        {
            DateTime beginTime = DateTime.Now;
            MemoryStream destms = new MemoryStream();
            MemoryStream ms = new MemoryStream(compresseddata);
            ms.Position = 0;

            int read;
            byte[] decompressdata = new byte[8192];
            GZipStream zipsm = new GZipStream(ms, CompressionMode.Decompress, true);

            while ((read = zipsm.Read(decompressdata, 0, decompressdata.Length)) > 0)
            {
                destms.Write(decompressdata, 0, read);
            }

            zipsm.Close();
            TimeSpan timeConsume = DateTime.Now - beginTime;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "<Decompress> Decompress time (by ticks): " + timeConsume.Ticks);

            return destms.ToArray();
        }

        /// <summary>
        /// Public key
        /// </summary>
        private static byte[] IV = { 
            (byte)'m',
            (byte)'s',
            (byte)'n',
            (byte)'p',
            (byte)'s',
            (byte)'h',
            (byte)'a',
            (byte)'r',
            (byte)'p',
            (byte)'l',
            (byte)'i',
            (byte)'b',
            (byte)'r',
            (byte)'a',
            (byte)'r',
            (byte)'y'
        };

        private static byte[] Encyrpt(byte[] val, byte[] secretKey)
        {
            DateTime beginTime = DateTime.Now;
            byte[] ret = null;
            if (val != null)
            {
                using (RijndaelManaged rm = new RijndaelManaged())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, rm.CreateEncryptor(secretKey, IV), CryptoStreamMode.Write))
                        {
                            using (BinaryWriter bw = new BinaryWriter(cs))
                            {
                                bw.Write(val, 0, val.Length);
                                bw.Flush();
                                cs.FlushFinalBlock();
                                ret = ms.ToArray();
                            }
                        }
                    }
                }
            }

            TimeSpan timeConsume = DateTime.Now - beginTime;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "<Encyrpt> Encyrpt time (by ticks): " + timeConsume.Ticks);

            return ret;
        }

        private static byte[] Decyrpt(byte[] buffer, byte[] secretKey)
        {
            DateTime beginTime = DateTime.Now;

            MemoryStream ret = new MemoryStream();
            if (buffer != null)
            {
                using (RijndaelManaged rm = new RijndaelManaged())
                {
                    using (MemoryStream ms = new MemoryStream(buffer))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, rm.CreateDecryptor(secretKey, IV), CryptoStreamMode.Read))
                        {
                            using (BinaryReader br = new BinaryReader(cs))
                            {
                                byte[] tmp = new byte[16384];
                                int length;
                                while ((length = br.Read(tmp, 0, tmp.Length)) > 0)
                                {
                                    ret.Write(tmp, 0, length);
                                }
                            }
                        }
                    }
                }
            }

            TimeSpan timeConsume = DateTime.Now - beginTime;
            Trace.WriteLineIf(Settings.TraceSwitch.TraceVerbose, "<Decyrpt> Decyrpt time (by ticks): " + timeConsume.Ticks);
            return ret.ToArray();
        }

        /// <summary>
        /// Compress/Encrypt the data.
        /// </summary>
        /// <param name="xml">Xml data</param>
        /// <returns></returns>
        private byte[] FillFileStruct(byte[] xml)
        {
            byte[] ret = null;
            if (xml != null)
            {
                switch (mclSerialization)
                {
                    case MclSerialization.None:
                        ret = xml;
                        break;

                    case MclSerialization.Compression:
                        byte[] compressed = Compress(xml);
                        ret = new byte[MclBytes.Length + compressed.Length];
                        Array.Copy(MclBytes, 0, ret, 0, MclBytes.Length);
                        Array.Copy(compressed, 0, ret, MclBytes.Length, compressed.Length);
                        break;

                    case MclSerialization.Cryptography:
                        byte[] encyrpted = Encyrpt(xml, sha256Password);
                        ret = new byte[MpwBytes.Length + encyrpted.Length];
                        Array.Copy(MpwBytes, 0, ret, 0, MpwBytes.Length);
                        Array.Copy(encyrpted, 0, ret, MpwBytes.Length, encyrpted.Length);
                        break;

                    case MclSerialization.Compression | MclSerialization.Cryptography:
                        byte[] compressed2 = Compress(xml);
                        byte[] encyrpted2 = Encyrpt(compressed2, sha256Password);
                        ret = new byte[McpBytes.Length + encyrpted2.Length];
                        Array.Copy(McpBytes, 0, ret, 0, McpBytes.Length);
                        Array.Copy(encyrpted2, 0, ret, McpBytes.Length, encyrpted2.Length);
                        break;
                }
            }
            return ret;
        }

        /// <summary>
        /// Decompress/Decyrpt the file if the serialization type is not XML.
        /// </summary>
        /// <returns></returns>
        private MclFileStruct GetStruct()
        {
            MclFileStruct mclfile = new MclFileStruct();
            if (File.Exists(fileName))
            {
                MemoryStream ms = new MemoryStream();
                FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read);
                try
                {
                    byte[] first3 = new byte[MclBytes.Length];
                    if (MclBytes.Length == fs.Read(first3, 0, first3.Length))
                    {
                        MclSerialization st = MclSerialization.None;

                        if (first3[0] == MclBytes[0] && first3[1] == MclBytes[1] && first3[2] == MclBytes[2])
                        {
                            st = MclSerialization.Compression;
                        }
                        else if (first3[0] == MpwBytes[0] && first3[1] == MpwBytes[1] && first3[2] == MpwBytes[2])
                        {
                            st = MclSerialization.Cryptography;
                        }
                        else if (first3[0] == McpBytes[0] && first3[1] == McpBytes[1] && first3[2] == McpBytes[2])
                        {
                            st = MclSerialization.Compression | MclSerialization.Cryptography;
                        }
                        else
                        {
                            st = MclSerialization.None;
                            ms.Write(first3, 0, first3.Length);
                        }

                        int read;
                        byte[] tmp = new byte[16384];
                        while ((read = fs.Read(tmp, 0, tmp.Length)) > 0)
                        {
                            ms.Write(tmp, 0, read);
                        }

                        switch (st)
                        {
                            case MclSerialization.None:
                                mclfile.content = ms.ToArray();
                                break;

                            case MclSerialization.Compression:
                                mclfile.content = Decompress(ms.ToArray());
                                break;

                            case MclSerialization.Cryptography:
                                mclfile.content = Decyrpt(ms.ToArray(), sha256Password);
                                break;

                            case MclSerialization.Compression | MclSerialization.Cryptography:
                                byte[] compressed = Decyrpt(ms.ToArray(), sha256Password);
                                mclfile.content = Decompress(compressed);
                                break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, exception.Message, GetType().Name);
                    return new MclFileStruct();
                }
                finally
                {
                    fs.Close();
                    ms.Close();
                }
            }
            return mclfile;
        }

        public override string ToString()
        {
            return Encoding.UTF8.GetString(Content);
        }
        #endregion

        #region Open

        static Dictionary<string, MclInfo> storage = new Dictionary<string, MclInfo>(0);
        static object syncObject;
        static object SyncObject
        {
            get
            {
                if (syncObject == null)
                {
                    object newobj = new object();
                    Interlocked.CompareExchange(ref syncObject, newobj, null);
                }
                return syncObject;
            }
        }

        /// <summary>
        /// Get the file from disk or from the storage cache.
        /// </summary>
        /// <param name="filePath">Full file path</param>
        /// <param name="access">If the file is opened for reading, file content is loaded</param>
        /// <param name="st">Serialization type for SAVING</param>
        /// <param name="password">File password</param>
        /// <param name="useCache"></param>
        /// <returns>Msnpsharp contact list file</returns>
        /// <remarks>This method is thread safe</remarks>
        public static MclFile Open(string filePath, FileAccess access, MclSerialization st, string password, bool useCache)
        {
            if (useCache)
            {
                if (storage.ContainsKey(filePath))
                {
                    lock (SyncObject)
                    {
                        if (storage[filePath].Refresh())
                        {
                            storage[filePath] = new MclInfo(new MclFile(filePath, st, access, password));
                        }
                    }
                }
                else
                {
                    lock (SyncObject)
                    {
                        if (!storage.ContainsKey(filePath))
                        {
                            storage[filePath] = new MclInfo(new MclFile(filePath, st, access, password));
                        }
                    }
                }

                return storage[filePath].File;
            }
            else
            {
                return new MclFile(filePath, st, access, password);
            }
        }

        #endregion
    }

    #endregion
};
