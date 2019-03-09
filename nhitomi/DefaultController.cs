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
        public string Get() => @"nhitomi - Discord doujinshi bot by phosphene47#7788

            ___              ___                                    
           (   )       .-.  (   )                              .-.  
 ___ .-.    | | .-.   ( __)  | |_       .--.    ___ .-. .-.   ( __) 
(   )   \   | |/   \  (''"") (   __)    /    \  (   )   '   \  (''"") 
 |  .-. .   |  .-. .   | |   | | ___  |  .-. ;  |  .-.  .-. ;  | |  
 | |  | |   | |  | |   | |   | |(   ) | |  | |  | |  | |  | |  | |  
 | |  | |   | |  | |   | |   ' `-' ;  '  `-' /  | |  | |  | |  | |  
(___)(___) (___)(___) (___)   `.__.    `.__.'  (___)(___)(___)(___) 

- Discord: https://discord.gg/JFNga7q
- GitHub: https://github.com/phosphene47/nhitomi";
    }
}