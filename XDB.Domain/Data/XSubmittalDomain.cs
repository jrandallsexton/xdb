
using System;
using System.Collections.Generic;

using XDB.Models;
using XDB.Common;
using XDB.Common.Enumerations;
using XDB.Common.Exceptions;
using XDB.Common.Interfaces;
using XDB.Repositories;

namespace XDB.Domains
{

    public class XSubmittalDomain
    {

        private XSubmittalRepository _dal = new XSubmittalRepository();

        public XSubmittalDomain() { }

        public KeyValuePair<Guid, string> AssetInfo(Guid submittalId)
        {
            return this._dal.AssetInfo(submittalId);
        }

        /// <summary>
        /// Gets an instance of a property matching the specified id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public XSubmittal PropertyValueSubmittal_Get(Guid id)
        {
            XSubmittal submittal = this._dal.Get(id);

            XPropertyDomain pLayer = new XPropertyDomain();
            foreach (XValue pv in submittal.PropertyValues)
            {
                pv.Property = pLayer.DisplayValue(pv.PropertyId);
            }

            return submittal;
        }

        /// <summary>
        /// Save a property value submittal group and all of its propertyValues to the database
        /// </summary>
        /// <param name="submittal">submittal group</param>
        /// <param name="isAutoApprove">whether or not all of the propertyValues in this group should be approved right now</param>
        /// <param name="userId">id of the user submitting these values</param>
        /// <returns>true if operation was successful; false otherwise</returns>
        public void Save(XSubmittal submittal, bool isAutoApprove, Guid userId)
        {

            //Helpers.Log("PropertyValueSubmittalLayer", "PropertyValueSubmittal_Save()");

            if (submittal.AssetId.CompareTo(new Guid()) == 0)
            {
                throw new LogicalException("AssetId was not provided for the Submittal Group", string.Empty);
            }

            //if (isAutoApprove && (userId.HasValue == false))
            //{
            //    throw new LogicalException("Submittal marked as auto-approve, but user id not provided");
            //}

            //List<SynchTrigger> triggers = this.CreateSynchTriggers(submittal);

            bool wasNewSubmittal = submittal.IsNew; // set it here b/c will become false after saving

            this._dal.Save(submittal);

            new XValueDomain().PropertyValueList_Save(submittal.PropertyValues, userId);

            //new SynchTriggerDal().Save(triggers);

            if (isAutoApprove)
            {
                this.Approve(submittal, userId);
                //ThreadPool.QueueUserWorkItem(o => this.GenerateNarrative(submittal.Id));
            }

            if (!wasNewSubmittal)
            {
                //ThreadPool.QueueUserWorkItem(o => new AssetLayer().MarkAsUpdated(submittal.AssetId, userId));
            }

            if (Config.NotificationsEnabled && wasNewSubmittal)
            {
                // notify admins of the submittal
                //return new NotificationLayer().Notification_SendForSubmittal(submittal);
            }

        }

        public bool Approve(Guid submittalId, Guid userId)
        {
            XSubmittal submittal = this.PropertyValueSubmittal_Get(submittalId);
            if (this.Approve(submittal, userId))
            {
                //ThreadPool.QueueUserWorkItem(o => this.GenerateNarrative(submittalId));
                return true;
            }
            return false;
        }

