using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models.Requests
{
    public class RollbackRequest
    {
        /// <summary>
        /// ID of the snapshot to rollback the target object to.
        /// </summary>
        [Required, nhitomiId]
        public string SnapshotId { get; set; }
    }
}