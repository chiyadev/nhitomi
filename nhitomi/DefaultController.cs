using Microsoft.AspNetCore.Mvc;

namespace nhitomi
{
    [Route("/")]
    public class DefaultController : ControllerBase
    {
        [HttpGet]
        public string Get() =>
@"nhitomi - Discord doujinshi bot by phosphene47#7788

            ___              ___                                    
           (   )       .-.  (   )                              .-.  
 ___ .-.    | | .-.   ( __)  | |_       .--.    ___ .-. .-.   ( __) 
(   )   \   | |/   \  (''"") (   __)    /    \  (   )   '   \  (''"") 
 |  .-. .   |  .-. .   | |   | | ___  |  .-. ;  |  .-.  .-. ;  | |  
 | |  | |   | |  | |   | |   | |(   ) | |  | |  | |  | |  | |  | |  
 | |  | |   | |  | |   | |   ' `-' ;  '  `-' /  | |  | |  | |  | |  
(___)(___) (___)(___) (___)   `.__.    `.__.'  (___)(___)(___)(___) 

- Discord: https://discord.gg/JFNga7q
- Github: https://github.com/phosphene47/nhitomi";
    }
}