using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using nhitomi.Database;
using nhitomi.Discord;
using nhitomi.Models;
using nhitomi.Models.Queries;
using IElasticClient = nhitomi.Database.IElasticClient;

namespace nhitomi.Controllers
{
    public sealed class DiscordOAuthUser
    {
        [JsonProperty("id")] public ulong Id;
        [JsonProperty("username")] public string Username;
        [JsonProperty("discriminator")] public int Discriminator;
        [JsonProperty("locale")] public string Locale;
        [JsonProperty("email")] public string Email;

        public void ApplyOn(DbUser user)
        {
            user.Username = Username;
            user.Email    = Email;
            user.Language = Locale?.ParseAsLanguage() ?? LanguageType.English;

            user.DiscordConnection = new DbUserDiscordConnection
            {
                Id            = Id,
                Username      = Username,
                Discriminator = Discriminator,
                Email         = Email
            };
        }
    }

    public interface IDiscordOAuthHandler : IOAuthHandler
    {
        Task<DbUser> GetOrCreateUserAsync(DiscordOAuthUser user, CancellationToken cancellationToken = default);
    }

    public class DiscordOAuthHandler : IDiscordOAuthHandler
    {
        readonly IUserService _users;
        readonly IElasticClient _client;
        readonly ISnapshotService _snapshots;
        readonly HttpClient _http;
        readonly IOptionsMonitor<DiscordOptions> _options;
        readonly IResourceLocker _locker;
        readonly ILinkGenerator _link;

        public string AuthorizeUrl
        {
            get
            {
                var options = _options.CurrentValue.OAuth;

                return $"https://discordapp.com/oauth2/authorize?response_type=code&prompt=none&client_id={options.ClientId}&scope={string.Join("%20", options.Scopes)}&redirect_uri={WebUtility.UrlEncode(RedirectUrl)}";
            }
        }

        public string RedirectUrl => _link.GetWebLink("oauth/discord");

        public DiscordOAuthHandler(IUserService users, IElasticClient client, IHttpClientFactory http, ISnapshotService snapshots,
                                   IOptionsMonitor<DiscordOptions> options, IResourceLocker locker, ILinkGenerator link)
        {
            _users     = users;
            _client    = client;
            _snapshots = snapshots;
            _http      = http.CreateClient(nameof(DiscordOAuthHandler));
            _options   = options;
            _locker    = locker;
            _link      = link;
        }

        async Task<DiscordOAuthUser> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var options = _options.CurrentValue.OAuth;

            if (options == null)
                throw new NotSupportedException("Discord OAuth not supported.");

            string token;

            using (var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method     = HttpMethod.Post,
                RequestUri = new Uri("https://discordapp.com/api/oauth2/token"),

                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]     = options.ClientId.ToString(),
                    ["client_secret"] = options.ClientSecret,
                    ["grant_type"]    = "authorization_code",
                    ["code"]          = code,
                    ["redirect_uri"]  = RedirectUrl,
                    ["scope"]         = string.Join(' ', options.Scopes)
                })
            }, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                dynamic obj = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                token = obj.access_token;
            }

            using (var response = await _http.SendAsync(new HttpRequestMessage
            {
                Method     = HttpMethod.Get,
                RequestUri = new Uri("https://discordapp.com/api/users/@me"),
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", token)
                }
            }, cancellationToken))
            {
                response.EnsureSuccessStatusCode();

                return JsonConvert.DeserializeObject<DiscordOAuthUser>(await response.Content.ReadAsStringAsync());
            }
        }

        sealed class UserQuery : IQueryProcessor<DbUser>
        {
            readonly ulong _id;

            public UserQuery(ulong id)
            {
                _id = id;
            }

            public SearchDescriptor<DbUser> Process(SearchDescriptor<DbUser> descriptor)
                => descriptor.Take(1)
                             .MultiQuery(q => q.Filter((FilterQuery<ulong>) _id, u => u.DiscordId));
        }

        async Task<IDbEntry<DbUser>> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
        {
            var result = await _client.SearchEntriesAsync(new UserQuery(id), cancellationToken);

            return result.Items.Length == 0 ? null : result.Items[0];
        }

        public async Task<DbUser> GetOrCreateUserAsync(string code, CancellationToken cancellationToken = default)
        {
            var user = await ExchangeCodeAsync(code, cancellationToken);

            return await GetOrCreateUserAsync(user, cancellationToken);
        }

        public async Task<DbUser> GetOrCreateUserAsync(DiscordOAuthUser user, CancellationToken cancellationToken = default)
        {
            await using (await _locker.EnterAsync($"oauth:discord:{user.Id}", cancellationToken))
            {
                var entry = await GetByIdAsync(user.Id, cancellationToken);

                // if user already exists, update their info
                if (entry != null)
                {
                    do
                    {
                        user.ApplyOn(entry.Value);
                    }
                    while (!await entry.TryUpdateAsync(cancellationToken));
                }

                // otherwise create new user
                else
                {
                    entry = _client.Entry(_users.MakeUserObject());

                    user.ApplyOn(entry.Value);

                    await entry.CreateAsync(cancellationToken);

                    await _snapshots.CreateAsync(entry.Value, new SnapshotArgs
                    {
                        Source    = SnapshotSource.User,
                        Committer = entry.Value,
                        Event     = SnapshotEvent.AfterCreation,
                        Reason    = $"Registered via Discord OAuth2 '{user.Username}'."
                    }, cancellationToken);
                }

                return entry.Value;
            }
        }
    }
}