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
