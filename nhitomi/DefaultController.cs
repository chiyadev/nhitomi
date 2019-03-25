// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Mvc;

namespace nhitomi
{
    [Route("/")]
    public class DefaultController : ControllerBase
    {
        [HttpGet]
        public string Get() =>
            @"nhitomi — a Discord bot for searching and downloading doujinshi.

- Discord: https://discord.gg/JFNga7q
- GitHub: https://github.com/fateloli/nhitomi";
    }
}