using System;

namespace nhitomi.Database
{
    /// <summary>
    /// Indicates that a property should be excluded from the source field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DbSourceExcludeAttribute : Attribute { }

    /// <summary>
    /// Indicates that a property is a cached value derived from other properties, used solely for querying.
    /// </summary>
    public class DbCachedAttribute : DbSourceExcludeAttribute { }
}