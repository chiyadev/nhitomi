const localStorageEx: any = {}

for (const key in localStorage) { // tslint:disable-line: forin
  const func = (localStorage as any)[key]

  if (typeof func === 'function')
    localStorageEx[key] = func.bind(localStorage)
}

// overrides localstorage with an in-memory store for performance.
// setItem will still pass through.
// this should also allow localStorage to work on browsers without localStorage support (e.g. iOS Safari private mode).
const store: { [key: string]: string } = {}

localStorage.clear = () => {
  for (const key of Object.keys(store))
    delete store[key]

  try { localStorageEx.clear() }
  catch { }
}

localStorage.getItem = key => {
  const value = store[key]

  if (typeof value === 'undefined')
    return null

  return value
}

localStorage.removeItem = key => {
  delete store[key]

  try { localStorageEx.removeItem(key) }
  catch { }
}

localStorage.setItem = (key, value) => {
  store[key] = value

  try { localStorageEx.setItem(key, value) }
  catch { }
}

try {
  // hydrate store with existing data
  for (let i = 0; i < localStorage.length; i++) {
    const key = localStorage.key(i)!
    const value = localStorageEx.getItem(key)!

    store[key] = value
  }

  // update store on change
  window.addEventListener('storage', ({ key, newValue }) => {
    if (!key)
      return

    if (newValue)
      store[key] = newValue

    else
      delete store[key]
  })
}
catch { }

export { }
