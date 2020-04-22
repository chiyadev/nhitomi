using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using nhitomi.Database;
using nhitomi.Models;
using nhitomi.Models.Requests;

namespace nhitomi.Controllers
{
    [Route("/")]
    public class InfoController : nhitomiControllerBase
    {
        readonly IConfiguration _config;
        readonly ServerOptions _serverOptions;
        readonly RecaptchaOptions _recaptchaOptions;

        readonly IElasticClient _elastic;
        readonly IUserService _users;
        readonly IBookService _books;
        readonly ISnapshotService _snapshots;

        public InfoController(IConfiguration config, IOptionsSnapshot<ServerOptions> serverOptions, IOptionsSnapshot<RecaptchaOptions> recaptchaOptions,
                              IElasticClient elastic, IUserService users, IBookService books, ISnapshotService snapshots)
        {
            _config           = config;
            _serverOptions    = serverOptions.Value;
            _recaptchaOptions = recaptchaOptions.Value;

            _elastic   = elastic;
            _users     = users;
            _books     = books;
            _snapshots = snapshots;
        }

        /// <summary>
        /// Prints art.
        /// </summary>
        /// <returns></returns>
        [HttpGet, AllowAnonymous, ApiExplorerSettings(IgnoreApi = true)]
        public ActionResult Get() => new ArtResult();

        sealed class ArtResult : ActionResult
        {
            public override async Task ExecuteResultAsync(ActionContext context)
            {
                var art = $@"

☆.｡.:*　nhitomi　.｡.:*☆ by chiya.dev

Version: {VersionInfo.Version}
Commit: {VersionInfo.Commit.Hash}
GitHub: https://github.com/chiyadev/nhitomi
API docs: {context.HttpContext.Request.Scheme}://{context.HttpContext.Request.Host}{Startup.ApiBasePath}/docs

MIT License

Copyright (c) 2018-{DateTime.UtcNow.Year} chiya.dev

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the ""Software""), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

".Trim();

                await new OkObjectResult(art).ExecuteResultAsync(context);
            }
        }

        /// <summary>
        /// Retrieves API information.
        /// </summary>
        [HttpGet("info", Name = "getInfo"), AllowAnonymous]
        public async Task<GetInfoResponse> GetInfo() => new GetInfoResponse
        {
            Version          = VersionInfo.Commit,
            RecaptchaSiteKey = _recaptchaOptions.SiteKey,
            Counters = new Dictionary<ObjectType, int>
            {
                [ObjectType.User]     = await _users.CountAsync(),
                [ObjectType.Book]     = await _books.CountAsync(),
                [ObjectType.Image]    = 0,
                [ObjectType.Snapshot] = await _snapshots.CountAsync()
            },
            User = User?.Convert()
        };

        /// <summary>
        /// Retrieves internal server configuration values.
        /// </summary>
        [HttpGet("internal/config", Name = "getServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public Dictionary<string, string> GetServerConfig()
            => _config.Get<Dictionary<string, string>>()
                      .ToDictionary(
                           x => x.Key.Replace(":", "."),
                           x => x.Value);

        /// <summary>
        /// Updates internal server configuration values.
        /// Changes may not take effect immediately. Some changes will require a full server restart.
        /// </summary>
        /// <param name="data">New configuration object to replace with.</param>
        [HttpPut("internal/config", Name = "setServerConfig"), RequireUser(Permissions = UserPermissions.ManageServer)]
        public async Task<Dictionary<string, string>> SetServerConfig(Dictionary<string, string> data)
        {
            data = data.ToDictionary(
                x => x.Key.Replace(".", ":"),
                x => x.Value);

            var entry = _elastic.Entry<DbCompositeConfig>(DbCompositeConfig.DefaultId);

            if (entry.Value == null)
                entry.Value = new DbCompositeConfig();

            entry.Value.Config = data;

            await entry.UpdateAsync();
            await Task.Delay(_serverOptions.ConfigurationReloadInterval);

            return GetServerConfig();
        }
    }
}