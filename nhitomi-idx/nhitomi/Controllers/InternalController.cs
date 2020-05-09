using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints used internally by nhitomi.
    /// </summary>
    [Route("internal")]
    public class InternalController : nhitomiControllerBase
    {
        readonly IConfiguration _config;
        readonly ServerOptions _serverOptions;
        readonly IElasticClient _elastic;

        public InternalController(IConfiguration config, IOptionsSnapshot<ServerOptions> serverOptions, IElasticClient elastic)
        {
            _config        = config;
            _serverOptions = serverOptions.Value;
            _elastic       = elastic;
        }

        /// <summary>
        /// Retrieves internal server configuration values.
        /// </summary>
        /// <remarks>
        /// This requires <see cref="UserPermissions.ManageServer"/> permission.
        /// </remarks>
        [HttpGet("internal/config", Name = "getServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public Dictionary<string, string> GetServerConfig()
            => _config.Get<Dictionary<string, string>>();

        /// <summary>
        /// Updates internal server configuration values.
        /// </summary>
        /// <remarks>
        /// This requires <see cref="UserPermissions.ManageServer"/> permission.
        /// Changes may not take effect immediately. Some changes will require a full server restart.
        /// </remarks>
        /// <param name="data">New configuration object to replace with.</param>
        [HttpPut("internal/config", Name = "setServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public async Task<Dictionary<string, string>> SetServerConfig(Dictionary<string, string> data)
        {
            var entry = _elastic.Entry<DbCompositeConfig>(DbCompositeConfig.DefaultId);

            entry.Value ??= new DbCompositeConfig();

            entry.Value.Config = data;

            await entry.UpdateAsync();
            await Task.Delay(_serverOptions.ConfigurationReloadInterval);

            return GetServerConfig();
        }
    }
}