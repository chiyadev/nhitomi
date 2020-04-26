namespace nhitomi.Models.Queries
{
    public enum QueryMatchMode
    {
        /// <summary>
        /// At least one of the specified query values must be matched.
        /// </summary>
        Any = 0,

        /// <summary>
        /// All of the specified query values must be matched.
        /// </summary>
        All = 1
    }
}