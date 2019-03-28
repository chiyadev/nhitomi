// Copyright (c) 2018-2019 fate/loli
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Microsoft.AspNetCore.Mvc;

namespace nhitomi.Proxy
{
    [Route("/")]
    public class DefaultController : ControllerBase
    {
        [HttpGet]
        public string Get() => "nhitomi proxy server";
    }
}
