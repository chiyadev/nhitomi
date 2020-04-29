import { EventEmitter } from 'events'
import { useContext, useEffect } from 'react'
import { useUpdate } from 'react-use'
import StrictEventEmitter from 'strict-event-emitter-types/types/src'
import { Client } from '.'
import { ClientContext } from '../ClientContext'
import { ModifierKey } from '../shortcuts'

/** Returns a configuration value, and a function to update it. */
export function useConfig<TKey extends keyof ConfigStore>(key: TKey): [ConfigStore[TKey], (value: ConfigStore[TKey]) => void] {
  const rerender = useUpdate()

  const { config } = useContext(ClientContext)

  useEffect(() => {
    // refresh component on key config change
    config.on(key, rerender)

    return () => { config.off(key, rerender) }
  }, [config, key, rerender])

  return [config.get(key), v => config.set(key, v)]
}

export type ConfigStore = IStore

interface IStore {
  // low-level settings
  token: string | undefined
  baseUrl: string | undefined
  workerConcurrency: number

  // global settings
  cancelKey: ShortcutConfig
  sidebarKey: ShortcutConfig
  scrollDownKey: ShortcutConfig
  scrollUpKey: ShortcutConfig
  nextPageKey: ShortcutConfig
  previousPageKey: ShortcutConfig
  firstPageKey: ShortcutConfig
  lastPageKey: ShortcutConfig

  // book reader
  bookReaderViewportBound: boolean
  bookReaderLeftToRight: boolean
  bookReaderImagesPerRow: number
  bookReaderInitialRowLimit: number
  bookReaderSnapping: boolean

  bookReaderViewportBoundKey: ShortcutConfig
  bookReaderLeftToRightKey: ShortcutConfig
  bookReaderImagesPerRowKey: ShortcutConfig
  bookReaderInitialRowLimitKey: ShortcutConfig
  bookReaderSnappingKey: ShortcutConfig

  // ocr
  ocrVisualization: boolean
}

const DefaultStore: IStore = {
  token: undefined,
  baseUrl: undefined,
  workerConcurrency: navigator.hardwareConcurrency || 2,

  cancelKey: { keys: [27] },                // esc
  sidebarKey: { keys: [81] },               // q
  scrollDownKey: { keys: [83, 40] },        // s down
  scrollUpKey: { keys: [87, 38] },          // w up
  nextPageKey: { keys: [65, 37, 34] },      // a left pageDown
  previousPageKey: { keys: [68, 39, 33] },  // d right pageUp
  firstPageKey: { keys: [36] },             // home
  lastPageKey: { keys: [35] },              // end

  bookReaderViewportBound: true,
  bookReaderLeftToRight: false,
  bookReaderImagesPerRow: 2,
  bookReaderInitialRowLimit: 1,
  bookReaderSnapping: false,

  bookReaderViewportBoundKey: { keys: [67] },   // c
  bookReaderLeftToRightKey: { keys: [76] },     // l
  bookReaderImagesPerRowKey: { keys: [88] },    // x
  bookReaderInitialRowLimitKey: { keys: [75] }, // k
  bookReaderSnappingKey: { keys: [77] },        // m

  ocrVisualization: true
}

export type ShortcutConfig = {
  keys: number[]
  modifiers?: ModifierKey[]
}

const StoreKeys = Object.keys(DefaultStore) as (keyof IStore)[]

export class ConfigManager extends (EventEmitter as new () => StrictEventEmitter<EventEmitter, { [key in keyof IStore]: (value: IStore[key]) => void }>) implements IStore {
  public token!: string | undefined
  public baseUrl!: string | undefined
  public workerConcurrency!: number

  public cancelKey!: ShortcutConfig
  public sidebarKey!: ShortcutConfig
  public scrollDownKey!: ShortcutConfig
  public scrollUpKey!: ShortcutConfig
  public nextPageKey!: ShortcutConfig
  public previousPageKey!: ShortcutConfig
  public firstPageKey!: ShortcutConfig
  public lastPageKey!: ShortcutConfig

  public bookReaderViewportBound!: boolean
  public bookReaderLeftToRight!: boolean
  public bookReaderImagesPerRow!: number
  public bookReaderInitialRowLimit!: number
  public bookReaderSnapping!: boolean

  public bookReaderViewportBoundKey!: ShortcutConfig
  public bookReaderLeftToRightKey!: ShortcutConfig
  public bookReaderImagesPerRowKey!: ShortcutConfig
  public bookReaderInitialRowLimitKey!: ShortcutConfig
  public bookReaderSnappingKey!: ShortcutConfig

  public ocrVisualization!: boolean

  constructor(public readonly client: Client) {
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

  public get<TKey extends keyof IStore>(key: TKey) {
    const [success, value] = this.parse(localStorage.getItem(key))

    return success ? value as IStore[TKey] : DefaultStore[key]
  }

  public set<TKey extends keyof IStore>(key: TKey, value: IStore[TKey]) {
    if (typeof value === 'undefined')
      localStorage.removeItem(key)
    else
      localStorage.setItem(key, JSON.stringify(value))

    this.emit(key as any, value)
  }

  public async import(data: IStore) {
    for (const key of StoreKeys)
      this.set(key, data[key] as any)
  }

  public export() {
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
    catch { }

    return [false, undefined]
  }
}
