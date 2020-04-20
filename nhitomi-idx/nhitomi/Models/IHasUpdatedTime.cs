using System;

namespace nhitomi.Models
{
    public interface IHasUpdatedTime : IHasCreatedTime
    {
        /// <summary>
        /// Time when this object was updated.
        /// </summary>
        DateTime UpdatedTime { get; set; }
    }
}