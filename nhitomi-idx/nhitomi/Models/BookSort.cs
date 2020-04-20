namespace nhitomi.Models
{
    public enum BookSort
    {
        /// <summary>
        /// Sort by relevance.
        /// </summary>
        Relevance = 0,

        /// <summary>
        /// Sort by created time.
        /// </summary>
        CreatedTime = 1,

        /// <summary>
        /// Sort by updated time.
        /// </summary>
        UpdatedTime = 2,

        /// <summary>
        /// Sort by page count.
        /// </summary>
        PageCount = 3,

        /// <summary>
        /// Sort by total content size.
        /// </summary>
        Size = 4,

        /// <summary>
        /// Sort by availability.
        /// </summary>
        Availability = 5,

        /// <summary>
        /// Sort by total availability.
        /// </summary>
        TotalAvailability = 6
    }
}