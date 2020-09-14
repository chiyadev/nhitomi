import React, { useLayoutEffect, useCallback, ReactNode, createContext, useContext, useMemo } from 'react'
import { EventEmitter } from 'events'
import StrictEventEmitter from 'strict-event-emitter-types'
import { useUpdate } from 'react-use'
import { LanguageType } from 'nhitomi-api'
import stringify from 'json-stable-stringify'
import { AvailableLocalizations } from './Languages/languages'

export function useUpdateOnEvent<TEmitter extends StrictEventEmitter<EventEmitter, TEventRecord>, TEventRecord extends {}>(emitter: TEmitter, event: keyof TEventRecord) {
  const update = useUpdate()

  useLayoutEffect(() => {
    emitter.on(event, update)
    return () => { emitter.off(event as string, update) }
  }, [emitter, event, update])
}

export function useConfig<TKey extends keyof ConfigStore>(key: TKey): [ConfigStore[TKey], (value: ConfigStore[TKey]) => void] {
  const config = useConfigManager()

  useUpdateOnEvent(config, key)

  return [config.get(key), useCallback(v => config.set(key, v), [config, key])]
}

export type AnimationMode = 'normal' | 'faster' | 'none'
export type KeyModifier = 'alt' | 'ctrl' | 'meta' | 'shift'
export type ShortcutConfig = {
  key: number
  modifiers?: KeyModifier[]
}

export type ConfigStore = {
  token: string | undefined
  baseUrl: string | undefined
  language: LanguageType
  searchLanguages: LanguageType[]
  animation: AnimationMode
  blur: boolean

  cancelKey: ShortcutConfig[]
  scrollDownKey: ShortcutConfig[]
  scrollUpKey: ShortcutConfig[]
  shortcutsKey: ShortcutConfig[]

  bookReaderPreferEnglishName: boolean
  bookReaderViewportBound: boolean
  bookReaderLeftToRight: boolean
  bookReaderImagesPerRow: number
  bookReaderSingleCover: boolean

  bookReaderNextPageKey: ShortcutConfig[]
  bookReaderPreviousPageKey: ShortcutConfig[]
  bookReaderFirstPageKey: ShortcutConfig[]
  bookReaderLastPageKey: ShortcutConfig[]

  bookReaderViewportBoundKey: ShortcutConfig[]
  bookReaderLeftToRightKey: ShortcutConfig[]
  bookReaderImagesPerRowKey: ShortcutConfig[]
  bookReaderSingleCoverKey: ShortcutConfig[]
  bookReaderPageNumberKey: ShortcutConfig[]
}

export const BlurSupported = CSS.supports('backdrop-filter', 'blur(0)')
export const UserPreferredLanguages = navigator.languages.map(lang => Object.values(LanguageType).find(l => l === lang) || Object.values(LanguageType).find(l => l.startsWith(lang))).filter(l => l) as LanguageType[]

const DefaultStore: ConfigStore = {
  token: undefined,
  baseUrl: undefined,
  language: UserPreferredLanguages.find(lang => AvailableLocalizations.indexOf(lang) !== -1) || LanguageType.EnUS,
  searchLanguages: [...UserPreferredLanguages, LanguageType.JaJP].filter((v, i, a) => a.indexOf(v) === i),
  animation: 'normal',
  blur: BlurSupported,

  cancelKey: [{ key: 27 }],                           // esc
  scrollDownKey: [{ key: 83 }, { key: 40 }],          // s down
  scrollUpKey: [{ key: 87 }, { key: 38 }],            // w up
  shortcutsKey: [{ key: 191, modifiers: ['ctrl'] }],  // ctrl+/

  bookReaderPreferEnglishName: true,
  bookReaderViewportBound: true,
  bookReaderLeftToRight: false,
  bookReaderImagesPerRow: 2,
  bookReaderSingleCover: true,

  bookReaderNextPageKey: [{ key: 65 }, { key: 37 }, { key: 34 }],     // a left pageDown
  bookReaderPreviousPageKey: [{ key: 68 }, { key: 39 }, { key: 33 }], // d right pageUp
  bookReaderFirstPageKey: [{ key: 36 }],                              // home
  bookReaderLastPageKey: [{ key: 35 }],                               // end

  bookReaderViewportBoundKey: [{ key: 67 }],  // c
  bookReaderLeftToRightKey: [{ key: 76 }],    // l
  bookReaderImagesPerRowKey: [{ key: 88 }],   // x
  bookReaderSingleCoverKey: [{ key: 75 }],    // k
  bookReaderPageNumberKey: [{ key: 32 }]     // space
}

