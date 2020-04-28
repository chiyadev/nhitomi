using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Database;
using nhitomi.Models.Requests;

namespace nhitomi.Controllers
{
    public interface IOAuthHandler
    {
        string AuthorizeUrl { get; }

        Task<DbUser> GetOrCreateUserAsync(string code, CancellationToken cancellationToken = default);
    }

    [Route("users")]
    public class UserController : nhitomiControllerBase
    {
        readonly IAuthService _auth;
        readonly IDiscordOAuthHandler _discord;

        public UserController(IAuthService auth, IDiscordOAuthHandler discord)
        {
            _auth    = auth;
            _discord = discord;
        }

        /// <summary>
        /// Authenticates using Discord OAuth.
        /// </summary>
        /// <param name="request">OAuth data.</param>
        [HttpPost("oauth/discord", Name = "authenticateUserDiscord"), AllowAnonymous]
        public async Task<ActionResult<AuthenticateResponse>> AuthDiscordAsync(AuthenticateDiscordRequest request)
        {
            var user = await _discord.GetOrCreateUserAsync(request.Code);

            return new AuthenticateResponse
            {
                Token = await _auth.GenerateTokenAsync(user),
                User  = ProcessUser(user.Convert())
            };
        }
    }
}