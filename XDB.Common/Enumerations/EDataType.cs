using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XDB.Common.Enumerations
{

    /// <summary>
    /// Data Type; used for Property objects
    /// </summary>
    public enum EDataType
    {

        /// <summary>
        /// Used for showing an error in helper operations; this is a non-existant type
        /// </summary>
        Undefined = -1,

        /// <summary>
        /// Corresponds to a date-only data type
        /// </summary>
        Date = 0,

        /// <summary>
        /// Corresponds to a date and time data type
        /// </summary>
        DateTime = 1,

        /// <summary>
        /// Property's data type is a document
        /// </summary>
        Document = 2,

        /// <summary>
        /// Corresponds to a floating point value data type
        /// </summary>
        Float = 3,

        /// <summary>
        /// Property's data type is an image (of some type)
        /// </summary>
        Image = 4,

        /// <summary>
        /// Corresponds to an integer data type
        /// </summary>
        Int = 5,

        /// <summary>
        /// Corresponds to a string data type
        /// </summary>
        String = 6,

        /// <summary>
        /// Property's data type is a URL (also a string), but provided as an intrinsic data type for
        /// data validation purposes.  If we know the user is submitting a URL, we can validate it.
        /// </summary>
        URL = 7,

        /// <summary>
        /// Property's data type is a user (known as a member in this framework)
        /// </summary>
        User = 8,

        Asset = 9,

        Memo = 10,

        Relation_ParentChild = 11,

        Relation_ChildParent = 12,

        Dependency = 13,

        Currency = 14,

        Relation_Other = 15,

        System_AssetType = 16,

        System_Description = 17,

        //System_InstanceOf = 18,

        //System_AssetName = 19,

        IPv4 = 20,

        IPv6 = 21,

        System_InstanceOfDesc = 22,

        PickList = 23,

        HTML = 24,

        Geo = 25,

        Boolean = 26

        // just a role id
        //Role = 23,

        // someone in a specified role
        //RoleUser = 24

        //Supercedes_Sibling = 16

    }

}