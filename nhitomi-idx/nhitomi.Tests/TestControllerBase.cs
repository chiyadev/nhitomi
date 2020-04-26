using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using nhitomi.Controllers;

namespace nhitomi
{
    [AllowAnonymous, ApiExplorerSettings(IgnoreApi = true)]
    public abstract class TestControllerBase : nhitomiControllerBase { }
}