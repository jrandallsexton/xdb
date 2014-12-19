
using System;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;

using XDB.UI.Constants;
using XDB.DAL;
using XDB.DataObjects;
using XDB.Enumerations;

using XDB.UI.DataObjects;

namespace XDB.UI.DAL
{

    internal class PropertyGroupPropertyRelationDal : XSqlDal
    {

        public PropertyGroupPropertyRelationDal() { }

        public PropertyGroupPropertyRelationDal(string connString) { this.ConnectionString = connString; }

        private const string spPropertyGroupPropertyRelation_Get = "spr_PropertyGroupPropertyRelation_Get";
        private const string spPropertyGroupPropertyRelation_Save = "spr_PropertyGroupPropertyRelation_Save";
        private const string spPropertyGroupPropertyRelation_Delete = "spr_PropertyGroupPropertyRelation_Delete";

        //Collection SPs
        private const string spPropertyGroupPropertyRelationList_Get = "spr_PropertyGroupPropertyRelationList_Get";
        private const string spPropertyGroupPropertyRelationList_Save = "spr_PropertyGroupPropertyRelationList_Save";
        private const string spPropertyGroupPropertyRelationList_Delete = "spr_PropertyGroupPropertyRelationList_Delete";

        //Collection by FK SPs
        private const string spPropertyGroupPropertyRelationList_GetByPropertyGroupId = "spr_PropertyGroupPropertyRelationList_GetByPropertyGroupId";
        private const string spPropertyGroupPropertyRelationList_DeleteByPropertyGroupId = "spr_PropertyGroupPropertyRelationList_DeleteByPropertyGroupId";

        //Collection by FK SPs
        private const string spPropertyGroupPropertyRelationList_GetByPropertyId = "spr_PropertyGroupPropertyRelationList_GetByPropertyId";
        private const string spPropertyGroupPropertyRelationList_DeleteByPropertyId = "spr_PropertyGroupPropertyRelationList_DeleteByPropertyId";

        public XPropertyGroupPropertyRelation PropertyGroupPropertyRelation_Get(Guid relationId)
        {

            List<SqlParameter> paramList = null;
            XPropertyGroupPropertyRelation relation = null;

            paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", relationId));

            using (SqlDataReader rdr = base.OpenDataReader(spPropertyGroupPropertyRelation_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                relation = new XPropertyGroupPropertyRelation();

                rdr.Read();

                if (!rdr.IsDBNull(rdr.GetOrdinal("PropertyGroupId"))) relation.PropertyGroupId = (Guid)rdr[rdr.GetOrdinal("PropertyGroupId")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("PropertyId"))) relation.PropertyId = (Guid)rdr[rdr.GetOrdinal("PropertyId")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("Order"))) relation.Index = rdr.GetInt32(rdr.GetOrdinal("Order"));

                if (!rdr.IsDBNull(rdr.GetOrdinal("IsRequired"))) relation.IsRequired = (bool)rdr[rdr.GetOrdinal("IsRequired")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("IsReadOnly"))) relation.IsReadOnly = (bool)rdr[rdr.GetOrdinal("IsReadOnly")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("DefaultValue"))) { relation.DefaultValue = rdr.GetString(rdr.GetOrdinal("DefaultValue")); }

