const palx = require('palx')
const theme = require('tailwindcss/defaultTheme')
const { writeFileSync } = require('fs')

const colorSrc = palx('#5da2d5')
const colors = Object.keys(colorSrc).reduce((hues, hue) => {
  if (typeof colorSrc[hue] === 'object') {
    hues[hue] = colorSrc[hue].reduce((lums, lum, i) => {
      if (1 <= i && i <= 9) {
        lums[i * 100] = lum
      }

      return lums
    }, {})
  }

  return hues
}, {
  white: '#fcfcfc',
  black: '#18171a'
})

writeFileSync('src/theme.json', JSON.stringify({ colors }))

module.exports = {
  theme: {
    screens: {
      sm: { max: '767px' },
      lg: { min: '768px' }
    },
    colors,
    fontFamily: {
      sans: [
        '"Lexend Deca"',
        '"M PLUS Rounded 1c"',
        ...theme.fontFamily.sans
      ]
    }
  }
}
