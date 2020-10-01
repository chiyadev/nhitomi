// this is a port of https://github.com/zgoat/goatcounter/blob/master/public/count.js to Typescript, 2020/10/02
import { randomStr } from "./random";

type VariableContainer = {
  rnd: string;
  p: string;
  r: string;
  t: string;
  e: boolean;
  s: [number, number, number];
  b: number;
  q: string;
};

/** Returns all data to send to the counter endpoint. */
export function getData(): VariableContainer {
  return {
    rnd: randomStr(5), // browsers don't always listen to Cache-Control
    p: location.pathname + location.search,
    r: document.referrer,
    t: document.title,
    e: false,
    s: [window.screen.width, window.screen.height, window.devicePixelRatio || 1],
    b: isBot(),
    q: location.search,
  };
}

/** Determines whether the current browser is likely to be a bot. */
export function isBot() {
  // Headless browsers are probably a bot.
  const w: any = window;
  const d: any = document;

  if (w.callPhantom || w._phantom || w.phantom) return 150;
  if (w.__nightmare) return 151;
  if (d.__selenium_unwrapped || d.__webdriver_evaluate || d.__driver_evaluate) return 152;
  if (navigator.webdriver) return 153;

  return 0;
}

/** Converts given object to urlencoded string, starting with a ?. */
export function urlEncode(obj: Record<string, any>) {
  // qs is deliberately not used, for compatibility with Goat
  const p: string[] = [];

  for (const k in obj) {
    if (obj[k] !== "" && obj[k] !== null && obj[k] !== undefined && obj[k] !== false)
      p.push(encodeURIComponent(k) + "=" + encodeURIComponent(obj[k]));
  }

  return "?" + p.join("&");
}

/** Returns the endpoint to send requests to. */
export function getEndpoint() {
  return process.env.REACT_APP_GOAT_URL || "https://a.chiya.dev/count";
}

const loopbackRegex = /(localhost$|^127\.|^10\.|^172\.(1[6-9]|2[0-9]|3[0-1])\.|^192\.168\.)/;

/** Determines whether requests should be filtered. */
export function filter() {
  return false;
  if ("visibilityState" in document && document.visibilityState === "hidden") return "visibilityState";
  if (location !== parent.location) return "frame";
  if (location.hostname.match(loopbackRegex)) return "localhost";
  if (location.protocol === "file:") return "localfile";

  return false;
}

/** Builds the url to send to Goat. */
export function url(vars?: Partial<VariableContainer>) {
  const endpoint = getEndpoint();
  if (!endpoint) return;

  let data = getData();

  if (vars) {
    data = {
      ...data,
      ...vars,
    };
  }

  return endpoint + urlEncode(data);
}

/** Counts a hit. */
export function count(vars?: Partial<VariableContainer>) {
  try {
    const f = filter();
    if (f) return;

    const u = url(vars);
    if (!u) return;

    const img = document.createElement("img");

    img.src = u;
    img.style.position = "absolute"; // affects layout less
    img.setAttribute("alt", "");
    img.setAttribute("aria-hidden", "true");

    const remove = () => {
      if (img.parentNode) img.parentNode.removeChild(img);
    };

    setTimeout(remove, 5000); // in case the onload isn't triggered
    img.addEventListener("load", remove, false);

    document.body.appendChild(img);
  } catch {
    //ignored
  }
}

/** Collects a page view metric. */
export function trackView(path?: string, title?: string, referrer?: string) {
  const vars: Partial<VariableContainer> = {
    p: path,
  };

  if (title) vars.t = title;
  if (referrer) vars.r = referrer;

  count(vars);
}

/** Collects an event metric. */
export function trackEvent(type: string, value?: string) {
  let name = type;
  if (value) name = `${name}-${value}`;

  const vars: Partial<VariableContainer> = {
    p: name.replace(" ", "-"),
    e: true,
  };

  count(vars);
}
