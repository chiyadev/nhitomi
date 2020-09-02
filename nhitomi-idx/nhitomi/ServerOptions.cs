using System;
using System.Security.Cryptography.X509Certificates;

namespace nhitomi
{
    public class ServerOptions
    {
        /// <summary>
        /// Public facing server URL, without trailing slash.
        /// </summary>
        public string PublicUrl { get; set; } = "http://localhost:80";

        /// <summary>
        /// HTTP port to be used in non-development environment.
        /// </summary>
        public int? HttpPort { get; set; }

        /// <summary>
        /// HTTP port to be used in development environment.
        /// <see cref="HttpPort"/> and <see cref="HttpsPort"/> will be ignored.
        /// </summary>
        public int? HttpPortDev { get; set; }

        /// <summary>
        /// HTTPS port to be used in non-development environment.
        /// </summary>
        public int? HttpsPort { get; set; }

        /// <summary>
        /// HTTP port to be used for publishing Prometheus metrics.
        /// </summary>
        public int? MetricsPort { get; set; }

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
        /// True to enable dynamic server configuration stored in Elasticsearch.
        /// </summary>
        public bool DynamicConfigEnabled { get; set; } = true;

        /// <summary>
        /// Interval of dynamic configuration reloads.
        /// </summary>
        public TimeSpan DynamicConfigReloadInterval { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Whether the server is in maintenance mode, which blocks all writes to the database.
        /// </summary>
        public bool Maintenance { get; set; }
    }
}