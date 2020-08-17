import { colors } from './theme.json'
import { TinyColor } from '@ctrl/tinycolor'
import autoBind from 'auto-bind'

const SupportsHex8 = CSS.supports('color', '#ffffffff')

export type ColorHue = Exclude<keyof typeof colors, 'white' | 'black'>
export type ColorLuminance = keyof typeof colors[ColorHue]

export class Color {
  constructor(readonly color: TinyColor) {
    if (!color.isValid)
      throw Error(`'${color.originalInput}' is not a valid color.`)

    autoBind(this)
  }

  get hex() { return SupportsHex8 ? this.color.toHex8String() : this.color.toHexString() }
  get rgb() { return this.color.toRgbString() }

  mix(other: Color, amount = 0.5) { return new Color(this.color.mix(other.color, amount * 100)) }
  opacity(value: number) { return new Color(this.color.setAlpha(value)) }

  tint(value: number) { return this.mix(getColor('white'), value) }
  shade(value: number) { return this.mix(getColor('black'), value) }
}

/** Retrieves a color instance from the current theme. */
export function getColor(color: ColorHue | 'white' | 'black' | 'transparent', luminance: ColorLuminance = 'default') {
  switch (color) {
    case 'white': return new Color(new TinyColor(colors.white))
    case 'black': return new Color(new TinyColor(colors.black))
    case 'transparent': return new Color(new TinyColor('transparent'))
  }

  return new Color(new TinyColor(colors[color][luminance]))
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
