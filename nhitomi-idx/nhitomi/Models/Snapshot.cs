using System;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a snapshot of an object in the database after an event.
    /// </summary>
    public class Snapshot : IHasId, IHasCreatedTime
    {
        /// <summary>
        /// Snapshot ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Time when this snapshot was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// What entity caused this snapshot to be created.
        /// </summary>
        [Required]
        public SnapshotSource Source { get; set; }

        /// <summary>
        /// What event caused this snapshot to be created.
        /// </summary>
        [Required]
        public SnapshotEvent Event { get; set; }

        /// <summary>
        /// ID of the snapshot that the object was reverted to, or null if this is not a revert snapshot.
        /// </summary>
        [nhitomiId]
        public string RollbackId { get; set; }

        /// <summary>
        /// ID of the user who created this snapshot, or null if the source of this snapshot is the system.
        /// </summary>
        [nhitomiId]
        public string CommitterId { get; set; }

        /// <summary>
        /// Type of the target object of this snapshot.
        /// </summary>
        [Required]
        public ObjectType Target { get; set; }

        /// <summary>
        /// ID of the target object of this snapshot.
        /// Note that it is possible for the target object to not exist if it was deleted after this snapshot was created.
        /// </summary>
        [Required, nhitomiId]
        public string TargetId { get; set; }

        /// <summary>
        /// Reason describing why this snapshot was created.
        /// </summary>
        public string Reason { get; set; }

        public override string ToString() => $"{Target} {TargetId} \"{Reason ?? "<no reason>"}\" @{Source} #{Id}";
    }

    public enum SnapshotSource
    {
        /// <summary>
        /// Snapshot was created by a process of the system.
        /// </summary>
        System = 0,

        /// <summary>
        /// Snapshot was created by a normal user.
        /// </summary>
        User = 1
    }

    public enum SnapshotEvent
    {
        /// <summary>
        /// Snapshot was created for an unknown reason.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Snapshot was created with the creation of an object.
        /// </summary>
        AfterCreation = 1,

        /// <summary>
        /// Snapshot was created before the modification of an object.
        /// </summary>
        BeforeModification = 2,

        /// <summary>
        /// Snapshot was created after the modification of an object.
        /// </summary>
        AfterModification = 3,

        /// <summary>
        /// Snapshot was created before an object was deleted.
        /// </summary>
        BeforeDeletion = 4
    }
}