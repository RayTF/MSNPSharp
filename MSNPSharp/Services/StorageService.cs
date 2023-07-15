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
using System.Net;
using System.Xml;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Net.Sockets;
using System.Collections.Generic;

namespace MSNPSharp
{
    using MSNPSharp.IO;
    using MSNPSharp.MSNWS.MSNABSharingService;
    using MSNPSharp.MSNWS.MSNStorageService;
    using MSNPSharp.Core;
    using MSNPSharp.Services;

    /// <summary>
    /// Provide webservice operations for Storage Service
    /// </summary>
    public class MSNStorageService : MSNService
    {
        public MSNStorageService(NSMessageHandler nsHandler)
            : base(nsHandler)
        {
        }

        #region Internal implementation

        private static ExpressionProfileAttributesType CreateFullExpressionProfileAttributes()
        {
            ExpressionProfileAttributesType expAttrib = new ExpressionProfileAttributesType();
            expAttrib.DateModified = true;
            expAttrib.DateModifiedSpecified = true;
            expAttrib.DisplayName = true;
            expAttrib.DisplayNameLastModified = true;
            expAttrib.DisplayNameLastModifiedSpecified = true;
            expAttrib.DisplayNameSpecified = true;
            expAttrib.Flag = true;
            expAttrib.FlagSpecified = true;
            expAttrib.PersonalStatus = true;
            expAttrib.PersonalStatusLastModified = true;
            expAttrib.PersonalStatusLastModifiedSpecified = true;
            expAttrib.PersonalStatusSpecified = true;
            expAttrib.Photo = true;
            expAttrib.PhotoSpecified = true;
            expAttrib.Attachments = true;
            expAttrib.AttachmentsSpecified = true;
            expAttrib.ResourceID = true;
            expAttrib.ResourceIDSpecified = true;
            expAttrib.StaticUserTilePublicURL = true;
            expAttrib.StaticUserTilePublicURLSpecified = true;

            return expAttrib;
        }

        private bool CreateProfileSync(PartnerScenario scenario, out string profileResourceId)
        {

            //1. CreateProfile, create a new profile and return its resource id.
            MsnServiceState serviceState = new MsnServiceState(scenario, "CreateProfile", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            CreateProfileRequestType createRequest = new CreateProfileRequestType();
            createRequest.profile = new CreateProfileRequestTypeProfile();
            createRequest.profile.ExpressionProfile = new ExpressionProfile();
            createRequest.profile.ExpressionProfile.PersonalStatus = "";
            createRequest.profile.ExpressionProfile.RoleDefinitionName = "ExpressionProfileDefault";

            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, createRequest);
                CreateProfileResponse createResponse = storageService.CreateProfile(createRequest);
                profileResourceId = createResponse.CreateProfileResult;
                NSMessageHandler.ContactService.Deltas.Profile.ResourceID = profileResourceId;
                NSMessageHandler.ContactService.Deltas.Save(true);
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("CreateProfile", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateProfile error: " + ex.Message, GetType().Name);
                profileResourceId = string.Empty;

                return false;
            }

            return true;
        }

        private bool ShareItemSync(PartnerScenario scenario, string profileResourceId)
        {
            MsnServiceState serviceState = new MsnServiceState(scenario, "ShareItem", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            ShareItemRequestType shareItemRequest = new ShareItemRequestType();
            shareItemRequest.resourceID = profileResourceId;
            shareItemRequest.displayName = "Messenger Roaming Identity";
            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, shareItemRequest);
                storageService.ShareItem(shareItemRequest);
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("ShareItem", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "ShareItem error: " + ex.Message, GetType().Name); //Item already shared.
                return false;
            }

            return true;
        }

        private bool AddProfileExpressionRoleMemberSync(PartnerScenario scenario)
        {
            HandleType srvHandle = new HandleType();
            srvHandle.ForeignId = "MyProfile";
            srvHandle.Id = "0";
            srvHandle.Type = ServiceFilterType.Profile;
            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                MsnServiceState serviceState = new MsnServiceState(scenario, "AddMember", false);
                SharingServiceBinding sharingService = (SharingServiceBinding)CreateService(MsnServiceType.Sharing, serviceState);

                AddMemberRequestType addMemberRequest = new AddMemberRequestType();

                addMemberRequest.serviceHandle = srvHandle;

                Membership memberShip = new Membership();
                memberShip.MemberRole = MemberRole.ProfileExpression;
                RoleMember roleMember = new RoleMember();
                roleMember.Type = "Role";
                roleMember.Id = "Allow";
                roleMember.State = MemberState.Accepted;
                roleMember.MaxRoleRecursionDepth = "0";
                roleMember.MaxDegreesSeparation = "0";

                RoleMemberDefiningService defService = new RoleMemberDefiningService();
                defService.ForeignId = "";
                defService.Id = "0";
                defService.Type = "Messenger";

                roleMember.DefiningService = defService;
                memberShip.Members = new RoleMember[] { roleMember };
                addMemberRequest.memberships = new Membership[] { memberShip };
                try
                {
                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(sharingService, MsnServiceType.Sharing, serviceState, addMemberRequest);
                    sharingService.AddMember(addMemberRequest);
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(sharingService, new ServiceOperationFailedEventArgs("ShareItem", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "AddMember error: " + ex.Message, GetType().Name);
                    return false;
                }

                return true;
            }

            return false;
        }

