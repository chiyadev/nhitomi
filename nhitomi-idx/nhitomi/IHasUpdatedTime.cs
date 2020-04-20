using System;

namespace nhitomi
{
    public interface IHasUpdatedTime : IHasCreatedTime
    {
        /// <summary>
        /// Time when this object was updated.
        /// </summary>
        DateTime UpdatedTime { get; set; }
    }
}