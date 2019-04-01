"use strict";

$(document).ready(function () {
  var doujin = {doujin};
  var proxies = {proxies};

  var $status = $("#status");
  var $progress = $("#progress");
  var $thumbnail = $("#thumbnail");
  var $galleryButton = $("#galleryButton");

  function updateStatus(text, error) {
    $status.text(text);

    if (error) {
      $status.parent().addClass("text-danger");
      $progress.addClass("bg-danger");
    } else {
      $status.parent().removeClass("text-danger");
      $progress.removeClass("bg-danger");
    }
  }

  function updateProgress(state, name) {
    var progress = Math.round(state / doujin.pages.length * 100) + "%";

    $progress
      .width(progress)
      .text(name ? progress + " — " + name : progress);

    $progress
      .parent()
      .fadeIn();
  }

  var proxyIndex = Math.floor(Math.random() * proxies.length);

  function getProxiedImage(url) {
    return proxies[proxyIndex++ % proxies.length]
      + "/proxy/get/image?url=" + encodeURIComponent(url)
      + "&token=" + encodeURIComponent("{token}");
  }

  try {
    if (proxies.length === 0) {
      updateStatus("No proxies are available!", true);
    } else {
      $galleryButton
        .attr("href", doujin.sourceUrl)
        .text(doujin.source.name + "/" + doujin.id)
        .fadeIn();

      $thumbnail
        .on('load', function () {
          $thumbnail.fadeIn();

          var zip = new JSZip();
          zip.file("_nhitomi.json", JSON.stringify(doujin, null, 2));

          function saveZip() {
            updateStatus("Saving...");
            $progress.addClass("bg-warning");

            zip.generateAsync({
              type: "blob"
            }).then(function (content) {
              updateStatus("Saved!");
              $progress
                .removeClass("bg-warning")
                .addClass("bg-success");

              saveAs(content, (doujin.originalName || doujin.prettyName) + ".zip");
            });
          }

          var queue = doujin.pages.map(function (page) {
            return {
              name: ("00" + ((page.i || 0) + 1)).substr(-3) + page.e,
              url: page.u
            };
          });
          queue.reverse();

          var running = true;
          var downloadedCount = 0;
          var concurrentDownloads = 0;

          function downloadNext() {
            while (concurrentDownloads < proxies.length && queue.length !== 0) {
              concurrentDownloads++;

              (function (image) {
                var retryCount = 0;

                function downloadCurrent() {
                  JSZipUtils.getBinaryContent(getProxiedImage(image.url), function (error, data) {
                    if (!running)
                      return;

                    if (error) {
                      if (retryCount++ === 20) {
                        queue.length = 0;
                        updateStatus("Could not download '" + image.url + "': " + error, !(running = false));
                      } else
                        setTimeout(downloadCurrent, 1000);

                      return;
                    }

                    zip.file(image.name, data, {
                      binary: true
                    });

                    concurrentDownloads--;
                    updateProgress(++downloadedCount, image.name);

                    if (downloadedCount === doujin.pages.length)
                      saveZip();
                    else
                      downloadNext();
                  });
                }

                downloadCurrent();
              })(queue.pop());
            }
          }

          updateStatus("Downloading...");
          downloadNext();
        })
        .on('error', function () {
          updateStatus("No proxies are available!", true);
        })
        .attr("src", getProxiedImage(doujin.pages[0].u))
        .each(function () {
          if (this.complete)
            $(this).trigger('load');
        });
    }
  } catch (e) {
    updateStatus(e, true);
  }
});
