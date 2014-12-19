
using System;

namespace XDB.Common.Interfaces
{
    public interface IXDate
    {
        int Order { get; set; }
        string SqlDatePart { get; set; }
        string Value { get; set; }
    }

}