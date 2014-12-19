
using System;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;

using XDB.Common;
using XDB.Common.SqlDal;
using XDB.Common.Constants;
using XDB.Common.Interfaces;
using XDB.Common.Enumerations;

using XDB.Models;

namespace XDB.Repositories
{

    public class XObjectRelationRepository : XSqlDal
    {

        public XObjectRelationRepository() { }

        #region Private Members

        private const string spAssetAssetRelation_Get = "spr_AssetAssetRelation_Get";
        private const string spAssetAssetRelation_Save = "spr_AssetAssetRelation_Save";
        private const string spAssetAssetRelation_Delete = "spr_AssetAssetRelation_Delete";

        //Collection SPs
        private const string spAssetAssetRelationList_Get = "spr_AssetAssetRelationList_Get";
        private const string spAssetAssetRelationList_Save = "spr_AssetAssetRelationList_Save";
        private const string spAssetAssetRelationList_Delete = "spr_AssetAssetRelationList_Delete";

        //Collection by FK SPs
        private const string spAssetAssetRelationList_GetByFromAssetId = "spr_AssetAssetRelationList_GetByFromAssetId";
        private const string spAssetAssetRelationList_DeleteByFromAssetId = "spr_AssetAssetRelationList_DeleteByFromAssetId";

        //Collection by FK SPs
        private const string spAssetAssetRelationList_GetByToAssetId = "spr_AssetAssetRelationList_GetByToAssetId";
        private const string spAssetAssetRelationList_DeleteByToAssetId = "spr_AssetAssetRelationList_DeleteByToAssetId";

        //Collection by FK SPs
        private const string spAssetAssetRelationList_GetByAssetRelationTypeId = "spr_AssetAssetRelationList_GetByAssetRelationTypeId";
        private const string spAssetAssetRelationList_DeleteByAssetRelationTypeId = "spr_AssetAssetRelationList_DeleteByAssetRelationTypeId";

        #endregion

        public bool AssetAssetRelationExists(Guid fromId, Guid toId, int relationTypeId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("SELECT COUNT(*) FROM [AssetRelations] WITH (NoLock)");
            sql.AppendLine("WHERE [FromAssetId] = @From");
            sql.AppendLine("AND [ToAssetId] = @To");
            sql.AppendLine("AND [AssetRelationTypeId] = @TypeId");
            sql.AppendLine("AND [Deleted] IS NULL");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@From", fromId));
            paramList.Add(new SqlParameter("@To", toId));
            paramList.Add(new SqlParameter("@TypeId", relationTypeId));

            return (base.ExecuteScalarInLine(sql.ToString(), paramList) == 1);
        }

        public bool RemoveExistingParentRelations(Guid fromId, int relationTypeId)
        {
            StringBuilder sql = new StringBuilder();
            sql.AppendLine("UPDATE [AssetRelations]");
            sql.AppendLine("SET [Deleted] = GetDate()");
            sql.AppendLine("WHERE [FromAssetId] = @From");
            sql.AppendLine("AND [AssetRelationTypeId] = @TypeId");
            sql.AppendLine("AND [Deleted] IS NULL");

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@From", fromId));
            paramList.Add(new SqlParameter("@TypeId", relationTypeId));

            return base.ExecuteInLineSql(sql.ToString(), paramList);
        }

        public XObjectRelation Get(Guid Id)
        {

            XObjectRelation relation = null;

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", Id));

            using (SqlDataReader rdr = base.OpenDataReader(spAssetAssetRelation_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) { return null; }

                relation = new XObjectRelation();

                rdr.Read();

                if (!rdr.IsDBNull(rdr.GetOrdinal("FromAssetId"))) relation.FromAssetId = rdr.GetGuid(rdr.GetOrdinal("FromAssetId"));

                if (!rdr.IsDBNull(rdr.GetOrdinal("ToAssetId"))) relation.ToAssetId = rdr.GetGuid(rdr.GetOrdinal("ToAssetId"));

                if (!rdr.IsDBNull(rdr.GetOrdinal("AssetRelationTypeId"))) relation.AssetRelationType = EnumerationOps.EAssetRelationTypeFromValue((Int16)rdr.GetByte(rdr.GetOrdinal("AssetRelationTypeId")));

                if (!rdr.IsDBNull(rdr.GetOrdinal("Created"))) relation.Created = (DateTime)rdr[rdr.GetOrdinal("Created")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("Approved"))) relation.Approved = (DateTime)rdr[rdr.GetOrdinal("Approved")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("CreatedBy"))) relation.CreatedBy = rdr.GetGuid(rdr.GetOrdinal("CreatedBy"));

