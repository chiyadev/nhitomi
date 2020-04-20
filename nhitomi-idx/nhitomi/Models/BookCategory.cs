namespace nhitomi.Models
{
    public enum BookCategory
    {
        /// <summary>
        /// Book is a doujinshi.
        /// </summary>
        Doujinshi = 0,

        /// <summary>
        /// Book is a manga.
        /// </summary>
        Manga = 1,

        /// <summary>
        /// Book is a set of artist CG.
        /// </summary>
        ArtistCg = 2,

        /// <summary>
        /// Book is a set of game CG.
        /// </summary>
        GameCg = 3,

        /// <summary>
        /// Book is a light novel scan.
        /// </summary>
        LightNovel = 5
    }
}