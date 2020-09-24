import { randomStr } from "./random";

const queryName = "_v";

function hideCacheQuery() {
  const url = new URL(window.location.href);
  url.searchParams.delete(queryName);

  history.replaceState(history.state, document.title, url.href);
}

// as soon as window reloads, hide query used for cache busting
hideCacheQuery();

/** Similar to window.location.reload, but appends a query to avoid the current page cache. */
export function reloadWithoutCache() {
  const url = new URL(window.location.href);
  url.searchParams.append(queryName, randomStr(20));

  window.location.replace(url.href);
}
