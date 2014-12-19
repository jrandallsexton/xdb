
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

    internal class ViewPropertyGroupRelationDal : XSqlDal
    {

        public ViewPropertyGroupRelationDal() { }

        public ViewPropertyGroupRelationDal(string connString) { this.ConnectionString = connString; }

        public ViewPropertyGroupRelation ViewPropertyGroupRelation_Get(Guid id)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.spViewPropertyGroupRelation_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                ViewPropertyGroupRelation relation = new ViewPropertyGroupRelation();

                rdr.Read();

                relation.Id = id;

                int ViewId = rdr.GetOrdinal("ViewId");
                int PropertyGroupId = rdr.GetOrdinal("PropertyGroupId");
                int Order = rdr.GetOrdinal("Order");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");
                int Deleted = rdr.GetOrdinal("Deleted");
                int DeletedBy = rdr.GetOrdinal("DeletedBy");

                relation.ViewId = rdr.IsDBNull(ViewId) ? new Guid() : rdr.GetGuid(ViewId);

                relation.PropertyGroupId = rdr.IsDBNull(PropertyGroupId) ? new Guid() : rdr.GetGuid(PropertyGroupId);

                relation.Index = rdr.IsDBNull(Order) ? 0 : rdr.GetInt32(Order);

                if (!rdr.IsDBNull(Created)) { relation.Created = rdr.GetDateTime(Created); }

                relation.CreatedBy = rdr.IsDBNull(CreatedBy) ? new Guid() : rdr.GetGuid(CreatedBy);

                if (!rdr.IsDBNull(Deleted)) { relation.Deleted = rdr.GetDateTime(Deleted); }

                if (!rdr.IsDBNull(DeletedBy)) { relation.DeletedBy = rdr.GetGuid(DeletedBy); }

                relation.IsNew = false;
                relation.IsDirty = false;

                return relation;

            }

        }

        public bool ViewPropertyGroupRelation_Save(ViewPropertyGroupRelation relation)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", relation.Id));
            paramList.Add(new SqlParameter("@ViewId", relation.ViewId));
            paramList.Add(new SqlParameter("@PropertyGroupId", relation.PropertyGroupId));
            paramList.Add(new SqlParameter("@Order", relation.Index));
            paramList.Add(new SqlParameter("@Created", relation.Created));
            paramList.Add(new SqlParameter("@CreatedBy", relation.CreatedBy));
            paramList.Add(new SqlParameter("@Deleted", relation.Deleted));
            paramList.Add(new SqlParameter("@DeletedBy", relation.DeletedBy));

            if (base.ExecuteSql(StoredProcs.spViewPropertyGroupRelation_Save, paramList))
            {

                relation.IsNew = false;
                relation.IsDirty = false;

                return true;

            }

            return false;

        }

        public bool ViewPropertyGroupRelation_Delete(Guid id, Guid userId)
        {

            List<SqlParameter> paramList = null;
            paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", id));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(StoredProcs.spViewPropertyGroupRelation_Delete, paramList);

        }

        public bool ViewPropertyGroupRelationList_Save(List<ViewPropertyGroupRelation> relations)
        {

            foreach (ViewPropertyGroupRelation relation in relations)
            {                
                if (relation.IsDirty)
                {
                    this.ViewPropertyGroupRelation_Save(relation);
                    relation.IsNew = false;
                    relation.IsDirty = false;
                }
            }

            return true;

        }

        public bool ViewPropertyGroupRelationList_Delete(List<ViewPropertyGroupRelation> relations, Guid userId)
        {
            foreach (ViewPropertyGroupRelation relation in relations)
            {
                this.ViewPropertyGroupRelation_Delete(relation.Id, userId);
            }
            return true;
        }

        public List<ViewPropertyGroupRelation> ViewPropertyGroupRelationList_GetByViewId(Guid viewid)
        {

            List<ViewPropertyGroupRelation> values = new List<ViewPropertyGroupRelation>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@ViewId", viewid));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.spViewPropertyGroupRelationList_GetByViewId, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                //get the index of each property we are going to load
                int Id = rdr.GetOrdinal("Id");
                int ViewId = rdr.GetOrdinal("ViewId");
                int PropertyGroupId = rdr.GetOrdinal("PropertyGroupId");
                int Order = rdr.GetOrdinal("Order");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");

                while (rdr.Read())
                {

                    ViewPropertyGroupRelation relation = new ViewPropertyGroupRelation();

                    if (!rdr.IsDBNull(Id)) relation.Id = rdr.GetGuid(Id);

                    if (!rdr.IsDBNull(ViewId)) relation.ViewId = rdr.GetGuid(ViewId);

                    if (!rdr.IsDBNull(PropertyGroupId)) relation.PropertyGroupId = rdr.GetGuid(PropertyGroupId);

                    if (!rdr.IsDBNull(Order)) relation.Index = rdr.GetInt32(Order);

                    if (!rdr.IsDBNull(Created)) relation.Created = rdr.GetDateTime(Created);

                    if (!rdr.IsDBNull(CreatedBy)) relation.CreatedBy = rdr.GetGuid(CreatedBy);

                    relation.IsNew = false;
                    relation.IsDirty = false;

                    values.Add(relation);

                }

                return values;

            }

        }

        public bool ViewPropertyGroupRelationList_DeleteByViewId(Guid ViewId)
        {

            List<SqlParameter> paramList = null;
            paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", ViewId));

            return base.ExecuteSql(StoredProcs.spViewPropertyGroupRelationList_DeleteByViewId, paramList);

        }

        public List<ViewPropertyGroupRelation> ViewPropertyGroupRelationList_GetByPropertyGroupId(Guid propertygroupid)
        {

            List<ViewPropertyGroupRelation> values = new List<ViewPropertyGroupRelation>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", propertygroupid));

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.spViewPropertyGroupRelationList_GetByPropertyGroupId, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return values; }

                //get the index of each property we are going to load
                int Id = rdr.GetOrdinal("Id");
                int ViewId = rdr.GetOrdinal("ViewId");
                int PropertyGroupId = rdr.GetOrdinal("PropertyGroupId");
                int Order = rdr.GetOrdinal("Order");
                int Created = rdr.GetOrdinal("Created");
                int CreatedBy = rdr.GetOrdinal("CreatedBy");
                int Deleted = rdr.GetOrdinal("Deleted");
                int DeletedBy = rdr.GetOrdinal("DeletedBy");

                while (rdr.Read())
                {

                    ViewPropertyGroupRelation relation = new ViewPropertyGroupRelation();

                    if (!rdr.IsDBNull(Id)) relation.Id = rdr.GetGuid(Id);

                    if (!rdr.IsDBNull(ViewId)) relation.ViewId = rdr.GetGuid(ViewId);

                    if (!rdr.IsDBNull(PropertyGroupId)) relation.PropertyGroupId = rdr.GetGuid(PropertyGroupId);

                    if (!rdr.IsDBNull(Order)) relation.Index = rdr.GetInt32(Order);

                    if (!rdr.IsDBNull(Created)) relation.Created = rdr.GetDateTime(Created);

                    if (!rdr.IsDBNull(CreatedBy)) relation.CreatedBy = rdr.GetGuid(CreatedBy);

                    if (!rdr.IsDBNull(Deleted)) relation.Deleted = rdr.GetDateTime(Deleted);

                    if (!rdr.IsDBNull(DeletedBy)) relation.DeletedBy = rdr.GetGuid(DeletedBy);

                    relation.IsNew = false;
                    relation.IsDirty = false;

                    values.Add(relation);

                }

                return values;

            }

        }

        public bool ViewPropertyGroupRelationList_DeleteByPropertyGroupId(Guid propertyGroupId, Guid userId)
        {

            List<SqlParameter> paramList = null;
            paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", propertyGroupId));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(StoredProcs.spViewPropertyGroupRelationList_DeleteByPropertyGroupId, paramList);

        }

    }

}