        private InternalOperationReturnValues GetProfileLiteSync(PartnerScenario scenario, out string profileResourceId, out string expressionProfileResourceId)
        {
            MsnServiceState serviceState = new MsnServiceState(scenario, "GetProfile", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            GetProfileRequestType getprofileRequest = new GetProfileRequestType();

            Alias alias = new Alias();
            alias.NameSpace = "MyCidStuff";
            alias.Name = Convert.ToString(NSMessageHandler.ContactList.Owner.CID);

            Handle pHandle = new Handle();
            pHandle.RelationshipName = "MyProfile";
            pHandle.Alias = alias;

            getprofileRequest.profileHandle = pHandle;
            getprofileRequest.profileAttributes = new profileAttributes();

            ExpressionProfileAttributesType expAttrib = CreateFullExpressionProfileAttributes();

            getprofileRequest.profileAttributes.ExpressionProfileAttributes = expAttrib;

            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, getprofileRequest);
                GetProfileResponse response = storageService.GetProfile(getprofileRequest);

                #region Process Profile
                profileResourceId = response.GetProfileResult.ResourceID;

                if (response.GetProfileResult.ExpressionProfile == null)
                {
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Get profile cannot get expression profile of this owner.");
                    NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile = false;
                    NSMessageHandler.ContactService.Deltas.Profile.DisplayName = NSMessageHandler.ContactList.Owner.Name;

                    expressionProfileResourceId = string.Empty;
                    return InternalOperationReturnValues.NoExpressionProfile;
                }
                else
                {
                    NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile = true;
                    NSMessageHandler.ContactService.Deltas.Profile.ExpressionProfile.ResourceID = response.GetProfileResult.ExpressionProfile.ResourceID;
                    NSMessageHandler.ContactService.Deltas.Profile.ExpressionProfile.DateModified = response.GetProfileResult.ExpressionProfile.DateModified;

                    expressionProfileResourceId = response.GetProfileResult.ExpressionProfile.ResourceID;
                }

                NSMessageHandler.ContactService.Deltas.Profile.DateModified = response.GetProfileResult.DateModified;
                NSMessageHandler.ContactService.Deltas.Profile.ResourceID = response.GetProfileResult.ResourceID;

                // Display name
                NSMessageHandler.ContactService.Deltas.Profile.DisplayName = response.GetProfileResult.ExpressionProfile.DisplayName;

                // Personal status
                if (response.GetProfileResult.ExpressionProfile.PersonalStatus != null)
                {
                    NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage = response.GetProfileResult.ExpressionProfile.PersonalStatus;
                }

                NSMessageHandler.ContactService.Deltas.Save(true);

                // Display photo
                if (null != response.GetProfileResult.ExpressionProfile.Photo)
                {
                    foreach (DocumentStream docStream in response.GetProfileResult.ExpressionProfile.Photo.DocumentStreams)
                    {
                        if (docStream.DocumentStreamType != "UserTileStatic")
                        {
                            continue;
                        }

                        if (NSMessageHandler.ContactService.Deltas.Profile.Photo.PreAthURL == docStream.PreAuthURL)
                        {

                            DisplayImage newDisplayImage = new DisplayImage(NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant(), NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage);

                            NSMessageHandler.ContactList.Owner.DisplayImage = newDisplayImage;
                        }
                        else
                        {
                            string requesturi = docStream.PreAuthURL;
                            if (requesturi.StartsWith("/"))
                            {
                                requesturi = "http://blufiles.storage.msn.com" + requesturi;  //I found it http://byfiles.storage.msn.com is also ok
                            }

                            // Don't urlencode t= :))
                            string usertitleURL = requesturi + "?t=" + System.Web.HttpUtility.UrlEncode(NSMessageHandler.MSNTicket.SSOTickets[SSOTicketType.Storage].Ticket.Substring(2));
                            SyncUserTile(usertitleURL,
                                delegate(object nullParam)
                                {
                                    NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = response.GetProfileResult.ExpressionProfile.Photo.Name;
                                    NSMessageHandler.ContactService.Deltas.Profile.Photo.DateModified = response.GetProfileResult.ExpressionProfile.Photo.DateModified;
                                    NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID = response.GetProfileResult.ExpressionProfile.Photo.ResourceID;
                                    NSMessageHandler.ContactService.Deltas.Profile.Photo.PreAthURL = docStream.PreAuthURL;
                                    SerializableMemoryStream ms = new SerializableMemoryStream();
                                    NSMessageHandler.ContactList.Owner.DisplayImage.Image.Save(ms, NSMessageHandler.ContactList.Owner.DisplayImage.Image.RawFormat);
                                    NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = ms;
                                    NSMessageHandler.ContactService.Deltas.Save(true);
                                },
                                null,
                                delegate(object param)
                                {
                                    Exception ex = param as Exception;
                                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "Get DisplayImage error: " + ex.Message, GetType().Name);
                                    if (NSMessageHandler.ContactList.Owner.UserTileURL != null)
                                    {
                                        SyncUserTile(NSMessageHandler.ContactList.Owner.UserTileURL.AbsoluteUri, null, null, null);
                                    }
                                });

                        }
                    }
                } 

