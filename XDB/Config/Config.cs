
using System.Configuration;

using XDB.Enumerations;

namespace XDB
{

    public static class Config
    {

        public static string DbSchemaPrefix
        {
            get { return "xdb"; }
        }

        public static string DbConnString
        {
            get { return GetAppSetting("DbConnString"); }
        }

        public static string DbConnStringByInstance(Enumerations.EApplicationInstance instance)
        {
            return ConfigurationManager.AppSettings["DbConnString" + instance.ToString()];
        }

        public static string Notification_FromDisplay
        {
            get
            {
                return ConfigurationManager.AppSettings["Notification_FromDisplay"];
            }
        }

        public static string Notification_FromEmail
        {
            get
            {
                return ConfigurationManager.AppSettings["Notification_FromEmail"];
            }
        }

        public static string NotificationLogoUrl
        {
            get { return GetAppSetting("notificationLogoUrl"); }
        }

        public static bool NotificationsEnabled
        {
            get { return bool.Parse(GetAppSetting("notificationsEnabled")); }
        }

        public static string SmtpHostName
        {
            get
            {
                return ConfigurationManager.AppSettings["SmtpHostName"];
            }
        }

        public static string ViewImageUrl
        {
            get { return GetAppSetting("viewImageUrl"); }
        }

        public static EApplicationInstance ApplicationInstance
        {
            get
            {
                string applicationInstance = ConfigurationManager.AppSettings["applicationInstance"];

                switch (applicationInstance.ToUpper())
                {
                    case "LOCAL": return EApplicationInstance.LOCAL;
                    case "DEV": return EApplicationInstance.DEV;
                    case "QA": return EApplicationInstance.QA;
                    case "STG": return EApplicationInstance.STG;
                    case "PROD": return EApplicationInstance.PROD;
                    default: return EApplicationInstance.LOCAL;
                }
            }
        }

        public static string GetAppSetting(string key)
        {
            return ConfigurationManager.AppSettings[key + ApplicationInstance.ToString()];
        }

        public static int ReportCacheDuration
        {
            get
            {
                return 60;
                //XSqlDal dal = new XSqlDal();
                //string sql = "SELECT [ReportCache] FROM [SysOptions]";
                //return dal.ExecuteScalar(sql);
            }
        }

    }

}