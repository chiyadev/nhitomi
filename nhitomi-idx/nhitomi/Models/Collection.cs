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
        /// Collection ID.
        /// </summary>
        [Required, nhitomiId]
        public string Id { get; set; }

        /// <summary>
        /// Time when this collection was created.
        /// </summary>
        [Required]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// Time when this collection was updated.
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
        [Required, MinLength(1), MaxLength(64)]
        public string Name { get; set; }

        /// <summary>
        /// Text describing this collection.
        /// </summary>
        [MaxLength(512)]
        public string Description { get; set; }
    }

    /// <summary>
    /// Special types of collections.
    /// </summary>
    public enum SpecialCollection
    {
        /// <summary>
        /// Indicates that a collection be used for objects marked favorite.
        /// </summary>
        Favorites = 0,

        /// <summary>
        /// Indicates that a collection be used for objects marked "see later".
        /// </summary>
        Later = 1
    }
}