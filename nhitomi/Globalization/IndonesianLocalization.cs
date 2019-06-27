using System.Globalization;

// ReSharper disable StringLiteralTypo

namespace nhitomi.Globalization
{
    public class IndonesianLocalization : Localization
    {
        public override CultureInfo Culture { get; } = new CultureInfo("id");

        protected override object CreateDefinition() => new
        {
            meta = new
            {
                translators = new[]
                {
                    "[NekoDays](https://twitter.com/nekodayz)"
                }
            },

            doujinNotFound = "Tidak dapat menemukan doujinshi itu!",
            invalidQuery = "`{query}` tidak valid.",

            listLoading = "Memuat...",
            listBeginning = "Awal daftar!",
            listEnd = "Akhir daftar!",

            collectionNotFound = "Tidak ada koleksi bernama `{name}`.",
            collectionDeleted = "Koleksi `{collection.Name}` telah dihapus.",
            collectionSorted = "Koleksi yang diurutkan `{collection.Name}` oleh `{atribute}`.",

            addedToCollection = "Menambahkan `{doujin.OriginalName}` ke `{collection.Name}`.",
            removedFromCollection = "`{doujin.OriginalName}` dihapus dari `{collection.Name}`.",
            alreadyInCollection = "`{doujin.OriginalName}` sudah ada di `{collection.Name}`.",
            notInCollection = "`{doujin.OriginalName}` tidak ditemukan di `{collection.Name}`.",

            localizationChanged = "Bahasa server diatur ke `{localization.Culture.NativeName}`!",
            localizationNotFound = "Bahasa `{language}` tidak didukung.",

            qualityFilterChanged = "Filter kualitas pencarian default ke " +
                                   "`{state:diaktifkan|dinonaktifkan}`.",

            commandInvokeNotInGuild = "Anda hanya dapat menggunakan perintah ini di server.",
            notGuildAdmin = "Anda harus menjadi administrator server untuk menggunakan perintah ini.",

            tagNotFound = "Tag `{tag}` tidak ditemukan.",
            feedTagAdded = "`#{channel.Name}` sekarang berlangganan `{tag}`.",
            feedTagAlreadyAdded = "`#{channel.Name}` sudah berlangganan `{tag}`.",
            feedTagRemoved = "`#{channel.Name}` sekarang berhenti berlangganan dari `{tag}`.",
            feedTagNotRemoved = "`#{channel.Name}` tidak berlangganan `{tag}`.",
            feedModeChanged = "Mengubah mode umpan dari `#{channel.Name}` ke `{type}`.",

            doujinMessage = new
            {
                language = "Bahasa",
                group = "Kelompok",
                parody = "Parodi",
                categories = "Kategori",
                characters = "Karakter",
                tags = "Tag",
                contents = "Konten",
                contentsValue = "{doujin.PageCount} halaman",
                emptyList = new
                {
                    title = "Tidak ada doujinshi",
                    text = "Tidak ada doujinshi dalam daftar ini."
                }
            },

            doujinReadMessage = new
            {
                text = "Halaman {page} dari {doujin.PageCount}"
            },

            downloadMessage = new
            {
                text = "Klik tautan di atas untuk mulai mengunduh `{doujin.OriginalName}`."
            },

            collectionMessage = new
            {
                emptyCollection = "Koleksi kosong",
                sort = "Menyortir",
                sortValues = new
                {
                    uploadTime = "Waktu unggah",
                    processTime = "Waktu proses",
                    identifier = "Identifier",
                    name = "Nama",
                    artist = "Artis",
                    group = "Kelompok",
                    language = "Bahasa",
                    parody = "Parodi"
                },
                contents = "Konten",
                contentsValue = "{collection.Doujins.Count} doujinshi",
                empty = new
                {
                    title = "**{context.User.Username}**: Tidak ada koleksi",
                    text = "Anda tidak punya koleksi."
                }
            },

            helpMessage = new
            {
                title = "**nhitomi**: Bantuan",
                footer = "v{version} {codename} — didukung oleh chiya.dev",
                about = "bot Discord untuk mencari dan mengunduh doujinshi",
                invite = "[Bergabunglah]({guildInvite}) dengan server resmi atau " +
                         "[undang nhitomi]({botInvite}) ke server.",
                doujins = new
                {
                    heading = "Doujinshi",
                    get = "Menampilkan informasi doujinshi lengkap.",
                    from = "Menampilkan semua doujinshi dari sumber.",
                    read = "Memperlihatkan halaman dalam doujinshi.",
                    download = "Kirim tautan unduhan dari doujinshi.",
                    search = "Mencari doujinshi berdasarkan judul dan tag mereka."
                },
                collections = new
                {
                    heading = "Manajemen Koleksi",
                    list = "Menampilkan koleksi yang Anda miliki.",
                    view = "Menampilkan doujinshi dalam koleksi.",
                    add = "Menambahkan doujinshi ke koleksi.",
                    remove = "Menghapus doujinshi dari koleksi.",
                    sort = "Mengurutkan koleksi berdasarkan atribut doujinshi.",
                    delete = "Bersihkan koleksi dan hapus semua doujinshi dalam koleksi."
                },
                options = new
                {
                    heading = "Pengaturan",
                    language = "Mengubah bahasa antarmuka dalam server.",
                    filter = "Mengalihkan filter kualitas pencarian.",
                    feed = new
                    {
                        add = "Berlangganan saluran ke tag yang diberikan.",
                        remove = "Berhenti berlangganan saluran dari tag yang diberikan.",
                        mode = "Mengubah mode daftar putih tag saluran antara `any` dan` all`."
                    }
                },
                aliases = new
                {
                    heading = "Alias Komando"
                },
                examples = new
                {
                    heading = "Contohnya",
                    doujins = "Menemukan doujinshi",
                    collections = "Mengelola koleksi",
                    language = "Mengatur bahasa antarmuka"
                },
                sources = new
                {
                    heading = "Sumber"
                },
                languages = new
                {
                    heading = "Bahasa"
                },
                translations = new
                {
                    heading = "Terjemahan",
                    text = "Penerjemah: {translators}\n" +
                           "Beberapa terjemahan bahasa Indonesia berasal dari Google Translate."
                },
                openSource = new
                {
                    heading = "Sumber Terbuka",
                    license = "Proyek ini dilisensikan di bawah Lisensi MIT.",
                    contribution = "Kontribusi dipersilahkan!\n<{repoUrl}>"
                }
            },

            errorMessage = new
            {
                title = "**nhitomi**: Kesalahan",
                text = "Kesalahan telah dilaporkan. " +
                       "Untuk bantuan lebih lanjut, silahkan bergabung dengan [server resmi]({invite})."
            }
        };
    }
}