        private bool Approve(XSubmittal submittal, Guid userId)
        {

            //Helpers.Log("PropertyValueSubmittalLayer", "PropertyValueSubmittal_Approve()");            

            if (submittal != null)
            {

                foreach (XValue pv in submittal.PropertyValues)
                {
                    pv.Approved = DateTime.Now;
                    pv.ApprovedBy = userId;
                    pv.IsDirty = true;
                }

                submittal.Approved = DateTime.Now;
                submittal.ApprovedBy = userId;
                submittal.IsDirty = true;

                //List<SynchTrigger> triggers = this.CreateSynchTriggers(submittal);

                this.Save(submittal, false, userId);
                //new SynchTriggerDal().Save(triggers);                    

                List<Guid> propIds = new List<Guid>();
                foreach (XValue pv in submittal.PropertyValues)
                {
                    if (!propIds.Contains(pv.PropertyId)) { propIds.Add(pv.PropertyId); }
                }

                IDictionary<Guid, XProperty> properties = new XPropertyDomain().GetObjectDictionary(propIds);

                foreach (XValue pv in submittal.PropertyValues)
                {

                    XProperty prop = properties[pv.PropertyId];

                    if (prop != null)
                    {

                        XObjectRelation relation = null;

                        if ((prop.DataType == EDataType.Relation_ParentChild) || (prop.DataType == EDataType.Relation_ChildParent))
                        {
                            #region Relations

                            // need to create a relation for this item
                            relation = new XObjectRelation();

                            if (prop.DataType == EDataType.Relation_ParentChild)
                            {
                                relation.FromAssetId = pv.AssetId;
                                relation.ToAssetId = new Guid(pv.Value);
                            }
                            else
                            {
                                relation.FromAssetId = new Guid(pv.Value);
                                relation.ToAssetId = pv.AssetId;
                            }

                            relation.AssetRelationType = EObjectRelationType.ParentChild;

                            #endregion
                        }
                        else if (prop.DataType == EDataType.Dependency)
                        {
                            #region Dependency
                            relation = new XObjectRelation();
                            relation.FromAssetId = pv.AssetId;
                            relation.ToAssetId = new Guid(pv.Value);
                            relation.AssetRelationType = EObjectRelationType.Dependency;
                            #endregion
                        }
                        else if ((prop.DataType == EDataType.User) && (prop.RoleId.HasValue))
                        {

                            // TODO: Complete implementation of the dynamic role-based security

                            if (!string.IsNullOrEmpty(pv.Value))
                            {
                                Guid memberId;

                                if (Guid.TryParse(pv.Value, out memberId))
                                {
                                    XRoleDomain roleLayer = new XRoleDomain();
                                    if (!roleLayer.ContainsUser(prop.RoleId.Value, memberId))
                                    {
                                        //roleLayer.AddMemberToRole(memberId, prop.RoleId.Value, Constants.MemberIds.Admin);
                                    }
                                }

                            }

                            // determine if someone else is already in this value
                            // if they are, we need to determine whether or not they are in this role for another asset
                            // if not, remove the role association
                            // if they are, do nothing
                            // determine whether or not the newly-supplied user is in this role
                            // if not, add them to the role
                            // if they are, do nothing
                        }

                        if (prop.IsSystem && (prop.SystemType == ESystemType.AssetName))
                        {
                            bool renamed = new XObjectDomain().Rename(pv.AssetId, pv.Value);
                        }

                        if (relation != null)
                        {
                            #region more relation stuff

                            relation.Created = DateTime.Now;
                            relation.CreatedBy = submittal.CreatedBy;
                            relation.Approved = relation.Created;
                            relation.ApprovedBy = submittal.CreatedBy;

                            XObjectRelationDomain relationDal = new XObjectRelationDomain();

                            // a child can only have one parent
                            // delete any previous relations
                            if (prop.DataType == EDataType.Relation_ChildParent)
                            {
                                relationDal.RemoveExistingParentRelations(relation.FromAssetId, relation.AssetRelationType.GetHashCode());
                            }

                            if (!relationDal.AssetAssetRelationExists(relation.FromAssetId, relation.ToAssetId, relation.AssetRelationType.GetHashCode()))
                            {
                                relationDal.Save(relation);
                            }

                            // TODO: Need more logic in here.  Other cases
                            // What if relation is removed?

                            #endregion
                        }

                    }

                }

                return new XObjectDomain().MarkAsUpdated(submittal.AssetId, submittal.CreatedBy);
            }

            return false;

        }

