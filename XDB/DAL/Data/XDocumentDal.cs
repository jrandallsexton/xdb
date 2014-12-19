
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Constants;
using XDB.DataObjects;

namespace XDB.DAL
{

    internal class XDocumentDal : XSqlDal
    {

        public XDocumentDal() { }

        public XDocumentDal(string connString) { this.ConnectionString = connString; }

        internal XDocument Get(Guid id, bool omitData)
        {
            List<SqlParameter> paramList = new List<SqlParameter> { new SqlParameter("@Id", id) };

            using (SqlDataReader rdr = base.OpenDataReader(StoredProcs.Document_Get, paramList))
            {

                if ((rdr == null) || (!rdr.HasRows)) return null;

                if (!rdr.Read()) return null;

                XDocument document = new XDocument();

                if (!rdr.IsDBNull(rdr.GetOrdinal("DocumentTypeId"))) document.DocumentType = EnumerationOps.EDocumentTypeFromValue(rdr.GetInt32(rdr.GetOrdinal("DocumentTypeId")));

                if (!rdr.IsDBNull(1)) { document.Name = rdr.GetString(1); }

                if (!rdr.IsDBNull(rdr.GetOrdinal("Title"))) document.Title = (string)rdr[rdr.GetOrdinal("Title")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("Description"))) document.Description = (string)rdr[rdr.GetOrdinal("Description")];

                if (!omitData)
                {
                    if (!rdr.IsDBNull(rdr.GetOrdinal("Data"))) document.Data = (Byte[])rdr[rdr.GetOrdinal("Data")];
                }

                if (!rdr.IsDBNull(rdr.GetOrdinal("Created"))) document.Created = (DateTime)rdr[rdr.GetOrdinal("Created")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("Approved"))) document.Approved = (DateTime)rdr[rdr.GetOrdinal("Approved")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("CreatedBy"))) document.CreatedBy = (Guid)rdr[rdr.GetOrdinal("CreatedBy")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("ApprovedBy"))) document.ApprovedBy = (Guid)rdr[rdr.GetOrdinal("ApprovedBy")];

                if (!rdr.IsDBNull(rdr.GetOrdinal("DeletedBy"))) document.DeletedBy = (Guid)rdr[rdr.GetOrdinal("DeletedBy")];

                document.Id = id;
                document.IsNew = false;
                document.IsDirty = false;

                return document;
            }

        }

        internal bool Save(XDocument document)
        {

            if (!document.IsDirty) return true;

            List<SqlParameter> paramList = new List<SqlParameter>();

            paramList.Add(new SqlParameter("@Id", document.Id));
            paramList.Add(new SqlParameter("@DocumentTypeId", document.DocumentType.GetHashCode()));
            paramList.Add(new SqlParameter("@Name", document.Name));
            paramList.Add(new SqlParameter("@Title", document.Title));

            paramList.Add(string.IsNullOrEmpty(document.Description)
                              ? new SqlParameter("@Description", null)
                              : new SqlParameter("@Description", document.Description));

            paramList.Add(new SqlParameter("@Data", document.Data));
            paramList.Add(new SqlParameter("@Created", document.Created));
            paramList.Add(new SqlParameter("@Approved", document.Approved));
            paramList.Add(new SqlParameter("@CreatedBy", document.CreatedBy));
            paramList.Add(new SqlParameter("@ApprovedBy", document.ApprovedBy));
            paramList.Add(new SqlParameter("@DeletedBy", document.DeletedBy));

            if (!base.ExecuteSql(StoredProcs.Document_Save, paramList)) return false;

            //if ((document.TagMembers != null) && (document.TagMembers.Count > 0))
            //{
            //    new DocumentTagRelationDal().DocumentTagRelationList_Save(document.TagMembers);
            //}

            document.IsNew = false;
            document.IsDirty = false;

            return true;
        }

        internal bool Delete(Guid documentId, Guid userId)
        {
            List<SqlParameter> paramList = new List<SqlParameter>
                    {
                        new SqlParameter("@Id", documentId),
                        new SqlParameter("@DeletedBy", userId)
                    };

            return base.ExecuteSql(StoredProcs.Document_Delete, paramList);
        }

        private bool DocumentIdIsValid(Guid id)
        {
            const string sql = "SELECT COUNT(*) FROM [Documents] WITH (NoLock) WHERE ([Id] = @Id) AND ([Deleted] IS NULL)";
            return (base.ExecuteScalarInLine(sql, new List<SqlParameter> { new SqlParameter("@Id", id) }) == 1);
        }

    }

}