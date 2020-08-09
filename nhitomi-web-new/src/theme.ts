function css(s: TemplateStringsArray) {
  const style = document.createElement('style')
  style.type = 'text/css'
  style.innerText = s.toString()

  document.head.appendChild(style)
}

// add beautiful frosty backgrounds for all browsers that support it! *looks with disgust at firefox*
if (CSS.supports('backdrop-filter', 'blur(0)')) {
  css`
    .bg-blur.bg-blur { /** double specificity to override bg-color opacities */
      backdrop-filter: blur(1em);
      --bg-opacity: 75%;
    }
  `
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