                #endregion
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("GetProfile", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "GetProfile error: " + ex.Message, GetType().FullName);
                expressionProfileResourceId = string.Empty;
                profileResourceId = string.Empty;

                if (ex.Message.ToLowerInvariant().Contains("does not exist"))
                {
                    return InternalOperationReturnValues.ProfileNotExist;
                }

                
                return InternalOperationReturnValues.RequestFailed;
            }

            return InternalOperationReturnValues.Succeed;
        }

        private bool CreatePhotoDocumentSync(PartnerScenario scenario, out string documentResourceId, string photoName, byte[] photoData)
        {
            if (photoData == null)
            {
                documentResourceId = string.Empty;
                return false;
            }

            MsnServiceState serviceState = new MsnServiceState(scenario, "CreateDocument", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);


            CreateDocumentRequestType createDocRequest = new CreateDocumentRequestType();
            createDocRequest.relationshipName = "Messenger User Tile";

            Handle parenthandle = new Handle();
            parenthandle.RelationshipName = "/UserTiles";

            Alias alias = new Alias();
            alias.NameSpace = "MyCidStuff";
            alias.Name = Convert.ToString(NSMessageHandler.ContactList.Owner.CID);

            parenthandle.Alias = alias;
            createDocRequest.parentHandle = parenthandle;
            createDocRequest.document = new Photo();
            createDocRequest.document.Name = photoName;

            PhotoStream photoStream = new PhotoStream();
            photoStream.DataSize = 0;
            photoStream.MimeType = "png";
            photoStream.DocumentStreamType = "UserTileStatic";
            photoStream.Data = photoData;
            createDocRequest.document.DocumentStreams = new PhotoStream[] { photoStream };

            DisplayImage displayImage = new DisplayImage(NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant(), new MemoryStream(photoData));

            NSMessageHandler.ContactList.Owner.DisplayImage = displayImage;

            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, createDocRequest);
                CreateDocumentResponseType createDocResponse = storageService.CreateDocument(createDocRequest);
                documentResourceId = createDocResponse.CreateDocumentResult;
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("CreateDocument", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateDocument error: " + ex.Message, GetType().Name);
                documentResourceId = string.Empty;
                return false;
            }

            NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = photoName;
            NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = new SerializableMemoryStream();
            NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage.Write(photoData, 0, photoData.Length);
            NSMessageHandler.ContactService.Deltas.Save(true);

            return true;
        }

        private bool CreateRelationshipsSync(PartnerScenario scenario, string expressionProfileResourceId, string documentResourceId)
        {
            if (string.IsNullOrEmpty(expressionProfileResourceId) || string.IsNullOrEmpty(documentResourceId))
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateRelationships error: expression profile Id or document resource Id is empty.");
                return false;
            }

            MsnServiceState serviceState = new MsnServiceState(scenario, "CreateRelationships", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            CreateRelationshipsRequestType createRelationshipRequest = new CreateRelationshipsRequestType();
            Relationship relationship = new Relationship();
            relationship.RelationshipName = "ProfilePhoto";
            relationship.SourceType = "SubProfile"; //From SubProfile
            relationship.TargetType = "Photo";      //To Photo
            relationship.SourceID = expressionProfileResourceId;  //From Expression profile
            relationship.TargetID = documentResourceId;     //To Document

            createRelationshipRequest.relationships = new Relationship[] { relationship };
            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, createRelationshipRequest);
                storageService.CreateRelationships(createRelationshipRequest);
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("CreateRelationships", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "CreateRelationships error: " + ex.Message, GetType().Name);
                return false;
            }

            return true;
        }

        private bool DeleteRelationshipByNameSync(PartnerScenario scenario, string relationshipName, string targetHandlerResourceId)
        {
            MsnServiceState serviceState = new MsnServiceState(scenario, "DeleteRelationships", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            Alias mycidAlias = new Alias();
            mycidAlias.Name = Convert.ToString(NSMessageHandler.ContactList.Owner.CID);
            mycidAlias.NameSpace = "MyCidStuff";

            // 3. DeleteRelationships. If an error occurs, don't return, continue...

            // 3.1 UserTiles -> Photo
            DeleteRelationshipsRequestType request = new DeleteRelationshipsRequestType();
            request.sourceHandle = new Handle();
            request.sourceHandle.RelationshipName = relationshipName; //"/UserTiles";
            request.sourceHandle.Alias = mycidAlias;
            request.targetHandles = new Handle[] { new Handle() };
            request.targetHandles[0].ResourceID = targetHandlerResourceId;
            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, request);
                storageService.DeleteRelationships(request);
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("DeleteRelationships", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                return false;
            }

            return true;

            
        }

        private bool DeleteRelationshipByResourceIdSync(PartnerScenario scenario, string sourceHandlerResourceId, string targetHandlerResourceId)
        {
            if (string.IsNullOrEmpty(sourceHandlerResourceId) || string.IsNullOrEmpty(targetHandlerResourceId))
                return false;

            MsnServiceState serviceState = new MsnServiceState(scenario, "DeleteRelationships", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            //3.2 Profile -> Photo
            DeleteRelationshipsRequestType request = new DeleteRelationshipsRequestType();
            request.sourceHandle = new Handle();
            request.sourceHandle.ResourceID = sourceHandlerResourceId;
            request.targetHandles = new Handle[] { new Handle() };
            request.targetHandles[0].ResourceID = targetHandlerResourceId;
            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, request);
                storageService.DeleteRelationships(request);
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("DeleteRelationships", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                return false;
            }

            return true;
        }

        private bool UpdatePhotoDocumentSync(PartnerScenario scenario, string photoName, string documentResourceId, byte[] photoData)
        {
            if (string.IsNullOrEmpty(documentResourceId) || photoData == null)
                return false;

            MsnServiceState serviceState = new MsnServiceState(scenario, "UpdateDocument", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            UpdateDocumentRequestType request = new UpdateDocumentRequestType();
            request.document = new Photo();

            if (!string.IsNullOrEmpty(photoName))
            {
                request.document.Name = photoName;
            }


            request.document.ResourceID = documentResourceId;
            request.document.DocumentStreams = new PhotoStream[] { new PhotoStream() };
            request.document.DocumentStreams[0].DataSize = 0;
            request.document.DocumentStreams[0].MimeType = "image/png";
            request.document.DocumentStreams[0].DocumentStreamType = "UserTileStatic";
            request.document.DocumentStreams[0].Data = photoData;

            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, request);
                storageService.UpdateDocument(request);
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("UpdateDocument", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, ex.Message, GetType().Name);
                return false;
            }

            NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = photoName;
            NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = new SerializableMemoryStream();
            NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage.Write(photoData, 0, photoData.Length);
            NSMessageHandler.ContactService.Deltas.Save(true);

            return true;
        }

        private bool UpdateProfileLiteSync(PartnerScenario scenario, string profileResourceId, string displayName, string personalStatus, string freeText, int flag)
        {
            MsnServiceState serviceState = new MsnServiceState(scenario, "UpdateProfile", false);
            StorageService storageService = (StorageService)CreateService(MsnServiceType.Storage, serviceState);

            UpdateProfileRequestType updateProfileRequest = new UpdateProfileRequestType();
            updateProfileRequest.profile = new UpdateProfileRequestTypeProfile();
            updateProfileRequest.profile.ResourceID = profileResourceId;
            ExpressionProfile expProf = new ExpressionProfile();
            expProf.FreeText = freeText;
            expProf.DisplayName = displayName;
            expProf.Flags = flag;
            expProf.FlagsSpecified = true;

            if (!string.IsNullOrEmpty(personalStatus))
            {
                expProf.PersonalStatus = personalStatus;
            }
            updateProfileRequest.profile.ExpressionProfile = expProf;

            NSMessageHandler.ContactService.Deltas.Profile.DisplayName = displayName;
            NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage = personalStatus;
            NSMessageHandler.ContactService.Deltas.Save(true);  //Save no matter the request succeed or fail.

            try
            {
                ChangeCacheKeyAndPreferredHostForSpecifiedMethod(storageService, MsnServiceType.Storage, serviceState, updateProfileRequest);
                storageService.UpdateProfile(updateProfileRequest);
            }
            catch (Exception ex)
            {
                OnServiceOperationFailed(storageService, new ServiceOperationFailedEventArgs("UpdateProfile", ex));
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "UpdateProfile error: " + ex.Message, GetType().Name);
                return false;
            }

            return true;
        }

        private bool AddDynamicItemSync(PartnerScenario scenario)
        {
            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                MsnServiceState serviceState = new MsnServiceState(scenario, "AddDynamicItem", false);
                ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, serviceState);

                PassportDynamicItem newDynamicItem = new PassportDynamicItem();
                newDynamicItem.Type = "Passport";
                newDynamicItem.PassportName = NSMessageHandler.ContactList.Owner.Mail;

                AddDynamicItemRequestType addDynamicItemRequest = new AddDynamicItemRequestType();
                addDynamicItemRequest.abId = WebServiceConstants.MessengerIndividualAddressBookId;
                addDynamicItemRequest.dynamicItems = new BaseDynamicItemType[] { newDynamicItem };

                try
                {

                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, MsnServiceType.AB, serviceState, addDynamicItemRequest);
                    abService.AddDynamicItem(addDynamicItemRequest);
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("AddDynamicItem", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "AddDynamicItem error: " + ex.Message, GetType().Name);
                    return false;
                }
                return true;
            }

            return false;
        }

        private bool UpdateDynamicItemSync(PartnerScenario scenario)
        {
            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                //9. UpdateDynamicItem
                MsnServiceState serviceState = new MsnServiceState(scenario, "UpdateDynamicItem", false);
                ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, serviceState);

                UpdateDynamicItemRequestType updateDyItemRequest = new UpdateDynamicItemRequestType();
                updateDyItemRequest.abId = Guid.Empty.ToString();

                PassportDynamicItem passportDyItem = new PassportDynamicItem();
                passportDyItem.Type = "Passport";
                passportDyItem.PassportName = NSMessageHandler.ContactList.Owner.Mail;
                passportDyItem.Changes = "Notifications";

                NotificationDataType notification = new NotificationDataType();
                notification.Status = "Exist Access";
                notification.InstanceId = "0";
                notification.Gleam = false;
                notification.LastChanged = NSMessageHandler.ContactService.Deltas.Profile.DateModified;

                ServiceType srvInfo = new ServiceType();
                srvInfo.Changes = "";

                HandleType srvHandle = new HandleType();
                srvHandle.ForeignId = "MyProfile";
                srvHandle.Id = "0";
                srvHandle.Type = ServiceFilterType.Profile;

                InfoType info = new InfoType();
                info.Handle = srvHandle;
                info.IsBot = false;
                info.InverseRequired = false;

                srvInfo.Info = info;
                notification.StoreService = srvInfo;
                passportDyItem.Notifications = new NotificationDataType[] { notification };
                updateDyItemRequest.dynamicItems = new PassportDynamicItem[] { passportDyItem };
                try
                {

                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, MsnServiceType.AB, serviceState, updateDyItemRequest);
                    abService.UpdateDynamicItem(updateDyItemRequest);
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("UpdateDynamicItem", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "UpdateDynamicItem error: You don't receive any contact updates, vice versa! " + ex.Message, GetType().Name);

                    AddDynamicItemRequestType addDynamicItemRequest = new AddDynamicItemRequestType();
                    addDynamicItemRequest.abId = updateDyItemRequest.abId;
                    addDynamicItemRequest.dynamicItems = updateDyItemRequest.dynamicItems;
                    foreach (BaseDynamicItemType dynamicItem in addDynamicItemRequest.dynamicItems)
                    {
                        dynamicItem.Notifications = null;
                        dynamicItem.Changes = null;
                        dynamicItem.LastChanged = null;
                    }

                    try
                    {
                        ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, MsnServiceType.AB, serviceState, addDynamicItemRequest);
                        abService.AddDynamicItem(addDynamicItemRequest);
                    }
                    catch (Exception exAddDynamicItem)
                    {
                        OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("AddDynamicItem", exAddDynamicItem));
                        Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "AddDynamicItem error: You don't receive any contact updates, vice versa! " + exAddDynamicItem.Message, GetType().Name);
                        return false;
                    }
                    return true;
                }
                return true;
            }

            return false;
        }

        private bool UpdateContactSync(PartnerScenario scenario)
        {
            if (NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                MsnServiceState serviceState = new MsnServiceState(scenario, "ABContactUpdate", false);
                ABServiceBinding abService = (ABServiceBinding)CreateService(MsnServiceType.AB, serviceState);

                ABContactUpdateRequestType abcontactUpdateRequest = new ABContactUpdateRequestType();
                abcontactUpdateRequest.abId = Guid.Empty.ToString();

                ContactType meContact = new ContactType();
                meContact.propertiesChanged = PropertyString.Annotation; //"Annotation";

                contactInfoType meinfo = new contactInfoType();
                meinfo.contactType = MessengerContactType.Me;

                Annotation anno = new Annotation();
                anno.Name = AnnotationNames.MSN_IM_RoamLiveProperties;
                anno.Value = "1";

                meinfo.annotations = new Annotation[] { anno };
                meContact.contactInfo = meinfo;
                abcontactUpdateRequest.contacts = new ContactType[] { meContact };
                try
                {

                    ChangeCacheKeyAndPreferredHostForSpecifiedMethod(abService, MsnServiceType.AB, serviceState, abcontactUpdateRequest);
                    abService.ABContactUpdate(abcontactUpdateRequest);
                }
                catch (Exception ex)
                {
                    OnServiceOperationFailed(abService, new ServiceOperationFailedEventArgs("ABContactUpdate", ex));
                    Trace.WriteLineIf(Settings.TraceSwitch.TraceError, "ABContactUpdate error: " + ex.Message, GetType().Name);
                    return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Initialize the user profile if the contact connect to live network the firt time.
        /// 
        /// CreateProfile
        /// ShareItem
        /// AddMember
        /// [GetProfile]
        /// CreateDocument
        /// CreateRelationships
        /// UpdateProfile
        /// FindDocuments
        /// AddDynamicItem
        /// UpdateDynamicItem
        /// ABContactUpdate
        /// 
        /// 10 steps, what the hell!! If M$ change any protocol in their strageservice, it will be a disaster to find the difference.
        /// </summary>
        private void CreateProfile()
        {
            if (NSMessageHandler.MSNTicket == MSNTicket.Empty || NSMessageHandler.ContactService.Deltas == null)
                return;
            if (NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile == false)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "No expression profile exists, create profile skipped.");
                NSMessageHandler.ContactList.Owner.CreateDefaultDisplayImage(null);  //Set default

                return;
            }

            try
            {
                string profileResourceId = string.Empty;
                string expressionProfileResourceId = string.Empty;
                string documentReourceId = string.Empty;
                bool nextStep = false;
                InternalOperationReturnValues getprofileResult = InternalOperationReturnValues.Succeed;


                //1. CreateProfile, create a new profile and return its resource id.
                nextStep = CreateProfileSync(PartnerScenario.RoamingSeed, out profileResourceId);

                //2. ShareItem, share the profile.
                if (nextStep)
                {

                    nextStep = ShareItemSync(PartnerScenario.RoamingSeed, profileResourceId);

                    //3. AddMember, add a ProfileExpression role member into the newly created profile and define messenger service.
                    AddProfileExpressionRoleMemberSync(PartnerScenario.RoamingSeed);
                }

                // [GetProfile], get the new ProfileExpression resource id.
                getprofileResult = GetProfileLiteSync(PartnerScenario.RoamingSeed,out profileResourceId, out expressionProfileResourceId);

                //4. CreateDocument, create a new document for this profile and return its resource id.
                MemoryStream defaultDisplayImageStream = new MemoryStream();
                Properties.Resources.MSNPSharp_logo.Save(defaultDisplayImageStream, Properties.Resources.MSNPSharp_logo.RawFormat);
                nextStep = CreatePhotoDocumentSync(PartnerScenario.RoamingSeed, out documentReourceId, "MSNPSharp", defaultDisplayImageStream.ToArray());

                //5. CreateRelationships, create a relationship between ProfileExpression role member and the new document.
                if (expressionProfileResourceId != string.Empty && documentReourceId != string.Empty)
                {
                    nextStep = CreateRelationshipsSync(PartnerScenario.RoamingSeed, expressionProfileResourceId, documentReourceId);
                }

                //6.1 UpdateProfile
                if (profileResourceId != string.Empty)
                {
                    nextStep = UpdateProfileLiteSync(PartnerScenario.RoamingSeed, profileResourceId, NSMessageHandler.ContactList.Owner.NickName, string.Empty, "Update", 0);
                }

                // 6.2 Get Profile again to get notification.LastChanged
                if (expressionProfileResourceId != string.Empty)
                {
                    getprofileResult = GetProfileLiteSync(PartnerScenario.RoamingSeed, out profileResourceId, out expressionProfileResourceId);
                }

                //7. FindDocuments Hmm....

                //8. AddDynamicItem
                nextStep = AddDynamicItemSync(PartnerScenario.RoamingSeed);
                if (nextStep)
                {
                    //9. UpdateDynamicItem
                    nextStep = UpdateDynamicItemSync(PartnerScenario.RoamingSeed);
                }

                //10. ABContactUpdate
                nextStep = UpdateContactSync(PartnerScenario.RoamingSeed);

                //11. OK, there's no 11, that's all....
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        private OwnerProfile GetProfileImpl(PartnerScenario scenario)
        {
            string expressProfileId = string.Empty;
            string profileResourceId = string.Empty;

            try
            {
                InternalOperationReturnValues result = GetProfileLiteSync(scenario, out profileResourceId, out expressProfileId);

                if (result == InternalOperationReturnValues.ProfileNotExist)
                {
                    CreateProfile();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceError,
                    "GetProfileLiteSync Error." + ex.Message + "\r\n" + ex.StackTrace);
            }

            if (NSMessageHandler.ContactService.Deltas == null)
            {
                return null;
            }

            return NSMessageHandler.ContactService.Deltas.Profile;
        }

        internal delegate void GetUsertitleByURLhandler(object param);

        internal void SyncUserTile(string usertitleURL, GetUsertitleByURLhandler callBackHandler, object param, GetUsertitleByURLhandler errorHandler)
        {
            try
            {
                Uri uri = new Uri(usertitleURL);

                HttpWebRequest fwr = (HttpWebRequest)WebRequest.Create(uri);
                fwr.Proxy = WebProxy;
                fwr.Timeout = 30000;
                fwr.ServicePoint.BindIPEndPointDelegate = new BindIPEndPoint(new IPEndPointCallback(new IPEndPoint(String.IsNullOrEmpty(NSMessageHandler.ConnectivitySettings.LocalHost) ? IPAddress.Any : IPAddress.Parse(NSMessageHandler.ConnectivitySettings.LocalHost), NSMessageHandler.ConnectivitySettings.LocalPort)).BindIPEndPointCallback);

                fwr.BeginGetResponse(delegate(IAsyncResult result)
                {
                    try
                    {
                        Stream stream = ((WebRequest)result.AsyncState).EndGetResponse(result).GetResponseStream();
                        SerializableMemoryStream ms = new SerializableMemoryStream();
                        byte[] data = new byte[8192];
                        int read;
                        while ((read = stream.Read(data, 0, data.Length)) > 0)
                        {
                            ms.Write(data, 0, read);
                        }
                        stream.Close();

                        DisplayImage newDisplayImage = new DisplayImage(NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant(), ms);

                        NSMessageHandler.ContactList.Owner.DisplayImage = newDisplayImage;
                        if (callBackHandler != null)
                        {
                            callBackHandler(param);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (errorHandler != null)
                            errorHandler(ex);

                        return;
                    }

                }, fwr);
            }
            catch (Exception e)
            {
                if (errorHandler != null)
                    errorHandler(e);

                return;
            }
        }


        private void UpdateProfileImpl(string displayName, string personalStatus, string freeText, int flags)
        {


            if (NSMessageHandler.ContactList.Owner.RoamLiveProperty == RoamLiveProperty.Enabled &&
                NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile &&
                NSMessageHandler.BotMode == false)
            {
                UpdateProfileLiteSync(PartnerScenario.RoamingIdentityChanged,
                    NSMessageHandler.ContactService.Deltas.Profile.ResourceID,
                    displayName,
                    personalStatus,
                    freeText,
                    flags);

                // And get profile again
                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl(PartnerScenario.RoamingIdentityChanged);

                // UpdateDynamicItem
                UpdateDynamicItemSync(PartnerScenario.RoamingIdentityChanged);

            }
            else
            {
                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "Roaming disabled or invalid expression profile. Update skipped.");
                NSMessageHandler.ContactService.Deltas.Profile.DisplayName = displayName;
                NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage = personalStatus;
                NSMessageHandler.ContactService.Deltas.Save(true);
            }

        }

        #endregion

        /// <summary>
        /// Get my profile. Display name, personal status and display photo.
        /// </summary>
        public OwnerProfile GetProfile()
        {
            if (NSMessageHandler.BotMode == false)
            {
                if (NSMessageHandler.ContactService.Deltas == null)
                {
                    OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("GetProfile", new MSNPSharpException("You don't have access right on this action anymore.")));
                    return null;
                }

                if (NSMessageHandler.ContactList.Owner.RoamLiveProperty == RoamLiveProperty.Enabled && NSMessageHandler.MSNTicket != MSNTicket.Empty)
                {
                    DateTime deltasProfileDateModified = WebServiceDateTimeConverter.ConvertToDateTime(NSMessageHandler.ContactService.Deltas.Profile.DateModified);
                    DateTime annotationLiveProfileExpressionLastChanged = WebServiceDateTimeConverter.ConvertToDateTime(NSMessageHandler.ContactService.AddressBook.MyProperties[AnnotationNames.Live_Profile_Expression_LastChanged]);

                    if ((annotationLiveProfileExpressionLastChanged == DateTime.MinValue) ||
                        (deltasProfileDateModified < annotationLiveProfileExpressionLastChanged))
                    {
                        return GetProfileImpl(PartnerScenario.Initial);
                    }
                }
            }
            else
            {
                if (NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage == null)
                {
                    SerializableMemoryStream serStream = new SerializableMemoryStream();
                    Properties.Resources.MSNPSharp_logo.Save(serStream, Properties.Resources.MSNPSharp_logo.RawFormat);

                    NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = serStream;
                    NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile = false;
                    NSMessageHandler.ContactService.Deltas.Save(true);
                }
            }

            return NSMessageHandler.ContactService.Deltas.Profile;
        }

        /// <summary>
        /// Update personal displayname and status in profile
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="personalStatus"></param>
        public void UpdateProfile(string displayName, string personalStatus)
        {
            if (NSMessageHandler.ContactService.Deltas == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("UpdateProfile", new MSNPSharpException("You don't have access right on this action anymore.")));
                return;
            }

            if (NSMessageHandler.MSNTicket != MSNTicket.Empty &&
                (NSMessageHandler.ContactService.Deltas.Profile.DisplayName != displayName ||
                NSMessageHandler.ContactService.Deltas.Profile.PersonalMessage != personalStatus))
            {
                UpdateProfileImpl(displayName, personalStatus, "Update", 0);
            }
        }


        /// <summary>
        /// Update the display photo of current user.
        /// <list type="bullet">
        /// <item>GetProfile with scenario = "RoamingIdentityChanged"</item>
        /// <item>UpdateDynamicItem</item>
        /// </list>
        /// </summary>
        /// <param name="photo">New photo to display</param>
        /// <param name="photoName">The resourcename</param>
        public bool UpdateProfile(Image photo, string photoName)
        {
            if (NSMessageHandler.ContactService.Deltas == null)
            {
                OnServiceOperationFailed(this, new ServiceOperationFailedEventArgs("UpdateProfile", new MSNPSharpException("You don't have access right on this action anymore.")));
                return false;
            }

            if (NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile == false)  //Non-expression id or provisioned account.
            {
                NSMessageHandler.ContactService.Deltas.Profile.Photo.Name = photoName;
                NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage = new SerializableMemoryStream();
                photo.Save(NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage, photo.RawFormat);
                NSMessageHandler.ContactService.Deltas.Save(true);

                DisplayImage newDisplayImage = new DisplayImage(NSMessageHandler.ContactList.Owner.Mail.ToLowerInvariant(), NSMessageHandler.ContactService.Deltas.Profile.Photo.DisplayImage);

                NSMessageHandler.ContactList.Owner.DisplayImage = newDisplayImage;

                Trace.WriteLineIf(Settings.TraceSwitch.TraceInfo, "No expression profile exists, new profile is saved locally.");
                return true;
            }

            if (NSMessageHandler.ContactList.Owner.RoamLiveProperty == RoamLiveProperty.Enabled &&
                NSMessageHandler.MSNTicket != MSNTicket.Empty)
            {
                SerializableMemoryStream mem = SerializableMemoryStream.FromImage(photo);

                // 1. Getprofile
                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl(PartnerScenario.RoamingIdentityChanged);

                bool updateDocumentResult = false;
                // 1.1 UpdateDocument
                if (!String.IsNullOrEmpty(NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID))
                {
                    updateDocumentResult = UpdatePhotoDocumentSync(PartnerScenario.RoamingIdentityChanged,
                        NSMessageHandler.ContactService.Deltas.Profile.Photo.Name,
                        NSMessageHandler.ContactService.Deltas.Profile.Photo.ResourceID,
                        mem.ToArray());

                    // UpdateDynamicItem
                    if (updateDocumentResult)
                    {
                        updateDocumentResult = UpdateDynamicItemSync(PartnerScenario.RoamingIdentityChanged);
                    }
                }

                if (updateDocumentResult == false &&
                    NSMessageHandler.ContactService.Deltas.Profile.HasExpressionProfile)
                {
                    string resourceId = string.Empty;
                    CreatePhotoDocumentSync(PartnerScenario.RoamingIdentityChanged, out resourceId, photoName, mem.ToArray());
                    UpdateDynamicItemSync(PartnerScenario.RoamingIdentityChanged);
                }

                NSMessageHandler.ContactService.Deltas.Profile = GetProfileImpl(PartnerScenario.RoamingIdentityChanged);

                return true;
            }

            return false;
        }
    }
};
