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
using System.Xml.Serialization;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.Core;
using System.Drawing;

    /// <summary>
    /// Storage class for deltas request
    /// </summary>
    [Serializable]
    public class DeltasList : MCLSerializer
    {
        private const int MaxSlot = 1000;

        private SerializableDictionary<CacheKeyType, string> cacheKeys = new SerializableDictionary<CacheKeyType, string>(0);
        private SerializableDictionary<string, string> preferredHosts = new SerializableDictionary<string, string>(0);
        private SerializableDictionary<string, byte[]> userTileSlots = new SerializableDictionary<string, byte[]>(MaxSlot);
        private SerializableDictionary<string, uint> visitCount = new SerializableDictionary<string, uint>(MaxSlot);
        private SerializableDictionary<string, string> userImageRelationships = new SerializableDictionary<string, string>();

        public SerializableDictionary<string, string> UserImageRelationships
        {
            get { return userImageRelationships; }
            set { userImageRelationships = value; }
        }


        public SerializableDictionary<string, uint> VisitCount
        {
            get { return visitCount; }
            set { visitCount = value; }
        }

        /// <summary>
        /// Data structure that store the user's display images.
        /// </summary>
        public SerializableDictionary<string, byte[]> UserTileSlots
        {
            get { return userTileSlots; }
            set { userTileSlots = value; }
        }

        /// <summary>
        /// CacheKeys for webservices.
        /// </summary>
        public SerializableDictionary<CacheKeyType, string> CacheKeys
        {
            get
            {
                return cacheKeys;
            }
            set
            {
                cacheKeys = value;
            }
        }

        /// <summary>
        /// Preferred hosts for different methods.
        /// </summary>
        public SerializableDictionary<string, string> PreferredHosts
        {
            get
            {
                return preferredHosts;
            }
            set
            {
                preferredHosts = value;
            }
        }

        #region Profile
        private OwnerProfile profile = new OwnerProfile();

        /// <summary>
        /// Profile of current user.
        /// </summary>
        [XmlElement("Profile")]
        public OwnerProfile Profile
        {
            get
            {
                return profile;
            }
            set
            {
                profile = value;
            }
        }

        #endregion

        #region Private methods

        private bool HasRelationship(string siblingAccount)
        {
            lock (UserImageRelationships)
                return UserImageRelationships.ContainsKey(siblingAccount.ToLowerInvariant());
        }

        private bool HasImage(string imageKey)
        {
            lock (UserTileSlots)
                return UserTileSlots.ContainsKey(imageKey);
        }

        private bool HasRelationshipAndImage(string siblingAccount, out string imageKey)
        {
            imageKey = string.Empty;
            if (!HasRelationship(siblingAccount))
            {
                return false;
            }

            string imgKey = string.Empty;
            lock (UserImageRelationships)
                imgKey = UserImageRelationships[siblingAccount.ToLowerInvariant()];

            if (!HasImage(imgKey))
                return false;

            imageKey = imgKey;
            return true;
        }

        private void AddImage(string imageKey, byte[] data)
        {
            if (HasImage(imageKey))
                return;

            lock (UserTileSlots)
                UserTileSlots[imageKey] = data;
        }

        private void AddRelationship(string siblingAccount, string imageKey)
        {
            lock (UserImageRelationships)
                UserImageRelationships[siblingAccount.ToLowerInvariant()] = imageKey;
        }

        private void AddImageAndRelationship(string siblingAccount, string imageKey, byte[] data)
        {
            AddImage(imageKey, data);
            AddRelationship(siblingAccount, imageKey);
        }

        private bool RemoveImage(string imageKey)
        {
            bool noerror = true;

            lock (UserTileSlots)
                noerror |= UserTileSlots.Remove(imageKey);

            lock (VisitCount)
                noerror |= VisitCount.Remove(imageKey);

            lock (UserImageRelationships)
            {
                Dictionary<string, string> cp = new Dictionary<string, string>(UserImageRelationships);
                foreach (string account in cp.Keys)
                {
                    if (cp[account] == imageKey)
                        UserImageRelationships.Remove(account);
                }
            }

            return noerror;
        }

        private bool RemoveRelationship(string siblingAccount)
        {
            if (!HasRelationship(siblingAccount))
                return false;

            lock (UserImageRelationships)
            {
                string key = UserImageRelationships[siblingAccount];
                UserImageRelationships.Remove(siblingAccount);
            }

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="imageKey"></param>
        /// <remarks>This function does NOT exam whether the correspondent slot exist.</remarks>
        private uint IncreaseVisitCount(string imageKey)
        {
            lock (VisitCount)
            {
                if (!VisitCount.ContainsKey(imageKey))
                    VisitCount[imageKey] = 0;

                return ++VisitCount[imageKey];
            }
        }

        private uint DecreaseVisitCount(string imageKey)
        {
            lock (VisitCount)
            {
                if (!VisitCount.ContainsKey(imageKey))
                    return uint.MinValue;

                if (VisitCount[imageKey] > 0)
                {
                    return --VisitCount[imageKey];
                }
            }

            //error
            return 0;
        }

        private bool GetLeastVisitImage(out string imageKey)
        {
            imageKey = string.Empty;
            lock (VisitCount)
            {
                if (VisitCount.Count == 0)
                    return false;
                uint minValue = uint.MaxValue;
                uint maxValue = 0;
                ulong sum = 0;

                string lastKey = string.Empty;

                foreach (string key in VisitCount.Keys)
                {
                    if (VisitCount[key] <= minValue)
                    {
                        minValue = VisitCount[key];
                        lastKey = key;
                    }

                    if (VisitCount[key] >= maxValue)
                        maxValue = VisitCount[key];

                    sum += VisitCount[key];
                }

                if (string.IsNullOrEmpty(lastKey))
                    return false;

                imageKey = lastKey;
                if (maxValue == uint.MaxValue)  //Prevent overflow.
                {
                    uint avg = (uint)(sum / (ulong)VisitCount.Count);
                    if (avg == uint.MaxValue)
                        avg = 0;

                    lock (VisitCount)
                    {
                        Dictionary<string, uint> cp = new Dictionary<string, uint>(VisitCount);
                        foreach (string imgKey in cp.Keys)
                        {
                            if (cp[imgKey] == uint.MaxValue)
                                VisitCount[imgKey] = avg;
                        }
                    }
                }

                return true;

            }
        }

        #endregion

        #region Internal Methods

        internal byte[] GetRawImageDataBySiblingString(string siblingAccount, out string imageKey)
        {
            imageKey = string.Empty;
            if (HasRelationshipAndImage(siblingAccount, out imageKey))
            {
                lock (UserTileSlots)
                {
                    IncreaseVisitCount(imageKey);
                    return UserTileSlots[imageKey];
                }

            }

            return null;
        }

        internal bool SaveImageAndRelationship(string siblingAccount, string imageKey, byte[] userTile)
        {

            lock (UserTileSlots)
            {
                if (UserTileSlots.Count == MaxSlot)
                {
                    //The heaven is full.
                    string deleteKey = string.Empty;
                    if (GetLeastVisitImage(out deleteKey))
                    {
                        RemoveImage(deleteKey);
                    }
                    else
                    {
                        //OMG no one want to give a place?
                        return false;
                    }
                }

                AddImageAndRelationship(siblingAccount, imageKey, userTile);
            }

            return true;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Empty all of the lists
        /// </summary>
        public void Empty()
        {

        }

        /// <summary>
        /// Truncate file. This is useful after calling of Addressbook.Save
        /// </summary>
        public void Truncate()
        {
            Empty();
            Save(true);
        }

        public static DeltasList LoadFromFile(string filename, MclSerialization st, NSMessageHandler handler, bool useCache)
        {
            return (DeltasList)LoadFromFile(filename, st, typeof(DeltasList), handler, useCache);
        } 

        #endregion

        #region Overrides

        /// <summary>
        /// Save the <see cref="DeltasList"/> into a specified file.
        /// </summary>
        /// <param name="filename"></param>
        public override void Save(string filename)
        {
            FileName = filename;
            Save(false);
        }

        public override void Save()
        {
            Save(false);
        }

        public void Save(bool saveImmediately)
        {
            Version = Properties.Resources.DeltasListVersion;

            if (saveImmediately == false &&
                File.Exists(FileName) &&
                File.GetLastWriteTime(FileName) > DateTime.Now.AddSeconds(-5))
            {
                return;
            }

            base.Save(FileName);
        }

        #endregion
    }
};
