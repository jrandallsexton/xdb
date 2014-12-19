
namespace XDB.Common.Enumerations
{

    /// <summary>
    /// Used by TMCP.XDB.Core.Config.SystemFrameworkHelper for loading connection strings and such
    /// This enumeration is mapped to the 'applicationInstance' key within the ApplicationSettings of the app/web.config
    /// </summary>
    public enum EApplicationInstance
    {
        /// <summary>
        /// Specifies that the framework is running against a local server
        /// </summary>
        LOCAL = 0,

        /// <summary>
        /// Specifies that the framework is running against a development server
        /// </summary>
        DEV = 1,

        /// <summary>
        /// Specifies that the framework is running against a QA server
        /// </summary>
        QA = 2,

        /// <summary>
        /// Specifies that the framework is running against a staging server
        /// </summary>
        STG = 3,

        /// <summary>
        /// Specifies that the framework is running against a production server
        /// </summary>
        PROD = 4
    }

}