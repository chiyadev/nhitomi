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

        [HttpGet("{source}/{id}")]
        public async Task<IDoujin> GetDoujinAsync(string source, string id)
        {
            // Find client by name
            var client = _clients.First(c => c.Name == source);

            return await client.GetAsync(id);
        }
    }
}