const connectionStr = process.env.REACT_APP_UMAMI; // connection format "https://umami.is/{websiteId}"
let connection: undefined | { collectUrl: string; websiteId: string };

if (connectionStr) {
  const url = new URL(connectionStr, new URL(window.location.href));

  connection = {
    collectUrl: new URL("/api/collect", url).href,
    websiteId: url.pathname.substring(1),
  };
}

/** Collects an arbitrary Umami metric. */
export function collect(type: string, data: Record<string, any>) {
  if (!connection) return;

  fetch(connection.collectUrl, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      type,

      payload: {
        website: connection.websiteId,
        hostname: location.hostname,
        screen: `${window.screen.width}x${window.screen.height}`,
        language: navigator.language,

        ...data,
      },
    }),
  });
}

let currentPath: string | undefined;

/** Collects a page view metric. */
export function trackView(path: string, referrer = document.referrer) {
  collect("pageview", {
    url: currentPath = path,
    referrer,
  });
}

/** Collects an event metric for the current page. */
export function trackEvent(type: string, value: string) {
  if (!currentPath) return;

  collect("event", {
    url: currentPath,
    event_type: type.replace(" ", "-").toLowerCase(),
    event_value: value,
  });
}
