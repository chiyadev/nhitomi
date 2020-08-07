import { createBrowserHistory, History as Hisotry, Hash, Search, Pathname } from 'history'

// https://stackoverflow.com/a/53307588/13160620
let refreshed = false

try {
  const entry = performance.getEntriesByType('navigation')[0]

  refreshed = entry instanceof PerformanceNavigationTiming && entry.type === 'reload'
}
catch {
  try { refreshed = performance.navigation.type === 1 }
  catch { /* ignored */ }
}

if (refreshed) {
  // clear all stale states except scroll position
  window.history.replaceState({ scroll: window.history.state?.scroll }, document.title)
}

export const History: Hisotry<null | HistoryState> = createBrowserHistory() as any

export type HistoryState = {
  [key: string]: undefined | {
    value: unknown
    version: number
  }
}

export type NavigationMode = 'push' | 'replace'

export function navigate(mode: NavigationMode, { path, search, hash, state }: { path?: Pathname, search?: Search, hash?: Hash, state?: HistoryState | ((state: HistoryState) => HistoryState) }) {
  let run: typeof History['push']

  switch (mode) {
    case 'push': run = History.push.bind(History); break
    case 'replace': run = History.replace.bind(History); break
  }

  const current = History.location

  if (typeof state === 'function')
    state = state(current.state || {})

  run({
    pathname: path,
    search,
    hash,
    state: state || current.state
  })
}
