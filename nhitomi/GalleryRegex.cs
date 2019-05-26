namespace nhitomi
{
    public static class GalleryRegex
    {
        public const string nhentai = @"\b((http|https):\/\/)?nhentai(\.net)?\/(g\/)?(?<source_nhentai>[0-9]{1,6})\b";
        public const string Hitomi = @"\b((http|https):\/\/)?hitomi(\.la)?\/(galleries\/)?(?<Hitomi>[0-9]{1,7})\b";
    }
}