                if (!rdr.IsDBNull(rdr.GetOrdinal("Created"))) relation.Created = (DateTime)rdr[rdr.GetOrdinal("Created")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("CreatedBy"))) relation.CreatedBy = (Guid)rdr[rdr.GetOrdinal("CreatedBy")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("DeletedBy"))) relation.DeletedBy = (Guid)rdr[rdr.GetOrdinal("DeletedBy")];

                relation.Id = relationId;
                relation.IsNew = false;
                relation.IsDirty = false;

                return relation;

            }

        }

        public bool PropertyGroupPropertyRelation_Save(XPropertyGroupPropertyRelation relation)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", relation.Id));
            paramList.Add(new SqlParameter("@PropertyGroupId", relation.PropertyGroupId));
            paramList.Add(new SqlParameter("@PropertyId", relation.PropertyId));
            paramList.Add(new SqlParameter("@Order", relation.Index));
            paramList.Add(new SqlParameter("@IsRequired", relation.IsRequired));
            paramList.Add(new SqlParameter("@IsReadOnly", relation.IsReadOnly));

            if (string.IsNullOrEmpty(relation.DefaultValue))
            {
                paramList.Add(new SqlParameter("@DefaultValue", null));
            }
            else
            {
                paramList.Add(new SqlParameter("@DefaultValue", relation.DefaultValue));
            }

            paramList.Add(new SqlParameter("@Created", relation.Created));
            paramList.Add(new SqlParameter("@Deleted", relation.Deleted));
            paramList.Add(new SqlParameter("@CreatedBy", relation.CreatedBy));
            paramList.Add(new SqlParameter("@DeletedBy", relation.DeletedBy));

            if (base.ExecuteSql(spPropertyGroupPropertyRelation_Save, paramList))
            {

                relation.IsNew = false;
                relation.IsDirty = false;

                return true;

            }

            return false;

        }

        /// <summary>
        /// Deletes a PropertyGroup-Property relation based on the specified Id
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public bool PropertyGroupPropertyRelation_Delete(Guid Id, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", Id));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(spPropertyGroupPropertyRelation_Delete, paramList);

        }

        public List<XPropertyGroupPropertyRelation> PropertyGroupPropertyRelationList_Get()
        {

            List<XPropertyGroupPropertyRelation> list = new List<XPropertyGroupPropertyRelation>();

            List<SqlParameter> paramList = new List<SqlParameter>();

            using (SqlDataReader rdr = base.OpenDataReader(spPropertyGroupPropertyRelationList_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return list; }

                int Id = rdr.GetOrdinal("Id");
                int PropertyGroupId = rdr.GetOrdinal("PropertyGroupId");
                int PropertyId = rdr.GetOrdinal("PropertyId");
                int Order = rdr.GetOrdinal("Order");
                int IsRequired = rdr.GetOrdinal("IsRequired");
                int IsReadOnly = rdr.GetOrdinal("IsReadOnly");
                int DefaultValue = rdr.GetOrdinal("DefaultValue");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");
                int DeletedBy = rdr.GetOrdinal("DeletedBy");

                while (rdr.Read())
                {

                    XPropertyGroupPropertyRelation relation = new XPropertyGroupPropertyRelation();

                    if (!rdr.IsDBNull(Id)) relation.Id = rdr.GetGuid(Id);

                    if (!rdr.IsDBNull(PropertyGroupId)) relation.PropertyGroupId = rdr.GetGuid(PropertyGroupId);

                    if (!rdr.IsDBNull(PropertyId)) relation.PropertyId = rdr.GetGuid(PropertyId);

                    if (!rdr.IsDBNull(Order)) relation.Index = rdr.GetInt32(Order);

                    if (!rdr.IsDBNull(IsRequired)) relation.IsRequired = (bool)rdr[IsRequired];

                    if (!rdr.IsDBNull(IsReadOnly)) relation.IsReadOnly = (bool)rdr[IsReadOnly];

                    if (!rdr.IsDBNull(DefaultValue)) { relation.DefaultValue = rdr.GetString(DefaultValue); }

                    if (!rdr.IsDBNull(Created)) relation.Created = (DateTime)rdr.GetValue(Created);

                    if (!rdr.IsDBNull(CreatedBy)) relation.CreatedBy = rdr.GetGuid(CreatedBy);

                    if (!rdr.IsDBNull(DeletedBy)) relation.DeletedBy = rdr.GetGuid(DeletedBy);

                    relation.IsNew = false;

                    relation.IsDirty = false;

                    list.Add(relation);

                }
                return list;

            }

        }

        public bool PropertyGroupPropertyRelationList_Save(List<XPropertyGroupPropertyRelation> propertygrouppropertyrelationList)
        {

            foreach (XPropertyGroupPropertyRelation _propertygrouppropertyrelation in propertygrouppropertyrelationList)
            {
                if (_propertygrouppropertyrelation.IsDirty)
                {
                    PropertyGroupPropertyRelation_Save(_propertygrouppropertyrelation);
                    _propertygrouppropertyrelation.IsNew = false;
                    _propertygrouppropertyrelation.IsDirty = false;
                }

            }

            return true;

        }

        public bool PropertyGroupPropertyRelationList_Delete(List<XPropertyGroupPropertyRelation> relations, Guid userId)
        {
            foreach (XPropertyGroupPropertyRelation relation in relations)
            {
                PropertyGroupPropertyRelation_Delete(relation.Id, userId);
            }
            return true;

        }

        public List<XPropertyGroupPropertyRelation> PropertyGroupPropertyRelationList_GetByPropertyGroupId(Guid propertygroupid)
        {

            List<XPropertyGroupPropertyRelation> list = new List<XPropertyGroupPropertyRelation>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@PropertyGroupId", propertygroupid));

            using (SqlDataReader rdr = base.OpenDataReader(spPropertyGroupPropertyRelationList_GetByPropertyGroupId, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return list; }

                //get the index of each property we are going to load
                int Id = rdr.GetOrdinal("Id");
                int PropertyGroupId = rdr.GetOrdinal("PropertyGroupId");
                int PropertyId = rdr.GetOrdinal("PropertyId");
                int Order = rdr.GetOrdinal("Order");
                int IsRequired = rdr.GetOrdinal("IsRequired");
                int IsReadOnly = rdr.GetOrdinal("IsReadOnly");
                int DefaultValue = rdr.GetOrdinal("DefaultValue");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");
                int DeletedBy = rdr.GetOrdinal("DeletedBy");

                while (rdr.Read())
                {

                    XPropertyGroupPropertyRelation relation = new XPropertyGroupPropertyRelation();

                    if (!rdr.IsDBNull(Id)) relation.Id = rdr.GetGuid(Id);

                    if (!rdr.IsDBNull(PropertyGroupId)) relation.PropertyGroupId = rdr.GetGuid(PropertyGroupId);

                    if (!rdr.IsDBNull(PropertyId)) relation.PropertyId = rdr.GetGuid(PropertyId);

                    if (!rdr.IsDBNull(Order)) relation.Index = rdr.GetInt32(Order);

                    if (!rdr.IsDBNull(IsRequired)) relation.IsRequired = (bool)rdr[IsRequired];

                    if (!rdr.IsDBNull(IsReadOnly)) relation.IsReadOnly = (bool)rdr[IsReadOnly];

                    if (!rdr.IsDBNull(DefaultValue)) { relation.DefaultValue = rdr.GetString(DefaultValue); }

                    if (!rdr.IsDBNull(Created)) relation.Created = (DateTime)rdr.GetValue(Created);

                    if (!rdr.IsDBNull(CreatedBy)) relation.CreatedBy = rdr.GetGuid(CreatedBy);

                    if (!rdr.IsDBNull(DeletedBy)) relation.DeletedBy = rdr.GetGuid(DeletedBy);

                    relation.IsNew = false;

                    relation.IsDirty = false;

                    list.Add(relation);

                }

                return list;

            }

        }

        public bool PropertyGroupPropertyRelationList_DeleteByPropertyGroupId(Guid PropertyGroupId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", PropertyGroupId));

            return base.ExecuteSql(spPropertyGroupPropertyRelationList_DeleteByPropertyGroupId, paramList);

        }

        public List<XPropertyGroupPropertyRelation> PropertyGroupPropertyRelationList_GetByPropertyId(Guid propertyid)
        {

            List<XPropertyGroupPropertyRelation> list = new List<XPropertyGroupPropertyRelation>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", propertyid));

            using (SqlDataReader rdr = base.OpenDataReader(spPropertyGroupPropertyRelationList_GetByPropertyId, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return list; }

                //get the index of each property we are going to load
                int Id = rdr.GetOrdinal("Id");
                int PropertyGroupId = rdr.GetOrdinal("PropertyGroupId");
                int PropertyId = rdr.GetOrdinal("PropertyId");
                int Order = rdr.GetOrdinal("Order");
                int IsRequired = rdr.GetOrdinal("IsRequired");
                int IsReadOnly = rdr.GetOrdinal("IsReadOnly");
                int DefaultValue = rdr.GetOrdinal("DefaultValue");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");
                int DeletedBy = rdr.GetOrdinal("DeletedBy");

                while (rdr.Read())
                {

                    XPropertyGroupPropertyRelation relation = new XPropertyGroupPropertyRelation();

                    if (!rdr.IsDBNull(Id)) relation.Id = rdr.GetGuid(Id);

                    if (!rdr.IsDBNull(PropertyGroupId)) relation.PropertyGroupId = rdr.GetGuid(PropertyGroupId);

                    if (!rdr.IsDBNull(PropertyId)) relation.PropertyId = rdr.GetGuid(PropertyId);

                    if (!rdr.IsDBNull(Order)) relation.Index = rdr.GetInt32(Order);

                    if (!rdr.IsDBNull(IsRequired)) relation.IsRequired = (bool)rdr[IsRequired];

                    if (!rdr.IsDBNull(IsReadOnly)) relation.IsReadOnly = (bool)rdr[IsReadOnly];

                    if (!rdr.IsDBNull(DefaultValue)) { relation.DefaultValue = rdr.GetString(DefaultValue); }

                    if (!rdr.IsDBNull(Created)) relation.Created = (DateTime)rdr.GetValue(Created);

                    if (!rdr.IsDBNull(CreatedBy)) relation.CreatedBy = rdr.GetGuid(CreatedBy);

                    if (!rdr.IsDBNull(DeletedBy)) relation.DeletedBy = rdr.GetGuid(DeletedBy);

                    relation.IsNew = false;

                    relation.IsDirty = false;

                    list.Add(relation);

                }

                return list;

            }

        }

        public bool PropertyGroupPropertyRelationList_DeleteByPropertyId(Guid PropertyId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", PropertyId));

            return base.ExecuteSql(spPropertyGroupPropertyRelationList_DeleteByPropertyId, paramList);

        }

    }

}