
using System;
using System.Text;
using System.Collections.Generic;
using System.Data.SqlClient;

using XDB.Constants;
using XDB.DataObjects;
using XDB.Enumerations;

namespace XDB.DAL
{

    internal class XImageDal : XSqlDal
    {

        public XImageDal() { }

        public XImageDal(string connString) { this.ConnectionString = connString; }

        internal XImage Get(Guid id, bool omitData)
        {

            XImage image = null;

            using (
                SqlDataReader drdSql = base.OpenDataReader(StoredProcs.Image_Get,
                                                           new List<SqlParameter> { new SqlParameter("@Id", id) }))
            {

                if ((drdSql != null) && (drdSql.HasRows))
                {

                    image = new XImage();

                    drdSql.Read();

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("ImageTypeId")))
                        image.ImageType =
                            EnumerationOps.EImageTypeFromValue((Int16)drdSql.GetByte(drdSql.GetOrdinal("ImageTypeId")));

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("Order")))
                        image.Order = (Int16)drdSql.GetByte(drdSql.GetOrdinal("Order"));

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("Name")))
                        image.Name = drdSql.GetString(drdSql.GetOrdinal("Name"));

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("Description")))
                        image.Description = (string)drdSql[drdSql.GetOrdinal("Description")];

                    if (!omitData)
                    {
                        if (!drdSql.IsDBNull(drdSql.GetOrdinal("ImageData")))
                            image.ImageData = (Byte[])drdSql[drdSql.GetOrdinal("ImageData")];
                    }

                    //if (!drdSql.IsDBNull(drdSql.GetOrdinal("Width")))
                    //{
                    //    image.Width = drdSql.GetInt32(drdSql.GetOrdinal("Width"));
                    //}

                    //if (!drdSql.IsDBNull(drdSql.GetOrdinal("Height")))
                    //{
                    //    image.Height = drdSql.GetInt32(drdSql.GetOrdinal("Height"));
                    //}

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("Created")))
                        image.Created = (DateTime)drdSql[drdSql.GetOrdinal("Created")];

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("Approved")))
                        image.Approved = (DateTime)drdSql[drdSql.GetOrdinal("Approved")];

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("CreatedBy")))
                        image.CreatedBy = (Guid)drdSql[drdSql.GetOrdinal("CreatedBy")];

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("ApprovedBy")))
                        image.ApprovedBy = (Guid)drdSql[drdSql.GetOrdinal("ApprovedBy")];

                    if (!drdSql.IsDBNull(drdSql.GetOrdinal("DeletedBy")))
                        image.DeletedBy = (Guid)drdSql[drdSql.GetOrdinal("DeletedBy")];

                    image.Id = id;

                    image.IsNew = false;
                    image.IsDirty = false;

                }

            }

            return image;

        }

        internal bool Save(XImage image)
        {

            if (!image.IsDirty) { return true; }

            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", image.Id));
            paramList.Add(new SqlParameter("@ImageTypeId", image.ImageType.GetHashCode()));
            paramList.Add(new SqlParameter("@Order", image.Order));
            paramList.Add(new SqlParameter("@Name", image.Name));

            paramList.Add(string.IsNullOrEmpty(image.Description)
                              ? new SqlParameter("@Description", string.Empty)
                              : new SqlParameter("@Description", image.Description));

            paramList.Add(new SqlParameter("@IsSystem", image.IsSystem));
            paramList.Add(new SqlParameter("@ImageData", image.ImageData));
            //paramList.Add(new SqlParameter("@Width", image.Width));
            //paramList.Add(new SqlParameter("@Height", image.Height));
            paramList.Add(new SqlParameter("@Created", image.Created));
            paramList.Add(new SqlParameter("@Approved", image.Approved));
            paramList.Add(new SqlParameter("@CreatedBy", image.CreatedBy));
            paramList.Add(new SqlParameter("@ApprovedBy", image.ApprovedBy));
            paramList.Add(new SqlParameter("@DeletedBy", image.DeletedBy));

            if (base.ExecuteSql(StoredProcs.Image_Save, paramList))
            {

                //if ((image.TagMembers != null) && (image.TagMembers.Count > 0))
                //{
                //    new ImageTagRelationDal().ImageTagRelationList_Save(image.TagMembers);
                //}

                image.IsNew = false;
                image.IsDirty = false;

                return true;

            }

            return false;

        }

        internal bool Delete(Guid imageId, Guid userId)
        {

            List<SqlParameter> paramList = new List<SqlParameter>
                {
                    new SqlParameter("@Id", imageId),
                    new SqlParameter("@DeletedBy", userId)
                };

            return base.ExecuteSql(StoredProcs.Image_Delete, paramList);

        }

        internal bool IsValidId(Guid id)
        {
            const string sql = "SELECT COUNT(*) FROM [Images] WITH (NoLock) WHERE [Id] = @Id AND [Deleted] IS NULL";
            return (base.ExecuteScalarInLine(sql, new List<SqlParameter> { new SqlParameter("@Id", id) }) == 1);
        }

        internal Dictionary<Guid, string> ImageDictionary_GetSystem()
        {
            const string sql = "SELECT [Id], [Name] from [Images] WITH (NoLock) WHERE [IsSystem] = 1 ORDER BY [Name]";
            return base.GetDictionary(sql);
        }

        internal List<XImage> GetSystem()
        {

            List<XImage> values = new List<XImage>();

            const string sql = "SELECT [Id] from [Images] WITH (NoLock) WHERE [IsSystem] = 1 ORDER BY [Name]";

            using (var rdr = base.OpenDataReaderInLine(sql, new List<SqlParameter>()))
            {
                if ((rdr == null) || (!rdr.HasRows)) return values;
                while (rdr.Read())
                {
                    var id = rdr.GetGuid(0);
                    values.Add(this.Get(id, true));
                }
            }

            return values;
        }

    }
}