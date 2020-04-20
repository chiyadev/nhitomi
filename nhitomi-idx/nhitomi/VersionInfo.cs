using System;
using System.Globalization;
using System.IO;
using System.Text;
using nhitomi.Models;

namespace nhitomi
{
    /// <summary>
    /// Contains the current build version information.
    /// </summary>
    public static class VersionInfo
    {
        /// <summary>
        /// Git commit information.
        /// </summary>
        public static readonly GitCommit Commit = new GitCommit
        {
            Hash      = "<unknown>",
            ShortHash = "<unknow",
            Author    = "<unknown>",
            Time      = DateTime.UtcNow,
            Message   = ""
        };

        /// <summary>
        /// Version number based on <see cref="Commit"/>'s <see cref="GitCommit.Time"/>.
        /// </summary>
        public static Version Version => new Version(Commit.Time.Year, Commit.Time.Month, Commit.Time.Day);

        /// <summary>
        /// Assembly version number.
        /// </summary>
        public static readonly Version AssemblyVersion = typeof(VersionInfo).Assembly.GetName().Version;

        static VersionInfo()
        {
            try
            {
                using var stream = File.OpenRead("version.txt");
                using var reader = new StreamReader(stream);

                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.StartsWith("commit "))
                    {
                        Commit.Hash      = line.Split(' ', 2)[1];
                        Commit.ShortHash = Commit.Hash.Substring(0, 7);
                    }

                    else if (line.StartsWith("Author:"))
                    {
                        Commit.Author = line[new Range(line.IndexOf(':') + 1, line.LastIndexOf('<'))].Trim();
                    }

                    else if (line.StartsWith("Date:"))
                    {
                        Commit.Time = DateTimeOffset.Parse(line.Split(':', 2)[1].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).UtcDateTime;
                    }

                    else if (line == "")
                    {
                        var builder = new StringBuilder();

                        while ((line = reader.ReadLine()) != null)
                            builder.AppendLine(line.Trim());

                        Commit.Message = builder.ToString().Trim();
                    }
                }
            }
            catch (IOException) { }
        }
    }
}