export type ConfigKey = keyof ConfigStore
export type ShortcutConfigKey = { [key in keyof ConfigStore]: ConfigStore[key] extends ShortcutConfig[] ? key : never }[keyof ConfigStore]

export const KeyModifiers: KeyModifier[] = ['alt', 'ctrl', 'meta', 'shift']

export const ConfigKeys = Object.keys(DefaultStore) as ConfigKey[]
export const ShortcutConfigKeys = ConfigKeys.filter(k => k.toLowerCase().endsWith('key')) as ShortcutConfigKey[]

export class ConfigSource extends (EventEmitter as new () => StrictEventEmitter<EventEmitter, { [key in keyof ConfigStore]: (value: ConfigStore[key]) => void }>) implements ConfigStore {
  token!: string | undefined
  baseUrl!: string | undefined
  sidebar!: boolean
  language!: LanguageType
  searchLanguages!: LanguageType[]
  animation!: AnimationMode
  blur!: boolean

  cancelKey!: ShortcutConfig[]
  scrollDownKey!: ShortcutConfig[]
  scrollUpKey!: ShortcutConfig[]
  shortcutsKey!: ShortcutConfig[]

  bookReaderPreferEnglishName!: boolean
  bookReaderViewportBound!: boolean
  bookReaderLeftToRight!: boolean
  bookReaderImagesPerRow!: number
  bookReaderSingleCover!: boolean

  bookReaderNextPageKey!: ShortcutConfig[]
  bookReaderPreviousPageKey!: ShortcutConfig[]
  bookReaderFirstPageKey!: ShortcutConfig[]
  bookReaderLastPageKey!: ShortcutConfig[]

  bookReaderViewportBoundKey!: ShortcutConfig[]
  bookReaderLeftToRightKey!: ShortcutConfig[]
  bookReaderImagesPerRowKey!: ShortcutConfig[]
  bookReaderSingleCoverKey!: ShortcutConfig[]
  bookReaderPageNumberKey!: ShortcutConfig[]

  constructor() {
    super()

    this.setMaxListeners(0)

    window.addEventListener('storage', ({ key, newValue }) => {
      const [success, value] = this.parse(newValue)

      this.emit(key as any, success ? value : DefaultStore[key as keyof ConfigStore])
    })

    // define getter and setter properties
    for (const key of ConfigKeys) {
      Object.defineProperty(this, key, {
        get: () => this.get(key),
        set: v => this.set(key, v)
      })

      this.on(key, (value: any) => console.log('set', key, value))
    }
  }

  get<TKey extends keyof ConfigStore>(key: TKey) {
    const [success, value] = this.parse(localStorage.getItem(key))

    return success ? value as ConfigStore[TKey] : DefaultStore[key]
  }

  set<TKey extends keyof ConfigStore>(key: TKey, value: ConfigStore[TKey]) {
    if (typeof value === 'undefined')
      localStorage.removeItem(key)
    else
      localStorage.setItem(key, stringify(value))

    this.emit(key as any, value)
  }

  async import(data: ConfigStore) {
    for (const key of ConfigKeys)
      this.set(key, data[key] as any)
  }

  export() {
    const data = JSON.parse(JSON.stringify(DefaultStore)) // clone

    for (const key of ConfigKeys)
      data[key] = this.get(key)

    return data as ConfigStore
  }

  private parse(value: string | null | undefined): [boolean, unknown] {
    try {
      if (value)
        return [true, JSON.parse(value)]
    }
    catch { /* ignored */ }

    return [false, undefined]
  }
}

const ConfigContext = createContext<ConfigSource>(undefined as any)

export function useConfigManager() {
  return useContext(ConfigContext)
}

export const ConfigManager = ({ children }: { children?: ReactNode }) => {
  const config = useMemo(() => new ConfigSource(), [])

  return (
    <ConfigContext.Provider value={config} children={children} />
  )
}
