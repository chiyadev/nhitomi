const { override, addWebpackAlias } = require('customize-cra')
const { execSync } = require('child_process')

// compile Tailwind
execSync('yarn tailwind build tailwind.css -c tailwind.config.js -o src/theme.css')

module.exports = override(
  // use Preact
  addWebpackAlias({
    'react': 'preact/compat',
    'react-dom': 'preact/compat'
  })
)
