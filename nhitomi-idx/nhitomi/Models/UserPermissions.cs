using System;

namespace nhitomi.Models
{
    [Flags]
    public enum UserPermissions
    {
        /// <summary>
        /// User has no special permissions.
        /// </summary>
        None = 0,

        /// <summary>
        /// User is an administrator.
        /// This flag effectively enables every other flag.
        /// </summary>
        Administrator = 1,

        /// <summary>
        /// Can access endpoints of other users.
        /// </summary>
        ManageUsers = 1 << 1,

        /// <summary>
        /// Can restrict and derestrict users.
        /// </summary>
        RestrictUsers = 1 << 2,

        /// <summary>
        /// Can manage internal server configuration, maintenance mode, restart, etc.
        /// </summary>
        ManageConfig = 1 << 3
    }
}