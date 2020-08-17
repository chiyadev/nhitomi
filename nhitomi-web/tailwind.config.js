const theme = require('tailwindcss/defaultTheme')
const { colors } = require('./src/theme.json')

module.exports = {
  theme: {
    screens: false,
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
