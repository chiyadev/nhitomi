const { override, addWebpackAlias } = require("customize-cra");
const { execSync } = require("child_process");

// compile Tailwind
execSync("yarn tailwind build tailwind.css -c tailwind.config.js -o src/theme.css");

module.exports = override(
  // use Preact
  addWebpackAlias({
    react: "preact/compat",
    "react-dom": "preact/compat",
  }),

  // https://medium.com/@poshakajay/heres-how-i-reduced-my-bundle-size-by-90-2e14c8a11c11
  /*setWebpackOptimizationSplitChunks({
    chunks: "all",
    cacheGroups: {
      commons: {
        test: /[\\/]node_modules[\\/]/,
        name: "vendors",
        chunks: "all",
      },
    },
  })*/
);
