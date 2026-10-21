using System;

namespace Xdb.Core.Common;

public interface IDateTimeProvider
{
    DateTime UtcNow();
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow() => DateTime.UtcNow;
}
