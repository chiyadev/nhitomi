let umamiInfo:
  | undefined
  | {
      url: string;
      websiteId: string;
    };

/** Initializes Umami for the current session. */
export function umami(url: string, websiteId: string) {
  if (url.endsWith("/")) {
    url = url.slice(0, -1);
  }

  umamiInfo = { url, websiteId };
}

/** Collects an arbitrary Umami metric. */
export function collect(type: string, data: Record<string, any>) {
  if (!umamiInfo) return;

  fetch(umamiInfo.url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      type,

      payload: {
        website: umamiInfo.websiteId,
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
    event_type: type,
    event_value: value,
  });
}
