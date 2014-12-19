
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using XDB.Common.Enumerations;

namespace XDB.Common
{

    /// <summary>
    /// Helper class for obtaining enumerations from their associated hashcodes
    /// </summary>
    public class EnumerationOps
    {
        /// <summary>
        /// Gets the correct EDataType based on the specified value (hashcode)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static EDataType EDataTypeFromValue(int value)
        {
            foreach (EDataType item in EDataType.GetValues(typeof(EDataType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return EDataType.Undefined;
        }

        public static ESystemType ESystemTypeFromValue(int value)
        {
            foreach (ESystemType item in ESystemType.GetValues(typeof(ESystemType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return ESystemType.NotApplicable;
        }

        /// <summary>
        /// Gets the correct EImageType based on the specified value (hashcode)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static EImageType EImageTypeFromValue(int value)
        {
            foreach (EImageType item in EImageType.GetValues(typeof(EImageType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return EImageType.Undefined;
        }

        /// <summary>
        /// Gets the correct EDocumentType based on the specified value (hashcode)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static EDocumentType EDocumentTypeFromValue(int value)
        {
            foreach (EDocumentType item in EDocumentType.GetValues(typeof(EDocumentType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return EDocumentType.Undefined;
        }

        public static EAssetRequestType EAssetRequestTypeFromValue(int value)
        {
            foreach (EAssetRequestType item in EAssetRequestType.GetValues(typeof(EAssetRequestType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            throw new Exception("Invalid value for EAssetRequestType");
        }

        public static EObjectRelationType EAssetRelationTypeFromValue(System.Int16 value)
        {
            foreach (EObjectRelationType item in EObjectRelationType.GetValues(typeof(EObjectRelationType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return EObjectRelationType.Undefined;
        }

        public static ECustomReportFieldType ECustomReportFieldTypeFromValue(int value)
        {
            foreach (ECustomReportFieldType item in ECustomReportFieldType.GetValues(typeof(ECustomReportFieldType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return ECustomReportFieldType.NotApplicable;
        }

        public static ETimeElapsedType ETimeElapsedTypeFromValue(int value)
        {
            foreach (ETimeElapsedType item in ETimeElapsedType.GetValues(typeof(ETimeElapsedType)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return ETimeElapsedType.NotApplicable;
        }

        public static ETimeElapsedFormat ETimeElapsedFormatFromValue(int value)
        {
            foreach (ETimeElapsedFormat item in ETimeElapsedFormat.GetValues(typeof(ETimeElapsedFormat)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            return ETimeElapsedFormat.NotApplicable;
        }

        public static EFilterOperator EFilterOperatorFromValue(int value)
        {
            foreach (EFilterOperator item in EFilterOperator.GetValues(typeof(EFilterOperator)))
            {
                if (item.GetHashCode() == value) { return item; }
            }
            throw new Exception("Invalid value for EFilterOperator");
        }

        public static string EFilterOperatorToSql(EFilterOperator op)
        {
            switch (op)
            {
                case EFilterOperator.EqualTo:
                    return "=";
                case EFilterOperator.GreaterThan:
                    return ">";
                case EFilterOperator.GreaterThanEqualTo:
                    return ">=";
                case EFilterOperator.LessThan:
                    return "<";
                case EFilterOperator.LessThanEqualTo:
                    return "<=";
                case EFilterOperator.Like:
                    return "LIKE";
                case EFilterOperator.NotEqual:
                    return "!=";
            }
            return string.Empty;
        }

    }

}