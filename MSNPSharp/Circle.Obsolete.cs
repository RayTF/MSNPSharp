using System;
using System.Collections.Generic;
using System.Text;
using MSNPSharp.MSNWS.MSNABSharingService;

namespace MSNPSharp
{
    public partial class Circle
    {
        private ContactType hiddenRepresentative = null;

        /// <summary>
        /// Circle constructor
        /// </summary>
        /// <param name="me">The "Me Contact" in the addressbook.</param>
        /// <param name="hidden"></param>
        /// <param name="circleInfo"></param>
        /// <param name="handler"></param>
        [Obsolete("No hidden representative is needed anymore.", true)]
        public Circle(ContactType me, ContactType hidden, CircleInverseInfoType circleInfo, NSMessageHandler handler)
            : base(circleInfo.Content.Handle.Id.ToLowerInvariant(), circleInfo.Content.Handle.Id.ToLowerInvariant() + "@" + circleInfo.Content.Info.HostedDomain.ToLowerInvariant(), ClientType.CircleMember, me.contactInfo.CID, handler)
        {
            hostDomain = circleInfo.Content.Info.HostedDomain.ToLowerInvariant();
            hiddenRepresentative = hidden;

            CircleRole = (CirclePersonalMembershipRole)Enum.Parse(typeof(CirclePersonalMembershipRole), circleInfo.PersonalInfo.MembershipInfo.CirclePersonalMembership.Role);

            SetName(circleInfo.Content.Info.DisplayName);
            SetNickName(Name);

            meContact = me;

            if (hidden != null)
            {
                Guid = new Guid(hidden.contactId);

                if (hidden.contactInfo != null && hidden.contactInfo.CIDSpecified)
                    CID = hidden.contactInfo.CID;
            }

            contactList = new ContactList(AddressBookId, new Owner(AddressBookId, me.contactInfo.passportName, me.contactInfo.CID, NSMessageHandler), NSMessageHandler);
            Initialize();
        }
    }
}
