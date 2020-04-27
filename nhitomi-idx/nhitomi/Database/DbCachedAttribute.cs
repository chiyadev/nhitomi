using System;

namespace nhitomi.Database
{
    /// <summary>
    /// Indicates a property as a cached value used purely for querying.
    /// Cached properties are excluded from source field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DbCachedAttribute : Attribute { }
}