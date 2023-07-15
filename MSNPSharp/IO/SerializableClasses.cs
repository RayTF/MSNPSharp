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
using System.Xml;
using System.Text;
using System.Collections.Generic;

namespace MSNPSharp.IO
{
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.Core;

    #region Service

    /// <summary>
    /// Membership service
    /// </summary>
    [Serializable]
    public class Service : ICloneable, IComparable, IComparable<Service>
    {
        private int id;
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

        private string serviceType;
        public string ServiceType
        {
            get
            {
                return serviceType;
            }
            set
            {
                serviceType = value;
            }
        }

        private string lastChange;
        public string LastChange
        {
            get
            {
                return lastChange;
            }
            set
            {
                lastChange = value;
            }
        }

        private string foreignId;
        public string ForeignId
        {
            get
            {
                return foreignId;
            }
            set
            {
                foreignId = value;
            }
        }

        public Service()
        {
        }

        public Service(Service copy)
        {
            Id = copy.Id;
            ServiceType = copy.ServiceType;
            LastChange = copy.LastChange;
            ForeignId = String.Copy(copy.ForeignId);
        }

        public override string ToString()
        {
            return Convert.ToString(ServiceType);
        }

        public static bool operator ==(Service svc1, Service svc2)
        {
            if (((object)svc1) == null && ((object)svc2) == null)
                return true;

            if (((object)svc1) == null || ((object)svc2) == null)
                return false;

            return svc1.GetHashCode() == svc2.GetHashCode();
        }

        public static bool operator !=(Service svc1, Service svc2)
        {
            return !(svc1 == svc2);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return id;
        }

        #region ICloneable Members

        public object Clone()
        {
            return new Service(this);
        }

        #endregion

        #region IComparable Members

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (!(obj is Service))
            {
                throw new ArgumentException("obj");
            }

            return CompareTo((Service)obj);
        }

        #endregion

        #region IComparable<Service> Members

        public int CompareTo(Service other)
        {
            if (other == null)
            {
                return 1;
            }

            return DateTime.Compare(WebServiceDateTimeConverter.ConvertToDateTime(LastChange), WebServiceDateTimeConverter.ConvertToDateTime(other.LastChange));
        }

        #endregion
    }

    #endregion

    #region ServiceMembership
    [Serializable]
    public class ServiceMembership
    {
        private Service service;
        private SerializableDictionary<string, SerializableDictionary<string, BaseMember>> memberships = new SerializableDictionary<string, SerializableDictionary<string, BaseMember>>(0);

        public Service Service
        {
            get
            {
                return service;
            }
            set
            {
                service = value;
            }
        }

        public SerializableDictionary<string, SerializableDictionary<string, BaseMember>> Memberships
        {
            get
            {
                return memberships;
            }
            set
            {
                memberships = value;
            }
        }

        public ServiceMembership()
        {
        }

        public ServiceMembership(Service srvc)
        {
            service = srvc;
        }
    }
    #endregion

    #region Owner properties

    /// <summary>
    /// Base class for profile resource
    /// </summary>
    [Serializable]
    public class ProfileResource
    {
        private string dateModified;
        private string resourceID;

        /// <summary>
        /// Last modify time of the resource
        /// </summary>
        public string DateModified
        {
            get
            {
                return dateModified;
            }
            set
            {
                dateModified = value;
            }
        }

        /// <summary>
        /// Identifier of the resource
        /// </summary>
        public string ResourceID
        {
            get
            {
                return resourceID;
            }
            set
            {
                resourceID = value;
            }
        }
    }

    /// <summary>
    /// Owner's photo resource in profile
    /// </summary>
    [Serializable]
    public class ProfilePhoto : ProfileResource
    {
        private string preAthURL;
        private string name = string.Empty;
        private SerializableMemoryStream displayImage;        

        public string PreAthURL
        {
            get
            {
                return preAthURL;
            }
            set
            {
                preAthURL = value;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public SerializableMemoryStream DisplayImage
        {
            get
            {
                return displayImage;
            }
            set
            {
                displayImage = value;
            }
        }
    }

    /// <summary>
    /// Owner profile
    /// </summary>
    [Serializable]
    public class OwnerProfile : ProfileResource
    {
        private string displayName = string.Empty;
        private string personalMessage = string.Empty;
        private ProfilePhoto photo = new ProfilePhoto();
        private bool hasExpressionProfile = true;
        private ProfileResource expressionProfile = new ProfileResource();

        public ProfileResource ExpressionProfile
        {
            get { return expressionProfile; }
            set { expressionProfile = value; }
        }

        /// <summary>
        /// Whether the profile owner is using hotmail or live account for msn login.
        /// </summary>
        public bool HasExpressionProfile
        {
            get 
            { 
                return hasExpressionProfile; 
            }

            set 
            { 
                hasExpressionProfile = value; 
            }
        }

        /// <summary>
        /// DisplayImage of owner.
        /// </summary>
        public ProfilePhoto Photo
        {
            get
            {
                return photo;
            }
            set
            {
                photo = value;
            }
        }

        /// <summary>
        /// DisplayName of owner
        /// </summary>
        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
            }
        }

        /// <summary>
        /// Personal description of owner.
        /// </summary>
        public string PersonalMessage
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


    }

    [Serializable()]
    public class CircleInfo
    {
        private string memberRole = MSNPSharp.MemberRole.Allow;
        private ContactType circleMember = null;
        private CircleInverseInfoType circleResultInfo = null;

        public string MemberRole
        {
            get { return memberRole; }
            set { memberRole = value; }
        }

        public CircleInverseInfoType CircleResultInfo
        {
            get { return circleResultInfo; }
            set { circleResultInfo = value; }
        }


        public ContactType CircleMember
        {
            get { return circleMember; }
            set { circleMember = value; }
        }

        public CircleInfo()
        {
        }


        public CircleInfo(ContactType contact, CircleInverseInfoType circle)
        {
            CircleMember = contact;
            CircleResultInfo = circle;
        }
    }
    #endregion
};
