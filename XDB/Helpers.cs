
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB
{

    public class Helpers
    {

        public static string ListOfGuidToCommaDelimString(List<Guid> values)
        {
            if ((values == null) || (values.Count == 0)) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            int valCount = values.Count;
            for (int i = 0; i < valCount; i++)
            {
                if (i == (valCount - 1))
                {
                    sb.AppendFormat("{0}", values[i]);
                }
                else
                {
                    sb.AppendFormat("{0},", values[i]);
                }
            }
            return sb.ToString();
        }

        public static string ListOfGuidToCommaDelimQuotedString(List<Guid> values)
        {
            if ((values == null) || (values.Count == 0)) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            int valCount = values.Count;
            for (int i = 0; i < valCount; i++)
            {
                if (i == (valCount - 1))
                {
                    sb.AppendFormat("'{0}'", values[i]);
                }
                else
                {
                    sb.AppendFormat("'{0}',", values[i]);
                }
            }
            return sb.ToString();
        }

        public static string ListOfStringToCommaDelimString(List<string> values)
        {
            if ((values == null) || (values.Count == 0)) { return string.Empty; }
            StringBuilder sb = new StringBuilder();
            int valCount = values.Count;
            for (int i = 0; i < valCount; i++)
            {
                if (i == (valCount - 1))
                {
                    sb.AppendFormat("{0}", values[i]);
                }
                else
                {
                    sb.AppendFormat("{0},", values[i]);
                }
            }
            return sb.ToString();
        }

        public static Dictionary<Guid, string> Sort(Dictionary<Guid, string> values)
        {
            SortedDictionary<string, Guid> sort = new SortedDictionary<string, Guid>();

            foreach (KeyValuePair<Guid, string> kvp in values)
            {
                if (!sort.ContainsKey(kvp.Value)) { sort.Add(kvp.Value, kvp.Key); }
            }

            Dictionary<Guid, string> returnValues = new Dictionary<Guid, string>();

            foreach (KeyValuePair<string, Guid> kvp in sort) { returnValues.Add(kvp.Value, kvp.Key); }

            return returnValues;
        }

        public static string ProperCase(string stringInput)
        {

            if (string.IsNullOrEmpty(stringInput)) { return stringInput; }

            StringBuilder sb = new System.Text.StringBuilder();
            bool fEmptyBefore = true;
            foreach (char ch in stringInput)
            {
                char chThis = ch;
                if (Char.IsWhiteSpace(chThis))
                    fEmptyBefore = true;
                else
                {
                    if (Char.IsLetter(chThis) && fEmptyBefore)
                        chThis = Char.ToUpper(chThis);
                    else
                        chThis = Char.ToLower(chThis);
                    fEmptyBefore = false;
                }
                sb.Append(chThis);
            }
            return sb.ToString();
        }

        internal static string BuildAddressName(string addr1, string addr2, string city, string county, string state, string country, string postal)
        {
            // TODO: Finish this method
            string returnValue = string.Empty;

            if (!string.IsNullOrEmpty(country)) { returnValue = country; }

            if (!string.IsNullOrEmpty(state))
            {

                if (!string.IsNullOrEmpty(returnValue))
                {
                    returnValue = string.Format("{0}, {1}", state, returnValue);
                }
                else
                {
                    returnValue = state;
                }
            }

            return returnValue;
        }

        public static void Log(string category, string message)
        {
            return;
            //XSqlDal dal = new DataAccess.XSqlDal();
            //string sql = string.Format("INSERT INTO [Log].[DebugLogs] ([Category], [Message]) VALUES ('{0}', '{1}')", category, message);
            //dal.ExecuteInLineSql(sql, new List<System.Data.SqlClient.SqlParameter>());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static string IntToLetter(int index)
        {
            // adopted from http://stackoverflow.com/questions/10373561/convert-a-number-to-a-letter-in-c-sharp-for-use-in-microsoft-excel
            const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var value = "";

            if (index >= letters.Length)
                value += letters[index / letters.Length - 1];

            value += letters[index % letters.Length];

            return value;
        }

    }

}