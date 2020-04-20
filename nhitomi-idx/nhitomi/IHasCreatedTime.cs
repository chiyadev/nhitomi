using System;

namespace nhitomi
{
    public interface IHasCreatedTime
    {
        /// <summary>
        /// Time when this object was created.
        /// </summary>
        DateTime CreatedTime { get; set; }
    }
}