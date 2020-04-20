namespace nhitomi.Models
{
    public enum ImageTag
    {
        /// <summary>
        /// Tag has no specific type.
        /// </summary>
        Tag = 0,

        /// <summary>
        /// Tag is an artist.
        /// </summary>
        Artist = 1,

        /// <summary>
        /// Tag is a character.
        /// </summary>
        Character = 2,

        /// <summary>
        /// Tag is a copyright.
        /// </summary>
        Copyright = 3,

        /// <summary>
        /// Tag is metadata.
        /// </summary>
        Metadata = 4,

        /// <summary>
        /// Tag references a pool name.
        /// </summary>
        Pool = 5
    }
}