using System;
using System.Collections.Generic;
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
    public interface IDiscordOAuthHandler : IOAuthHandler { }

    public class DiscordOAuthHandler : IDiscordOAuthHandler
    {
        readonly IUserService _users;
        readonly IElasticClient _client;
        readonly ISnapshotService _snapshots;
        readonly HttpClient _http;
        readonly IOptionsMonitor<DiscordOptions> _options;
        readonly IResourceLocker _locker;

        public DiscordOAuthHandler(IUserService users, IElasticClient client, IHttpClientFactory http, ISnapshotService snapshots, IOptionsMonitor<DiscordOptions> options, IResourceLocker locker)
        {
            _users     = users;
            _client    = client;
            _snapshots = snapshots;
            _http      = http.CreateClient(nameof(DiscordOAuthHandler));
            _options   = options;
            _locker    = locker;
        }

        sealed class UserInfo
        {
            [JsonProperty("id")] public string Id;
            [JsonProperty("username")] public string Username;
            [JsonProperty("discriminator")] public string Discriminator;
            [JsonProperty("avatar")] public string Avatar;
            [JsonProperty("locale")] public string Locale;
            [JsonProperty("verified")] public bool Verified;
            [JsonProperty("email")] public string Email;

            public void ApplyOn(DbUser user)
            {
                user.Username = Username;
                user.Email    = Email;

                user.DiscordConnection = new DbUserDiscordConnection
                {
                    Id            = ulong.Parse(Id),
                    Username      = Username,
                    Discriminator = int.Parse(Discriminator),
                    Verified      = Verified,
                    Email         = Email
                };
            }
        }

        async Task<UserInfo> LoadUserAsync(string code, CancellationToken cancellationToken = default)
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
                    ["redirect_uri"]  = options.RedirectUri,
                    ["scope"]         = "identify email connections"
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

                return JsonConvert.DeserializeObject<UserInfo>(await response.Content.ReadAsStringAsync());
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

        public async Task<DbUser> GetOrCreateUserAsync(string code, CancellationToken cancellationToken = default)
        {
            var info = await LoadUserAsync(code, cancellationToken);

            var id = ulong.Parse(info.Id);

            await using (await _locker.EnterAsync($"oauth:discord:{id}", cancellationToken))
            {
                // check if user already exists
                var search = await _client.SearchEntriesAsync(new UserQuery(id), cancellationToken);

                DbUser user;

                if (search.Items.Length == 0)
                {
                    info.ApplyOn(user = _users.MakeUserObject());

                    await _client.Entry(user).CreateAsync(cancellationToken);

                    await _snapshots.CreateAsync(user, new SnapshotArgs
                    {
                        Source    = SnapshotSource.User,
                        Committer = user,
                        Event     = SnapshotEvent.AfterCreation,
                        Reason    = $"Registered via Discord OAuth2 '{info.Username}'."
                    }, cancellationToken);
                }

                else
                {
                    var entry = search.Items[0];

                    do
                    {
                        info.ApplyOn(entry.Value);
                    }
                    while (!await entry.TryUpdateAsync(cancellationToken));

                    user = entry.Value;
                }

                return user;
            }
        }
    }
}