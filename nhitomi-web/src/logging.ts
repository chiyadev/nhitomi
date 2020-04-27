const consoleEx: any = {}

for (const key in console) { // tslint:disable-line: forin
  const func = (console as any)[key]

  if (typeof func === 'function')
    consoleEx[key] = func.bind(console)
}

let enabled = true

/** Returns true if console logging is enabled. */
export function consoleEnabled() { return enabled }

const noop = () => { }

function refresh() {
  const flag = localStorage.getItem('logging')
  const flagSet = flag === 'false' ? false : process.env.NODE_ENV === 'development' || flag === 'true'

  if (enabled && !flagSet) {
    console.warn('Console logging has been disabled. You can set logging=true in localStorage to enable it.')

    // overwrite all console functions with noop
    for (const key in consoleEx)
      (console as any)[key] = noop
  }

  else if (!enabled && flagSet) {
    // restore console functions
    for (const key in consoleEx)
      (console as any)[key] = consoleEx[key]
  }

  enabled = flagSet
}

refresh()
window.addEventListener('storage', refresh)
