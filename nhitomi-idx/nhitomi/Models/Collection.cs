using System;
using System.ComponentModel.DataAnnotations;
using nhitomi.Models.Validation;

namespace nhitomi.Models
{
    /// <summary>
    /// Represents a collection of objects in the database.
    /// </summary>
    public class Collection : CollectionBase, IHasId
    {
        /// <summary>
        /// Book ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Time when this book was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Time when this book was updated.
        /// </summary>
        [Required]
        public DateTime UpdatedTime { get; set; }

        /// <summary>
        /// IDs of users that jointly own this collection.
        /// </summary>
        [Required]
        public string[] OwnerIds { get; set; }

        /// <summary>
        /// Type of objects in this collection.
        /// </summary>
        [Required]
        public ObjectType Type { get; set; }

        /// <summary>
        /// IDs of objects in this collection.
        /// </summary>
        [Required]
        public string[] Items { get; set; }
    }

    public class CollectionBase
    {
        /// <summary>
        /// Name of this collection.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Text describing this collection.
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// True if this collection can be accessed by any user publicly.
        /// </summary>
        [Required]
        public bool IsPublic { get; set; }
    }
}