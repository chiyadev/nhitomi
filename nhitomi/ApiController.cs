using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace nhitomi
{
    [Route("api")]
    public class ApiController : ControllerBase
    {
        readonly ISet<IDoujinClient> _clients;

        public ApiController(
            ISet<IDoujinClient> clients
        )
        {
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

        // TODO: appsettings.json
        public const int ItemsPerPage = 20;

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
                .Skip(page * ItemsPerPage)
                .Take(ItemsPerPage);

            return await enumerable.ToArray();
        }
    }
}