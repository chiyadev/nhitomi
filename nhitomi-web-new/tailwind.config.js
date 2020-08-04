const palx = require('palx')
const theme = require('tailwindcss/defaultTheme')

const colors = palx('#5da2d5')

module.exports = {
  purge: [],
  theme: {
    colors: Object.keys(colors).reduce((hues, hue) => {
      if (typeof colors[hue] === 'object') {
        hues[hue] = colors[hue].reduce((lums, lum, i) => {
          if (0 <= i && i < 9) {
            lums[(i + 1) * 100] = lum
          }

          return lums
        }, {})
      }

      return hues
    }, {}),
    backgroundColor: colors => ({
      ...colors,
      default: '#18171a'
    }),
    fontFamily: {
      sans: [
        '"Lexend Deca"',
        '"M PLUS Rounded 1c"',
        ...theme.fontFamily.sans
      ]
    }
  },
  variants: {},
  plugins: []
}
