const theme = require("tailwindcss/defaultTheme");
const { colors } = require("./src/theme.json");

module.exports = {
  future: {
    removeDeprecatedGapUtilities: true,
    purgeLayersByDefault: true,
  },
  theme: {
    screens: false,
    colors,
    fontFamily: {
      sans: ['"Lexend Deca"', '"M PLUS Rounded 1c"', ...theme.fontFamily.sans],
    },
  },
};
