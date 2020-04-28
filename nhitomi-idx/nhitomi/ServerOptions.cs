using System;
using System.Security.Cryptography.X509Certificates;

namespace nhitomi
{
    public class ServerOptions
    {
        /// <summary>
        /// Public facing server URL, without trailing slash.
        /// </summary>
        public string PublicUrl { get; set; }

        /// <summary>
        /// URL to proxy frontend requests, used for proxying requests to nhitomi-web WebPack dev server in development.
        /// This is enabled by default to localhost:3000.
        /// </summary>
        public string ProxyUrl { get; set; }

        /// <summary>
        /// HTTP port to be used in non-development environment.
        /// </summary>
        public int HttpPort { get; set; } = 80;

        /// <summary>
        /// HTTP port to be used in development environment.
        /// <see cref="HttpPort"/> and <see cref="HttpsPort"/> will be ignored.
        /// </summary>
        public int HttpPortDev { get; set; } = 5000;

        /// <summary>
        /// HTTPS port to be used in non-development environment.
        /// </summary>
        public int HttpsPort { get; set; } = 443;

        /// <summary>
        /// Path to SSL certificate for HTTPS connections.
        /// The certificate format must be compatible with <see cref="X509Certificate2"/> which is a PFX or PKCS12.
        /// </summary>
        public string CertificatePath { get; set; }

        /// <summary>
        /// Optional SSL certificate password.
        /// </summary>
        public string CertificatePassword { get; set; }

        /// <summary>
        /// Whether HTTP response compression is enabled or not. This is only valid for static files.
        /// See https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression about response compression guidelines.
        /// </summary>
        public bool ResponseCompression { get; set; }

        /// <summary>
        /// Interval of dynamic configuration reloads.
        /// </summary>
        public TimeSpan ConfigurationReloadInterval { get; set; } = TimeSpan.FromSeconds(2);
    }
}