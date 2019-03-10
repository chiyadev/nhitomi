// Copyright (c) 2018-2019 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using nhitomi.Core;
using Newtonsoft.Json;

namespace nhitomi
{
    [Route("dl")]
    public class DownloadController : ControllerBase
    {
        readonly AppSettings.DiscordSettings _settings;
        readonly ISet<IDoujinClient> _clients;
        readonly JsonSerializer _json;
        readonly ILogger _logger;

        public DownloadController(
            IOptions<AppSettings> options,
            ISet<IDoujinClient> clients,
            JsonSerializer json,
            ILogger<DownloadController> logger
        )
        {
            _settings = options.Value.Discord;
            _clients = clients;
            _json = json;
            _logger = logger;
        }

        const string _downloader = @"
<!DOCTYPE html>
<html lang=""en"">

<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1, shrink-to-fit=no"">
    <meta name=""description"" content=""nhitomi downloader client"">
    <link rel=""icon"" type=""image/png"" href=""https://github.com/fateloli/nhitomi/raw/master/nhitomi.png"">

    <title>{title}</title>

    <link href=""https://stackpath.bootstrapcdn.com/bootstrap/4.3.1/css/bootstrap.min.css"" rel=""stylesheet""
          integrity=""sha384-ggOyR0iXCbMQv3Xipma34MD+dH/1fQ784/j6cY/iJTQUOhcWr7x9JvoRxT2MZw1T"" crossorigin=""anonymous"">
</head>

<body class=""container"" style=""height: 100vh; display: flex; flex-direction: column;"">

<header>
    <h1 class=""mt-5"">{title}</h1>
    <p class=""lead"">{subtitle}</p>

    <a href=""{source}"" target=""_blank"" role=""button"" class=""btn btn-outline-info"" style=""margin-bottom: 1em;"">
        Open {sourceName}
    </a>

    <div class=""progress"" style=""height: 2em; margin-bottom: 1em;"">
        <div id=""progress"" class=""progress-bar progress-bar-striped"" role=""progressbar""></div>
    </div>
</header>

<div style=""flex: 1; display: flex; flex-direction: row;"">
    <img src=""{thumb}"" alt=""thumbnail"" class=""img-thumbnail rounded"" style=""object-fit: contain; height: 94%; flex: 0;"">
</div>

<footer style=""line-height: 3em"">
    <span class=""text-muted"">Copyright (c) 2018-2019 phosphene47</span>
</footer>

<script src=""https://cdnjs.cloudflare.com/ajax/libs/jquery/3.3.1/jquery.slim.min.js""
        integrity=""sha256-3edrmyuQ0w65f8gfBsqowzjJe2iM6n0nKciPUp8y+7E="" crossorigin=""anonymous""></script>
<script src=""https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/4.3.1/js/bootstrap.min.js""
        integrity=""sha256-CjSoeELFOcH0/uxWu6mC/Vlrc1AARqbm/jiiImDGV3s="" crossorigin=""anonymous""></script>
<script src=""https://cdnjs.cloudflare.com/ajax/libs/jszip/3.2.0/jszip.js""
        integrity=""sha256-EHgAIxZ/n1IaWzhk7MwPaOux4UOWSr3czJ4dEVimBvo="" crossorigin=""anonymous""></script>
<script src=""https://cdnjs.cloudflare.com/ajax/libs/jszip-utils/0.0.2/jszip-utils.min.js""
        integrity=""sha256-AIk6chbus7IS5RVpqSNV1X7Qihbi1YC0lOLuUXQZ+mw="" crossorigin=""anonymous""></script>
<script src=""https://cdnjs.cloudflare.com/ajax/libs/FileSaver.js/1.3.8/FileSaver.min.js""
        integrity=""sha256-FPJJt8nA+xL4RU6/gsriA8p8xAeLGatoyTjldvQKGdE="" crossorigin=""anonymous""></script>

<script>
    var doujinStr = '{doujin}';
    var doujin = JSON.parse(doujinStr);

    var $progBar = $('#progress');

    function updateProgress(state, name) {
        var progress = Math.round(state / doujin.pageUrls.length * 100) + '%';

        $progBar
            .width(progress)
            .text(name ? progress + ' — ' + name : progress);
    }

    updateProgress(0);

    var zip = new JSZip();

    zip.file('_nhitomi.json', doujinStr);

    var i = 0;

    doujin.pageUrls.forEach(function (p) {
        JSZipUtils.getBinaryContent(p, function (error, data) {
            if (error) {
                throw error;
            }

            var filename = p.substring(p.lastIndexOf('/') + 1);

            zip.file(filename, data, {
                binary: true
            });

            updateProgress(++i, filename);

            if (i === doujin.pageUrls.length) {
                $progBar.addClass('bg-success');

                zip.generateAsync({
                    type: 'blob'
                }).then(function (content) {
                    saveAs(content, (doujin.originalName || doujin.prettyName) + '.zip');
                });
            }
        })
    });
</script>

</body>

</html>";

        [HttpGet("{*token}")]
        public async Task<ActionResult> GetAsync(string token)
        {
            if (!TokenGenerator.TryDeserializeToken(token, _settings.Token, out var sourceName, out var id))
                return BadRequest("Download token has expired. Please try again.");

            _logger.LogDebug($"Received download request: token {token}");

            // Retrieve doujin
            var client = _clients.First(c => c.Name == sourceName);
            var doujin = await client.GetAsync(id);

            // Create javascript downloader
            var downloader = _downloader.NamedFormat(new Dictionary<string, object>
            {
                {"title", doujin.PrettyName},
                {"subtitle", doujin.OriginalName},
                {"source", doujin.SourceUrl},
                {"sourceName", $"{doujin.Source.Name}/{doujin.Id}"},
                {"thumb", doujin.PageUrls.First()},
                {"doujin", HttpUtility.JavaScriptStringEncode(_json.Serialize(doujin))}
            });

            return Content(downloader, "text/html");
        }
    }
}