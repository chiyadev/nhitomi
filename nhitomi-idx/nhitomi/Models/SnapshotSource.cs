namespace nhitomi.Models
{
    public enum SnapshotSource
    {
        /// <summary>
        /// Snapshot was created by a process of the system.
        /// </summary>
        System = 0,

        /// <summary>
        /// Snapshot was created by a normal user.
        /// </summary>
        User = 1,

        /// <summary>
        /// Snapshot was created by a moderator user.
        /// </summary>
        Moderator = 2
    }
}