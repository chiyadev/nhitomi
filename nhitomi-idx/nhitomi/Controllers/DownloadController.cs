using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Models;

namespace nhitomi.Controllers
{
    /// <summary>
    /// Contains endpoints for managing download sessions.
    /// </summary>
    [Route("downloads")]
    public class DownloadController : nhitomiControllerBase
    {
        readonly IDownloadService _downloads;

        public DownloadController(IDownloadService downloads)
        {
            _downloads = downloads;
        }

        /// <summary>
        /// Retrieves download session information.
        /// </summary>
        /// <remarks>
        /// Sessions expire after a period of time. This endpoint can be called regularly to prevent the session from expiring.
        /// </remarks>
        /// <param name="id">Session ID.</param>
        [HttpGet("{id}", Name = "getDownloadSession")]
        public async Task<ActionResult<DownloadSession>> GetSessionAsync(string id)
        {
            var result = await _downloads.GetSessionAsync(id);

            if (!result.TryPickT0(out var session, out _))
                return ResultUtilities.NotFound(id);

            return session.Convert();
        }

        public class CreateSessionRequest { }

        /// <summary>
        /// Creates a new download session.
        /// </summary>
        [HttpPost(Name = "createDownloadSession"), RequireUser(Unrestricted = true)]
        public async Task<ActionResult<DownloadSession>> CreateSessionAsync(CreateSessionRequest request)
        {
            var __ = request;

            var result = await _downloads.CreateSessionAsync(UserId);

            if (!result.TryPickT0(out var session, out _))
                return ResultUtilities.BadRequest("Maximum number of download sessions has been reached.");

            return session.Convert();
        }

        /// <summary>
        /// Deletes a download session.
        /// </summary>
        /// <param name="id">Session ID.</param>
        [HttpDelete("{id}", Name = "deleteDownloadSession"), RequireUser(Unrestricted = true)]
        public async Task<ActionResult> DeleteSessionAsync(string id)
        {
            await _downloads.DeleteSessionAsync(id);

            return Ok();
        }
    }
}