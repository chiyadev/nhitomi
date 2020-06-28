import { EventEmitter } from 'events'
import { useContext } from 'react'
import StrictEventEmitter from 'strict-event-emitter-types'
import { Client } from '.'
import { ClientContext } from '../ClientContext'
import { useUpdateOnEvent } from '../hooks'

/** Returns a configuration value, and a function to update it. */
export function useConfig<TKey extends keyof ConfigStore>(key: TKey): [ConfigStore[TKey], (value: ConfigStore[TKey]) => void] {
  const { config } = useContext(ClientContext)

  useUpdateOnEvent(config, key)

  return [config.get(key), v => config.set(key, v)]
}

export type ConfigStore = IStore

interface IStore {
  // low-level settings
  token: string | undefined
  baseUrl: string | undefined
  sidebar: boolean

  // global settings
  cancelKey: ShortcutConfig[]
  sidebarKey: ShortcutConfig[]
  scrollDownKey: ShortcutConfig[]
  scrollUpKey: ShortcutConfig[]
  nextPageKey: ShortcutConfig[]
  previousPageKey: ShortcutConfig[]
  firstPageKey: ShortcutConfig[]
  lastPageKey: ShortcutConfig[]

  // book reader
  bookReaderViewportBound: boolean
  bookReaderLeftToRight: boolean
  bookReaderImagesPerRow: number
  bookReaderSingleCover: boolean

  bookReaderViewportBoundKey: ShortcutConfig[]
  bookReaderLeftToRightKey: ShortcutConfig[]
  bookReaderImagesPerRowKey: ShortcutConfig[]
  bookReaderSingleCoverKey: ShortcutConfig[]
  bookReaderPageNumberKey: ShortcutConfig[]
  bookReaderMenuKey: ShortcutConfig[]
  bookReaderJumpKey: ShortcutConfig[]
}

const DefaultStore: IStore = {
  token: undefined,
  baseUrl: undefined,
  sidebar: false,

  cancelKey: [{ key: 27 }],                                 // esc
  sidebarKey: [{ key: 81 }],                                // q
  scrollDownKey: [{ key: 83 }, { key: 40 }],                // s down
  scrollUpKey: [{ key: 87 }, { key: 38 }],                  // w up
  nextPageKey: [{ key: 65 }, { key: 37 }, { key: 34 }],     // a left pageDown
  previousPageKey: [{ key: 68 }, { key: 39 }, { key: 33 }], // d right pageUp
  firstPageKey: [{ key: 36 }],                              // home
  lastPageKey: [{ key: 35 }],                               // end

  bookReaderViewportBound: true,
  bookReaderLeftToRight: false,
  bookReaderImagesPerRow: 2,
  bookReaderSingleCover: true,

  bookReaderViewportBoundKey: [{ key: 67 }],  // c
  bookReaderLeftToRightKey: [{ key: 76 }],    // l
  bookReaderImagesPerRowKey: [{ key: 88 }],   // x
  bookReaderSingleCoverKey: [{ key: 75 }],    // k
  bookReaderPageNumberKey: [{ key: 32 }],     // space
  bookReaderMenuKey: [{ key: 69 }],           // e
  bookReaderJumpKey: [{ key: 71 }]            // g
}

export type ModifierKey = 'alt' | 'ctrl' | 'meta' | 'shift'

export type ShortcutConfig = {
  key: number
  modifiers?: ModifierKey[]
}

const StoreKeys = Object.keys(DefaultStore) as (keyof IStore)[]

export class ConfigManager extends (EventEmitter as new () => StrictEventEmitter<EventEmitter, { [key in keyof IStore]: (value: IStore[key]) => void }>) implements IStore {
  token!: string | undefined
  baseUrl!: string | undefined
  sidebar!: boolean

  cancelKey!: ShortcutConfig[]
  sidebarKey!: ShortcutConfig[]
  scrollDownKey!: ShortcutConfig[]
  scrollUpKey!: ShortcutConfig[]
  nextPageKey!: ShortcutConfig[]
  previousPageKey!: ShortcutConfig[]
  firstPageKey!: ShortcutConfig[]
  lastPageKey!: ShortcutConfig[]

  bookReaderViewportBound!: boolean
  bookReaderLeftToRight!: boolean
  bookReaderImagesPerRow!: number
  bookReaderSingleCover!: boolean

  bookReaderViewportBoundKey!: ShortcutConfig[]
  bookReaderLeftToRightKey!: ShortcutConfig[]
  bookReaderImagesPerRowKey!: ShortcutConfig[]
  bookReaderSingleCoverKey!: ShortcutConfig[]
  bookReaderPageNumberKey!: ShortcutConfig[]
  bookReaderMenuKey!: ShortcutConfig[]
  bookReaderJumpKey!: ShortcutConfig[]

  constructor(readonly client: Client) {
    super()

    window.addEventListener('storage', ({ key, newValue }) => {
      const [success, value] = this.parse(newValue)

      this.emit(key as any, success ? value : DefaultStore[key as keyof IStore])
    })

    // add getter and setter properties
    for (const key of StoreKeys) {
      Object.defineProperty(this, key, {
        get: () => this.get(key),
        set: v => this.set(key, v)
      })

      this.on(key, (value: any) => console.log('set', key, value))
    }
  }

  get<TKey extends keyof IStore>(key: TKey) {
    const [success, value] = this.parse(localStorage.getItem(key))

    return success ? value as IStore[TKey] : DefaultStore[key]
  }

  set<TKey extends keyof IStore>(key: TKey, value: IStore[TKey]) {
    if (typeof value === 'undefined')
      localStorage.removeItem(key)
    else
      localStorage.setItem(key, JSON.stringify(value))

    this.emit(key as any, value)
  }

  async import(data: IStore) {
    for (const key of StoreKeys)
      this.set(key, data[key] as any)
  }

  export() {
    const data = { ...DefaultStore } as any

    for (const key of StoreKeys)
      data[key] = this.get(key)

    return data as IStore
  }

  private parse(value: string | null | undefined): [boolean, unknown] {
    try {
      if (value)
        return [true, JSON.parse(value)]
    }
    catch {
      // ignored
    }

    return [false, undefined]
  }
}
