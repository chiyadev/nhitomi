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
export async function collect(type: string, data: Record<string, any>) {
  if (!umamiInfo) return;

  await fetch(umamiInfo.url, {
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
export async function trackView(path: string, referrer = document.referrer) {
  await collect("pageview", {
    url: currentPath = path,
    referrer,
  });
}

/** Collects an event metric for the current page. */
export async function trackEvent(type: string, value: string) {
  if (!currentPath) return;

  await collect("event", {
    url: currentPath,
    event_type: type,
    event_value: value,
  });
}