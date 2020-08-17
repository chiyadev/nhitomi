import autoBind from 'auto-bind'
import { colors } from './theme.json'

export type ColorHue = Exclude<keyof typeof colors, 'white' | 'black'>
export type ColorLuminance = 100 | 200 | 300 | 400 | 500 | 600 | 700 | 800 | 900

export class ThemeColor {
  static get transparentRgba() {
    return 'rgba(0, 0, 0, 0)'
  }

  get hex() {
    return colors[this.hue][this.luminance]
  }

  get rgb() {
    return convertHex(this.hex)
  }

  rgba(alpha = 1) {
    return convertHex(this.hex, alpha)
  }

  constructor(public hue: ColorHue, public luminance: ColorLuminance = 500) {
    autoBind(this)
  }

  toString() {
    return this.hex
  }
}

/** Retrieves a color instance from the current theme. */
export function getColor(hue: ColorHue, luminance: ColorLuminance = 500) {
  return new ThemeColor(hue, luminance)
}

/** Converts a hex color to CSS rgba(...) format. */
export function convertHex(hex: string, alpha?: number) {
  hex = hex.startsWith('#') ? hex.substring(1) : hex

  if (hex.length === 3)
    hex = hex.split('').flatMap(x => [x, x]).join('')

  const r = parseInt(hex.slice(0, 2), 16)
  const g = parseInt(hex.slice(2, 4), 16)
  const b = parseInt(hex.slice(4, 6), 16)

  if (typeof alpha === 'undefined')
    return `rgba(${r}, ${g}, ${b})`
  else
    return `rgba(${r}, ${g}, ${b}, ${alpha})`
}
