import { colors } from './theme.json'
import { TinyColor, ColorInput } from '@ctrl/tinycolor'
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

  mix(other: Color, amount = 0.5) { return createColor(this.color.clone().mix(other.color, amount * 100)) }
  opacity(value: number) { return createColor(this.color.clone().setAlpha(this.color.getAlpha() * value)) }

  tint(value: number) { return this.mix(getColor('white'), value) }
  shade(value: number) { return this.mix(getColor('black'), value) }
}

/** Retrieves a color instance from the current theme. */
export function getColor(color: ColorHue | 'white' | 'black' | 'transparent', luminance: ColorLuminance = 'default') {
  switch (color) {
    case 'white': return createColor(colors.white)
    case 'black': return createColor(colors.black)
    case 'transparent': return createColor('transparent')
  }

  return createColor(colors[color][luminance])
}

/** Creates a color instance from the given value. */
export function createColor(color: ColorInput) {
  if (color instanceof TinyColor)
    return new Color(color)
  else
    return new Color(new TinyColor(color))
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