        //public PropertyValueSubmittalList PropertyValueSubmittalList_GetDisplay()
        //{
        //    return this._dal.PropertyValueSubmittalList_GetDisplay();
        //}

        //public List<SubmittalActivityHelper> SubmittalActivity_GetByRoleMembership(Guid userId, int maxItems)
        //{
        //    RoleLayer rLayer = new RoleLayer();

        //    if (rLayer.ContainsMember(userId, Core.Constants.RoleIds.Admin))
        //    {
        //        return this._dal.SubmittalActivity_GetForAdmin(maxItems);
        //    }
        //    else
        //    {
        //        // first we have to get all of the editable roles the user is in
        //        List<EditableRoleHelper> roles = rLayer.Roles_GetEditable(userId);

        //        // construct the sql statement
        //        StringBuilder sql = new SqlDatabaseLayer().GetSqlString(roles);

        //        return this._dal.SubmittalActivity_GetByRoleMembership(sql.ToString(), maxItems);
        //    }

        //}

        //public Dictionary<Guid, string> SubmittalActivity_Get(Dictionary<Guid, EAssetRequestType> assetTypeIds, int? maxRecords)
        //{
        //    AssetLayer aLayer = new AssetLayer();

        //    Dictionary<Guid, string> values = new Dictionary<Guid, string>();
        //    foreach (PropertyValueSubmittal submittal in this._dal.PropertyValueSubmittalList_GetDisplay(assetTypeIds, maxRecords))
        //    {
        //        DateTime atc = aLayer.Created(submittal.AssetId);
        //        string action = string.Empty;

        //        if (atc.CompareTo(submittal.Created) == 0)
        //        {
        //            action = "was created";
        //        }
        //        else
        //        {
        //            action = "was modified";
        //        }

        //        string display = string.Format("{0} {1} on {2} by {3}", submittal.AssetName, action, submittal.Created.ToString(), submittal.CreatedByDisplay);
        //        values.Add(submittal.Id, display);
        //    }
        //    return values;
        //}

        //public Dictionary<Guid, string> Submittals_Get()
        //{
        //    Dictionary<Guid, string> values = new Dictionary<Guid, string>();
        //    foreach (PropertyValueSubmittal submittal in this._dal.PropertyValueSubmittalList_GetDisplay())
        //    {
        //        string display = string.Format("{0} on {1} by {2}", submittal.AssetName, submittal.Created.ToString(), submittal.CreatedByDisplay);
        //        values.Add(submittal.Id, display);
        //    }
        //    return values;
        //}

        //private List<SynchTrigger> CreateSynchTriggers(PropertyValueSubmittal submittal)
        //{
        //    List<SynchTrigger> triggers = new List<SynchTrigger>();
        //    Guid groupId = Guid.NewGuid();
        //    int index = 0;

        //    if (submittal.IsNew)
        //    {
        //        triggers.Add(new SynchTrigger(groupId, ESynchTriggerType.PropertyValuesSubmitted, submittal.Id, index, submittal.CreatedBy));
        //        return triggers;
        //    }
        //    else
        //    {
        //        foreach (PropertyValue pv in submittal.PropertyValues)
        //        {
        //            if (pv.IsDirty && pv.Approved.HasValue && pv.ApprovedBy.HasValue && !pv.Deleted.HasValue && !pv.DeletedBy.HasValue)
        //            {
        //                triggers.Add(new SynchTrigger(groupId, ESynchTriggerType.PropertyValueApproved, pv.Id, index, pv.ApprovedBy.Value));
        //            }
        //        }
        //    }

        //    return triggers;
        //}

        public DateTime Created(Guid submittalId)
        {
            return this._dal.Created(submittalId);
        }

        public Dictionary<string, KeyValuePair<string, string>> GetSummary(Guid submittalId)
        {
            return this._dal.GetSummary(submittalId);
        }

        public List<XSubmittal> GetUnprocessedForTriggers()
        {
            return this._dal.GetUnprocessedForTriggers();
        }

    }

}