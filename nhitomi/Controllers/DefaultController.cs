// Copyright (c) 2018-2019 chiya.dev
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace nhitomi.Controllers
{
    [Route("/")]
    public class DefaultController : ControllerBase
    {
        readonly AppSettings _settings;

        public DefaultController(
            IOptions<AppSettings> options)
        {
            _settings = options.Value;
        }

        [HttpGet]
        public string Get() =>
            $@"nhitomi — a Discord bot for searching and downloading doujinshi by chiya.dev

- Discord: {_settings.Discord.Guild.GuildInvite}
- GitHub: https://github.com/chiyadev/nhitomi";
    }
}
