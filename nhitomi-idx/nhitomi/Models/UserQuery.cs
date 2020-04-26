using System;
using nhitomi.Models.Queries;

namespace nhitomi.Models
{
    public class UserQuery : QueryBase<UserSort>
    {
        public RangeQuery<DateTime> CreatedTime { get; set; }
        public RangeQuery<DateTime> UpdatedTime { get; set; }
        public FilterQuery<string> Username { get; set; }
        public FilterQuery<string> Email { get; set; }
        public FilterQuery<UserPermissions> Permissions { get; set; }
        public FilterQuery<LanguageType> Language { get; set; }
    }

    public enum UserSort
    {
        /// <summary>
        /// Sort by created time.
        /// </summary>
        CreatedTime = 0,

        /// <summary>
        /// Sort by updated time.
        /// </summary>
        UpdatedTime = 1,

        /// <summary>
        /// Sort by username.
        /// </summary>
        Username = 2,

        /// <summary>
        /// Sort by email.
        /// </summary>
        Email = 3
    }
}