using System;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a snapshot of an object in the database after an event.
    /// </summary>
    public class Snapshot : INanokaObject, IHasCreatedTime
    {
        /// <summary>
        /// Snapshot ID.
        /// </summary>
        [Required, NanokaId]
        public string Id { get; set; }

        /// <summary>
        /// Time when this snapshot was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Snapshot type.
        /// </summary>
        [Required]
        public SnapshotType Type { get; set; }

        /// <summary>
        /// What caused this snapshot to be created.
        /// </summary>
        [Required]
        public SnapshotSource Source { get; set; }

        /// <summary>
        /// If the snapshot type is rollback, the ID of the snapshot that was reverted to.
        /// Otherwise, this field should be null.
        /// </summary>
        [NanokaId]
        public string RollbackId { get; set; }

        /// <summary>
        /// ID of the user who created this snapshot.
        /// This field can be null if the <see cref="Source"/> of this snapshot is the system.
        /// </summary>
        [NanokaId]
        public string CommitterId { get; set; }

        /// <summary>
        /// Type of the target object of this snapshot.
        /// </summary>
        [Required]
        public SnapshotTarget Target { get; set; }

        /// <summary>
        /// ID of the target object of this snapshot.
        /// </summary>
        /// <remarks>
        /// It is possible for the target object to not exist if it was deleted after this snapshot was created.
        /// </remarks>
        [Required, NanokaId]
        public string TargetId { get; set; }

        /// <summary>
        /// Reason describing why this snapshot was created.
        /// </summary>
        public string Reason { get; set; }

        public override string ToString() => $"{Target} {TargetId} [{Source}]: \"{Reason ?? "<unknown reason>"}\" #{Id}";
    }
}