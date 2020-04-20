namespace nhitomi.Models
{
    public enum SnapshotType
    {
        /// <summary>
        /// Snapshot was created when the object was created.
        /// </summary>
        Creation = 0,

        /// <summary>
        /// Snapshot was created when the object was modified.
        /// </summary>
        Modification = 1,

        /// <summary>
        /// Snapshot was created when the object was deleted.
        /// </summary>
        Deletion = 2,

        /// <summary>
        /// Snapshot was created when the object was rolled back to a previous snapshot.
        /// </summary>
        Rollback = 3
    }
}