                if (!rdr.IsDBNull(rdr.GetOrdinal("ApprovedBy"))) relation.ApprovedBy = rdr.GetGuid(rdr.GetOrdinal("ApprovedBy"));

                if (!rdr.IsDBNull(rdr.GetOrdinal("DeletedBy"))) relation.DeletedBy = rdr.GetGuid(rdr.GetOrdinal("DeletedBy"));

                relation.Id = Id;
                relation.IsNew = false;
                relation.IsDirty = false;

            }

            return relation;

        }

        public bool Save(XObjectRelation relation)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", relation.Id));
            paramList.Add(new SqlParameter("@FromAssetId", relation.FromAssetId));
            paramList.Add(new SqlParameter("@ToAssetId", relation.ToAssetId));
            paramList.Add(new SqlParameter("@AssetRelationTypeId", Byte.Parse(relation.AssetRelationType.GetHashCode().ToString())));
            //SqlParameter p = new SqlParameter("@AssetRelationTypeId", SqlDbType.TinyInt);
            //p.Value = Byte.Parse(assetassetrelation.AssetRelationType.GetHashCode().ToString());
            //paramList.Add(p);
            paramList.Add(new SqlParameter("@Created", relation.Created));
            paramList.Add(new SqlParameter("@Approved", relation.Approved));
            paramList.Add(new SqlParameter("@CreatedBy", relation.CreatedBy));
            paramList.Add(new SqlParameter("@ApprovedBy", relation.ApprovedBy));
            paramList.Add(new SqlParameter("@DeletedBy", relation.DeletedBy));

            return base.ExecuteSql(spAssetAssetRelation_Save, paramList);

        }

        public bool Delete(Guid Id, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", Id));
            paramList.Add(new SqlParameter("@DeletedBy", userId));

            return base.ExecuteSql(spAssetAssetRelation_Delete, paramList);

        }

        public bool Save(List<XObjectRelation> relations)
        {

            foreach (XObjectRelation relation in relations)
            {
                if (relation.IsDirty)
                {
                    this.Save(relation);
                    relation.IsNew = false;
                    relation.IsDirty = false;
                }

            }


            return true;

        }

        public List<XObjectRelation> AssetRelationList_GetByFromAssetId(Guid fromassetid)
        {
            List<XObjectRelation> list = new List<XObjectRelation>();

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", fromassetid));

            using (SqlDataReader drdSql = base.OpenDataReader(spAssetAssetRelationList_GetByFromAssetId, paramList))
            {
                if ((drdSql != null) && (drdSql.HasRows))
                {

                    //get the index of each property we are going to load
                    int Id = drdSql.GetOrdinal("Id");
                    int FromAssetId = drdSql.GetOrdinal("FromAssetId");
                    int ToAssetId = drdSql.GetOrdinal("ToAssetId");
                    int AssetRelationTypeId = drdSql.GetOrdinal("AssetRelationTypeId");
                    int Created = drdSql.GetOrdinal("Created");
                    int Approved = drdSql.GetOrdinal("Approved");
                    int CreatedBy = drdSql.GetOrdinal("CreatedBy");
                    int ApprovedBy = drdSql.GetOrdinal("ApprovedBy");
                    int DeletedBy = drdSql.GetOrdinal("DeletedBy");

                    while (drdSql.Read())
                    {

                        XObjectRelation relation = new XObjectRelation();

                        if (!drdSql.IsDBNull(Id)) relation.Id = drdSql.GetGuid(Id);

                        if (!drdSql.IsDBNull(FromAssetId)) relation.FromAssetId = drdSql.GetGuid(FromAssetId);

                        if (!drdSql.IsDBNull(ToAssetId)) relation.ToAssetId = drdSql.GetGuid(ToAssetId);

                        if (!drdSql.IsDBNull(AssetRelationTypeId)) relation.AssetRelationType = EnumerationOps.EAssetRelationTypeFromValue((Int16)drdSql.GetByte(AssetRelationTypeId));

                        if (!drdSql.IsDBNull(Created)) relation.Created = (DateTime)drdSql.GetValue(Created);

                        if (!drdSql.IsDBNull(Approved)) relation.Approved = (DateTime)drdSql.GetValue(Approved);

                        if (!drdSql.IsDBNull(CreatedBy)) relation.CreatedBy = drdSql.GetGuid(CreatedBy);

                        if (!drdSql.IsDBNull(ApprovedBy)) relation.ApprovedBy = drdSql.GetGuid(ApprovedBy);

                        if (!drdSql.IsDBNull(DeletedBy)) relation.DeletedBy = drdSql.GetGuid(DeletedBy);

                        relation.IsNew = false;
                        relation.IsDirty = false;

                        list.Add(relation);

                    }

                }

            }

            return list;

        }

        public bool AssetAssetRelationList_DeleteByFromAssetId(Guid FromAssetId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>();
            paramList.Add(new SqlParameter("@Id", FromAssetId));

            return base.ExecuteSql(spAssetAssetRelationList_DeleteByFromAssetId, paramList);

        }

        //internal AssetAssetRelationList AssetAssetRelationList_GetByToAssetId(Guid toassetid)
        //{

        //    List<SqlParameter> paramList = null;
        //    AssetAssetRelationList list = new AssetAssetRelationList();
        //    AssetAssetRelation _assetassetrelation = null;

        //    try
        //    {

        //        paramList = new List<SqlParameter>();
        //        paramList.Add(new SqlParameter("@Id", toassetid));

        //        using (SqlDataReader drdSql = base.OpenDataReader(spAssetAssetRelationList_GetByToAssetId, paramList))
        //        {
        //            if ((drdSql != null) && (drdSql.HasRows))
        //            {

        //                //get the index of each property we are going to load
        //                int Id = drdSql.GetOrdinal("Id");
        //                int FromAssetId = drdSql.GetOrdinal("FromAssetId");
        //                int ToAssetId = drdSql.GetOrdinal("ToAssetId");
        //                int AssetRelationTypeId = drdSql.GetOrdinal("AssetRelationTypeId");
        //                int Created = drdSql.GetOrdinal("Created");
        //                int Approved = drdSql.GetOrdinal("Approved");
        //                int CreatedBy = drdSql.GetOrdinal("CreatedBy");
        //                int ApprovedBy = drdSql.GetOrdinal("ApprovedBy");
        //                int DeletedBy = drdSql.GetOrdinal("DeletedBy");

        //                while (drdSql.Read())
        //                {

        //                    _assetassetrelation = new AssetAssetRelation();

        //                    if (!drdSql.IsDBNull(Id)) _assetassetrelation.Id = drdSql.GetGuid(Id);

        //                    if (!drdSql.IsDBNull(FromAssetId)) _assetassetrelation.FromAssetId = drdSql.GetGuid(FromAssetId);

        //                    if (!drdSql.IsDBNull(ToAssetId)) _assetassetrelation.ToAssetId = drdSql.GetGuid(ToAssetId);

        //                    if (!drdSql.IsDBNull(AssetRelationTypeId)) _assetassetrelation.AssetRelationType = EnumerationOps.EAssetRelationTypeFromValue((Int16)drdSql.GetByte(AssetRelationTypeId));

        //                    if (!drdSql.IsDBNull(Created)) _assetassetrelation.Created = (DateTime)drdSql.GetValue(Created);

        //                    if (!drdSql.IsDBNull(Approved)) _assetassetrelation.Approved = (DateTime)drdSql.GetValue(Approved);

        //                    if (!drdSql.IsDBNull(CreatedBy)) _assetassetrelation.CreatedBy = drdSql.GetGuid(CreatedBy);

        //                    if (!drdSql.IsDBNull(ApprovedBy)) _assetassetrelation.ApprovedBy = drdSql.GetGuid(ApprovedBy);

        //                    if (!drdSql.IsDBNull(DeletedBy)) _assetassetrelation.DeletedBy = drdSql.GetGuid(DeletedBy);

        //                    _assetassetrelation.IsNew = false;

        //                    _assetassetrelation.IsDirty = false;

        //                    list.Add(_assetassetrelation);

        //                }

        //            }
        //        }

        //        list.IsNew = false;
        //        list.IsDirty = false;

        //        return list;

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        throw;
        //    }

        //}

        //internal bool AssetAssetRelationList_DeleteByToAssetId(Guid ToAssetId)
        //{

        //    List<SqlParameter> paramList = null;

        //    try
        //    {

        //        paramList = new List<SqlParameter>();
        //        paramList.Add(new SqlParameter("@Id", ToAssetId));

        //        return base.ExecuteSql(spAssetAssetRelationList_DeleteByToAssetId, paramList);

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        throw;
        //    }

        //}

        //internal AssetAssetRelationList AssetAssetRelationList_GetByAssetRelationTypeId(int assetrelationtypeid)
        //{

        //    List<SqlParameter> paramList = null;
        //    AssetAssetRelationList list = new AssetAssetRelationList();
        //    AssetAssetRelation _assetassetrelation = null;

        //    try
        //    {

        //        paramList = new List<SqlParameter>();
        //        paramList.Add(new SqlParameter("@Id", assetrelationtypeid));

        //        using (SqlDataReader drdSql = base.OpenDataReader(spAssetAssetRelationList_GetByAssetRelationTypeId, paramList))
        //        {

        //            if ((drdSql != null) && (drdSql.HasRows))
        //            {

        //                //get the index of each property we are going to load
        //                int Id = drdSql.GetOrdinal("Id");
        //                int FromAssetId = drdSql.GetOrdinal("FromAssetId");
        //                int ToAssetId = drdSql.GetOrdinal("ToAssetId");
        //                int AssetRelationTypeId = drdSql.GetOrdinal("AssetRelationTypeId");
        //                int Created = drdSql.GetOrdinal("Created");
        //                int Approved = drdSql.GetOrdinal("Approved");
        //                int CreatedBy = drdSql.GetOrdinal("CreatedBy");
        //                int ApprovedBy = drdSql.GetOrdinal("ApprovedBy");
        //                int DeletedBy = drdSql.GetOrdinal("DeletedBy");

        //                while (drdSql.Read())
        //                {

        //                    _assetassetrelation = new AssetAssetRelation();

        //                    if (!drdSql.IsDBNull(Id)) _assetassetrelation.Id = drdSql.GetGuid(Id);

        //                    if (!drdSql.IsDBNull(FromAssetId)) _assetassetrelation.FromAssetId = drdSql.GetGuid(FromAssetId);

        //                    if (!drdSql.IsDBNull(ToAssetId)) _assetassetrelation.ToAssetId = drdSql.GetGuid(ToAssetId);

        //                    if (!drdSql.IsDBNull(AssetRelationTypeId)) _assetassetrelation.AssetRelationType = EnumerationOps.EAssetRelationTypeFromValue((Int16)drdSql.GetByte(AssetRelationTypeId));

        //                    if (!drdSql.IsDBNull(Created)) _assetassetrelation.Created = (DateTime)drdSql.GetValue(Created);

        //                    if (!drdSql.IsDBNull(Approved)) _assetassetrelation.Approved = (DateTime)drdSql.GetValue(Approved);

        //                    if (!drdSql.IsDBNull(CreatedBy)) _assetassetrelation.CreatedBy = drdSql.GetGuid(CreatedBy);

        //                    if (!drdSql.IsDBNull(ApprovedBy)) _assetassetrelation.ApprovedBy = drdSql.GetGuid(ApprovedBy);

        //                    if (!drdSql.IsDBNull(DeletedBy)) _assetassetrelation.DeletedBy = drdSql.GetGuid(DeletedBy);

        //                    _assetassetrelation.IsNew = false;

        //                    _assetassetrelation.IsDirty = false;

        //                    list.Add(_assetassetrelation);

        //                }

        //            }
        //        }

        //        list.IsNew = false;
        //        list.IsDirty = false;

        //        return list;

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        throw;
        //    }

        //}

        //internal bool AssetAssetRelationList_DeleteByAssetRelationTypeId(Guid AssetRelationTypeId)
        //{

        //    List<SqlParameter> paramList = null;

        //    try
        //    {

        //        paramList = new List<SqlParameter>();
        //        paramList.Add(new SqlParameter("@Id", AssetRelationTypeId));

        //        return base.ExecuteSql(spAssetAssetRelationList_DeleteByAssetRelationTypeId, paramList);

        //    }
        //    catch (Exception ex)
        //    {
        //        ExceptionManager.Publish(ex);
        //        throw;
        //    }

        //}

    }

}