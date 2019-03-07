// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using nhitomi.Core;

namespace nhitomi
{
    [Route("api")]
    public class ApiController : ControllerBase
    {
        readonly AppSettings.HttpSettings _settings;
        readonly ISet<IDoujinClient> _clients;

        public ApiController(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients
        )
        {
            _settings = options.Value.Http;
            _clients = clients;
        }

        [HttpGet("doujin/{source}/{id}")]
        public async Task<IDoujin> GetDoujinAsync(
            string source,
            string id
        )
        {
            // Find client by name
            var client = _clients.First(c => c.Name == source);

            return await client.GetAsync(id);
        }

        [HttpGet("doujins/{*source}")]
        public async Task<IEnumerable<IDoujin>> EnumerateDoujinsAsync(
            string source = null,
            [FromQuery] string query = null,
            [FromQuery] int page = 0
        )
        {
            IAsyncEnumerable<IDoujin> enumerable;

            if (source == null || source == "all")
                enumerable = Extensions.Interleave(await Task.WhenAll(_clients.Select(c => c.SearchAsync(query))));
            else
                enumerable = await _clients.First(c => c.Name == source).SearchAsync(query);

            enumerable = enumerable
                .Skip(page * _settings.ItemsPerPage)
                .Take(_settings.ItemsPerPage);

            return await enumerable.ToArray();
        }

        [HttpGet("sources")]
        public IEnumerable<IDoujinClient> GetSources() => _clients;
    }
}