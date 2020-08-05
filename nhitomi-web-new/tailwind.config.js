const palx = require('palx')
const theme = require('tailwindcss/defaultTheme')

const colors = palx('#5da2d5')

module.exports = {
  theme: {
    screens: {
      sm: { max: '767px' },
      lg: { min: '768px' }
    },
    colors: Object.keys(colors).reduce((hues, hue) => {
      if (typeof colors[hue] === 'object') {
        hues[hue] = colors[hue].reduce((lums, lum, i) => {
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
    }),
    fontFamily: {
      sans: [
        '"Lexend Deca"',
        '"M PLUS Rounded 1c"',
        ...theme.fontFamily.sans
      ]
    }
  }
}
