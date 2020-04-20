namespace nhitomi.Models
{
    public enum BookTag
    {
        /// <summary>
        /// Tag is generic.
        /// </summary>
        Tag = 0,

        /// <summary>
        /// Tag is an artist.
        /// </summary>
        Artist = 1,

        /// <summary>
        /// Tag is a parody.
        /// </summary>
        /// <remarks>
        /// Not to be confused with series.
        /// </remarks>
        Parody = 2,

        /// <summary>
        /// Tag is a character.
        /// </summary>
        Character = 3,

        /// <summary>
        /// Tag is a convention.
        /// </summary>
        Convention = 4,

        /// <summary>
        /// Tag is a series.
        /// </summary>
        /// <remarks>
        /// Not to be confused with parody.
        /// </remarks>
        Series = 5,

        /// <summary>
        /// Tag is a circle.
        /// </summary>
        Circle = 6,

        /// <summary>
        /// Tag is metadata.
        /// </summary>
        Metadata = 7
    }
}