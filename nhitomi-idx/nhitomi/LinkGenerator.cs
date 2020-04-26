using Microsoft.Extensions.Options;

namespace nhitomi
{
    public interface ILinkGenerator
    {
        /// <summary>
        /// Makes a publicly accessible link to an API route.
        /// </summary>
        string GetApiLink(string path);

        /// <summary>
        /// Makes a publicly accessible link to a frontend route.
        /// </summary>
        string GetWebLink(string path);
    }

    public class LinkGenerator : ILinkGenerator
    {
        readonly IOptionsMonitor<ServerOptions> _options;

        public LinkGenerator(IOptionsMonitor<ServerOptions> options)
        {
            _options = options;
        }

        static string FormatPath(string path)
        {
            path = path?.Trim();

            if (string.IsNullOrEmpty(path))
                return null;

            if (path == "/")
                return null;

            if (!path.StartsWith('/'))
                path = "/" + path;

            return path;
        }

        public string GetApiLink(string path) => string.Concat(_options.CurrentValue.PublicUrl, Startup.ApiBasePath, FormatPath(path));
        public string GetWebLink(string path) => string.Concat(_options.CurrentValue.PublicUrl, FormatPath(path));
    }
}